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
}
