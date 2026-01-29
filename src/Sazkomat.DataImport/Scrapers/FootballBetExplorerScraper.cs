using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Scrapers;

public class FootballBetExplorerScraper : ILeagueScraper
{
    private readonly IHttpClient _httpClient;
    private readonly ILogger<FootballBetExplorerScraper> _logger;

    public FootballBetExplorerScraper(
        IHttpClient httpClient,
        ILogger<FootballBetExplorerScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool CanHandle(Sport sport)
    {
        return sport.Code.Equals("football", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<Round>> ScrapeSeasonAsync(League league, string season)
    {
        // Delegate to multi-season method with single season
        var results = await ScrapeMultipleSeasonsAsync(league, new[] { season });
        return results.TryGetValue(season, out var rounds) ? rounds : new List<Round>();
    }

    /// <summary>
    /// Scrapes multiple seasons from BetExplorer in a single browser session.
    /// More efficient - loads page once, then iterates through seasons via dropdown.
    /// </summary>
    public async Task<Dictionary<string, List<Round>>> ScrapeMultipleSeasonsAsync(
        League league,
        IEnumerable<string> seasons)
    {
        var results = new Dictionary<string, List<Round>>();
        var countrySlug = league.Country?.Code?.ToLowerInvariant() ?? "unknown";
        var baseUrl = $"/football/{countrySlug}/{league.BetExplorerSlug}/";
        var debugPattern = $"/tmp/betexplorer_{league.BetExplorerSlug}_{{season}}.html";

        _logger.LogInformation("Starting multi-season scrape for {League} from {BaseUrl}",
            league.Name, baseUrl);

        await foreach (var (season, html) in
            _httpClient.GetBetExplorerMultiSeasonResultsAsync(baseUrl, seasons, debugPattern))
        {
            try
            {
                var rounds = ParseHtmlContent(html, league.Id, season);
                results[season] = rounds;
                _logger.LogInformation("Scraped {RoundCount} rounds for {League} season {Season}",
                    rounds.Count, league.Name, season);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse HTML for {League} season {Season}",
                    league.Name, season);
                results[season] = new List<Round>();
            }
        }

        return results;
    }

    /// <summary>
    /// Parses HTML content and returns rounds.
    /// Extracted from ScrapeSeasonAsync for reuse in multi-season scraping.
    /// </summary>
    private List<Round> ParseHtmlContent(string html, Guid leagueId, string season)
    {
        var rounds = new List<Round>();

        _logger.LogDebug("HTML downloaded, size: {Size} characters", html.Length);

        // Parse HTML
        _logger.LogDebug("Parsing HTML document...");
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        _logger.LogDebug("HTML parsed successfully");

        // Find the main results container - try multiple strategies
        // BetExplorer changed HTML structure around 2011
        HtmlNode? resultsContainer = doc.DocumentNode.SelectSingleNode("//div[@id='js-leagueresults-all']");
        HtmlNodeCollection? tables = null;

        if (resultsContainer != null)
        {
            // Old format (pre-2011) - tables inside container
            tables = resultsContainer.SelectNodes(".//table[contains(@class, 'table-main')]");
            _logger.LogDebug("Using old format: found container with {TableCount} tables", tables?.Count ?? 0);
        }
        else
        {
            // Try finding tables directly
            tables = doc.DocumentNode.SelectNodes("//table[contains(@class, 'table-main')]");
            _logger.LogDebug("Trying direct table search: found {TableCount} tables", tables?.Count ?? 0);
        }

        // If no tables found, try NEW list-based format (post-2011)
        if (tables == null || !tables.Any())
        {
            var matchItems = doc.DocumentNode.SelectNodes("//ul[contains(@class, 'table-main__matchInfo')] | //li[contains(@class, 'table-main__matchInfo')]");
            if (matchItems != null && matchItems.Any())
            {
                _logger.LogInformation("Using NEW list-based format: found {MatchCount} match items", matchItems.Count);
                rounds = ParseNewFormat(matchItems, leagueId, season);
                _logger.LogInformation("Successfully scraped {RoundCount} rounds for season {Season}",
                    rounds.Count, season);
                return rounds;
            }

            _logger.LogWarning("No match tables or list items found for season {Season}", season);
            return rounds;
        }

        _logger.LogInformation("Found {TableCount} tables to process (old format)", tables.Count);

        var tableIndex = 0;
        foreach (var table in tables)
        {
            tableIndex++;
            _logger.LogDebug("Processing table {TableIndex}/{TotalTables}", tableIndex, tables.Count);

            var rows = table.SelectNodes(".//tr");
            if (rows == null || !rows.Any())
            {
                _logger.LogDebug("Table {TableIndex} has no rows, skipping", tableIndex);
                continue;
            }

            _logger.LogDebug("Table {TableIndex} has {RowCount} rows", tableIndex, rows.Count);

            // Use dictionary to accumulate matches per (group, round) - handles leagues with groups like East/West
            var roundMatches = new Dictionary<(string? GroupName, int RoundNumber), List<MatchResult>>();
            string? currentGroupName = null;
            int? currentRoundNumber = null;

            foreach (var row in rows)
            {
                // Check if this is a round header
                var roundHeader = row.SelectSingleNode(".//th[contains(text(), 'Round')]");
                if (roundHeader != null)
                {
                    // Parse group name and round number from header
                    // e.g., "East - 1. Round" → ("East", 1), "38. Round" → (null, 38)
                    var (groupName, roundNum) = ParseRoundHeader(roundHeader.InnerText);
                    currentGroupName = groupName;
                    currentRoundNumber = roundNum;

                    var key = (groupName, roundNum);
                    _logger.LogDebug("Found round header: {GroupName} Round {RoundNumber}",
                        groupName ?? "(no group)", roundNum);

                    // Initialize list if this (group, round) not seen yet
                    if (!roundMatches.ContainsKey(key))
                    {
                        roundMatches[key] = new List<MatchResult>();
                    }
                    continue;
                }

                // Parse match row
                var matchData = ParseMatchRow(row, season);
                if (matchData != null && currentRoundNumber.HasValue)
                {
                    var key = (currentGroupName, currentRoundNumber.Value);
                    roundMatches[key].Add(matchData);
                }
            }

            // Create rounds from accumulated matches
            foreach (var kvp in roundMatches.OrderBy(x => x.Key.GroupName).ThenBy(x => x.Key.RoundNumber))
            {
                var (groupName, roundNumber) = kvp.Key;
                var matches = kvp.Value;

                if (matches.Any())
                {
                    var round = CreateRoundFromMatches(leagueId, season, roundNumber, groupName, matches);
                    rounds.Add(round);
                    _logger.LogInformation("Parsed {GroupPrefix}Round {RoundNumber}: {MatchCount} matches, {OddsStatus} odds",
                        groupName != null ? $"{groupName} - " : "",
                        roundNumber, matches.Count, round.OddsComplete);
                }
            }
        }

        _logger.LogDebug("Successfully parsed {RoundCount} rounds for season {Season}",
            rounds.Count, season);

        return rounds;
    }

    /// <summary>
    /// Parses the NEW list-based HTML format (post-2011 seasons).
    /// Matches are in ul/li elements with class table-main__matchInfo.
    /// </summary>
    private List<Round> ParseNewFormat(HtmlNodeCollection matchItems, Guid leagueId, string season)
    {
        var rounds = new List<Round>();

        // In new format, matches don't have explicit round headers
        // We need to group matches by date or create a single "all matches" round
        // For now, create one round per detected match group
        var allMatches = new List<MatchResult>();

        foreach (var matchNode in matchItems)
        {
            var matchData = ParseNewFormatMatch(matchNode, season);
            if (matchData != null)
            {
                allMatches.Add(matchData);
            }
        }

        // Group all matches into a single round (round 1) for now
        // The new format doesn't have clear round delineation
        if (allMatches.Any())
        {
            var round = CreateRoundFromMatches(leagueId, season, 1, null, allMatches);
            rounds.Add(round);
            _logger.LogInformation("NEW FORMAT: Parsed {MatchCount} matches into 1 round", allMatches.Count);
        }

        return rounds;
    }

    /// <summary>
    /// Parses a single match from the new list-based format.
    /// </summary>
    private MatchResult? ParseNewFormatMatch(HtmlNode matchNode, string season)
    {
        try
        {
            // Find team names
            var homeTeamNode = matchNode.SelectSingleNode(".//*[contains(@class, 'table-main__participantHome')]//p | .//*[contains(@class, 'table-main__participantHome')]");
            var awayTeamNode = matchNode.SelectSingleNode(".//*[contains(@class, 'table-main__participantAway')]//p | .//*[contains(@class, 'table-main__participantAway')]");

            string homeTeam = homeTeamNode?.InnerText.Trim() ?? "Unknown";
            string awayTeam = awayTeamNode?.InnerText.Trim() ?? "Unknown";

            // Find score from data-live-cell="score" or mainResult/liveResult div
            var scoreNode = matchNode.SelectSingleNode(".//*[@data-live-cell='score'] | .//*[contains(@class, 'mainResult')] | .//*[contains(@class, 'liveResult')]");
            if (scoreNode == null)
            {
                _logger.LogDebug("No score found for {Home} vs {Away}", homeTeam, awayTeam);
                return null;
            }

            // Extract score digits from table-main__liveResults or table-main__finishedResults divs
            var scoreDigits = scoreNode.SelectNodes(".//*[contains(@class, 'Results')]");
            int homeScore = 0, awayScore = 0;

            if (scoreDigits != null && scoreDigits.Count >= 3)
            {
                // Format: [home score] [:] [away score]
                int.TryParse(scoreDigits[0].InnerText.Trim(), out homeScore);
                int.TryParse(scoreDigits[2].InnerText.Trim(), out awayScore);
            }
            else
            {
                // Try parsing from title attribute (format: "0:1, 1:0")
                var titleAttr = scoreNode.GetAttributeValue("title", "");
                if (!string.IsNullOrEmpty(titleAttr) && titleAttr.Contains(":"))
                {
                    var scoreParts = titleAttr.Split(',')[0].Trim().Split(':');
                    if (scoreParts.Length == 2)
                    {
                        int.TryParse(scoreParts[0].Trim(), out homeScore);
                        int.TryParse(scoreParts[1].Trim(), out awayScore);
                    }
                }
            }

            // Determine result
            string result = homeScore > awayScore ? "H" : (homeScore < awayScore ? "A" : "D");

            // Extract odds from data-odd attributes
            var oddNodes = matchNode.SelectNodes(".//*[contains(@class, 'table-main__odds')]//*[@data-odd]");
            decimal? homeOdds = null, drawOdds = null, awayOdds = null;

            if (oddNodes != null && oddNodes.Count >= 3)
            {
                var oddStr0 = oddNodes[0].GetAttributeValue("data-odd", "");
                var oddStr1 = oddNodes[1].GetAttributeValue("data-odd", "");
                var oddStr2 = oddNodes[2].GetAttributeValue("data-odd", "");

                if (decimal.TryParse(oddStr0, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var odd0))
                    homeOdds = odd0;
                if (decimal.TryParse(oddStr1, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var odd1))
                    drawOdds = odd1;
                if (decimal.TryParse(oddStr2, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var odd2))
                    awayOdds = odd2;
            }

            // Extract match URL
            var matchLink = matchNode.SelectSingleNode(".//a[contains(@href, '/football/')]");
            var matchUrl = matchLink?.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(matchUrl) && !matchUrl.StartsWith("http"))
            {
                matchUrl = "https://www.betexplorer.com" + matchUrl;
            }

            _logger.LogDebug("NEW FORMAT: Parsed {Home} {HomeScore}:{AwayScore} {Away}, odds: {H}/{D}/{A}",
                homeTeam, homeScore, awayScore, awayTeam, homeOdds, drawOdds, awayOdds);

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
                MatchDate = null, // New format doesn't have clear date in individual match
                BetExplorerUrl = matchUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse new format match");
            return null;
        }
    }

    // Helper methods for HTML parsing

    /// <summary>
    /// Parses round header to extract group name and round number.
    /// Examples:
    ///   "East - 1. Round" → ("East", 1)
    ///   "GROUP 1 - 15.ROUND" → ("GROUP 1", 15)
    ///   "38. Round" → (null, 38)
    /// </summary>
    private (string? GroupName, int RoundNumber) ParseRoundHeader(string headerText)
    {
        // Check if contains group prefix (text before " - " followed by round number)
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

        // No group prefix or couldn't parse group format
        return (null, ExtractRoundNumber(headerText));
    }

    private int ExtractRoundNumber(string headerText)
    {
        // Extract number from text like "38. Round" or "Round 38" or "15.ROUND"
        var match = System.Text.RegularExpressions.Regex.Match(headerText, @"\d+");
        if (match.Success && int.TryParse(match.Value, out var roundNumber))
        {
            return roundNumber;
        }

        _logger.LogWarning("Could not extract round number from: {HeaderText}", headerText);
        return 0;
    }

    private MatchResult? ParseMatchRow(HtmlNode row, string season)
    {
        try
        {
            // Check if this is a data row (has td elements)
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

            // Determine result (H/D/A) - winner has <strong> tag
            var matchLink = row.SelectSingleNode(".//a[contains(@class, 'in-match')]");
            if (matchLink == null) return null;

            // Extract team names from the match link text
            // Text format is typically: "HomeTeam - AwayTeam"
            var matchText = matchLink.InnerText.Trim();
            var teamParts = matchText.Split('-');
            string homeTeam = teamParts.Length > 0 ? teamParts[0].Trim() : "Unknown";
            string awayTeam = teamParts.Length > 1 ? teamParts[1].Trim() : "Unknown";

            // Extract BetExplorer URL
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

            // Extract odds (1, X, 2)
            var oddsCells = row.SelectNodes(".//td[contains(@class, 'table-main__odds')]");
            decimal? homeOdds = null, drawOdds = null, awayOdds = null;

            // DEBUG: Log odds cells
            _logger.LogDebug("Odds cells found: {Count}", oddsCells?.Count ?? 0);
            if (oddsCells == null || oddsCells.Count == 0)
            {
                // Try alternative selector - get all td cells and look for ones with odds
                var allCells = row.SelectNodes(".//td");
                _logger.LogDebug("Total td cells in row: {Count}", allCells?.Count ?? 0);

                if (allCells != null && allCells.Count >= 6)
                {
                    // BetExplorer structure is typically: home team, score, away team, odds(1), odds(X), odds(2), date
                    // So odds are at indices 3, 4, 5
                    oddsCells = new HtmlNodeCollection(null);
                    if (allCells.Count > 3) oddsCells.Add(allCells[3]);
                    if (allCells.Count > 4) oddsCells.Add(allCells[4]);
                    if (allCells.Count > 5) oddsCells.Add(allCells[5]);

                    _logger.LogDebug("Using fallback odds cells at indices 3,4,5. Cell 3 HTML: {Html}",
                        allCells[3]?.OuterHtml?.Substring(0, Math.Min(200, allCells[3].OuterHtml.Length)) ?? "NULL");
                }
            }

            if (oddsCells != null && oddsCells.Count >= 3)
            {
                homeOdds = ExtractOddsFromCell(oddsCells[0]);
                drawOdds = ExtractOddsFromCell(oddsCells[1]);
                awayOdds = ExtractOddsFromCell(oddsCells[2]);

                _logger.LogDebug("Extracted odds: H={Home}, D={Draw}, A={Away}",
                    homeOdds?.ToString() ?? "null",
                    drawOdds?.ToString() ?? "null",
                    awayOdds?.ToString() ?? "null");
            }

            // Try to extract match date if available (last cell in row)
            var dateCell = cells.LastOrDefault();
            DateTime? matchDate = null;

            // DEBUG: Log cell count and last cell content
            _logger.LogDebug("Row has {CellCount} cells, last cell text: '{LastCellText}'",
                cells.Count, dateCell?.InnerText.Trim() ?? "NULL");

            if (dateCell != null)
            {
                var dateText = dateCell.InnerText.Trim(); // Format: "DD.MM."

                // Parse date - supports both "dd.MM." and "dd.MM.yyyy" formats
                if (!string.IsNullOrEmpty(dateText) && dateText.Contains("."))
                {
                    // Try parsing with full year first (format: "dd.MM.yyyy")
                    if (DateTime.TryParseExact(dateText, "dd.MM.yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                        out var fullDate))
                    {
                        matchDate = DateTime.SpecifyKind(fullDate, DateTimeKind.Utc);
                        _logger.LogDebug("Parsed date with full year: {Date}", matchDate);
                    }
                    else
                    {
                        // Extract years from season (e.g., "2024/2025" -> [2024, 2025])
                        var seasonYears = season.Split('/').Select(y => int.TryParse(y.Trim(), out var year) ? year : 0).Where(y => y > 0).ToArray();

                        if (seasonYears.Length >= 2)
                        {
                            // Try parsing with format "dd.MM." to get month and day
                            if (DateTime.TryParseExact(dateText, "dd.MM.",
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None,
                                out var tempDate))
                            {
                                // Determine correct year based on month:
                                // Aug-Dec (8-12) = first year, Jan-Jul (1-7) = second year
                                var year = tempDate.Month >= 8 ? seasonYears[0] : seasonYears[1];
                                matchDate = DateTime.SpecifyKind(new DateTime(year, tempDate.Month, tempDate.Day), DateTimeKind.Utc);
                                _logger.LogDebug("Parsed date without year: {Date}, inferred year: {Year}", matchDate, year);
                            }
                        }
                    }
                }
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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse match row");
            return null;
        }
    }

    private decimal? ExtractOddsFromCell(HtmlNode cell)
    {
        try
        {
            // Try multiple strategies to extract odds

            // Strategy 1: data-odd attribute on cell itself
            var oddAttr = cell.GetAttributeValue("data-odd", "");
            if (!string.IsNullOrEmpty(oddAttr) && decimal.TryParse(oddAttr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var odd))
            {
                return odd;
            }

            // Strategy 2: data-odd on span child
            var oddSpan = cell.SelectSingleNode(".//span[@data-odd]");
            if (oddSpan != null)
            {
                oddAttr = oddSpan.GetAttributeValue("data-odd", "");
                if (decimal.TryParse(oddAttr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out odd))
                {
                    return odd;
                }
            }

            // Strategy 3: Look for any element with data-odd
            var oddElement = cell.SelectSingleNode(".//*[@data-odd]");
            if (oddElement != null)
            {
                oddAttr = oddElement.GetAttributeValue("data-odd", "");
                if (decimal.TryParse(oddAttr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out odd))
                {
                    return odd;
                }
            }

            // Strategy 4: Parse inner text as decimal (fallback)
            var cellText = cell.InnerText.Trim();
            if (decimal.TryParse(cellText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out odd))
            {
                return odd;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting odds from cell");
            return null;
        }
    }

    private Round CreateRoundFromMatches(Guid leagueId, string season, int roundNumber, string? groupName, List<MatchResult> matches)
    {
        // Validation: No league has more than 15 matches per round
        // If we see more, it's a parsing error (e.g., multiple rounds merged)
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

        // Calculate cumulative odds (product of all odds for each outcome)
        var cumulativeHome = matches.Where(m => m.HomeOdds.HasValue).Aggregate(1.0m, (acc, m) => acc * m.HomeOdds!.Value);
        var cumulativeDraw = matches.Where(m => m.DrawOdds.HasValue).Aggregate(1.0m, (acc, m) => acc * m.DrawOdds!.Value);
        var cumulativeAway = matches.Where(m => m.AwayOdds.HasValue).Aggregate(1.0m, (acc, m) => acc * m.AwayOdds!.Value);

        // Check if all matches have complete odds
        var allHaveOdds = matches.All(m => m.HomeOdds.HasValue && m.DrawOdds.HasValue && m.AwayOdds.HasValue);
        var oddsComplete = allHaveOdds ? "Yes" : "Partial";

        // Create Round
        // Note: SeasonId is set by ImportOrchestrator after scraping
        var round = new Round
        {
            LeagueId = leagueId,
            SeasonId = Guid.Empty, // Will be set by ImportOrchestrator
            RoundNumber = roundNumber,
            GroupName = groupName, // null for leagues without groups, e.g., "East", "West", "GROUP 1"
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

        // Create Match entities from MatchResult data
        round.Matches = matches.Select(m => new Match
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
        public string Result { get; set; } = string.Empty; // "H", "D", or "A"
        public decimal? HomeOdds { get; set; }
        public decimal? DrawOdds { get; set; }
        public decimal? AwayOdds { get; set; }
        public DateTime? MatchDate { get; set; }
        public string? BetExplorerUrl { get; set; }
    }
}
