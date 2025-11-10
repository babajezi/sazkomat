using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Scrapers;

namespace Sazkomat.DataImport.Services;

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
}
