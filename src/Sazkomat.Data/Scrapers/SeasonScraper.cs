using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Data.Scrapers;

public class BetExplorerSeasonScraper : ISeasonScraper
{
    private readonly IHttpClient _httpClient;
    private readonly ILogger<BetExplorerSeasonScraper> _logger;

    public BetExplorerSeasonScraper(
        IHttpClient httpClient,
        ILogger<BetExplorerSeasonScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("betexplorer", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<string>> ScrapeAvailableSeasonsAsync(League league)
    {
        var seasons = new List<string>();

        try
        {
            // Build URL to league page (without specific season)
            // Example: https://www.betexplorer.com/football/england/premier-league/
            var countrySlug = league.Country?.Code?.ToLowerInvariant() ?? "unknown";
            var url = $"https://www.betexplorer.com/football/{countrySlug}/{league.BetExplorerSlug}/";

            _logger.LogInformation("Scraping available seasons for {League} from {Url}",
                league.Name, url);

            var html = await _httpClient.GetHtmlAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Look for season selector - BetExplorer uses select element with options
            // Try method 1: Find ANY select element and check its options for year patterns
            var selectElements = doc.DocumentNode.SelectNodes("//select");
            if (selectElements != null)
            {
                foreach (var selectElement in selectElements)
                {
                    var options = selectElement.SelectNodes(".//option");
                    if (options != null)
                    {
                        foreach (var option in options)
                        {
                            var seasonText = option.InnerText.Trim();
                            // Check if option text contains dual-year pattern (e.g., "2024/2025" or "1999/2000")
                            var match = System.Text.RegularExpressions.Regex.Match(seasonText, @"(19|20)\d{2}[/-](19|20)\d{2}");
                            if (!match.Success)
                            {
                                // Fallback: match single year (e.g., "2024" or "1999") for calendar-year leagues
                                match = System.Text.RegularExpressions.Regex.Match(seasonText, @"^((19|20)\d{2})$");
                            }
                            if (match.Success)
                            {
                                var normalizedSeason = NormalizeSeason(match.Value);
                                if (!string.IsNullOrEmpty(normalizedSeason) && !seasons.Contains(normalizedSeason))
                                {
                                    seasons.Add(normalizedSeason);
                                }
                            }
                        }
                    }
                }
            }

            // Try method 2: Look for links with season patterns in href
            if (seasons.Count == 0)
            {
                var links = doc.DocumentNode.SelectNodes("//a[contains(@href, '-20') or contains(@href, '/20') or contains(@href, '-19') or contains(@href, '/19')]");
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        var href = link.GetAttributeValue("href", "");
                        // First try to match XXXX-YYYY format (preferred)
                        var match = System.Text.RegularExpressions.Regex.Match(href, @"(19|20)\d{2}[-/](19|20)\d{2}");
                        if (!match.Success)
                        {
                            // Fallback: try single year (only if not part of XXXX-YYYY)
                            match = System.Text.RegularExpressions.Regex.Match(href, @"(?<!\d)(19|20)\d{2}(?!\d|[-/](19|20)\d{2})");
                        }
                        if (match.Success)
                        {
                            var seasonText = match.Value;
                            var normalizedSeason = NormalizeSeason(seasonText);
                            if (!string.IsNullOrEmpty(normalizedSeason) && !seasons.Contains(normalizedSeason))
                            {
                                seasons.Add(normalizedSeason);
                            }
                        }
                    }
                }
            }

            // Try method 3: Look for div/span with class containing "season" and text with year patterns
            if (seasons.Count == 0)
            {
                var seasonElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'season')]");
                if (seasonElements != null)
                {
                    foreach (var element in seasonElements)
                    {
                        var text = element.InnerText.Trim();
                        // First try to match XXXX-YYYY format (preferred)
                        var match = System.Text.RegularExpressions.Regex.Match(text, @"(19|20)\d{2}[-/](19|20)\d{2}");
                        if (!match.Success)
                        {
                            // Fallback: try single year (only if not part of XXXX-YYYY)
                            match = System.Text.RegularExpressions.Regex.Match(text, @"(?<!\d)(19|20)\d{2}(?!\d|[-/](19|20)\d{2})");
                        }
                        if (match.Success)
                        {
                            var normalizedSeason = NormalizeSeason(match.Value);
                            if (!string.IsNullOrEmpty(normalizedSeason) && !seasons.Contains(normalizedSeason))
                            {
                                seasons.Add(normalizedSeason);
                            }
                        }
                    }
                }
            }

            // BetExplorer nikdy nevrací "2024" a "2024-2025" současně pro stejný rok
            // Prostě vezmeme všechny nalezené sezóny
            var uniqueSeasons = seasons.Distinct().ToList();

            _logger.LogInformation("Found {Count} seasons for {League}: {Seasons}",
                uniqueSeasons.Count, league.Name, string.Join(", ", uniqueSeasons));

            return uniqueSeasons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping seasons for {League}", league.Name);
            return seasons;
        }
    }

    private string NormalizeSeason(string season)
    {
        if (string.IsNullOrWhiteSpace(season))
        {
            return string.Empty;
        }

        // Replace / with - for consistency
        season = season.Replace("/", "-");

        // Validate format (should be YYYY-YYYY or YYYY, supporting both 19xx and 20xx years)
        var match = System.Text.RegularExpressions.Regex.Match(season, @"((19|20)\d{2})(?:[-]((19|20)\d{2}))?");
        if (match.Success)
        {
            return match.Value;
        }

        return string.Empty;
    }
}
