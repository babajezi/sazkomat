using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using System.Text.Json;
using System.Transactions;

namespace Sazkomat.DataImport.Services;

public class ImportService : IImportService
{
    private readonly IProviderCountryRepository _providerCountryRepo;
    private readonly IProviderLeagueRepository _providerLeagueRepo;
    private readonly IProviderSeasonRepository _providerSeasonRepo;
    private readonly ISyncJobRepository _syncJobRepo;
    private readonly IDataProviderRepository _dataProviderRepo;
    private readonly ICountryRepository _countryRepo;
    private readonly ILeagueRepository _leagueRepo;
    private readonly ISeasonRepository _seasonRepo;
    private readonly ILeagueSeasonRepository _leagueSeasonRepo;
    private readonly ICountryProviderRepository _countryProviderRepo;
    private readonly ILeagueProviderRepository _leagueProviderRepo;
    private readonly ISportRepository _sportRepo;
    private readonly ICountryNameMappingRepository _countryNameMappingRepo;
    private readonly ILogger<ImportService> _logger;

    public ImportService(
        IProviderCountryRepository providerCountryRepo,
        IProviderLeagueRepository providerLeagueRepo,
        IProviderSeasonRepository providerSeasonRepo,
        ISyncJobRepository syncJobRepo,
        IDataProviderRepository dataProviderRepo,
        ICountryRepository countryRepo,
        ILeagueRepository leagueRepo,
        ISeasonRepository seasonRepo,
        ILeagueSeasonRepository leagueSeasonRepo,
        ICountryProviderRepository countryProviderRepo,
        ILeagueProviderRepository leagueProviderRepo,
        ISportRepository sportRepo,
        ICountryNameMappingRepository countryNameMappingRepo,
        ILogger<ImportService> logger)
    {
        _providerCountryRepo = providerCountryRepo;
        _providerLeagueRepo = providerLeagueRepo;
        _providerSeasonRepo = providerSeasonRepo;
        _syncJobRepo = syncJobRepo;
        _dataProviderRepo = dataProviderRepo;
        _countryRepo = countryRepo;
        _leagueRepo = leagueRepo;
        _seasonRepo = seasonRepo;
        _leagueSeasonRepo = leagueSeasonRepo;
        _countryProviderRepo = countryProviderRepo;
        _leagueProviderRepo = leagueProviderRepo;
        _sportRepo = sportRepo;
        _countryNameMappingRepo = countryNameMappingRepo;
        _logger = logger;
    }

    public async Task<Guid> ImportCountriesFromCacheAsync(Guid providerId, List<Guid>? providerCountryIds = null)
    {
        _logger.LogInformation("Starting country import for provider {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // If providerCountryIds not provided or empty, use all cached countries for this provider
        List<Guid> countryIdsToImport = (providerCountryIds == null || !providerCountryIds.Any())
            ? (await _providerCountryRepo.GetByProviderIdAsync(providerId))
                .Select(pc => pc.Id)
                .ToList()
            : providerCountryIds;

        // Validate countryIdsToImport not empty
        if (!countryIdsToImport.Any())
        {
            throw new ArgumentException("No countries found in cache for this provider", nameof(providerId));
        }

        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending,
            CountryIds = countryIdsToImport,
            Priority = 1
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        await ImportCountriesFromCacheInternalAsync(syncJob.Id, countryIdsToImport);

        return syncJob.Id;
    }

    public async Task ImportCountriesFromCacheInternalAsync(Guid jobId, List<Guid> providerCountryIds)
    {
        // Load job
        var syncJob = await _syncJobRepo.GetByIdAsync(jobId);
        if (syncJob == null)
        {
            throw new ArgumentException($"Sync job {jobId} not found", nameof(jobId));
        }

        // Update status to Running
        syncJob.Status = SyncJobStatus.Running;
        syncJob.StartedAt = DateTime.UtcNow;
        await _syncJobRepo.UpdateAsync(syncJob);

        // Use TransactionScope to coordinate transactions across multiple DbContexts
        // This ensures atomic commit/rollback for both Configuration and DataImport schemas
        var transactionOptions = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TransactionManager.MaximumTimeout
        };

        try
        {
            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            // Get provider once for later use
            var provider = await _dataProviderRepo.GetByIdAsync(syncJob.ProviderId);

            // Wrap import loop in TransactionScope to ensure atomic commit across schemas
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var providerCountryId in providerCountryIds)
            {
                try
                {
                    // Get provider country from cache
                    var providerCountry = await _providerCountryRepo.GetByIdAsync(providerCountryId);
                    if (providerCountry == null)
                    {
                        _logger.LogWarning("ProviderCountry {Id} not found, skipping", providerCountryId);
                        skippedCount++;
                        errors.Add($"ProviderCountry {providerCountryId} not found");
                        continue;
                    }

                    // Skip if already imported - but verify the country actually exists
                    if (providerCountry.IsImported && providerCountry.CountryId.HasValue)
                    {
                        var existingCountry = await _countryRepo.GetByIdAsync(providerCountry.CountryId.Value);
                        if (existingCountry != null)
                        {
                            _logger.LogInformation("ProviderCountry {Id} ({Name}) already imported, skipping",
                                providerCountryId, providerCountry.ProviderName);
                            skippedCount++;
                            continue;
                        }
                        else
                        {
                            // Orphaned reference - reset and re-import
                            _logger.LogWarning("⚠ ProviderCountry {Id} has orphaned country reference {CountryId}, resetting and re-importing",
                                providerCountryId, providerCountry.CountryId.Value);
                            providerCountry.IsImported = false;
                            providerCountry.CountryId = null;
                            providerCountry.ImportedAt = null;
                        }
                    }

                    // Check if Country with same Code already exists (Code has unique constraint)
                    Country? country = null;
                    CountryNameMapping? countryMapping = null;

                    // STEP 1: Try manual country name mapping (highest priority)
                    if (provider != null)
                    {
                        countryMapping = await _countryNameMappingRepo.FindMappingAsync(
                            provider.Code.ToLowerInvariant(),
                            providerCountry.ProviderCode);

                        if (countryMapping != null)
                        {
                            country = await _countryRepo.GetByCodeAsync(countryMapping.BetExplorerCode);
                            if (country != null)
                            {
                                _logger.LogInformation("🗺️  Country found via manual mapping: {ProviderName} '{ProviderCode}' → '{BetExplorerCode}'",
                                    providerCountry.ProviderName, providerCountry.ProviderCode, countryMapping.BetExplorerCode);
                            }
                        }
                    }

                    // STEP 2: Try to find by IsoCode (for scrapers like BetExplorer)
                    if (country == null && !string.IsNullOrEmpty(providerCountry.IsoCode))
                    {
                        country = await _countryRepo.GetByCodeAsync(providerCountry.IsoCode);
                    }

                    // STEP 3: Try to find by ProviderCode (fallback for betting providers like Betano)
                    if (country == null)
                    {
                        country = await _countryRepo.GetByCodeAsync(providerCountry.ProviderCode);
                    }

                    if (country == null)
                    {
                        // CRITICAL RULE: Only Scraper providers (BetExplorer) can create new countries
                        // Betting providers (Betano, Fortuna) can ONLY create CountryProvider mappings
                        if (provider.Type != ProviderType.Scraper)
                        {
                            _logger.LogWarning("⊗ Skipping country - not found in configuration and provider is not Scraper: {Name} ({Code})",
                                providerCountry.ProviderName, providerCountry.ProviderCode);
                            skippedCount++;
                            continue;
                        }

                        // Create new Country (inactive by default)
                        // Countries are auto-activated during scan leagues when betting providers have leagues in them
                        country = new Country
                        {
                            Name = providerCountry.ProviderName,
                            Code = providerCountry.ProviderCode,
                            IsoCode = providerCountry.IsoCode ?? providerCountry.ProviderCode,
                            FlagEmoji = providerCountry.FlagEmoji ?? "",
                            IsActive = false
                        };
                        country = await _countryRepo.CreateAsync(country);
                        _logger.LogInformation("✓ Imported country → configuration.countries: {Name} ({Code}) {Flag}",
                            country.Name, country.Code, country.FlagEmoji);
                    }
                    else
                    {
                        _logger.LogInformation("⊘ Country already exists: {Name} ({Code}), reusing",
                            country.Name, country.Code);
                    }

                    // Create or update CountryProvider mapping
                    var existingMapping = await _countryProviderRepo.GetByCountryAndProviderAsync(
                        country.Id, syncJob.ProviderId);

                    if (existingMapping == null)
                    {
                        var countryProvider = new CountryProvider
                        {
                            CountryId = country.Id,
                            ProviderId = syncJob.ProviderId,
                            ProviderCode = providerCountry.ProviderCode,
                            ProviderName = providerCountry.ProviderName,
                            IsActive = true,
                            Metadata = providerCountry.RawData
                        };
                        await _countryProviderRepo.AddAsync(countryProvider);
                        _logger.LogInformation("✓ Created CountryProvider mapping: {Country} ↔ Provider {ProviderId}",
                            country.Name, syncJob.ProviderId);
                    }
                    else
                    {
                        // Update existing mapping
                        existingMapping.ProviderCode = providerCountry.ProviderCode;
                        existingMapping.ProviderName = providerCountry.ProviderName;
                        existingMapping.Metadata = providerCountry.RawData;
                        existingMapping.IsActive = true;
                        await _countryProviderRepo.UpdateAsync(existingMapping);
                        _logger.LogInformation("↻ Updated CountryProvider mapping: {Country}",
                            country.Name);
                    }

                    // Track usage of CountryNameMapping if it was used
                    if (countryMapping != null)
                    {
                        await _countryNameMappingRepo.TrackUsageAsync(countryMapping.Id, providerCountry.Id);
                    }

                    // Mark ProviderCountry as imported
                    providerCountry.IsImported = true;
                    providerCountry.CountryId = country.Id;
                    providerCountry.ImportedAt = DateTime.UtcNow;
                    await _providerCountryRepo.UpdateAsync(providerCountry);

                    importedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import ProviderCountry {Id}", providerCountryId);
                    errorCount++;
                    errors.Add($"ProviderCountry {providerCountryId}: {ex.Message}");
                }
            }

                // Complete the transaction - all changes committed atomically
                scope.Complete();
            }

            // Update sync job as completed - use PartiallyCompleted if there were some errors
            syncJob.Status = errorCount > 0 ? SyncJobStatus.PartiallyCompleted : SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                total = providerCountryIds.Count,
                imported = importedCount,
                skipped = skippedCount,
                errors = errorCount
            });
            if (errors.Any())
            {
                syncJob.ErrorMessage = string.Join("; ", errors);
            }
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("Country import completed. Imported: {Imported}, Skipped: {Skipped}, Errors: {Errors}",
                importedCount, skippedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country import failed");
            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;

            // Resilient status update with retry logic to prevent jobs stuck in Running status
            bool statusUpdated = false;
            for (int attempt = 1; attempt <= 3 && !statusUpdated; attempt++)
            {
                try
                {
                    await _syncJobRepo.UpdateAsync(syncJob);
                    statusUpdated = true;
                    _logger.LogInformation("Successfully updated job {JobId} to Failed status", syncJob.Id);
                }
                catch (Exception updateEx)
                {
                    if (attempt == 3)
                    {
                        _logger.LogCritical(updateEx,
                            "CRITICAL: Failed to update job {JobId} to Failed status after 3 attempts. " +
                            "Job will remain in Running status. Original error: {OriginalError}",
                            syncJob.Id, ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning(updateEx,
                            "Attempt {Attempt}/3 to update job status failed, retrying...", attempt);
                        await Task.Delay(500 * attempt); // Exponential backoff
                    }
                }
            }

            throw;
        }
    }

    public async Task<Guid> ImportLeaguesFromCacheAsync(Guid providerId, List<Guid>? providerLeagueIds = null)
    {
        _logger.LogInformation("Starting league import for provider {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // If providerLeagueIds not provided or empty, use all cached leagues for this provider
        List<Guid> leagueIdsToImport = (providerLeagueIds == null || !providerLeagueIds.Any())
            ? (await _providerLeagueRepo.GetByProviderIdAsync(providerId))
                .Select(pl => pl.Id)
                .ToList()
            : providerLeagueIds;

        // Validate leagueIdsToImport not empty
        if (!leagueIdsToImport.Any())
        {
            throw new ArgumentException("No leagues found in cache for this provider", nameof(providerId));
        }

        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Pending,
            LeagueIds = leagueIdsToImport,
            Priority = 2
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        await ImportLeaguesFromCacheInternalAsync(syncJob.Id, leagueIdsToImport);

        return syncJob.Id;
    }

    public async Task ImportLeaguesFromCacheInternalAsync(Guid jobId, List<Guid> providerLeagueIds)
    {
        // Load job
        var syncJob = await _syncJobRepo.GetByIdAsync(jobId);
        if (syncJob == null)
        {
            throw new ArgumentException($"Sync job {jobId} not found", nameof(jobId));
        }

        // Update status to Running
        syncJob.Status = SyncJobStatus.Running;
        syncJob.StartedAt = DateTime.UtcNow;
        await _syncJobRepo.UpdateAsync(syncJob);

        // Use TransactionScope to coordinate transactions across multiple DbContexts
        // This ensures atomic commit/rollback for both Configuration and DataImport schemas
        var transactionOptions = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TransactionManager.MaximumTimeout
        };

        try
        {
            // Get default sport (Football)
            var sports = await _sportRepo.GetAllAsync();
            var defaultSport = sports.FirstOrDefault(s => s.Name == "Football") ?? sports.First();

            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            // Get provider once for later use
            var provider = await _dataProviderRepo.GetByIdAsync(syncJob.ProviderId);

            // Wrap import loop in TransactionScope to ensure atomic commit across schemas
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var providerLeagueId in providerLeagueIds)
            {
                try
                {
                    // Get provider league from cache
                    var providerLeague = await _providerLeagueRepo.GetByIdAsync(providerLeagueId);
                    if (providerLeague == null)
                    {
                        _logger.LogWarning("ProviderLeague {Id} not found, skipping", providerLeagueId);
                        skippedCount++;
                        errors.Add($"ProviderLeague {providerLeagueId} not found");
                        continue;
                    }

                    // Skip if already imported - but verify the league actually exists
                    if (providerLeague.IsImported && providerLeague.LeagueId.HasValue)
                    {
                        var existingLeague = await _leagueRepo.GetByIdAsync(providerLeague.LeagueId.Value);
                        if (existingLeague != null)
                        {
                            _logger.LogInformation("ProviderLeague {Id} ({Name}) already imported, skipping",
                                providerLeagueId, providerLeague.ProviderName);
                            skippedCount++;
                            continue;
                        }
                        else
                        {
                            // Orphaned reference - reset and re-import
                            _logger.LogWarning("⚠ ProviderLeague {Id} has orphaned league reference {LeagueId}, resetting and re-importing",
                                providerLeagueId, providerLeague.LeagueId.Value);
                            providerLeague.IsImported = false;
                            providerLeague.LeagueId = null;
                            providerLeague.ImportedAt = null;
                        }
                    }

                    // For betting providers (ProviderCountryId is null), get country from league's mapped data
                    Configuration.Entities.Country? country = null;
                    if (providerLeague.ProviderCountryId.HasValue)
                    {
                        // Scraper providers (BetExplorer): get country via ProviderCountry
                        var providerCountry = await _providerCountryRepo.GetByIdAsync(providerLeague.ProviderCountryId.Value);
                        if (providerCountry == null)
                        {
                            _logger.LogWarning("ProviderCountry {Id} not found for ProviderLeague {LeagueId}, skipping",
                                providerLeague.ProviderCountryId, providerLeagueId);
                            skippedCount++;
                            errors.Add($"ProviderCountry {providerLeague.ProviderCountryId} not found");
                            continue;
                        }

                        if (providerCountry.CountryId.HasValue)
                        {
                            country = await _countryRepo.GetByIdAsync(providerCountry.CountryId.Value);
                        }
                    }
                    else
                    {
                        // Betting providers: get country via CountryCode
                        if (!string.IsNullOrEmpty(providerLeague.CountryCode))
                        {
                            var countries = await _countryRepo.GetAllAsync();
                            country = countries.FirstOrDefault(c => c.Code.Equals(providerLeague.CountryCode, StringComparison.OrdinalIgnoreCase));

                            if (country == null)
                            {
                                _logger.LogWarning("Country with code {CountryCode} not found for ProviderLeague {LeagueId}, skipping",
                                    providerLeague.CountryCode, providerLeagueId);
                                skippedCount++;
                                errors.Add($"Country {providerLeague.CountryCode} not found");
                                continue;
                            }
                        }
                    }

                    // Skip unmapped leagues (only import successfully mapped leagues)
                    if (providerLeague.MappingStatus == MappingStatus.Unmapped)
                    {
                        _logger.LogDebug("Skipping unmapped ProviderLeague {LeagueId} ({Name}) - no BetExplorer mapping",
                            providerLeagueId, providerLeague.ProviderName);
                        skippedCount++;
                        continue;
                    }

                    // Validate country is available
                    if (country == null)
                    {
                        _logger.LogWarning("Skipping ProviderLeague {LeagueId} ({Name}) - country not found",
                            providerLeagueId, providerLeague.ProviderName);
                        skippedCount++;
                        errors.Add($"Country not found for league {providerLeague.ProviderName}");
                        continue;
                    }

                    // Check if League with same ProviderSlug already exists (via LeagueProvider mapping)
                    var existingLeagueProvider = await _leagueProviderRepo.GetByProviderAndSlugAsync(
                        syncJob.ProviderId, providerLeague.ProviderSlug);

                    League? league = null;
                    if (existingLeagueProvider != null)
                    {
                        // League already exists, reuse it
                        league = await _leagueRepo.GetByIdAsync(existingLeagueProvider.LeagueId);
                        _logger.LogInformation("League {LeagueId} ({Name}) already exists for provider slug {Slug}, reusing",
                            league?.Id, league?.Name, providerLeague.ProviderSlug);
                    }

                    if (league == null)
                    {
                        // Create new League
                        // All providers (including betting providers) can create leagues if they are mapped to BetExplorer
                        league = new League
                        {
                            SportId = defaultSport.Id,
                            CountryId = country.Id,
                            Name = providerLeague.ProviderName,
                            DisplayName = providerLeague.DisplayName ?? providerLeague.ProviderName,
                            BetExplorerSlug = providerLeague.ProviderSlug, // Still used for backward compatibility
                            IsSyncEnabled = false,
                            IsBettable = providerLeague.IsBettable,
                            IsActive = false,
                            Priority = providerLeague.Priority,
                            Notes = $"Imported from provider {provider?.Name}"
                        };
                        league = await _leagueRepo.CreateAsync(league);
                        _logger.LogInformation("✓ Created new League {LeagueId} ({Name}) from provider {Provider}",
                            league.Id, league.Name, provider?.Name ?? "Unknown");
                    }
                    else
                    {
                        // Update existing league
                        league.DisplayName = providerLeague.DisplayName ?? providerLeague.ProviderName;
                        league.Priority = providerLeague.Priority;
                        league.IsBettable = providerLeague.IsBettable;
                        await _leagueRepo.UpdateAsync(league);
                        _logger.LogInformation("Updated League {LeagueId} ({Name})",
                            league.Id, league.Name);
                    }

                    // Create or update LeagueProvider mapping
                    if (existingLeagueProvider == null)
                    {
                        var leagueProvider = new LeagueProvider
                        {
                            LeagueId = league.Id,
                            ProviderId = syncJob.ProviderId,
                            ProviderSlug = providerLeague.ProviderSlug,
                            ProviderName = providerLeague.ProviderName,
                            IsActive = true,
                            Metadata = providerLeague.RawData
                        };
                        await _leagueProviderRepo.AddAsync(leagueProvider);
                        _logger.LogInformation("Created LeagueProvider mapping for League {LeagueId} and Provider {ProviderId}",
                            league.Id, syncJob.ProviderId);
                    }
                    else
                    {
                        // Update existing mapping
                        existingLeagueProvider.ProviderSlug = providerLeague.ProviderSlug;
                        existingLeagueProvider.ProviderName = providerLeague.ProviderName;
                        existingLeagueProvider.Metadata = providerLeague.RawData;
                        existingLeagueProvider.IsActive = true;
                        await _leagueProviderRepo.UpdateAsync(existingLeagueProvider);
                        _logger.LogInformation("Updated LeagueProvider mapping for League {LeagueId}",
                            league.Id);
                    }

                    // Mark ProviderLeague as imported
                    providerLeague.IsImported = true;
                    providerLeague.LeagueId = league.Id;
                    providerLeague.ImportedAt = DateTime.UtcNow;
                    await _providerLeagueRepo.UpdateAsync(providerLeague);

                    importedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import ProviderLeague {Id}", providerLeagueId);
                    errorCount++;
                    errors.Add($"ProviderLeague {providerLeagueId}: {ex.Message}");
                }
            }

                // Complete the transaction - all changes committed atomically
                scope.Complete();
            }

            // Update sync job - use PartiallyCompleted if there were some errors
            syncJob.Status = errorCount > 0 ? SyncJobStatus.PartiallyCompleted : SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                total = providerLeagueIds.Count,
                imported = importedCount,
                skipped = skippedCount,
                errors = errorCount
            });
            if (errors.Any())
            {
                syncJob.ErrorMessage = string.Join("; ", errors);
            }
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("League import completed. Imported: {Imported}, Skipped: {Skipped}, Errors: {Errors}",
                importedCount, skippedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "League import failed for provider {ProviderId}", syncJob.ProviderId);
            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;

            // Resilient status update with retry logic to prevent jobs stuck in Running status
            bool statusUpdated = false;
            for (int attempt = 1; attempt <= 3 && !statusUpdated; attempt++)
            {
                try
                {
                    await _syncJobRepo.UpdateAsync(syncJob);
                    statusUpdated = true;
                    _logger.LogInformation("Successfully updated job {JobId} to Failed status", syncJob.Id);
                }
                catch (Exception updateEx)
                {
                    if (attempt == 3)
                    {
                        _logger.LogCritical(updateEx,
                            "CRITICAL: Failed to update job {JobId} to Failed status after 3 attempts. " +
                            "Job will remain in Running status. Original error: {OriginalError}",
                            syncJob.Id, ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning(updateEx,
                            "Attempt {Attempt}/3 to update job status failed, retrying...", attempt);
                        await Task.Delay(500 * attempt); // Exponential backoff
                    }
                }
            }

            throw;
        }
    }

    public async Task<Guid> ImportSeasonsFromCacheAsync(Guid providerId, List<Guid>? providerSeasonIds = null)
    {
        _logger.LogInformation("Starting season import for provider {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // If providerSeasonIds not provided or empty, use all cached seasons for this provider
        List<Guid> seasonIdsToImport = (providerSeasonIds == null || !providerSeasonIds.Any())
            ? (await _providerSeasonRepo.GetByProviderIdAsync(providerId))
                .Select(ps => ps.Id)
                .ToList()
            : providerSeasonIds;

        // Validate seasonIdsToImport not empty
        if (!seasonIdsToImport.Any())
        {
            throw new ArgumentException("No seasons found in cache for this provider", nameof(providerId));
        }

        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Seasons,
            Status = SyncJobStatus.Pending,
            SeasonIds = seasonIdsToImport,
            Priority = 3
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        await ImportSeasonsFromCacheInternalAsync(syncJob.Id, seasonIdsToImport);

        return syncJob.Id;
    }

    public async Task ImportSeasonsFromCacheInternalAsync(Guid jobId, List<Guid> providerSeasonIds)
    {
        // Load job
        var syncJob = await _syncJobRepo.GetByIdAsync(jobId);
        if (syncJob == null)
        {
            throw new ArgumentException($"Sync job {jobId} not found", nameof(jobId));
        }

        // Update status to Running
        syncJob.Status = SyncJobStatus.Running;
        syncJob.StartedAt = DateTime.UtcNow;
        await _syncJobRepo.UpdateAsync(syncJob);

        // Use TransactionScope to coordinate transactions across multiple DbContexts
        // This ensures atomic commit/rollback for both Configuration and DataImport schemas
        var transactionOptions = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TransactionManager.MaximumTimeout
        };

        try
        {
            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            // Wrap import loop in TransactionScope to ensure atomic commit across schemas
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var providerSeasonId in providerSeasonIds)
            {
                try
                {
                    // Get provider season from cache
                    var providerSeason = await _providerSeasonRepo.GetByIdAsync(providerSeasonId);
                    if (providerSeason == null)
                    {
                        _logger.LogWarning("ProviderSeason {Id} not found, skipping", providerSeasonId);
                        skippedCount++;
                        errors.Add($"ProviderSeason {providerSeasonId} not found");
                        continue;
                    }

                    // Skip if already imported - but verify the season actually exists
                    if (providerSeason.IsImported && providerSeason.SeasonId.HasValue)
                    {
                        var existingSeason = await _seasonRepo.GetByIdAsync(providerSeason.SeasonId.Value);
                        if (existingSeason != null)
                        {
                            _logger.LogInformation("ProviderSeason {Id} ({Name}) already imported, skipping",
                                providerSeasonId, providerSeason.SeasonName);
                            skippedCount++;
                            continue;
                        }
                        else
                        {
                            // Orphaned reference - reset and re-import
                            _logger.LogWarning("⚠ ProviderSeason {Id} has orphaned season reference {SeasonId}, resetting and re-importing",
                                providerSeasonId, providerSeason.SeasonId.Value);
                            providerSeason.IsImported = false;
                            providerSeason.SeasonId = null;
                            providerSeason.ImportedAt = null;
                        }
                    }

                    // Get ProviderLeague to find corresponding League
                    var providerLeague = await _providerLeagueRepo.GetByIdAsync(providerSeason.ProviderLeagueId);
                    if (providerLeague == null)
                    {
                        _logger.LogWarning("ProviderLeague {Id} not found for ProviderSeason {SeasonId}, skipping",
                            providerSeason.ProviderLeagueId, providerSeasonId);
                        skippedCount++;
                        errors.Add($"ProviderLeague {providerSeason.ProviderLeagueId} not found");
                        continue;
                    }

                    // Ensure ProviderLeague is imported (has LeagueId)
                    if (!providerLeague.LeagueId.HasValue)
                    {
                        _logger.LogWarning("ProviderLeague {Id} ({Name}) is not imported yet, skipping season {SeasonId}",
                            providerLeague.Id, providerLeague.ProviderName, providerSeasonId);
                        skippedCount++;
                        errors.Add($"ProviderLeague {providerLeague.Id} not imported yet");
                        continue;
                    }

                    var league = await _leagueRepo.GetByIdAsync(providerLeague.LeagueId.Value);
                    if (league == null)
                    {
                        _logger.LogWarning("League {LeagueId} not found, skipping season {SeasonId}",
                            providerLeague.LeagueId.Value, providerSeasonId);
                        skippedCount++;
                        errors.Add($"League {providerLeague.LeagueId.Value} not found");
                        continue;
                    }

                    // Get or create Season
                    var season = await _seasonRepo.GetByNameAsync(providerSeason.SeasonName);
                    if (season == null)
                    {
                        season = new Season
                        {
                            Name = providerSeason.SeasonName,
                            StartYear = providerSeason.StartYear,
                            EndYear = providerSeason.EndYear
                        };
                        season = await _seasonRepo.CreateAsync(season);
                        _logger.LogInformation("Created new Season {SeasonId} ({Name})",
                            season.Id, season.Name);
                    }
                    else
                    {
                        _logger.LogInformation("Season {SeasonId} ({Name}) already exists, reusing",
                            season.Id, season.Name);
                    }

                    // Create or update LeagueSeason mapping
                    var existingLeagueSeason = await _leagueSeasonRepo.GetByLeagueAndSeasonAsync(
                        league.Id, season.Id);

                    if (existingLeagueSeason == null)
                    {
                        var leagueSeason = new LeagueSeason
                        {
                            LeagueId = league.Id,
                            SeasonId = season.Id,
                            IsAvailableOnBetExplorer = true,
                            HasData = false,
                            HasOdds = false,
                            RoundsCount = 0,
                            MatchesCount = 0,
                            SyncEnabled = false,
                            IsCurrent = providerSeason.IsCurrentSeason,
                            SyncMode = providerSeason.IsCurrentSeason ? SyncMode.Current : SyncMode.Historical
                        };
                        await _leagueSeasonRepo.CreateAsync(leagueSeason);
                        _logger.LogInformation("Created LeagueSeason mapping for League {LeagueId} and Season {SeasonId}",
                            league.Id, season.Id);
                    }
                    else
                    {
                        // Update existing mapping
                        existingLeagueSeason.IsAvailableOnBetExplorer = true;
                        existingLeagueSeason.IsCurrent = providerSeason.IsCurrentSeason;
                        existingLeagueSeason.SyncMode = providerSeason.IsCurrentSeason ? SyncMode.Current : SyncMode.Historical;
                        await _leagueSeasonRepo.UpdateAsync(existingLeagueSeason);
                        _logger.LogInformation("Updated LeagueSeason mapping for League {LeagueId} and Season {SeasonId}",
                            league.Id, season.Id);
                    }

                    // Mark ProviderSeason as imported
                    providerSeason.IsImported = true;
                    providerSeason.SeasonId = season.Id;
                    providerSeason.ImportedAt = DateTime.UtcNow;
                    await _providerSeasonRepo.UpdateAsync(providerSeason);

                    importedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import ProviderSeason {Id}", providerSeasonId);
                    errorCount++;
                    errors.Add($"ProviderSeason {providerSeasonId}: {ex.Message}");
                }
            }

                // Complete the transaction - all changes committed atomically
                scope.Complete();
            }

            // Update sync job - use PartiallyCompleted if there were some errors
            syncJob.Status = errorCount > 0 ? SyncJobStatus.PartiallyCompleted : SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                total = providerSeasonIds.Count,
                imported = importedCount,
                skipped = skippedCount,
                errors = errorCount
            });
            if (errors.Any())
            {
                syncJob.ErrorMessage = string.Join("; ", errors);
            }
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("Season import completed. Imported: {Imported}, Skipped: {Skipped}, Errors: {Errors}",
                importedCount, skippedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Season import failed for provider {ProviderId}", syncJob.ProviderId);
            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;

            // Resilient status update with retry logic to prevent jobs stuck in Running status
            bool statusUpdated = false;
            for (int attempt = 1; attempt <= 3 && !statusUpdated; attempt++)
            {
                try
                {
                    await _syncJobRepo.UpdateAsync(syncJob);
                    statusUpdated = true;
                    _logger.LogInformation("Successfully updated job {JobId} to Failed status", syncJob.Id);
                }
                catch (Exception updateEx)
                {
                    if (attempt == 3)
                    {
                        _logger.LogCritical(updateEx,
                            "CRITICAL: Failed to update job {JobId} to Failed status after 3 attempts. " +
                            "Job will remain in Running status. Original error: {OriginalError}",
                            syncJob.Id, ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning(updateEx,
                            "Attempt {Attempt}/3 to update job status failed, retrying...", attempt);
                        await Task.Delay(500 * attempt); // Exponential backoff
                    }
                }
            }

            throw;
        }
    }

    public async Task<ImportStats> GetImportStatsAsync(Guid providerId)
    {
        var allProviderCountries = await _providerCountryRepo.GetByProviderIdAsync(providerId);
        var allProviderLeagues = await _providerLeagueRepo.GetByProviderIdAsync(providerId);
        var allProviderSeasons = await _providerSeasonRepo.GetByProviderIdAsync(providerId);

        return new ImportStats(
            CachedCountries: allProviderCountries.Count,
            ImportedCountries: allProviderCountries.Count(pc => pc.IsImported),
            CachedLeagues: allProviderLeagues.Count,
            ImportedLeagues: allProviderLeagues.Count(pl => pl.IsImported),
            CachedSeasons: allProviderSeasons.Count,
            ImportedSeasons: allProviderSeasons.Count(ps => ps.IsImported)
        );
    }
}
