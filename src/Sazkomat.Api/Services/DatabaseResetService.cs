using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Data;
using Sazkomat.Data.Data;

namespace Sazkomat.Api.Services;

public class DatabaseResetService : IDatabaseResetService
{
    private readonly ConfigurationDbContext _configContext;
    private readonly DataDbContext _dataContext;
    private readonly ILogger<DatabaseResetService> _logger;

    public DatabaseResetService(
        ConfigurationDbContext configContext,
        DataDbContext dataContext,
        ILogger<DatabaseResetService> logger)
    {
        _configContext = configContext;
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task ResetDatabaseAsync()
    {
        _logger.LogWarning("Starting database reset - this will delete ALL data!");

        try
        {
            // Disable triggers and foreign key checks temporarily
            await _dataContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica';");
            await _configContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica';");

            // Delete data from data_import schema (in correct order due to FKs)
            _logger.LogInformation("Truncating data_import schema tables...");
            await _dataContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE data_import.matches CASCADE;");
            await _dataContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE data_import.rounds CASCADE;");
            await _dataContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE data_import.import_jobs CASCADE;");

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
            await _dataContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");
            await _configContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");

            _logger.LogWarning("Database reset completed - all data has been deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database reset");

            // Make sure to re-enable constraints even if error occurs
            try
            {
                await _dataContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");
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
            await _dataContext.Database.ExecuteSqlRawAsync("DELETE FROM data_import.import_jobs");

            _logger.LogInformation("Deleting rounds (and matches via cascade)...");
            await _dataContext.Database.ExecuteSqlRawAsync("DELETE FROM data_import.rounds");

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
            await _dataContext.Database.ExecuteSqlRawAsync("DELETE FROM data_import.import_jobs");

            _logger.LogInformation("Deleting rounds (and matches via cascade)...");
            await _dataContext.Database.ExecuteSqlRawAsync("DELETE FROM data_import.rounds");

            _logger.LogWarning("Imported data reset completed successfully");
            return (true, "Imported data deleted successfully. All configuration (countries, leagues, seasons) was preserved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset imported data");
            return (false, $"Database reset failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, Dictionary<string, int> DeletedCounts)> ResetSelectiveAsync(List<string> entities)
    {
        var deletedCounts = new Dictionary<string, int>();

        try
        {
            _logger.LogWarning("Starting selective database reset for entities: {Entities}", string.Join(", ", entities));

            // Define deletion order (respecting foreign key constraints)
            // Delete dependent tables first, then parent tables
            var deletionOrder = new List<string>
            {
                // Data import schema - most dependent first
                "rounds",           // cascade deletes matches
                "import_jobs",
                "sync_jobs",
                "provider_seasons",
                "provider_leagues",
                "provider_countries",
                "league_name_mappings",
                "country_name_mappings",
                "unmatched_leagues",
                // Configuration schema - junction tables first
                "league_seasons",
                "league_providers",
                "country_providers",
                // Configuration schema - main tables
                "leagues",
                "seasons",
                "countries"
            };

            // Filter to only requested entities and maintain order
            var entitiesToDelete = deletionOrder.Where(e => entities.Contains(e)).ToList();

            foreach (var entity in entitiesToDelete)
            {
                var count = await DeleteEntityAsync(entity);
                deletedCounts[entity] = count;
                _logger.LogInformation("Deleted {Count} records from {Entity}", count, entity);
            }

            var totalDeleted = deletedCounts.Values.Sum();
            _logger.LogWarning("Selective reset completed. Total records deleted: {Total}", totalDeleted);

            return (true, $"Successfully deleted {totalDeleted} records from {deletedCounts.Count} tables.", deletedCounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform selective reset");
            return (false, $"Selective reset failed: {ex.Message}", deletedCounts);
        }
    }

    private async Task<int> DeleteEntityAsync(string entity)
    {
        var sql = entity switch
        {
            // Data import schema
            "rounds" => "DELETE FROM data_import.rounds",
            "import_jobs" => "DELETE FROM data_import.import_jobs",
            "sync_jobs" => "DELETE FROM data_import.sync_jobs",
            "provider_countries" => "DELETE FROM data_import.provider_countries",
            "provider_leagues" => "DELETE FROM data_import.provider_leagues",
            "provider_seasons" => "DELETE FROM data_import.provider_seasons",
            "country_name_mappings" => "DELETE FROM data_import.country_name_mappings",
            "league_name_mappings" => "DELETE FROM data_import.league_name_mappings",
            "unmatched_leagues" => "DELETE FROM data_import.unmatched_leagues",
            // Configuration schema
            "league_seasons" => "DELETE FROM configuration.league_seasons",
            "league_providers" => "DELETE FROM configuration.league_providers",
            "country_providers" => "DELETE FROM configuration.country_providers",
            "leagues" => "DELETE FROM configuration.leagues",
            "seasons" => "DELETE FROM configuration.seasons",
            "countries" => "DELETE FROM configuration.countries",
            _ => throw new ArgumentException($"Unknown entity: {entity}")
        };

        // Use appropriate context based on schema
        var isDataSchema = entity is "rounds" or "import_jobs" or "sync_jobs"
            or "provider_countries" or "provider_leagues" or "provider_seasons"
            or "country_name_mappings" or "league_name_mappings" or "unmatched_leagues";

        var context = isDataSchema ? (DbContext)_dataContext : _configContext;
        return await context.Database.ExecuteSqlRawAsync(sql);
    }

    public async Task<Dictionary<string, int>> GetEntityCountsAsync()
    {
        var counts = new Dictionary<string, int>();

        try
        {
            // Data import schema
            counts["rounds"] = await CountTableAsync(_dataContext, "data_import.rounds");
            counts["import_jobs"] = await CountTableAsync(_dataContext, "data_import.import_jobs");
            counts["sync_jobs"] = await CountTableAsync(_dataContext, "data_import.sync_jobs");
            counts["provider_countries"] = await CountTableAsync(_dataContext, "data_import.provider_countries");
            counts["provider_leagues"] = await CountTableAsync(_dataContext, "data_import.provider_leagues");
            counts["provider_seasons"] = await CountTableAsync(_dataContext, "data_import.provider_seasons");
            counts["country_name_mappings"] = await CountTableAsync(_dataContext, "data_import.country_name_mappings");
            counts["league_name_mappings"] = await CountTableAsync(_dataContext, "data_import.league_name_mappings");
            counts["unmatched_leagues"] = await CountTableAsync(_dataContext, "data_import.unmatched_leagues");

            // Configuration schema - main tables
            counts["leagues"] = await CountTableAsync(_configContext, "configuration.leagues");
            counts["countries"] = await CountTableAsync(_configContext, "configuration.countries");
            counts["seasons"] = await CountTableAsync(_configContext, "configuration.seasons");

            // Configuration schema - binding tables
            counts["league_providers"] = await CountTableAsync(_configContext, "configuration.league_providers");
            counts["league_seasons"] = await CountTableAsync(_configContext, "configuration.league_seasons");
            counts["country_providers"] = await CountTableAsync(_configContext, "configuration.country_providers");

            return counts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity counts");
            throw;
        }
    }

    private async Task<int> CountTableAsync(DbContext context, string tableName)
    {
        var sql = $"SELECT COUNT(*) FROM {tableName}";
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        await context.Database.OpenConnectionAsync();
        try
        {
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    public async Task<Dictionary<string, Dictionary<string, int>>> GetBindingCountsByProviderAsync()
    {
        var result = new Dictionary<string, Dictionary<string, int>>();

        try
        {
            // Get all providers
            var providers = await _configContext.DataProviders
                .Select(p => new { p.Id, p.Code, p.Name })
                .ToListAsync();

            foreach (var provider in providers)
            {
                var providerKey = provider.Code;
                var counts = new Dictionary<string, int>();

                // Count league_providers for this provider
                counts["league_providers"] = await _configContext.LeagueProviders
                    .CountAsync(lp => lp.ProviderId == provider.Id);

                // Count country_providers for this provider
                counts["country_providers"] = await _configContext.CountryProviders
                    .CountAsync(cp => cp.ProviderId == provider.Id);

                // Count league_seasons - these are not per provider, skip or count all
                // league_seasons don't have ProviderId, so we'll count total once

                // Only add if provider has any bindings
                if (counts.Values.Any(c => c > 0))
                {
                    result[providerKey] = counts;
                }
            }

            // Add league_seasons as separate entry (not per provider)
            var leagueSeasonsCount = await _configContext.LeagueSeasons.CountAsync();
            if (leagueSeasonsCount > 0)
            {
                result["_league_seasons"] = new Dictionary<string, int>
                {
                    ["league_seasons"] = leagueSeasonsCount
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get binding counts by provider");
            throw;
        }
    }

    public async Task<(bool Success, string Message, Dictionary<string, int> DeletedCounts)> ResetBindingsForProviderAsync(
        string providerCode, List<string> bindingTypes)
    {
        var deletedCounts = new Dictionary<string, int>();

        try
        {
            // Get provider by code
            var provider = await _configContext.DataProviders
                .FirstOrDefaultAsync(p => p.Code == providerCode);

            if (provider == null)
            {
                return (false, $"Provider '{providerCode}' not found", deletedCounts);
            }

            _logger.LogWarning("Deleting bindings for provider {ProviderCode}: {BindingTypes}",
                providerCode, string.Join(", ", bindingTypes));

            foreach (var bindingType in bindingTypes)
            {
                int count = 0;

                switch (bindingType)
                {
                    case "league_providers":
                        count = await _configContext.Database.ExecuteSqlRawAsync(
                            "DELETE FROM configuration.league_providers WHERE provider_id = {0}",
                            provider.Id);
                        break;

                    case "country_providers":
                        count = await _configContext.Database.ExecuteSqlRawAsync(
                            "DELETE FROM configuration.country_providers WHERE provider_id = {0}",
                            provider.Id);
                        break;

                    default:
                        _logger.LogWarning("Unknown binding type: {BindingType}", bindingType);
                        continue;
                }

                deletedCounts[bindingType] = count;
                _logger.LogInformation("Deleted {Count} {BindingType} for provider {ProviderCode}",
                    count, bindingType, providerCode);
            }

            var totalDeleted = deletedCounts.Values.Sum();
            return (true, $"Successfully deleted {totalDeleted} bindings for provider '{providerCode}'", deletedCounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete bindings for provider {ProviderCode}", providerCode);
            return (false, $"Failed to delete bindings: {ex.Message}", deletedCounts);
        }
    }
}
