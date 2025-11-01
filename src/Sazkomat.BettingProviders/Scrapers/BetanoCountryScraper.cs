using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Scrapers;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Country scraper for Betano - extracts countries from scraped league data
/// </summary>
public class BetanoCountryScraper : ICountryScraper
{
    private readonly BetanoScraper _betanoScraper;
    private readonly ILogger<BetanoCountryScraper> _logger;

    public BetanoCountryScraper(
        BetanoScraper betanoScraper,
        ILogger<BetanoCountryScraper> logger)
    {
        _betanoScraper = betanoScraper;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("betano", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<CountryInfo>> ScrapeCountriesAsync(Sport sport)
    {
        _logger.LogInformation("Scraping countries from Betano for {Sport}", sport.Name);

        // Get all leagues from Betano
        var result = await _betanoScraper.GetAvailableLeaguesAsync(sport.Code);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to scrape Betano: {Error}", result.Error);
            return new List<CountryInfo>();
        }

        // Extract unique countries from league data
        var countries = result.Value!
            .Where(l => !string.IsNullOrEmpty(l.CountryCode) && !string.IsNullOrEmpty(l.CountryName))
            .GroupBy(l => l.CountryCode)
            .Select(g => new CountryInfo
            {
                Code = g.Key!,
                Name = g.First().CountryName!,
                ProviderCode = g.Key
            })
            .OrderBy(c => c.Name)
            .ToList();

        _logger.LogInformation("Found {Count} countries from Betano", countries.Count);
        return countries;
    }
}
