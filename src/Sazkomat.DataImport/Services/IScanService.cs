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
}
