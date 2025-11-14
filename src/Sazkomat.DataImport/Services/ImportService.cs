using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using System.Text.Json;

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

        // If providerCountryIds not provided, use all cached countries for this provider
        List<Guid> countryIdsToImport = providerCountryIds ??
            (await _providerCountryRepo.GetByProviderIdAsync(providerId))
                .Select(pc => pc.Id)
                .ToList();

        // Validate providerCountryIds not empty
        if (!countryIdsToImport.Any())
        {
            throw new ArgumentException("No provider country IDs provided", nameof(providerCountryIds));
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

        try
        {
            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

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

                    // Skip if already imported
                    if (providerCountry.IsImported)
                    {
                        _logger.LogInformation("ProviderCountry {Id} ({Name}) already imported, skipping",
                            providerCountryId, providerCountry.ProviderName);
                        skippedCount++;
                        continue;
                    }

                    // Check if Country with same IsoCode already exists
                    Country? country = null;
                    if (!string.IsNullOrEmpty(providerCountry.IsoCode))
                    {
                        country = await _countryRepo.GetByCodeAsync(providerCountry.IsoCode);
                    }

                    if (country == null)
                    {
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
                        _logger.LogInformation("Created new Country {CountryId} ({Name}) (inactive - will be activated when betting providers scan leagues)",
                            country.Id, country.Name);
                    }
                    else
                    {
                        _logger.LogInformation("Country {CountryId} ({Name}) already exists, reusing",
                            country.Id, country.Name);
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
                        _logger.LogInformation("Created CountryProvider mapping for Country {CountryId} and Provider {ProviderId}",
                            country.Id, syncJob.ProviderId);
                    }
                    else
                    {
                        // Update existing mapping
                        existingMapping.ProviderCode = providerCountry.ProviderCode;
                        existingMapping.ProviderName = providerCountry.ProviderName;
                        existingMapping.Metadata = providerCountry.RawData;
                        existingMapping.IsActive = true;
                        await _countryProviderRepo.UpdateAsync(existingMapping);
                        _logger.LogInformation("Updated CountryProvider mapping for Country {CountryId}",
                            country.Id);
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

        // If providerLeagueIds not provided, use all cached leagues for this provider
        List<Guid> leagueIdsToImport = providerLeagueIds ??
            (await _providerLeagueRepo.GetByProviderIdAsync(providerId))
                .Select(pl => pl.Id)
                .ToList();

        // Validate providerLeagueIds not empty
        if (!leagueIdsToImport.Any())
        {
            throw new ArgumentException("No provider league IDs provided", nameof(providerLeagueIds));
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

                    // Skip if already imported
                    if (providerLeague.IsImported)
                    {
                        _logger.LogInformation("ProviderLeague {Id} ({Name}) already imported, skipping",
                            providerLeagueId, providerLeague.ProviderName);
                        skippedCount++;
                        continue;
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
                        _logger.LogInformation("Created new League {LeagueId} ({Name})",
                            league.Id, league.Name);
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

        // If providerSeasonIds not provided, use all cached seasons for this provider
        List<Guid> seasonIdsToImport = providerSeasonIds ??
            (await _providerSeasonRepo.GetByProviderIdAsync(providerId))
                .Select(ps => ps.Id)
                .ToList();

        // Validate providerSeasonIds not empty
        if (!seasonIdsToImport.Any())
        {
            throw new ArgumentException("No provider season IDs provided", nameof(providerSeasonIds));
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

        try
        {
            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

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

                    // Skip if already imported
                    if (providerSeason.IsImported)
                    {
                        _logger.LogInformation("ProviderSeason {Id} ({Name}) already imported, skipping",
                            providerSeasonId, providerSeason.SeasonName);
                        skippedCount++;
                        continue;
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
