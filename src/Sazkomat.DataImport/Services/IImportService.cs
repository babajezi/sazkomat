using Sazkomat.Configuration.Entities;

namespace Sazkomat.DataImport.Services;

/// <summary>
/// Service for importing cached provider data into Configuration schema.
/// Import = Moving data from provider_* cache tables to configuration.* tables.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Imports selected provider countries into the Configuration.Countries table.
    /// Updates ProviderCountry.IsImported and ProviderCountry.CountryId.
    /// Creates a SyncJob with type=Import and entity_type=Countries.
    /// </summary>
    Task<Guid> ImportCountriesAsync(Guid providerId, List<Guid> providerCountryIds);

    /// <summary>
    /// Imports selected provider leagues into the Configuration.Leagues table.
    /// Creates League and LeagueSeason entities, links them via LeagueProvider.
    /// Updates ProviderLeague.IsImported and ProviderLeague.LeagueId.
    /// Creates a SyncJob with type=Import and entity_type=Leagues.
    /// </summary>
    Task<Guid> ImportLeaguesAsync(Guid providerId, List<Guid> providerLeagueIds);

    /// <summary>
    /// Imports selected provider seasons into the Configuration.Seasons/LeagueSeasons tables.
    /// Updates ProviderSeason.IsImported and ProviderSeason.SeasonId.
    /// Creates a SyncJob with type=Import and entity_type=Seasons.
    /// </summary>
    Task<Guid> ImportSeasonsAsync(Guid providerId, List<Guid> providerSeasonIds);

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
