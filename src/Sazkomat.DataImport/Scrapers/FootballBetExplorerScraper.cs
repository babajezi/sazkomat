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
        var rounds = new List<Round>();

        try
        {
            // Convert season to URL format (e.g., "2023/2024" -> "2023-2024")
            var seasonSlug = season.Replace("/", "-");

            // Build URL: https://www.betexplorer.com/football/{country}/{league_slug}-{season_slug}/results/
            var countrySlug = league.Country?.Code?.ToLowerInvariant() ?? "unknown";
            var url = $"https://www.betexplorer.com/football/{countrySlug}/{league.BetExplorerSlug}-{seasonSlug}/results/";

            _logger.LogInformation("Scraping {League} season {Season} from {Url}",
                league.Name, season, url);

            _logger.LogDebug("Fetching HTML from BetExplorer...");
            var html = await _httpClient.GetHtmlAsync(url);
            _logger.LogDebug("HTML downloaded, size: {Size} characters", html.Length);

            // Parse HTML
            _logger.LogDebug("Parsing HTML document...");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            _logger.LogDebug("HTML parsed successfully");

            // Find the main results container
            var resultsContainer = doc.DocumentNode.SelectSingleNode("//div[@id='js-leagueresults-all']");

            if (resultsContainer == null)
            {
                _logger.LogWarning("No results container found for {League} season {Season}",
                    league.Name, season);
                return rounds;
            }

            // Find all tables (each table contains matches for one or more rounds)
            var tables = resultsContainer.SelectNodes(".//table[contains(@class, 'table-main')]");

            if (tables == null || !tables.Any())
            {
                _logger.LogWarning("No match tables found for {League} season {Season}",
                    league.Name, season);
                return rounds;
            }

            _logger.LogInformation("Found {TableCount} tables to process", tables.Count);

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

                // Use dictionary to accumulate matches per round (handles postponed matches)
                var roundMatches = new Dictionary<int, List<MatchResult>>();
                int? currentRoundNumber = null;

                foreach (var row in rows)
                {
                    // Check if this is a round header
                    var roundHeader = row.SelectSingleNode(".//th[contains(text(), 'Round')]");
                    if (roundHeader != null)
                    {
                        // Parse round number
                        currentRoundNumber = ExtractRoundNumber(roundHeader.InnerText);
                        _logger.LogDebug("Found round header: Round {RoundNumber}", currentRoundNumber);

                        // Initialize list if this round number not seen yet
                        if (!roundMatches.ContainsKey(currentRoundNumber.Value))
                        {
                            roundMatches[currentRoundNumber.Value] = new List<MatchResult>();
                        }
                        continue;
                    }

                    // Parse match row
                    var matchData = ParseMatchRow(row, season);
                    if (matchData != null && currentRoundNumber.HasValue)
                    {
                        roundMatches[currentRoundNumber.Value].Add(matchData);
                    }
                }

                // Create rounds from accumulated matches
                foreach (var kvp in roundMatches.OrderBy(x => x.Key))
                {
                    var roundNumber = kvp.Key;
                    var matches = kvp.Value;

                    if (matches.Any())
                    {
                        var round = CreateRoundFromMatches(league.Id, season, roundNumber, matches);
                        rounds.Add(round);
                        _logger.LogInformation("Parsed Round {RoundNumber}: {MatchCount} matches, {OddsStatus} odds",
                            roundNumber, matches.Count, round.OddsComplete);
                    }
                }
            }

            _logger.LogInformation("Successfully scraped {RoundCount} rounds for {League} season {Season}",
                rounds.Count, league.Name, season);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping {League} season {Season}",
                league.Name, season);
            throw;
        }

        return rounds;
    }

    // Helper methods for HTML parsing

    private int ExtractRoundNumber(string headerText)
    {
        // Extract number from text like "38. Round" or "Round 38"
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

    private Round CreateRoundFromMatches(Guid leagueId, string season, int roundNumber, List<MatchResult> matches)
    {
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
