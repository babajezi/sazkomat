using Sazkomat.Configuration.Entities;

namespace Sazkomat.DataImport.Scrapers;

public interface ILeagueMetadataScraper
{
    /// <summary>
    /// Scrapes available leagues from the provider for a specific country and sport
    /// </summary>
    /// <param name="sport">The sport</param>
    /// <param name="country">The country</param>
    /// <returns>List of league metadata</returns>
    Task<List<LeagueMetadata>> ScrapeLeaguesAsync(Sport sport, Country country);

    /// <summary>
    /// Scrapes leagues only from the current season for a specific country and sport
    /// </summary>
    /// <param name="sport">The sport</param>
    /// <param name="country">The country</param>
    /// <param name="seasonPatterns">List of patterns to identify current season (e.g., "2025", "2025-2026")</param>
    /// <returns>List of league metadata from current season only</returns>
    Task<List<LeagueMetadata>> ScrapeLeaguesForCurrentSeasonAsync(Sport sport, Country country, List<string> seasonPatterns);

    /// <summary>
    /// Checks if this scraper can handle the given provider
    /// </summary>
    bool CanHandle(DataProvider provider);
}

public class LeagueMetadata
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string? ProviderLeagueId { get; set; }
    public int Priority { get; set; } = 5;
    public bool IsBettable { get; set; } = true;
    public string? SeasonName { get; set; }
    public bool IsCurrentSeason { get; set; } = false;
}
