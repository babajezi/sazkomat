using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Data.Scrapers;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Adapter that bridges BetanoScraper to ILeagueMetadataScraper interface
/// Enables Betano to work with ProviderSyncService
/// </summary>
public class BetanoLeagueMetadataScraper : ILeagueMetadataScraper
{
    private readonly BetanoScraper _betanoScraper;
    private readonly ILogger<BetanoLeagueMetadataScraper> _logger;

    public BetanoLeagueMetadataScraper(
        BetanoScraper betanoScraper,
        ILogger<BetanoLeagueMetadataScraper> logger)
    {
        _betanoScraper = betanoScraper;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("betano", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<LeagueMetadata>> ScrapeLeaguesAsync(Sport sport, Country country)
    {
        _logger.LogInformation("Scraping leagues from Betano for {Sport} / {Country}",
            sport.Name, country.Name);

        // Get leagues from BetanoScraper
        var result = await _betanoScraper.GetAvailableLeaguesAsync(sport.Code);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to scrape Betano: {Error}", result.Error);
            return new List<LeagueMetadata>();
        }

        // Filter by country and transform to LeagueMetadata
        var leagues = result.Value!
            .Where(l => l.CountryCode != null &&
                       l.CountryCode.Equals(country.Code, StringComparison.OrdinalIgnoreCase))
            .Select(l => new LeagueMetadata
            {
                Name = l.ProviderLeagueName,
                DisplayName = $"{l.ProviderLeagueName} ({l.CountryName})",
                Slug = GenerateSlug(l.ProviderLeagueName),
                CountryCode = country.Code,
                ProviderLeagueId = l.ProviderLeagueId,
                Priority = 5,
                IsBettable = true,
                IsCurrentSeason = true // Betano only shows current season
            })
            .ToList();

        _logger.LogInformation("Found {Count} leagues in {Country}", leagues.Count, country.Name);
        return leagues;
    }

    public async Task<List<LeagueMetadata>> ScrapeLeaguesForCurrentSeasonAsync(
        Sport sport,
        Country country,
        List<string> seasonPatterns)
    {
        // Betano only shows current season leagues, so this is the same as ScrapeLeaguesAsync
        return await ScrapeLeaguesAsync(sport, country);
    }

    private string GenerateSlug(string name)
    {
        return name
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace("'", "");
    }
}
