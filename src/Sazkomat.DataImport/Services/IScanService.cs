using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Services;

/// <summary>
/// Service for scanning and caching available data from providers.
/// Scan = Discovery without import (stores in provider_* cache tables).
/// </summary>
public interface IScanService
{
    /// <summary>
    /// Scans all available countries from a provider and stores them in cache.
    /// Creates a SyncJob with type=Scan and entity_type=Countries.
    /// </summary>
    Task<Guid> ScanCountriesAsync(Guid providerId);

    /// <summary>
    /// Scans all available leagues for specified countries from a provider.
    /// Creates a SyncJob with type=Scan and entity_type=Leagues.
    /// </summary>
    Task<Guid> ScanLeaguesAsync(Guid providerId, List<Guid>? countryIds = null);

    /// <summary>
    /// Scans all available seasons for specified leagues from a provider.
    /// Creates a SyncJob with type=Scan and entity_type=Seasons.
    /// </summary>
    Task<Guid> ScanSeasonsAsync(Guid providerId, List<Guid>? leagueIds = null);

    /// <summary>
    /// Creates a scan job without executing it.
    /// Used by API endpoints to queue jobs for background processing via Hangfire.
    /// </summary>
    /// <param name="providerId">The provider to scan from</param>
    /// <param name="entityType">Type of entity to scan (Countries, Leagues, Seasons)</param>
    /// <param name="countryIds">Optional country IDs for league scans</param>
    /// <param name="leagueIds">Optional league IDs for season scans</param>
    /// <returns>The created job ID</returns>
    Task<Guid> CreateScanJobAsync(Guid providerId, SyncEntityType entityType, List<Guid>? countryIds = null, List<Guid>? leagueIds = null);

    /// <summary>
    /// Internal method: Scans countries using an existing job.
    /// Called by SyncJobProcessor to avoid duplicate job creation.
    /// </summary>
    Task ScanCountriesInternalAsync(Guid providerId, Guid jobId);

    /// <summary>
    /// Internal method: Scans leagues using an existing job.
    /// Called by SyncJobProcessor to avoid duplicate job creation.
    /// </summary>
    Task ScanLeaguesInternalAsync(Guid providerId, List<Guid> countryIds, Guid jobId);

    /// <summary>
    /// Internal method: Scans seasons using an existing job.
    /// Called by SyncJobProcessor to avoid duplicate job creation.
    /// </summary>
    Task ScanSeasonsInternalAsync(Guid providerId, List<Guid> leagueIds, Guid jobId);

    /// <summary>
    /// Gets all unimported (cached but not yet imported) countries for a provider.
    /// </summary>
    Task<List<ProviderCountry>> GetUnimportedCountriesAsync(Guid providerId);

    /// <summary>
    /// Gets all unimported (cached but not yet imported) leagues for a provider.
    /// </summary>
    Task<List<ProviderLeague>> GetUnimportedLeaguesAsync(Guid providerId);

    /// <summary>
    /// Gets all unimported (cached but not yet imported) seasons for a provider.
    /// </summary>
    Task<List<ProviderSeason>> GetUnimportedSeasonsAsync(Guid providerId);

    /// <summary>
    /// Applies active country name mappings to create missing ProviderCountry entries.
    /// Use this after manually editing mappings to sync them into the cache without running a full scan.
    /// </summary>
    /// <returns>Number of ProviderCountry entries created</returns>
    Task<int> ApplyCountryMappingsAsync(Guid providerId);

    /// <summary>
    /// Backfills provider_leagues from resolved unmatched_leagues.
    /// Creates provider_leagues entries for all resolved (mapped) unmatched leagues
    /// that don't yet have a corresponding provider_leagues record.
    /// </summary>
    /// <param name="providerId">The provider to backfill for</param>
    /// <returns>Tuple with (CreatedCount, UpdatedCount)</returns>
    Task<(int Created, int Updated)> BackfillProviderLeaguesFromResolvedAsync(Guid providerId);

    /// <summary>
    /// Internal method: Scans both countries AND leagues in a single pass using an existing job.
    /// Optimized for betting providers like Betano where both come from a single HTTP request.
    /// Called by SyncJobProcessor to avoid duplicate job creation.
    /// </summary>
    Task ScanCountriesAndLeaguesInternalAsync(Guid providerId, Guid jobId);
}
