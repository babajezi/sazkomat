using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Scrapers;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Fortuna league metadata scraper - SKELETON for future implementation.
/// TODO: Implement Fortuna-specific scraping logic when strategy for identifying leagues is determined.
/// </summary>
public class FortunaLeagueMetadataScraper : ILeagueMetadataScraper
{
    private readonly ILogger<FortunaLeagueMetadataScraper> _logger;

    public FortunaLeagueMetadataScraper(ILogger<FortunaLeagueMetadataScraper> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("fortuna", StringComparison.OrdinalIgnoreCase);
    }

    public Task<List<LeagueMetadata>> ScrapeLeaguesAsync(Sport sport, Country country)
    {
        // TODO: Implement Fortuna league scraping
        // Challenge: Determining the right approach to identify all bettable leagues
        // Current status: Skeleton implementation returns empty list

        _logger.LogWarning("Fortuna league scraping not yet implemented - returning empty list");
        return Task.FromResult(new List<LeagueMetadata>());
    }

    public Task<List<LeagueMetadata>> ScrapeLeaguesForCurrentSeasonAsync(
        Sport sport,
        Country country,
        List<string> seasonPatterns)
    {
        // TODO: Implement Fortuna current season league scraping

        _logger.LogWarning("Fortuna current season league scraping not yet implemented - returning empty list");
        return Task.FromResult(new List<LeagueMetadata>());
    }
}
