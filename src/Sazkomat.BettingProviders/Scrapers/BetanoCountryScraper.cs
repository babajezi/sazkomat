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

    public async Task<List<CountryInfo>> ScrapeCountriesAsync(Sport sport, List<string>? excludedCountryIds = null)
    {
        _logger.LogInformation("Scraping countries from Betano for {Sport}", sport.Name);

        // Get ALL regions (countries) from Betano - not just those with leagues
        var regionsResult = await _betanoScraper.GetAvailableRegionsAsync(sport.Code);

        if (!regionsResult.IsSuccess)
        {
            _logger.LogError("Failed to get Betano regions: {Error}", regionsResult.Error);
            return new List<CountryInfo>();
        }

        var regions = regionsResult.Value!;

        // Filter out excluded region codes if provided
        if (excludedCountryIds != null && excludedCountryIds.Any())
        {
            var originalCount = regions.Count;
            regions = regions
                .Where(r => !excludedCountryIds.Contains(r.Code))
                .ToList();

            _logger.LogInformation("Filtered out {Excluded} regions based on ExcludedCountryIds configuration",
                originalCount - regions.Count);
        }

        // Log regions without leagues for debugging
        var regionsWithoutLeagues = regions.Where(r => !r.HasLeagues).ToList();
        if (regionsWithoutLeagues.Any())
        {
            _logger.LogInformation("Found {Count} regions WITHOUT leagues (will still be included):",
                regionsWithoutLeagues.Count);
            foreach (var r in regionsWithoutLeagues)
            {
                _logger.LogDebug("  ⚠️ No leagues: Code='{Code}', Name='{Name}'", r.Code, r.Name);
            }
        }

        // Convert regions to CountryInfo
        var countries = regions
            .Where(r => !string.IsNullOrEmpty(r.Code) && !string.IsNullOrEmpty(r.Name))
            .Select(r => new CountryInfo
            {
                Code = r.Code,
                Name = r.Name,
                ProviderCode = r.Code
            })
            .OrderBy(c => c.Name)
            .ToList();

        _logger.LogInformation("Found {Count} countries from Betano ({WithLeagues} with leagues, {WithoutLeagues} without)",
            countries.Count,
            regions.Count(r => r.HasLeagues),
            regions.Count(r => !r.HasLeagues));

        return countries;
    }
}
