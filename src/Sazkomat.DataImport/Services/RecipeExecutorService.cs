using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Sazkomat.DataImport.Debug;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Scrapers;

namespace Sazkomat.DataImport.Services;

/// <summary>
/// Executes scraper recipes by converting them to DebugRequest actions
/// and parsing the resulting HTML using the recipe's parsing rules.
/// </summary>
public class RecipeExecutorService
{
    private readonly ILogger<RecipeExecutorService> _logger;

    public RecipeExecutorService(ILogger<RecipeExecutorService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a recipe using the provided ScraperDebugService instance.
    /// Variables in action URLs are replaced (e.g., {baseUrl}, {season}).
    /// </summary>
    public async Task<RecipeExecutionResult> ExecuteRecipeAsync(
        ScraperDebugService debugService,
        ScraperRecipe recipe,
        Dictionary<string, string> variables)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing recipe '{RecipeName}' with variables: {Variables}",
                recipe.Name, string.Join(", ", variables.Select(kv => $"{kv.Key}={kv.Value}")));

            // Deserialize actions from JSON
            // DebugAction uses [JsonConverter(typeof(DebugActionConverter))] attribute
            // which handles polymorphic deserialization regardless of "type" property position
            var actions = JsonSerializer.Deserialize<List<DebugAction>>(recipe.ActionsJson);
            if (actions == null || actions.Count == 0)
            {
                return RecipeExecutionResult.Failed(
                    "Recipe has no actions defined",
                    new List<string> { "Actions JSON is empty or invalid" },
                    sw.ElapsedMilliseconds);
            }

            // Replace variables in NavigateAction URLs
            foreach (var action in actions.OfType<NavigateAction>())
            {
                action.Url = ReplaceVariables(action.Url, variables);
            }

            // Replace variables in EvaluateAction scripts
            foreach (var action in actions.OfType<EvaluateAction>())
            {
                action.Script = ReplaceVariables(action.Script, variables);
            }

            // Execute via ScraperDebugService
            var request = new DebugRequest { Actions = actions };
            var result = await debugService.ExecuteAsync(request);

            // Extract HTML from the last extractHtml action result
            ExtractHtmlDetails? htmlDetails = null;
            foreach (var stepResult in result.Results.AsEnumerable().Reverse())
            {
                if (stepResult.Action == "extractHtml" && stepResult.Details != null)
                {
                    // Details might be a JsonElement that needs to be deserialized
                    if (stepResult.Details is JsonElement jsonElement)
                    {
                        htmlDetails = JsonSerializer.Deserialize<ExtractHtmlDetails>(jsonElement.GetRawText());
                    }
                    else if (stepResult.Details is ExtractHtmlDetails details)
                    {
                        htmlDetails = details;
                    }
                    break;
                }
            }

            if (!result.Success)
            {
                var lastError = result.Results.LastOrDefault(r => !r.Success)?.Error ?? "Unknown error";
                return RecipeExecutionResult.Failed(
                    $"Recipe execution failed: {lastError}",
                    result.Logs,
                    sw.ElapsedMilliseconds);
            }

            if (htmlDetails == null || string.IsNullOrWhiteSpace(htmlDetails.Html))
            {
                return RecipeExecutionResult.Failed(
                    "Recipe did not extract any HTML (missing extractHtml action or empty result)",
                    result.Logs,
                    sw.ElapsedMilliseconds);
            }

            _logger.LogInformation("Recipe '{RecipeName}' extracted {Length} chars of HTML in {Duration}ms",
                recipe.Name, htmlDetails.Html.Length, sw.ElapsedMilliseconds);

            return RecipeExecutionResult.Succeeded(htmlDetails.Html, result.Logs, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing recipe '{RecipeName}'", recipe.Name);
            return RecipeExecutionResult.Failed(
                $"Exception: {ex.Message}",
                new List<string> { ex.ToString() },
                sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Parses HTML content using the recipe's parsing rules.
    /// </summary>
    public ScrapeResult ParseHtmlWithRules(string html, ScraperRecipe recipe, Guid leagueId, string season)
    {
        try
        {
            _logger.LogDebug("Parsing HTML with recipe '{RecipeName}' rules", recipe.Name);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rounds = new List<Round>();
            var totalRoundHeadersFound = 0;
            var totalMatchRowsFound = 0;

            // Find tables containing match data
            var tables = doc.DocumentNode.SelectNodes("//table[contains(@class, 'table-main')]");
            if (tables == null || !tables.Any())
            {
                _logger.LogWarning("No tables found with class 'table-main' using recipe {RecipeName}. Returning NoResults.", recipe.Name);
                var emptyResult = ScrapeResult.Success(new List<Round>(), 0, 0);
                _logger.LogInformation("Empty result FailureReason: {Reason}", emptyResult.FailureReason);
                return emptyResult;
            }

            _logger.LogInformation("Found {TableCount} tables to process", tables.Count);

            // Compile group pattern regex if provided
            Regex? groupRegex = null;
            if (!string.IsNullOrWhiteSpace(recipe.GroupPatternRegex))
            {
                try
                {
                    groupRegex = new Regex(recipe.GroupPatternRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid GroupPatternRegex in recipe: {Pattern}", recipe.GroupPatternRegex);
                }
            }

            foreach (var table in tables)
            {
                var rows = table.SelectNodes(".//tr");
                if (rows == null || !rows.Any())
                {
                    continue;
                }

                var roundMatches = new Dictionary<(string? GroupName, int RoundNumber), List<MatchResult>>();
                string? currentGroupName = null;
                int? currentRoundNumber = null;

                foreach (var row in rows)
                {
                    // Check for round header using recipe's selector
                    var roundHeader = row.SelectSingleNode(recipe.RoundHeaderSelector);
                    if (roundHeader != null)
                    {
                        var headerText = roundHeader.InnerText.Trim();
                        var (groupName, roundNum) = ParseRoundHeader(headerText, groupRegex);
                        currentGroupName = groupName;
                        currentRoundNumber = roundNum;

                        if (roundNum > 0)
                        {
                            var key = (groupName, roundNum);
                            if (!roundMatches.ContainsKey(key))
                            {
                                roundMatches[key] = new List<MatchResult>();
                                totalRoundHeadersFound++;
                            }
                        }
                        continue;
                    }

                    // Parse match row
                    var matchData = ParseMatchRow(row, season);
                    if (matchData != null)
                    {
                        totalMatchRowsFound++;

                        if (currentRoundNumber.HasValue && currentRoundNumber.Value > 0)
                        {
                            var key = (currentGroupName, currentRoundNumber.Value);
                            if (roundMatches.ContainsKey(key))
                            {
                                roundMatches[key].Add(matchData);
                            }
                        }
                    }
                }

                // Create rounds from accumulated matches
                foreach (var kvp in roundMatches.OrderBy(x => x.Key.GroupName).ThenBy(x => x.Key.RoundNumber))
                {
                    var (groupName, roundNumber) = kvp.Key;
                    var matches = kvp.Value;

                    if (matches.Any())
                    {
                        var round = CreateRoundFromMatches(leagueId, roundNumber, groupName, matches);
                        rounds.Add(round);
                    }
                }
            }

            _logger.LogInformation("Recipe parsing completed: {RoundCount} rounds, {TotalHeaders} headers, {TotalMatches} match rows found",
                rounds.Count, totalRoundHeadersFound, totalMatchRowsFound);

            var result = ScrapeResult.Success(rounds, totalRoundHeadersFound, totalMatchRowsFound);
            _logger.LogInformation("ScrapeResult created: IsSuccess={IsSuccess}, FailureReason={Reason}",
                result.IsSuccess, result.FailureReason);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing HTML with recipe '{RecipeName}'", recipe.Name);
            return ScrapeResult.ParsingError(ex.Message);
        }
    }

    private string ReplaceVariables(string input, Dictionary<string, string> variables)
    {
        var result = input;
        foreach (var kv in variables)
        {
            result = result.Replace($"{{{kv.Key}}}", kv.Value);
        }
        return result;
    }

    private (string? GroupName, int RoundNumber) ParseRoundHeader(string headerText, Regex? groupRegex)
    {
        // Try group regex first if provided
        if (groupRegex != null)
        {
            var match = groupRegex.Match(headerText);
            if (match.Success && match.Groups.Count >= 3)
            {
                var groupName = match.Groups[1].Value.Trim();
                if (int.TryParse(match.Groups[2].Value, out var roundNum))
                {
                    return (groupName, roundNum);
                }
            }
        }

        // Fallback: try parsing with dash separator (e.g., "East - 1. Round")
        var dashIndex = headerText.IndexOf(" - ");
        if (dashIndex > 0)
        {
            var groupName = headerText.Substring(0, dashIndex).Trim();
            var roundPart = headerText.Substring(dashIndex + 3);
            var roundNumber = ExtractRoundNumber(roundPart);
            if (roundNumber > 0)
            {
                return (groupName, roundNumber);
            }
        }

        // No group prefix
        return (null, ExtractRoundNumber(headerText));
    }

    private int ExtractRoundNumber(string headerText)
    {
        // Must match pattern "number. Round" or "number.Round" or "Round number"
        // Examples: "38. Round", "1.Round", "Round 15", "GROUP 1 - 22.ROUND"
        // This prevents false positives from dates like "22.10.1994"
        var patterns = new[]
        {
            @"(\d+)\.\s*Round",      // "38. Round", "1.Round"
            @"Round\s*(\d+)",        // "Round 15"
            @"(\d+)\s*Round",        // "22ROUND"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(headerText, pattern, RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var roundNumber))
            {
                return roundNumber;
            }
        }
        return 0; // No valid round number found
    }

    private MatchResult? ParseMatchRow(HtmlNode row, string season)
    {
        try
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 5) return null;

            // Extract match result (e.g., "2:1")
            var scoreCell = cells.FirstOrDefault(c => c.InnerText.Contains(":"));
            if (scoreCell == null) return null;

            var scoreText = scoreCell.InnerText.Trim();
            var scoreParts = scoreText.Split(':');
            if (scoreParts.Length != 2) return null;

            if (!int.TryParse(scoreParts[0].Trim(), out var homeScore) ||
                !int.TryParse(scoreParts[1].Trim(), out var awayScore))
            {
                return null;
            }

            // Extract team names
            var matchLink = row.SelectSingleNode(".//a[contains(@class, 'in-match')]");
            if (matchLink == null) return null;

            var matchText = matchLink.InnerText.Trim();
            var teamParts = matchText.Split('-');
            string homeTeam = teamParts.Length > 0 ? teamParts[0].Trim() : "Unknown";
            string awayTeam = teamParts.Length > 1 ? teamParts[1].Trim() : "Unknown";

            var matchUrl = matchLink.GetAttributeValue("href", string.Empty);
            if (!string.IsNullOrEmpty(matchUrl) && !matchUrl.StartsWith("http"))
            {
                matchUrl = "https://www.betexplorer.com" + matchUrl;
            }

            string result;
            if (homeScore > awayScore)
                result = "H";
            else if (homeScore < awayScore)
                result = "A";
            else
                result = "D";

            // Extract odds
            var oddsCells = row.SelectNodes(".//td[contains(@class, 'table-main__odds')]");
            decimal? homeOdds = null, drawOdds = null, awayOdds = null;

            if (oddsCells == null || oddsCells.Count == 0)
            {
                var allCells = row.SelectNodes(".//td");
                if (allCells != null && allCells.Count >= 6)
                {
                    oddsCells = new HtmlNodeCollection(null);
                    if (allCells.Count > 3) oddsCells.Add(allCells[3]);
                    if (allCells.Count > 4) oddsCells.Add(allCells[4]);
                    if (allCells.Count > 5) oddsCells.Add(allCells[5]);
                }
            }

            if (oddsCells != null && oddsCells.Count >= 3)
            {
                homeOdds = ExtractOddsFromCell(oddsCells[0]);
                drawOdds = ExtractOddsFromCell(oddsCells[1]);
                awayOdds = ExtractOddsFromCell(oddsCells[2]);
            }

            // Try to extract match date
            var dateCell = cells.LastOrDefault();
            DateTime? matchDate = null;

            if (dateCell != null)
            {
                var dateText = dateCell.InnerText.Trim();
                matchDate = ParseMatchDate(dateText, season);
            }

            return new MatchResult
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                HomeScore = homeScore,
                AwayScore = awayScore,
                Result = result,
                HomeOdds = homeOdds,
                DrawOdds = drawOdds,
                AwayOdds = awayOdds,
                MatchDate = matchDate,
                BetExplorerUrl = matchUrl
            };
        }
        catch
        {
            return null;
        }
    }

    private decimal? ExtractOddsFromCell(HtmlNode cell)
    {
        try
        {
            // Try data-odd attribute
            var oddAttr = cell.GetAttributeValue("data-odd", "");
            if (!string.IsNullOrEmpty(oddAttr) &&
                decimal.TryParse(oddAttr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var odd))
            {
                return odd;
            }

            // Try child element with data-odd
            var oddElement = cell.SelectSingleNode(".//*[@data-odd]");
            if (oddElement != null)
            {
                oddAttr = oddElement.GetAttributeValue("data-odd", "");
                if (decimal.TryParse(oddAttr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out odd))
                {
                    return odd;
                }
            }

            // Fallback: parse inner text
            var cellText = cell.InnerText.Trim();
            if (decimal.TryParse(cellText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out odd))
            {
                return odd;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private DateTime? ParseMatchDate(string dateText, string season)
    {
        try
        {
            if (string.IsNullOrEmpty(dateText) || !dateText.Contains("."))
                return null;

            // Try parsing with full year (format: "dd.MM.yyyy")
            if (DateTime.TryParseExact(dateText, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var fullDate))
            {
                return DateTime.SpecifyKind(fullDate, DateTimeKind.Utc);
            }

            // Extract years from season (e.g., "2024/2025" -> [2024, 2025])
            var seasonYears = season.Split('/').Select(y => int.TryParse(y.Trim(), out var year) ? year : 0).Where(y => y > 0).ToArray();

            if (seasonYears.Length >= 2)
            {
                if (DateTime.TryParseExact(dateText, "dd.MM.",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var tempDate))
                {
                    var year = tempDate.Month >= 8 ? seasonYears[0] : seasonYears[1];
                    return DateTime.SpecifyKind(new DateTime(year, tempDate.Month, tempDate.Day), DateTimeKind.Utc);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private Round CreateRoundFromMatches(Guid leagueId, int roundNumber, string? groupName, List<MatchResult> matches)
    {
        const int maxMatchesPerRound = 15;
        if (matches.Count > maxMatchesPerRound)
        {
            var groupInfo = groupName != null ? $" (Group: {groupName})" : "";
            throw new InvalidOperationException(
                $"Round {roundNumber}{groupInfo} has {matches.Count} matches, but max allowed is {maxMatchesPerRound}. " +
                $"This indicates a parsing error - rounds may have been incorrectly merged.");
        }

        var homeWins = matches.Count(m => m.Result == "H");
        var draws = matches.Count(m => m.Result == "D");
        var awayWins = matches.Count(m => m.Result == "A");

        var cumulativeHome = matches.Where(m => m.HomeOdds.HasValue).Aggregate(1.0m, (acc, m) => acc * m.HomeOdds!.Value);
        var cumulativeDraw = matches.Where(m => m.DrawOdds.HasValue).Aggregate(1.0m, (acc, m) => acc * m.DrawOdds!.Value);
        var cumulativeAway = matches.Where(m => m.AwayOdds.HasValue).Aggregate(1.0m, (acc, m) => acc * m.AwayOdds!.Value);

        var allHaveOdds = matches.All(m => m.HomeOdds.HasValue && m.DrawOdds.HasValue && m.AwayOdds.HasValue);
        var oddsComplete = allHaveOdds ? "Yes" : "Partial";

        var round = new Round
        {
            LeagueId = leagueId,
            SeasonId = Guid.Empty, // Set by caller
            RoundNumber = roundNumber,
            GroupName = groupName,
            MatchesCount = matches.Count,
            HomeWins = homeWins,
            Draws = draws,
            AwayWins = awayWins,
            CumulativeOddsHome = cumulativeHome,
            CumulativeOddsDraw = cumulativeDraw,
            CumulativeOddsAway = cumulativeAway,
            SummaryResult = $"{homeWins}-{draws}-{awayWins}",
            OddsComplete = oddsComplete,
            ScrapedAt = DateTime.UtcNow,
            DataSource = "betexplorer.com"
        };

        round.Matches = matches.Select(m => new Entities.Match
        {
            HomeTeam = m.HomeTeam,
            AwayTeam = m.AwayTeam,
            HomeScore = m.HomeScore,
            AwayScore = m.AwayScore,
            Result = m.Result,
            HomeOdds = m.HomeOdds,
            DrawOdds = m.DrawOdds,
            AwayOdds = m.AwayOdds,
            MatchDate = m.MatchDate,
            BetExplorerUrl = m.BetExplorerUrl
        }).ToList();

        return round;
    }

    private class MatchResult
    {
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public string Result { get; set; } = string.Empty;
        public decimal? HomeOdds { get; set; }
        public decimal? DrawOdds { get; set; }
        public decimal? AwayOdds { get; set; }
        public DateTime? MatchDate { get; set; }
        public string? BetExplorerUrl { get; set; }
    }
}
