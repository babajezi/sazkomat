using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Data;
using Sazkomat.DataImport.Data;

namespace Sazkomat.Api.Services;

public class DatabaseResetService : IDatabaseResetService
{
    private readonly ConfigurationDbContext _configContext;
    private readonly DataImportDbContext _dataImportContext;
    private readonly ILogger<DatabaseResetService> _logger;

    public DatabaseResetService(
        ConfigurationDbContext configContext,
        DataImportDbContext dataImportContext,
        ILogger<DatabaseResetService> logger)
    {
        _configContext = configContext;
        _dataImportContext = dataImportContext;
        _logger = logger;
    }

    public async Task ResetDatabaseAsync()
    {
        _logger.LogWarning("Starting database reset - this will delete ALL data!");

        try
        {
            // Disable triggers and foreign key checks temporarily
            await _dataImportContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica';");
            await _configContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica';");

            // Delete data from data_import schema (in correct order due to FKs)
            _logger.LogInformation("Truncating data_import schema tables...");
            await _dataImportContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE data_import.matches CASCADE;");
            await _dataImportContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE data_import.rounds CASCADE;");
            await _dataImportContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE data_import.import_jobs CASCADE;");

            // Delete data from configuration schema (in correct order due to FKs)
            _logger.LogInformation("Truncating configuration schema tables...");
            await _configContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE configuration.league_seasons CASCADE;");
            await _configContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE configuration.league_providers CASCADE;");
            await _configContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE configuration.country_providers CASCADE;");
            await _configContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE configuration.leagues CASCADE;");
            await _configContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE configuration.seasons CASCADE;");
            await _configContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE configuration.countries CASCADE;");
            await _configContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE configuration.sports CASCADE;");
            await _configContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE configuration.data_providers CASCADE;");

            // Re-enable triggers and foreign key checks
            await _dataImportContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");
            await _configContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");

            _logger.LogWarning("Database reset completed - all data has been deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database reset");

            // Make sure to re-enable constraints even if error occurs
            try
            {
                await _dataImportContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");
                await _configContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");
            }
            catch (Exception resetEx)
            {
                _logger.LogError(resetEx, "Failed to re-enable database constraints");
            }

            throw;
        }
    }

    public async Task SeedDatabaseAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        try
        {
            await ConfigurationSeeder.SeedAsync(_configContext);
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    public async Task ResetAndSeedAsync()
    {
        _logger.LogWarning("Starting database reset and seed operation...");

        await ResetDatabaseAsync();
        await SeedDatabaseAsync();

        _logger.LogInformation("Database reset and seed completed successfully");
    }

    public async Task<(bool Success, string Message)> ResetAllDataAsync()
    {
        try
        {
            _logger.LogWarning("Starting FULL database reset - all data will be deleted except sports and providers");

            // Delete data from data_import schema
            _logger.LogInformation("Deleting import jobs...");
            await _dataImportContext.Database.ExecuteSqlRawAsync("DELETE FROM data_import.import_jobs");

            _logger.LogInformation("Deleting rounds (and matches via cascade)...");
            await _dataImportContext.Database.ExecuteSqlRawAsync("DELETE FROM data_import.rounds");

            // Delete junction tables
            _logger.LogInformation("Deleting league seasons...");
            await _configContext.Database.ExecuteSqlRawAsync("DELETE FROM configuration.league_seasons");

            _logger.LogInformation("Deleting league providers...");
            await _configContext.Database.ExecuteSqlRawAsync("DELETE FROM configuration.league_providers");

            _logger.LogInformation("Deleting country providers...");
            await _configContext.Database.ExecuteSqlRawAsync("DELETE FROM configuration.country_providers");

            // Delete main configuration tables
            _logger.LogInformation("Deleting leagues...");
            await _configContext.Database.ExecuteSqlRawAsync("DELETE FROM configuration.leagues");

            _logger.LogInformation("Deleting seasons...");
            await _configContext.Database.ExecuteSqlRawAsync("DELETE FROM configuration.seasons");

            _logger.LogInformation("Deleting countries...");
            await _configContext.Database.ExecuteSqlRawAsync("DELETE FROM configuration.countries");

            // Sports and Providers are kept!

            _logger.LogWarning("Full database reset completed successfully");
            return (true, "All data deleted successfully. Sports and providers were preserved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset all data");
            return (false, $"Database reset failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ResetImportedDataOnlyAsync()
    {
        try
        {
            _logger.LogWarning("Starting imported data reset - only data_import schema will be cleared");

            // Delete only data_import schema tables
            _logger.LogInformation("Deleting import jobs...");
            await _dataImportContext.Database.ExecuteSqlRawAsync("DELETE FROM data_import.import_jobs");

            _logger.LogInformation("Deleting rounds (and matches via cascade)...");
            await _dataImportContext.Database.ExecuteSqlRawAsync("DELETE FROM data_import.rounds");

            _logger.LogWarning("Imported data reset completed successfully");
            return (true, "Imported data deleted successfully. All configuration (countries, leagues, seasons) was preserved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset imported data");
            return (false, $"Database reset failed: {ex.Message}");
        }
    }
}
