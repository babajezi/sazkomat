using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Data.Scrapers;

public class BetExplorerLeagueMetadataScraper : ILeagueMetadataScraper
{
    private readonly ResilientHttpClient _httpClient;
    private readonly ILogger<BetExplorerLeagueMetadataScraper> _logger;

    public BetExplorerLeagueMetadataScraper(
        ResilientHttpClient httpClient,
        ILogger<BetExplorerLeagueMetadataScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("betexplorer", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<LeagueMetadata>> ScrapeLeaguesAsync(Sport sport, Country country)
    {
        var leagues = new List<LeagueMetadata>();

        try
        {
            // Build URL for country's league listing
            // Example: https://www.betexplorer.com/football/england/
            var sportSlug = sport.Code.ToLowerInvariant();
            var countrySlug = country.Code.ToLowerInvariant();
            var url = $"https://www.betexplorer.com/{sportSlug}/{countrySlug}/";

            _logger.LogInformation("Scraping leagues for {Country} ({Sport}) from {Url}",
                country.Name, sport.Name, url);

            var html = await _httpClient.GetHtmlAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // BetExplorer lists leagues as links within the country page
            // Example: <a href="/football/england/premier-league/">Premier League</a>

            // Find all league links - don't require hyphen in slug (e.g., "npfl" has no hyphen)
            var leagueLinks = doc.DocumentNode.SelectNodes(
                $"//a[starts-with(@href, '/{sportSlug}/{countrySlug}/')]");

            if (leagueLinks != null)
            {
                var priorityCounter = 1; // Start from lowest priority

                foreach (var link in leagueLinks)
                {
                    var href = link.GetAttributeValue("href", "");
                    var leagueName = link.InnerText.Trim();

                    if (string.IsNullOrEmpty(leagueName) || string.IsNullOrEmpty(href))
                        continue;

                    // Extract league slug from href
                    // Example: "/football/england/premier-league/" -> "premier-league"
                    var parts = href.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        var leagueSlug = parts[2];

                        // Skip season-specific pages (contain year patterns - both 19XX and 20XX)
                        if (System.Text.RegularExpressions.Regex.IsMatch(leagueSlug, @"(19|20)\d{2}"))
                            continue;

                        // Skip cup/archive pages if needed
                        if (IsInvalidLeagueSlug(leagueSlug))
                            continue;

                        var metadata = new LeagueMetadata
                        {
                            Name = ExtractLeagueName(leagueName),
                            DisplayName = $"{ExtractLeagueName(leagueName)} ({country.Name})",
                            Slug = leagueSlug,
                            Priority = priorityCounter++,
                            IsBettable = true
                        };

                        // Avoid duplicates
                        if (!leagues.Any(l => l.Slug.Equals(metadata.Slug, StringComparison.OrdinalIgnoreCase)))
                        {
                            leagues.Add(metadata);
                            _logger.LogDebug("Found league: {League} (slug: {Slug})",
                                metadata.Name, metadata.Slug);
                        }
                    }
                }
            }

            _logger.LogInformation("Found {Count} leagues for {Country} ({Sport})",
                leagues.Count, country.Name, sport.Name);

            return leagues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping leagues for {Country} ({Sport})",
                country.Name, sport.Name);
            return leagues;
        }
    }

    public async Task<List<LeagueMetadata>> ScrapeLeaguesForCurrentSeasonAsync(
        Sport sport,
        Country country,
        List<string> seasonPatterns)
    {
        var leagues = new List<LeagueMetadata>();

        try
        {
            // Build URL for country's league listing
            // Example: https://www.betexplorer.com/football/england/
            var sportSlug = sport.Code.ToLowerInvariant();
            var countrySlug = country.Code.ToLowerInvariant();
            var url = $"https://www.betexplorer.com/{sportSlug}/{countrySlug}/";

            _logger.LogInformation("Scraping leagues for current season: {Country} ({Sport}) from {Url}",
                country.Name, sport.Name, url);
            _logger.LogInformation("Looking for season patterns: {Patterns}", string.Join(", ", seasonPatterns));

            var html = await _httpClient.GetHtmlAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // BetExplorer organizes leagues by season in separate <tbody> sections
            // Each section has a header: <tr><th class="h-text-left">{SEASON}</th></tr>
            var tbodySections = doc.DocumentNode.SelectNodes("//table//tbody");

            if (tbodySections == null || tbodySections.Count == 0)
            {
                _logger.LogWarning("No tbody sections found for {Country} ({Sport})", country.Name, sport.Name);
                return leagues;
            }

            _logger.LogInformation("Found {Count} season sections to analyze", tbodySections.Count);

            string? currentSeasonName = null;
            HtmlNode? currentSeasonSection = null;

            // Find the section that matches current season patterns
            foreach (var tbody in tbodySections)
            {
                // Get season header from first row
                var headerRow = tbody.SelectSingleNode(".//tr[1]//th[@class='h-text-left']");
                if (headerRow == null)
                    continue;

                var seasonHeader = headerRow.InnerText.Trim();
                if (string.IsNullOrEmpty(seasonHeader))
                    continue;

                // Normalize season format for comparison (e.g., "2025/2026" → "2025-2026")
                var normalizedSeason = seasonHeader.Replace("/", "-");

                _logger.LogDebug("Checking season section: '{Season}' (normalized: '{Normalized}')",
                    seasonHeader, normalizedSeason);

                // Check if this season matches any of the patterns
                foreach (var pattern in seasonPatterns)
                {
                    if (normalizedSeason.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Found matching current season: '{Season}' matches pattern '{Pattern}'",
                            seasonHeader, pattern);
                        currentSeasonName = seasonHeader;
                        currentSeasonSection = tbody;
                        break;
                    }
                }

                if (currentSeasonSection != null)
                    break;
            }

            // If no matching season found, return empty list (NO fallback)
            if (currentSeasonSection == null)
            {
                _logger.LogWarning("No current season section found for {Country} ({Sport}) matching patterns: {Patterns}",
                    country.Name, sport.Name, string.Join(", ", seasonPatterns));
                return leagues;
            }

            _logger.LogInformation("Processing leagues from current season: {Season}", currentSeasonName);

            // Extract league links from the current season section only
            // Don't require hyphen in slug (e.g., "npfl" has no hyphen)
            var leagueLinks = currentSeasonSection.SelectNodes(
                $".//a[starts-with(@href, '/{sportSlug}/{countrySlug}/')]");

            if (leagueLinks != null)
            {
                var priorityCounter = 1; // Start from lowest priority

                foreach (var link in leagueLinks)
                {
                    var href = link.GetAttributeValue("href", "");
                    var leagueName = link.InnerText.Trim();

                    if (string.IsNullOrEmpty(leagueName) || string.IsNullOrEmpty(href))
                        continue;

                    // Extract league slug from href
                    // Example: "/football/england/premier-league-2024-2025/" -> "premier-league-2024-2025"
                    var parts = href.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        var originalSlug = parts[2];

                        // Remove year suffix from slug (e.g., "premier-league-2024-2025" → "premier-league")
                        var cleanSlug = RemoveYearFromSlug(originalSlug);

                        // Skip cup/archive pages if needed
                        if (IsInvalidLeagueSlug(cleanSlug))
                            continue;

                        var metadata = new LeagueMetadata
                        {
                            Name = ExtractLeagueName(leagueName),
                            DisplayName = $"{ExtractLeagueName(leagueName)} ({country.Name})",
                            Slug = cleanSlug,
                            Priority = priorityCounter++,
                            IsBettable = true,
                            SeasonName = currentSeasonName,
                            IsCurrentSeason = true
                        };

                        // Avoid duplicates
                        if (!leagues.Any(l => l.Slug.Equals(metadata.Slug, StringComparison.OrdinalIgnoreCase)))
                        {
                            leagues.Add(metadata);
                            _logger.LogDebug("Found current season league: {League} (slug: {Slug}, season: {Season})",
                                metadata.Name, metadata.Slug, currentSeasonName);
                        }
                    }
                }
            }

            _logger.LogInformation("Found {Count} leagues from current season for {Country} ({Sport})",
                leagues.Count, country.Name, sport.Name);

            return leagues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping current season leagues for {Country} ({Sport})",
                country.Name, sport.Name);
            return leagues;
        }
    }

    private string RemoveYearFromSlug(string slug)
    {
        // Remove year patterns from slug (e.g., "premier-league-2024-2025" → "premier-league")
        // Matches patterns like: -2024, -2024-2025, -19XX, -19XX-20XX, -20XX, -20XX-20XX
        var yearPattern = @"-\d{4}(-\d{4})?$";
        return System.Text.RegularExpressions.Regex.Replace(slug, yearPattern, "");
    }

    private bool IsInvalidLeagueSlug(string slug)
    {
        // Filter out common non-league pages
        var invalidSlugs = new[] { "results", "fixtures", "standings", "archive", "cup" };
        return invalidSlugs.Any(invalid => slug.Contains(invalid, StringComparison.OrdinalIgnoreCase));
    }

    private string ExtractLeagueName(string name)
    {
        // Clean up league name
        return name.Trim()
            .Replace("&nbsp;", " ")
            .Replace("  ", " ");
    }
}
