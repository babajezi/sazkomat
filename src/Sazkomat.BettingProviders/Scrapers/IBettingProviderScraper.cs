using Sazkomat.BettingProviders.Entities;
using Sazkomat.Core.Common;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Interface for scraping betting providers to get available leagues
/// </summary>
public interface IBettingProviderScraper
{
    /// <summary>
    /// Provider code (e.g., "betano", "chance")
    /// </summary>
    string ProviderCode { get; }

    /// <summary>
    /// Get all available leagues for a specific sport from the betting provider
    /// </summary>
    /// <param name="sportCode">Sport code (e.g., "football", "basketball")</param>
    /// <returns>List of available leagues</returns>
    Task<Result<List<LeagueAvailability>>> GetAvailableLeaguesAsync(string sportCode);
}
