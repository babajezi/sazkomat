using Sazkomat.Configuration.Entities;
using Sazkomat.Data.Scrapers;

namespace Sazkomat.Data.Services;

/// <summary>
/// Service for enriching league metadata from betting providers with BetExplorer data.
/// This allows us to use betting providers as primary source (only bettable leagues)
/// and then enrich with BetExplorer details (slug, priority, metadata).
/// </summary>
public interface IBetExplorerEnrichmentService
{
    /// <summary>
    /// Enriches league metadata from a betting provider with BetExplorer data.
    /// </summary>
    /// <param name="providerLeague">League metadata from betting provider (Betano, Fortuna, etc.)</param>
    /// <param name="country">Country entity for the league</param>
    /// <param name="providerCode">Provider code (betano, fortuna, etc.) for manual mapping lookup</param>
    /// <returns>
    /// Enriched LeagueMetadata with BetExplorer slug and details, or null if league not found on BetExplorer.
    /// </returns>
    Task<LeagueMetadata?> EnrichLeagueAsync(LeagueMetadata providerLeague, Country country, string providerCode);

    /// <summary>
    /// Finds a matching league in the configuration database (BetExplorer source of truth).
    /// Uses manual mappings and slug matching to find the correct league.
    /// </summary>
    /// <param name="providerLeague">League metadata from betting provider</param>
    /// <param name="country">Country entity for the league</param>
    /// <param name="providerCode">Provider code for manual mapping lookup</param>
    /// <returns>Matching League entity from configuration, or null if not found</returns>
    Task<League?> FindMatchingLeagueAsync(LeagueMetadata providerLeague, Country country, string providerCode);

    /// <summary>
    /// Finds an existing league or creates a new one from BetExplorer data.
    /// This is the main method for betting provider workflow:
    /// 1. First tries to find existing league in configuration
    /// 2. If not found, scrapes BetExplorer on-demand for the country
    /// 3. Tries to match the betting provider league to a BetExplorer league
    /// 4. If match found, creates the league in configuration with BetExplorer data
    /// </summary>
    /// <param name="providerLeague">League metadata from betting provider</param>
    /// <param name="country">Country entity for the league</param>
    /// <param name="providerCode">Provider code (e.g., "betano")</param>
    /// <param name="sportId">Sport ID for the new league</param>
    /// <returns>
    /// Existing or newly created League entity, or null if no BetExplorer match found.
    /// When null is returned, the league should be added to unmatched_leagues for manual review.
    /// </returns>
    Task<League?> FindOrCreateLeagueFromBetExplorerAsync(
        LeagueMetadata providerLeague,
        Country country,
        string providerCode,
        Guid sportId);
}
