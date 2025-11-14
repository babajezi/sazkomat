using Sazkomat.Configuration.Entities;

namespace Sazkomat.DataImport.Services;

/// <summary>
/// Service for importing cached provider data into Configuration schema.
/// Import = Moving data from provider_* cache tables to configuration.* tables.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Imports provider countries from cache into the Configuration.Countries table.
    /// Creates a SyncJob with status=Pending, then executes the import asynchronously.
    /// Updates ProviderCountry.IsImported and ProviderCountry.CountryId.
    /// If providerCountryIds is null, imports all cached countries for the provider.
    /// Returns the SyncJob ID for tracking.
    /// </summary>
    Task<Guid> ImportCountriesFromCacheAsync(Guid providerId, List<Guid>? providerCountryIds = null);

    /// <summary>
    /// Imports provider leagues from cache into the Configuration.Leagues table.
    /// Creates a SyncJob with status=Pending, then executes the import asynchronously.
    /// Creates League and LeagueProvider entities with proper mappings.
    /// Updates ProviderLeague.IsImported and ProviderLeague.LeagueId.
    /// If providerLeagueIds is null, imports all cached leagues for the provider.
    /// Returns the SyncJob ID for tracking.
    /// </summary>
    Task<Guid> ImportLeaguesFromCacheAsync(Guid providerId, List<Guid>? providerLeagueIds = null);

    /// <summary>
    /// Imports provider seasons from cache into the Configuration.Seasons/LeagueSeasons tables.
    /// Creates a SyncJob with status=Pending, then executes the import asynchronously.
    /// Updates ProviderSeason.IsImported and ProviderSeason.SeasonId.
    /// If providerSeasonIds is null, imports all cached seasons for the provider.
    /// Returns the SyncJob ID for tracking.
    /// </summary>
    Task<Guid> ImportSeasonsFromCacheAsync(Guid providerId, List<Guid>? providerSeasonIds = null);

    /// <summary>
    /// Internal method: Executes countries import for an existing SyncJob.
    /// Should only be called by SyncJobProcessor with an existing jobId and the CountryIds to import.
    /// Handles all status updates and resilient error handling.
    /// </summary>
    Task ImportCountriesFromCacheInternalAsync(Guid jobId, List<Guid> providerCountryIds);

    /// <summary>
    /// Internal method: Executes leagues import for an existing SyncJob.
    /// Should only be called by SyncJobProcessor with an existing jobId and the LeagueIds to import.
    /// Handles all status updates and resilient error handling.
    /// </summary>
    Task ImportLeaguesFromCacheInternalAsync(Guid jobId, List<Guid> providerLeagueIds);

    /// <summary>
    /// Internal method: Executes seasons import for an existing SyncJob.
    /// Should only be called by SyncJobProcessor with an existing jobId and the SeasonIds to import.
    /// Handles all status updates and resilient error handling.
    /// </summary>
    Task ImportSeasonsFromCacheInternalAsync(Guid jobId, List<Guid> providerSeasonIds);

    /// <summary>
    /// Gets import statistics for a provider showing cached vs imported counts.
    /// </summary>
    Task<ImportStats> GetImportStatsAsync(Guid providerId);
}

public record ImportStats(
    int CachedCountries,
    int ImportedCountries,
    int CachedLeagues,
    int ImportedLeagues,
    int CachedSeasons,
    int ImportedSeasons
);
