using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Data.Scrapers;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Season scraper for Betano
/// Note: Betano typically only shows current season data, so we return current year patterns
/// </summary>
public class BetanoSeasonScraper : ISeasonScraper
{
    private readonly ILogger<BetanoSeasonScraper> _logger;

    public BetanoSeasonScraper(ILogger<BetanoSeasonScraper> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("betano", StringComparison.OrdinalIgnoreCase);
    }

    public Task<List<string>> ScrapeAvailableSeasonsAsync(League league)
    {
        _logger.LogInformation("Getting available seasons from Betano for league {League}", league.Name);

        // Betano shows only current season data
        // Generate current season pattern based on current date
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;

        var seasons = new List<string>();

        // If we're in the second half of the year (July-December), the season is YYYY-YYYY+1
        // Otherwise, it's YYYY-1-YYYY
        if (currentMonth >= 7)
        {
            // Current season: 2024-2025
            seasons.Add($"{currentYear}-{currentYear + 1}");
        }
        else
        {
            // Current season: 2023-2024
            seasons.Add($"{currentYear - 1}-{currentYear}");
        }

        _logger.LogInformation("Betano available seasons for {League}: {Seasons}",
            league.Name, string.Join(", ", seasons));

        return Task.FromResult(seasons);
    }
}
