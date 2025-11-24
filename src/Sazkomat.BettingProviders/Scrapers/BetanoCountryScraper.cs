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

        // Get all leagues from Betano
        var result = await _betanoScraper.GetAvailableLeaguesAsync(sport.Code);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to scrape Betano: {Error}", result.Error);
            return new List<CountryInfo>();
        }

        // LOG SPECIAL SECTIONS FOR DEBUGGING
        var specialNames = new[] { "Kluby CAF", "Kluby FIFA", "Mezinárodní", "Mezinárodní kluby",
                                   "Mistrovství světa", "Vylepšené kurzy", "kluby UEFA" };
        var specialLeagues = result.Value!
            .Where(l => specialNames.Contains(l.CountryName))
            .GroupBy(l => l.CountryName)
            .ToDictionary(g => g.Key, g => g.Select(l => new { l.ProviderLeagueId, l.ProviderLeagueName }).ToList());

        foreach (var kvp in specialLeagues)
        {
            _logger.LogWarning("🔍 SPECIAL SECTION '{Section}': {Count} leagues", kvp.Key, kvp.Value.Count);
            foreach (var league in kvp.Value)
            {
                _logger.LogWarning("   - ID: {Id}, Name: {Name}", league.ProviderLeagueId, league.ProviderLeagueName);
            }
        }

        // Filter out excluded league IDs if provided (e.g., special sections like "Kluby UEFA", "FIFA", etc.)
        var leagues = result.Value!;
        if (excludedCountryIds != null && excludedCountryIds.Any())
        {
            var originalCount = leagues.Count;
            leagues = leagues
                .Where(l => !excludedCountryIds.Contains(l.ProviderLeagueId))
                .ToList();

            _logger.LogInformation("Filtered out {Excluded} leagues based on ExcludedCountryIds configuration",
                originalCount - leagues.Count);
        }

        // Extract unique countries from league data
        var countries = leagues
            .Where(l => !string.IsNullOrEmpty(l.CountryCode) &&
                       !string.IsNullOrEmpty(l.CountryName))
            // Group by CountryName to handle "default" regionCode properly
            // (multiple countries can have regionCode="default" but different names)
            .GroupBy(l => l.CountryName)
            .Select(g => new CountryInfo
            {
                Code = g.First().CountryCode!,
                Name = g.Key!,
                ProviderCode = g.First().CountryCode!
            })
            .OrderBy(c => c.Name)
            .ToList();

        _logger.LogInformation("Found {Count} countries from Betano",
            countries.Count);
        return countries;
    }
}
