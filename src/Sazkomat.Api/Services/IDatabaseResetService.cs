namespace Sazkomat.Api.Services;

public interface IDatabaseResetService
{
    /// <summary>
    /// Deletes all data from all tables in both configuration and data_import schemas.
    /// This is a destructive operation intended for development only.
    /// </summary>
    Task ResetDatabaseAsync();

    /// <summary>
    /// Seeds the database with initial configuration data.
    /// </summary>
    Task SeedDatabaseAsync();

    /// <summary>
    /// Resets the database and then seeds it with initial data.
    /// </summary>
    Task ResetAndSeedAsync();

    /// <summary>
    /// Resets all data including configuration tables (countries, leagues, etc.)
    /// Keeps only sports and providers
    /// </summary>
    Task<(bool Success, string Message)> ResetAllDataAsync();

    /// <summary>
    /// Resets only imported data (rounds, matches, import_jobs)
    /// Keeps all configuration tables (countries, leagues, seasons, etc.)
    /// </summary>
    Task<(bool Success, string Message)> ResetImportedDataOnlyAsync();

    /// <summary>
    /// Selectively resets specified entities.
    /// Valid entity names: rounds, import_jobs, provider_countries, provider_leagues, provider_seasons,
    /// sync_jobs, country_name_mappings, league_name_mappings, unmatched_leagues, leagues, countries, seasons,
    /// league_providers, league_seasons, country_providers
    /// </summary>
    Task<(bool Success, string Message, Dictionary<string, int> DeletedCounts)> ResetSelectiveAsync(List<string> entities);

    /// <summary>
    /// Gets record counts for all resettable entities.
    /// </summary>
    Task<Dictionary<string, int>> GetEntityCountsAsync();

    /// <summary>
    /// Gets binding counts grouped by provider.
    /// Returns counts for league_providers, country_providers, league_seasons per provider.
    /// </summary>
    Task<Dictionary<string, Dictionary<string, int>>> GetBindingCountsByProviderAsync();

    /// <summary>
    /// Deletes bindings for a specific provider.
    /// Valid binding types: league_providers, country_providers
    /// </summary>
    Task<(bool Success, string Message, Dictionary<string, int> DeletedCounts)> ResetBindingsForProviderAsync(
        string providerCode, List<string> bindingTypes);
}
