using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using System.Text.Json;

namespace Sazkomat.DataImport.Services;

public class ScanService : IScanService
{
    private readonly IProviderCountryRepository _providerCountryRepo;
    private readonly IProviderLeagueRepository _providerLeagueRepo;
    private readonly IProviderSeasonRepository _providerSeasonRepo;
    private readonly ISyncJobRepository _syncJobRepo;
    private readonly IDataProviderRepository _dataProviderRepo;
    private readonly ISportRepository _sportRepo;
    private readonly ICountryRepository _countryRepo;
    private readonly ICountryProviderRepository _countryProviderRepo;
    private readonly ILeagueRepository _leagueRepo;
    private readonly ILeagueProviderRepository _leagueProviderRepo;
    private readonly ICountryNameMappingRepository _countryNameMappingRepo;
    private readonly IUnmatchedLeagueRepository _unmatchedLeagueRepo;
    private readonly IUnmatchedCountryRepository _unmatchedCountryRepo;
    private readonly IEnumerable<ICountryScraper> _countryScrapers;
    private readonly IEnumerable<ILeagueMetadataScraper> _leagueScrapers;
    private readonly IEnumerable<ISeasonScraper> _seasonScrapers;
    private readonly IBetExplorerEnrichmentService _enrichmentService;
    private readonly IBetanoFullDataProvider _betanoFullDataProvider;
    private readonly ILogger<ScanService> _logger;

    public ScanService(
        IProviderCountryRepository providerCountryRepo,
        IProviderLeagueRepository providerLeagueRepo,
        IProviderSeasonRepository providerSeasonRepo,
        ISyncJobRepository syncJobRepo,
        IDataProviderRepository dataProviderRepo,
        ISportRepository sportRepo,
        ICountryRepository countryRepo,
        ICountryProviderRepository countryProviderRepo,
        ILeagueRepository leagueRepo,
        ILeagueProviderRepository leagueProviderRepo,
        ICountryNameMappingRepository countryNameMappingRepo,
        IUnmatchedLeagueRepository unmatchedLeagueRepo,
        IUnmatchedCountryRepository unmatchedCountryRepo,
        IEnumerable<ICountryScraper> countryScrapers,
        IEnumerable<ILeagueMetadataScraper> leagueScrapers,
        IEnumerable<ISeasonScraper> seasonScrapers,
        IBetExplorerEnrichmentService enrichmentService,
        IBetanoFullDataProvider betanoFullDataProvider,
        ILogger<ScanService> logger)
    {
        _providerCountryRepo = providerCountryRepo;
        _providerLeagueRepo = providerLeagueRepo;
        _providerSeasonRepo = providerSeasonRepo;
        _syncJobRepo = syncJobRepo;
        _dataProviderRepo = dataProviderRepo;
        _sportRepo = sportRepo;
        _countryRepo = countryRepo;
        _countryProviderRepo = countryProviderRepo;
        _leagueRepo = leagueRepo;
        _leagueProviderRepo = leagueProviderRepo;
        _countryNameMappingRepo = countryNameMappingRepo;
        _unmatchedLeagueRepo = unmatchedLeagueRepo;
        _unmatchedCountryRepo = unmatchedCountryRepo;
        _countryScrapers = countryScrapers;
        _leagueScrapers = leagueScrapers;
        _seasonScrapers = seasonScrapers;
        _enrichmentService = enrichmentService;
        _betanoFullDataProvider = betanoFullDataProvider;
        _logger = logger;
    }

    public async Task<Guid> CreateScanJobAsync(Guid providerId, SyncEntityType entityType, List<Guid>? countryIds = null, List<Guid>? leagueIds = null)
    {
        _logger.LogInformation("Creating scan job for provider {ProviderId}, entity type {EntityType}", providerId, entityType);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // Create sync job (Pending status - will be executed by Hangfire)
        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Scan,
            EntityType = entityType,
            Status = SyncJobStatus.Pending,
            CountryIds = countryIds ?? new List<Guid>(),
            LeagueIds = leagueIds ?? new List<Guid>(),
            Priority = entityType switch
            {
                SyncEntityType.Countries => 1,
                SyncEntityType.CountriesAndLeagues => 1, // Same priority as countries (combined scan)
                SyncEntityType.Leagues => 2,
                SyncEntityType.Seasons => 3,
                _ => 5
            }
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        _logger.LogInformation("Created scan job {JobId} for provider {ProviderId}, entity type {EntityType}",
            syncJob.Id, providerId, entityType);

        return syncJob.Id;
    }

    public async Task<Guid> ScanCountriesAsync(Guid providerId)
    {
        _logger.LogInformation("Starting country scan for provider {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // Create sync job
        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending,
            Priority = 1
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        // Delegate to internal implementation
        await ScanCountriesInternalAsync(providerId, syncJob.Id);

        return syncJob.Id;
    }

    public async Task ScanCountriesInternalAsync(Guid providerId, Guid jobId)
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
            // Validate provider exists
            var provider = await _dataProviderRepo.GetByIdAsync(providerId);
            if (provider == null)
            {
                throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
            }

            // Get default sport (Football) for country scraping
            var sports = await _sportRepo.GetAllAsync();
            var defaultSport = sports.FirstOrDefault(s => s.Name == "Football") ?? sports.First();

            // Load provider configuration (if available)
            Configuration.DTOs.ProviderConfigurationDto? config = null;
            if (!string.IsNullOrEmpty(provider.Configuration))
            {
                try
                {
                    config = JsonSerializer.Deserialize<Configuration.DTOs.ProviderConfigurationDto>(provider.Configuration);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize provider configuration for {ProviderId}, continuing without filters", providerId);
                }
            }

            // Select the appropriate scraper for this provider
            var countryScraper = _countryScrapers.FirstOrDefault(s => s.CanHandle(provider));
            if (countryScraper == null)
            {
                var errorMessage = $"No country scraper available for provider {provider.Name} ({provider.Code})";
                _logger.LogError(errorMessage);
                syncJob.Status = SyncJobStatus.Failed;
                syncJob.ErrorMessage = errorMessage;
                syncJob.CompletedAt = DateTime.UtcNow;
                await _syncJobRepo.UpdateAsync(syncJob);
                throw new InvalidOperationException(errorMessage);
            }

            // Scrape countries from provider (with optional exclusion filters)
            var scrapedCountries = await countryScraper.ScrapeCountriesAsync(
                defaultSport,
                config?.ExcludedCountryIds);
            _logger.LogInformation("Scraped {Count} countries from provider {ProviderId}",
                scrapedCountries.Count, providerId);

            int newCount = 0;
            int updatedCount = 0;

            foreach (var country in scrapedCountries)
            {
                // For BETTING PROVIDERS: Check if there's an inactive mapping (skip non-countries like Copa Libertadores)
                if (provider.Type == Configuration.Entities.ProviderType.BettingProvider)
                {
                    var providerCodeLower = provider.Code.ToLowerInvariant();
                    var existingInactiveMapping = await _countryNameMappingRepo.FindAnyMappingAsync(
                        providerCodeLower,
                        country.Code);

                    if (existingInactiveMapping != null && !existingInactiveMapping.IsActive)
                    {
                        // Also delete from cache if it exists
                        var existingCached = await _providerCountryRepo.GetByProviderNameAsync(providerId, country.Name);
                        if (existingCached != null)
                        {
                            await _providerCountryRepo.DeleteAsync(existingCached.Id);
                            _logger.LogInformation("🗑️ Removed non-country '{Name}' ({Code}) from cache - has inactive mapping",
                                country.Name, country.Code);
                        }
                        continue; // Skip entirely - don't add to cache
                    }
                }

                // For BETTING PROVIDERS: Try to match country to BetExplorer catalog
                // Only create ProviderCountry if matched, otherwise create CountryNameMapping for manual review
                if (provider.Type == Configuration.Entities.ProviderType.BettingProvider)
                {
                    Configuration.Entities.Country? configCountry = null;

                    // STEP 1: Try manual country name mapping (highest priority)
                    var countryMapping = await _countryNameMappingRepo.FindMappingAsync(
                        provider.Code.ToLowerInvariant(),
                        country.Code);

                    if (countryMapping != null)
                    {
                        // Skip inactive mappings (e.g., Copa Libertadores - not a real country)
                        if (!countryMapping.IsActive)
                        {
                            _logger.LogDebug("Skipping inactive mapping for {CountryName} ({CountryCode})",
                                country.Name, country.Code);
                            continue;
                        }

                        configCountry = await _countryRepo.GetByCodeAsync(countryMapping.BetExplorerCode);
                        if (configCountry != null)
                        {
                            _logger.LogInformation("🗺️  Country found via manual mapping: {ProviderName} '{ProviderCode}' → '{BetExplorerCode}'",
                                country.Name, country.Code, countryMapping.BetExplorerCode);
                        }
                    }

                    // STEP 2: Try to find by IsoCode (if available)
                    if (configCountry == null && !string.IsNullOrEmpty(country.IsoCode))
                    {
                        configCountry = await _countryRepo.GetByCodeAsync(country.IsoCode);
                        if (configCountry != null)
                        {
                            _logger.LogDebug("Country matched by IsoCode: {CountryName} ({IsoCode})",
                                configCountry.Name, country.IsoCode);
                        }
                    }

                    // STEP 3: Try to find by ProviderCode (fallback)
                    if (configCountry == null)
                    {
                        configCountry = await _countryRepo.GetByCodeAsync(country.Code);
                        if (configCountry != null)
                        {
                            _logger.LogDebug("Country matched by ProviderCode: {CountryName} ({Code})",
                                configCountry.Name, country.Code);
                        }
                    }

                    if (configCountry != null)
                    {
                        // MATCHED: Create ProviderCountry (scan cache) with link to config country
                        var existing = await _providerCountryRepo.GetByProviderNameAsync(providerId, country.Name);

                        if (existing == null)
                        {
                            var providerCountry = new ProviderCountry
                            {
                                ProviderId = providerId,
                                CountryId = configCountry.Id,
                                ProviderCode = country.Code,
                                ProviderName = country.Name,
                                IsoCode = country.IsoCode,
                                FlagEmoji = country.FlagEmoji,
                                ScrapedAt = DateTime.UtcNow,
                                RawData = JsonSerializer.Serialize(country),
                                IsImported = false
                            };
                            await _providerCountryRepo.CreateAsync(providerCountry);
                            _logger.LogInformation("✓ Added country to scan: {Name} ({Code}) → {ConfigCountry}",
                                country.Name, country.Code, configCountry.Name);
                            newCount++;
                        }
                        else
                        {
                            existing.CountryId = configCountry.Id;
                            existing.ProviderName = country.Name;
                            existing.IsoCode = country.IsoCode;
                            existing.FlagEmoji = country.FlagEmoji;
                            existing.ScrapedAt = DateTime.UtcNow;
                            existing.RawData = JsonSerializer.Serialize(country);
                            await _providerCountryRepo.UpdateAsync(existing);
                            _logger.LogInformation("↻ Updated country in scan: {Name} ({Code}) → {ConfigCountry}",
                                country.Name, country.Code, configCountry.Name);
                            updatedCount++;
                        }

                        // Also create/update CountryProvider mapping for league scanning
                        var existingMapping = await _countryProviderRepo.GetByCountryAndProviderAsync(
                            configCountry.Id, providerId);

                        if (existingMapping == null)
                        {
                            var countryProvider = new Configuration.Entities.CountryProvider
                            {
                                CountryId = configCountry.Id,
                                ProviderId = providerId,
                                ProviderCode = country.Code,
                                ProviderName = country.Name,
                                IsActive = true
                            };
                            await _countryProviderRepo.AddAsync(countryProvider);

                            // Auto-activate country when creating CountryProvider mapping for betting provider
                            if (!configCountry.IsActive)
                            {
                                configCountry.IsActive = true;
                                await _countryRepo.UpdateAsync(configCountry);
                                _logger.LogInformation("✓ Auto-activated country {CountryName} ({CountryCode}) due to betting provider mapping",
                                    configCountry.Name, configCountry.Code);
                            }

                            _logger.LogInformation("✓ Created CountryProvider mapping: {CountryName} ({CountryCode}) ↔ Provider {ProviderCode}",
                                configCountry.Name, configCountry.Code, provider.Code);
                        }
                        else
                        {
                            existingMapping.ProviderCode = country.Code;
                            existingMapping.ProviderName = country.Name;
                            existingMapping.IsActive = true;
                            await _countryProviderRepo.UpdateAsync(existingMapping);

                            _logger.LogInformation("↻ Updated CountryProvider mapping: {CountryName} ({CountryCode})",
                                configCountry.Name, configCountry.Code);
                        }
                    }
                    else
                    {
                        // NOT MATCHED: Create UnmatchedCountry for manual resolution workflow
                        var existingUnmatched = await _unmatchedCountryRepo.FindExistingAsync(
                            providerId,
                            country.Name);

                        if (existingUnmatched == null)
                        {
                            var unmatchedCountry = new UnmatchedCountry
                            {
                                ProviderId = providerId,
                                ProviderCountryId = country.Code,
                                ProviderCountryName = country.Name,
                                ProviderSlug = country.Code,
                                ScrapedAt = DateTime.UtcNow,
                                IsResolved = false
                            };

                            await _unmatchedCountryRepo.CreateAsync(unmatchedCountry);

                            _logger.LogInformation("📝 Created UnmatchedCountry for manual review: '{ProviderName}' ({ProviderCode})",
                                country.Name, country.Code);
                        }
                        else
                        {
                            // Update ScrapedAt timestamp
                            existingUnmatched.ScrapedAt = DateTime.UtcNow;
                            await _unmatchedCountryRepo.UpdateAsync(existingUnmatched);

                            _logger.LogDebug("UnmatchedCountry already exists for {CountryName} ({CountryCode}), IsResolved={IsResolved}",
                                country.Name, country.Code, existingUnmatched.IsResolved);
                        }

                        // Also create CountryNameMapping for backward compatibility
                        var existingAnyMapping = await _countryNameMappingRepo.FindAnyMappingAsync(
                            provider.Code.ToLowerInvariant(),
                            country.Code);

                        if (existingAnyMapping == null)
                        {
                            var newMapping = new CountryNameMapping
                            {
                                ProviderCode = provider.Code.ToLowerInvariant(),
                                ProviderCountryName = country.Code,
                                BetExplorerCode = "", // Empty - not mapped yet
                                IsActive = false,     // Inactive - requires manual review
                                Priority = 100,       // Low priority
                                Notes = $"Auto-created: '{country.Name}' from {provider.Name} scan. Requires manual mapping or deactivation."
                            };

                            await _countryNameMappingRepo.CreateAsync(newMapping);
                        }
                    }
                }
                else
                {
                    // For NON-BETTING PROVIDERS (BetExplorer): Always create ProviderCountry
                    var existing = await _providerCountryRepo.GetByProviderNameAsync(providerId, country.Name);

                    if (existing == null)
                    {
                        var providerCountry = new ProviderCountry
                        {
                            ProviderId = providerId,
                            ProviderCode = country.Code,
                            ProviderName = country.Name,
                            IsoCode = country.IsoCode,
                            FlagEmoji = country.FlagEmoji,
                            ScrapedAt = DateTime.UtcNow,
                            RawData = JsonSerializer.Serialize(country),
                            IsImported = false
                        };
                        await _providerCountryRepo.CreateAsync(providerCountry);
                        _logger.LogInformation("✓ Added country to cache: {Name} ({Code}) {Flag}",
                            country.Name, country.Code, country.FlagEmoji ?? "");
                        newCount++;
                    }
                    else
                    {
                        existing.ProviderName = country.Name;
                        existing.IsoCode = country.IsoCode;
                        existing.FlagEmoji = country.FlagEmoji;
                        existing.ScrapedAt = DateTime.UtcNow;
                        existing.RawData = JsonSerializer.Serialize(country);
                        await _providerCountryRepo.UpdateAsync(existing);
                        _logger.LogInformation("↻ Updated country in cache: {Name} ({Code})",
                            country.Name, country.Code);
                        updatedCount++;
                    }
                }
            }

            // For BETTING PROVIDERS: Apply any active manual mappings at the end
            // This ensures countries added via manual mapping are also included
            int mappingCount = 0;
            if (provider.Type == Configuration.Entities.ProviderType.BettingProvider)
            {
                _logger.LogInformation("Applying manual country mappings for betting provider {ProviderName}...", provider.Name);
                mappingCount = await ApplyCountryMappingsAsync(providerId);
                _logger.LogInformation("Applied {Count} country mappings", mappingCount);
            }

            // Detect duplicate entries (same CountryId, different codes)
            var duplicates = await DetectCountryDuplicatesAsync(providerId);
            var warnings = new List<string>();

            if (duplicates.Count > 0)
            {
                foreach (var dup in duplicates)
                {
                    var variantCodes = string.Join(", ", dup.Variants.Select(v => v.ProviderCode));
                    var warning = $"Duplicate country entries: {variantCodes}";
                    warnings.Add(warning);
                    _logger.LogWarning("Duplicate detected for CountryId {CountryId}: {Variants}",
                        dup.CountryId, variantCodes);
                }
            }

            // Update sync job - use CompletedWithWarnings if duplicates found
            syncJob.Status = duplicates.Count > 0
                ? SyncJobStatus.CompletedWithWarnings
                : SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                total = scrapedCountries.Count,
                @new = newCount,
                updated = updatedCount,
                fromMappings = mappingCount,
                duplicatesDetected = duplicates.Count,
                warnings = warnings,
                duplicates = duplicates.Select(d => new
                {
                    countryId = d.CountryId.ToString(),
                    variants = d.Variants.Select(v => v.ProviderCode).ToList()
                }).ToList()
            });
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("Country scan completed. New: {New}, Updated: {Updated}, From mappings: {Mappings}, Duplicates: {Duplicates}",
                newCount, updatedCount, mappingCount, duplicates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country scan failed for job {JobId}", jobId);

            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;
            await _syncJobRepo.UpdateAsync(syncJob);

            throw;
        }
    }

    public async Task<Guid> ScanLeaguesAsync(Guid providerId, List<Guid>? countryIds = null)
    {
        _logger.LogInformation("Starting league scan for provider {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // Create sync job
        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Pending,
            CountryIds = countryIds ?? new List<Guid>(),
            Priority = 2
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        // Delegate to internal implementation
        await ScanLeaguesInternalAsync(providerId, countryIds ?? new List<Guid>(), syncJob.Id);

        return syncJob.Id;
    }

    public async Task ScanLeaguesInternalAsync(Guid providerId, List<Guid> countryIds, Guid jobId)
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
            // providerId and countryIds are passed as parameters

            // Validate provider exists
            var provider = await _dataProviderRepo.GetByIdAsync(providerId);
            if (provider == null)
            {
                throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
            }

            // Get default sport (Football) for league scraping
            var sports = await _sportRepo.GetAllAsync();
            var defaultSport = sports.FirstOrDefault(s => s.Name == "Football") ?? sports.First();

            // Load provider configuration (if available)
            Configuration.DTOs.ProviderConfigurationDto? config = null;
            if (!string.IsNullOrEmpty(provider.Configuration))
            {
                try
                {
                    config = JsonSerializer.Deserialize<Configuration.DTOs.ProviderConfigurationDto>(provider.Configuration);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize provider configuration for {ProviderId}, continuing without filters", providerId);
                }
            }

            // Select the appropriate league scraper for this provider
            var leagueScraper = _leagueScrapers.FirstOrDefault(s => s.CanHandle(provider));
            if (leagueScraper == null)
            {
                var errorMessage = $"No league scraper available for provider {provider.Name} ({provider.Code})";
                _logger.LogError(errorMessage);
                syncJob.Status = SyncJobStatus.Failed;
                syncJob.ErrorMessage = errorMessage;
                syncJob.CompletedAt = DateTime.UtcNow;
                await _syncJobRepo.UpdateAsync(syncJob);
                throw new InvalidOperationException(errorMessage);
            }

            // Get countries to scan
            // For betting providers, use configuration countries directly (they don't have their own country list)
            // For scrapers (BetExplorer), use provider cache countries
            List<Configuration.Entities.Country> countriesToScan;

            // Parse ScanCapabilities to check if provider can scan countries
            var scanCaps = ParseScanCapabilities(provider.ScanCapabilities);
            var canScanCountries = scanCaps?.CanScanCountries ?? true;

            if (provider.Type == Configuration.Entities.ProviderType.BettingProvider)
            {
                // Special handling for providers that don't scan countries (e.g., Tipsport)
                // They derive country from league names, so we need ALL countries from database
                // (not just active ones - we'll activate countries when leagues are found)
                if (!canScanCountries)
                {
                    _logger.LogInformation("Provider {ProviderName} cannot scan countries - using ALL countries from database",
                        provider.Name);
                    var allCountries = await _countryRepo.GetAllAsync();
                    countriesToScan = allCountries.ToList();  // Include inactive countries too
                    _logger.LogInformation("Using {Count} countries for league scan (active and inactive)", countriesToScan.Count);
                }
                // Betting providers with CountryProvider mapping
                else if (countryIds != null && countryIds.Any())
                {
                    // Specific countries selected - validate they have provider mapping
                    countriesToScan = new List<Configuration.Entities.Country>();
                    foreach (var countryId in countryIds)
                    {
                        var countryProvider = await _countryProviderRepo.GetByCountryAndProviderAsync(countryId, providerId);
                        if (countryProvider != null && countryProvider.IsActive && countryProvider.Country != null)
                        {
                            countriesToScan.Add(countryProvider.Country);
                        }
                        else
                        {
                            _logger.LogWarning("Country {CountryId} is not mapped to provider {ProviderId} or mapping is inactive",
                                countryId, providerId);
                        }
                    }
                }
                else
                {
                    // No countries specified - use all countries with active provider mapping
                    var countryProviders = await _countryProviderRepo.GetByProviderIdAsync(providerId);
                    countriesToScan = countryProviders
                        .Where(cp => cp.Country != null)
                        .Select(cp => cp.Country!)
                        .ToList();

                    _logger.LogInformation("Found {Count} countries with active provider mapping for provider {ProviderId}",
                        countriesToScan.Count, providerId);
                }
            }
            else
            {
                // Scrapers (BetExplorer): use provider cache countries and map to configuration
                List<ProviderCountry> providerCountries;
                if (countryIds != null && countryIds.Any())
                {
                    providerCountries = new List<ProviderCountry>();
                    foreach (var countryId in countryIds)
                    {
                        var pc = await _providerCountryRepo.GetByIdAsync(countryId);
                        if (pc != null) providerCountries.Add(pc);
                    }
                }
                else
                {
                    providerCountries = await _providerCountryRepo.GetByProviderIdAsync(providerId);
                    _logger.LogInformation("Loaded {Count} ProviderCountries for provider {ProviderId}",
                        providerCountries.Count, providerId);
                }

                // Map provider countries to configuration countries
                countriesToScan = new List<Configuration.Entities.Country>();
                foreach (var pc in providerCountries)
                {
                    Configuration.Entities.Country? country = null;
                    if (pc.CountryId.HasValue)
                    {
                        country = await _countryRepo.GetByIdAsync(pc.CountryId.Value);
                    }
                    else if (!string.IsNullOrEmpty(pc.IsoCode))
                    {
                        var allCountries = await _countryRepo.GetAllAsync();
                        country = allCountries.FirstOrDefault(c => c.Code == pc.IsoCode);
                    }

                    if (country != null)
                    {
                        countriesToScan.Add(country);
                    }
                    else
                    {
                        _logger.LogWarning("No matching Country found for ProviderCountry {ProviderCountryId} ({CountryName})",
                            pc.Id, pc.ProviderName);
                    }
                }

                _logger.LogInformation("Mapped {Count} countries for league scan", countriesToScan.Count);
            }

            int newCount = 0;
            int updatedCount = 0;
            int totalScraped = 0;

            foreach (var country in countriesToScan)
            {
                try
                {

                    // Scrape leagues for this country
                    var scrapedLeagues = await leagueScraper.ScrapeLeaguesAsync(defaultSport, country);
                    totalScraped += scrapedLeagues.Count;

                    // =====================================================================
                    // AUTO-CREATE COUNTRY MAPPING: For providers without canScanCountries
                    // (e.g., Tipsport), create CountryProvider + ProviderCountry when leagues are found
                    // =====================================================================
                    if (!canScanCountries && scrapedLeagues.Count > 0)
                    {
                        // Auto-activate country if needed
                        if (!country.IsActive)
                        {
                            country.IsActive = true;
                            await _countryRepo.UpdateAsync(country);
                            _logger.LogInformation("✓ Auto-activated country {CountryName} ({CountryCode}) - found {LeagueCount} leagues from {Provider}",
                                country.Name, country.Code, scrapedLeagues.Count, provider.Name);
                        }

                        // Create ProviderCountry cache record if not exists
                        var existingProviderCountry = await _providerCountryRepo.GetByProviderCodeAsync(providerId, country.Code);
                        if (existingProviderCountry == null)
                        {
                            var providerCountry = new ProviderCountry
                            {
                                ProviderId = providerId,
                                ProviderCode = country.Code,
                                ProviderName = country.Name,
                                IsoCode = country.Code.Length <= 10 ? country.Code : null, // IsoCode max 10 chars
                                ScrapedAt = DateTime.UtcNow,
                                IsImported = true,
                                CountryId = country.Id,
                                ImportedAt = DateTime.UtcNow
                            };
                            await _providerCountryRepo.CreateAsync(providerCountry);
                            _logger.LogInformation("✓ Created ProviderCountry cache: {CountryName} ({CountryCode}) for {Provider}",
                                country.Name, country.Code, provider.Name);
                        }

                        // Create CountryProvider mapping if not exists
                        var existingCountryProvider = await _countryProviderRepo.GetByCountryAndProviderAsync(
                            country.Id, providerId);

                        if (existingCountryProvider == null)
                        {
                            var countryProvider = new Configuration.Entities.CountryProvider
                            {
                                CountryId = country.Id,
                                ProviderId = providerId,
                                ProviderCode = country.Code,
                                ProviderName = country.Name,
                                IsActive = true
                            };
                            await _countryProviderRepo.AddAsync(countryProvider);
                            _logger.LogInformation("✓ Created CountryProvider mapping: {CountryName} ({CountryCode}) ↔ {Provider}",
                                country.Name, country.Code, provider.Name);
                        }
                    }

                    // DIFFERENT HANDLING FOR BETTING PROVIDERS vs REFERENCE PROVIDERS
                    int skippedCount = 0;

                    if (provider.Type == Configuration.Entities.ProviderType.BettingProvider)
                    {
                        // =====================================================================
                        // BETTING PROVIDERS: Find or create leagues via BetExplorer enrichment
                        // - Existing league found → create LeagueProvider mapping
                        // - No existing league → on-demand BetExplorer scrape → create league
                        // - No BetExplorer match → save to unmatched_leagues for manual review
                        // =====================================================================
                        _logger.LogInformation("Processing {Count} leagues from betting provider {Provider}",
                            scrapedLeagues.Count, provider.Name);

                        foreach (var scrapedLeague in scrapedLeagues)
                        {
                            Guid? leagueId = null;

                            // 1. Check for existing LeagueProvider mapping FIRST
                            var existingLeagueProvider = await _leagueProviderRepo.GetByProviderAndSlugAsync(
                                providerId, scrapedLeague.Slug);

                            if (existingLeagueProvider != null)
                            {
                                leagueId = existingLeagueProvider.LeagueId;
                                _logger.LogInformation("✓ Using existing LeagueProvider mapping: {ProviderLeague} → LeagueId={LeagueId} [{Country}]",
                                    scrapedLeague.Name, leagueId, country.Name);
                            }
                            else
                            {
                                // 2. No existing mapping - try BetExplorer enrichment
                                var configLeague = await _enrichmentService.FindOrCreateLeagueFromBetExplorerAsync(
                                    scrapedLeague, country, provider.Code, defaultSport.Id);

                                if (configLeague != null)
                                {
                                    leagueId = configLeague.Id;

                                    // Create LeagueProvider mapping
                                    var leagueProvider = new Configuration.Entities.LeagueProvider
                                    {
                                        LeagueId = configLeague.Id,
                                        ProviderId = providerId,
                                        ProviderSlug = scrapedLeague.Slug,
                                        ProviderName = scrapedLeague.Name,
                                        IsActive = true
                                    };
                                    await _leagueProviderRepo.AddAsync(leagueProvider);
                                    _logger.LogInformation("✓ Created LeagueProvider mapping: {ProviderLeague} → {ConfigLeague} [{Country}]",
                                        scrapedLeague.Name, configLeague.Name, country.Name);
                                    newCount++;
                                }
                            }

                            // 3. Save to provider_leagues ONLY if we have a valid leagueId
                            if (leagueId.HasValue)
                            {
                                var existingProviderLeague = await _providerLeagueRepo.GetByProviderSlugAsync(
                                    providerId, scrapedLeague.Slug);

                                if (existingProviderLeague == null)
                                {
                                    var providerLeague = new ProviderLeague
                                    {
                                        ProviderId = providerId,
                                        ProviderName = scrapedLeague.Name,
                                        ProviderSlug = scrapedLeague.Slug,
                                        CountryCode = country.Code,
                                        LeagueId = leagueId.Value,
                                        IsImported = true,
                                        ScrapedAt = DateTime.UtcNow,
                                        RawData = JsonSerializer.Serialize(scrapedLeague)
                                    };
                                    await _providerLeagueRepo.CreateAsync(providerLeague);
                                    _logger.LogDebug("✓ Cached league with mapping: {LeagueName} [{Country}]", scrapedLeague.Name, country.Name);
                                }
                                else
                                {
                                    existingProviderLeague.LeagueId = leagueId.Value;
                                    existingProviderLeague.IsImported = true;
                                    existingProviderLeague.ProviderName = scrapedLeague.Name;
                                    existingProviderLeague.CountryCode = country.Code;
                                    existingProviderLeague.ScrapedAt = DateTime.UtcNow;
                                    existingProviderLeague.RawData = JsonSerializer.Serialize(scrapedLeague);
                                    await _providerLeagueRepo.UpdateAsync(existingProviderLeague);
                                }
                                updatedCount++;
                            }
                            else
                            {
                                // 4. No match found - save to unmatched_leagues only (NOT to provider_leagues)
                                skippedCount++;

                                var existingUnmatched = await _unmatchedLeagueRepo.FindExistingAsync(
                                    providerId, scrapedLeague.Name, country.Code);

                                if (existingUnmatched == null)
                                {
                                    var unmatchedLeague = new UnmatchedLeague
                                    {
                                        ProviderId = providerId,
                                        ProviderLeagueId = scrapedLeague.ProviderLeagueId,
                                        ProviderLeagueName = scrapedLeague.Name,
                                        ProviderSlug = scrapedLeague.Slug,
                                        CountryCode = country.Code,
                                        CountryName = country.Name,
                                        ScrapedAt = DateTime.UtcNow
                                    };
                                    await _unmatchedLeagueRepo.CreateAsync(unmatchedLeague);
                                    _logger.LogWarning("✗ No BetExplorer match for '{ProviderLeague}' [{Country}] - saved to unmatched_leagues",
                                        scrapedLeague.Name, country.Name);
                                }
                                else
                                {
                                    // Already in unmatched - just log
                                    _logger.LogDebug("'{ProviderLeague}' [{Country}] already in unmatched_leagues",
                                        scrapedLeague.Name, country.Name);
                                }
                            }
                        }

                        _logger.LogInformation("Betting provider scan complete for {Country}: {New} new mappings, {Updated} updated, {Skipped} unmatched",
                            country.Name, newCount, updatedCount, skippedCount);
                    }
                    else
                    {
                        // =====================================================================
                        // REFERENCE PROVIDERS (BetExplorer): Cache leagues in ProviderLeagues
                        // These are the source of truth for league data
                        // =====================================================================
                        _logger.LogDebug("Using direct BetExplorer data for {Count} leagues", scrapedLeagues.Count);

                        // Apply ExcludedLeagueIds filter (if configured)
                        var leaguesToCache = scrapedLeagues;
                        if (config?.ExcludedLeagueIds != null && config.ExcludedLeagueIds.Any())
                        {
                            var beforeFilter = leaguesToCache.Count;
                            leaguesToCache = leaguesToCache
                                .Where(l => !config.ExcludedLeagueIds.Contains(l.Slug))
                                .ToList();

                            if (beforeFilter != leaguesToCache.Count)
                            {
                                _logger.LogInformation("Filtered out {Excluded} leagues based on ExcludedLeagueIds configuration for country {CountryName}",
                                    beforeFilter - leaguesToCache.Count, country.Name);
                            }
                        }

                        // Cache the leagues from BetExplorer
                        foreach (var league in leaguesToCache)
                        {
                            // Check if already exists
                            var existing = await _providerLeagueRepo.GetByProviderSlugAsync(providerId, league.Slug);

                            if (existing == null)
                            {
                                // Create new - BetExplorer is source of truth, so set MappingStatus to AutoMapped
                                var providerLeague = new ProviderLeague
                                {
                                    ProviderId = providerId,
                                    ProviderCountryId = null,
                                    ProviderSlug = league.Slug,
                                    ProviderName = league.Name,
                                    DisplayName = league.DisplayName,
                                    CountryCode = league.CountryCode ?? country.Code,
                                    Priority = league.Priority,
                                    IsBettable = league.IsBettable,
                                    ScrapedAt = DateTime.UtcNow,
                                    RawData = JsonSerializer.Serialize(league),
                                    IsImported = false,
                                    MappingStatus = MappingStatus.AutoMapped  // BetExplorer is source of truth
                                };
                                await _providerLeagueRepo.CreateAsync(providerLeague);
                                _logger.LogInformation("✓ Added league to cache: {Name} [{Country}]",
                                    league.DisplayName ?? league.Name, country.Name);
                                newCount++;
                            }
                            else
                            {
                                // Update existing - ensure MappingStatus is AutoMapped for BetExplorer
                                existing.ProviderName = league.Name;
                                existing.DisplayName = league.DisplayName;
                                existing.CountryCode = league.CountryCode ?? country.Code;
                                existing.Priority = league.Priority;
                                existing.IsBettable = league.IsBettable;
                                existing.ScrapedAt = DateTime.UtcNow;
                                existing.RawData = JsonSerializer.Serialize(league);
                                existing.MappingStatus = MappingStatus.AutoMapped;  // BetExplorer is source of truth
                                await _providerLeagueRepo.UpdateAsync(existing);
                                _logger.LogInformation("↻ Updated league in cache: {Name} [{Country}]",
                                    league.DisplayName ?? league.Name, country.Name);
                                updatedCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scrape leagues for country {CountryCode}",
                        country.Code);
                }
            }

            // Save leagues that couldn't be mapped to any country
            int unmappedCountryCount = 0;
            if (leagueScraper is IUnmappedCountryLeagueProvider unmappedProvider)
            {
                var unmappedLeagues = unmappedProvider.GetUnmappedCountryLeagues();
                foreach (var league in unmappedLeagues)
                {
                    // Check if already exists
                    var existing = await _unmatchedLeagueRepo.FindExistingAsync(
                        providerId, league.ProviderLeagueName, "UNMAPPED");

                    if (existing == null)
                    {
                        var unmatchedLeague = new UnmatchedLeague
                        {
                            ProviderId = providerId,
                            ProviderLeagueId = league.ProviderLeagueId,
                            ProviderLeagueName = league.ProviderLeagueName,
                            ProviderSlug = league.ProviderUrl,
                            CountryCode = "UNMAPPED",
                            CountryName = "Unknown - no country mapping",
                            ScrapedAt = DateTime.UtcNow
                        };
                        await _unmatchedLeagueRepo.CreateAsync(unmatchedLeague);
                        unmappedCountryCount++;
                        _logger.LogWarning("✗ Saved unmapped country league: '{LeagueName}'", league.ProviderLeagueName);
                    }
                }

                if (unmappedCountryCount > 0)
                {
                    _logger.LogWarning("Saved {Count} leagues with unmapped countries to unmatched_leagues (country_code='UNMAPPED')",
                        unmappedCountryCount);
                }
            }

            // Update sync job as completed
            syncJob.Status = SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                countries = countriesToScan.Count,
                total = totalScraped,
                @new = newCount,
                updated = updatedCount,
                unmappedCountry = unmappedCountryCount
            });
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("League scan completed. New: {New}, Updated: {Updated}, Unmapped countries: {Unmapped}",
                newCount, updatedCount, unmappedCountryCount);

            // Backfill provider_leagues from resolved unmatched_leagues
            var (backfillCreated, backfillUpdated) = await BackfillProviderLeaguesFromResolvedAsync(providerId);
            if (backfillCreated + backfillUpdated > 0)
            {
                _logger.LogInformation("Backfilled provider_leagues from resolved unmatched_leagues: {Created} created, {Updated} updated",
                    backfillCreated, backfillUpdated);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "League scan failed for job {JobId}", jobId);

            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;
            await _syncJobRepo.UpdateAsync(syncJob);

            throw;
        }
    }

    public async Task<Guid> ScanSeasonsAsync(Guid providerId, List<Guid>? leagueIds = null)
    {
        _logger.LogInformation("Starting season scan for provider {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // Create sync job
        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Seasons,
            Status = SyncJobStatus.Pending,
            LeagueIds = leagueIds ?? new List<Guid>(),
            Priority = 3
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        // Delegate to internal implementation
        await ScanSeasonsInternalAsync(providerId, leagueIds ?? new List<Guid>(), syncJob.Id);

        return syncJob.Id;
    }

    public async Task ScanSeasonsInternalAsync(Guid providerId, List<Guid> leagueIds, Guid jobId)
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
            // providerId and leagueIds are passed as parameters

            // Validate provider exists
            var provider = await _dataProviderRepo.GetByIdAsync(providerId);
            if (provider == null)
            {
                throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
            }

            // Select the appropriate season scraper for this provider
            var seasonScraper = _seasonScrapers.FirstOrDefault(s => s.CanHandle(provider));
            if (seasonScraper == null)
            {
                var errorMessage = $"No season scraper available for provider {provider.Name} ({provider.Code})";
                _logger.LogError(errorMessage);
                syncJob.Status = SyncJobStatus.Failed;
                syncJob.ErrorMessage = errorMessage;
                syncJob.CompletedAt = DateTime.UtcNow;
                await _syncJobRepo.UpdateAsync(syncJob);
                throw new InvalidOperationException(errorMessage);
            }

            // Get leagues from configuration (active leagues)
            List<Configuration.Entities.League> leagues;
            if (leagueIds != null && leagueIds.Any())
            {
                leagues = new List<Configuration.Entities.League>();
                foreach (var leagueId in leagueIds)
                {
                    var league = await _leagueRepo.GetByIdAsync(leagueId);
                    if (league != null && league.IsActive) leagues.Add(league);
                }
            }
            else
            {
                var allLeagues = await _leagueRepo.GetAllAsync(includeRelations: true);
                leagues = allLeagues.Where(l => l.IsActive).ToList();
            }

            _logger.LogInformation("Scanning seasons for {Count} active leagues", leagues.Count);

            int newCount = 0;
            int updatedCount = 0;
            int totalScraped = 0;
            int skippedCount = 0;

            foreach (var league in leagues)
            {
                try
                {
                    // Find ProviderLeague via LeagueProvider mapping
                    var leagueProviders = await _leagueProviderRepo.GetByLeagueIdAsync(league.Id);
                    var leagueProvider = leagueProviders.FirstOrDefault();

                    if (leagueProvider == null)
                    {
                        _logger.LogDebug("No LeagueProvider mapping for league {LeagueId} ({Name}), skipping",
                            league.Id, league.Name);
                        skippedCount++;
                        continue;
                    }

                    var providerLeague = await _providerLeagueRepo.GetByProviderSlugAsync(
                        leagueProvider.ProviderId, leagueProvider.ProviderSlug);

                    if (providerLeague == null)
                    {
                        _logger.LogDebug("No ProviderLeague found for league {LeagueId} ({Name}), skipping",
                            league.Id, league.Name);
                        skippedCount++;
                        continue;
                    }

                    // Scrape seasons for this league (returns list of season names like "2024-2025")
                    var scrapedSeasons = await seasonScraper.ScrapeAvailableSeasonsAsync(league);
                    totalScraped += scrapedSeasons.Count;

                    foreach (var seasonName in scrapedSeasons)
                    {
                        // Parse season name to extract years
                        var (startYear, endYear) = ParseSeasonName(seasonName);

                        // Check if already exists
                        var existing = await _providerSeasonRepo.GetBySeasonNameAsync(
                            providerLeague.Id,
                            seasonName);

                        if (existing == null)
                        {
                            // Create new
                            var providerSeason = new ProviderSeason
                            {
                                ProviderId = providerId,
                                ProviderLeagueId = providerLeague.Id,
                                SeasonName = seasonName,
                                StartYear = startYear,
                                EndYear = endYear,
                                IsCurrentSeason = IsCurrentSeason(startYear, endYear),
                                ScrapedAt = DateTime.UtcNow,
                                RawData = JsonSerializer.Serialize(new { seasonName, startYear, endYear }),
                                IsImported = false
                            };
                            await _providerSeasonRepo.CreateAsync(providerSeason);
                            newCount++;
                        }
                        else
                        {
                            // Update existing
                            existing.StartYear = startYear;
                            existing.EndYear = endYear;
                            existing.IsCurrentSeason = IsCurrentSeason(startYear, endYear);
                            existing.ScrapedAt = DateTime.UtcNow;
                            existing.RawData = JsonSerializer.Serialize(new { seasonName, startYear, endYear });
                            await _providerSeasonRepo.UpdateAsync(existing);
                            updatedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scrape seasons for league {LeagueId} ({LeagueName})",
                        league.Id, league.Name);
                }
            }

            // Update sync job as completed
            syncJob.Status = SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                leagues = leagues.Count,
                skipped = skippedCount,
                total = totalScraped,
                @new = newCount,
                updated = updatedCount
            });
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("Season scan completed. Leagues: {Leagues}, Skipped: {Skipped}, New: {New}, Updated: {Updated}",
                leagues.Count, skippedCount, newCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Season scan failed for job {JobId}", jobId);

            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;
            await _syncJobRepo.UpdateAsync(syncJob);

            throw;
        }
    }

    public async Task<List<ProviderCountry>> GetUnimportedCountriesAsync(Guid providerId)
    {
        return await _providerCountryRepo.GetUnimportedAsync(providerId);
    }

    public async Task<List<ProviderLeague>> GetUnimportedLeaguesAsync(Guid providerId)
    {
        return await _providerLeagueRepo.GetUnimportedAsync(providerId);
    }

    public async Task<List<ProviderSeason>> GetUnimportedSeasonsAsync(Guid providerId)
    {
        return await _providerSeasonRepo.GetUnimportedAsync(providerId);
    }

    /// <summary>
    /// Applies active country name mappings to create missing ProviderCountry entries.
    /// </summary>
    public async Task<int> ApplyCountryMappingsAsync(Guid providerId)
    {
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        var providerCode = provider.Code.ToLowerInvariant();
        var activeMappings = await _countryNameMappingRepo.GetActiveByProviderAsync(providerCode);

        int createdCount = 0;

        foreach (var mapping in activeMappings)
        {
            // Skip mappings without a target country code
            if (string.IsNullOrWhiteSpace(mapping.BetExplorerCode))
            {
                _logger.LogDebug("Skipping mapping {MappingId} - no BetExplorerCode set", mapping.Id);
                continue;
            }

            // Find the target country
            var country = await _countryRepo.GetByCodeAsync(mapping.BetExplorerCode);
            if (country == null)
            {
                _logger.LogWarning("Mapping {MappingId} references non-existent country code '{Code}'",
                    mapping.Id, mapping.BetExplorerCode);
                continue;
            }

            // Check if ProviderCountry already exists for this mapping
            var existing = await _providerCountryRepo.GetByProviderCodeAsync(providerId, mapping.ProviderCountryName);
            if (existing != null)
            {
                _logger.LogDebug("ProviderCountry already exists for {ProviderCountryName}", mapping.ProviderCountryName);
                continue;
            }

            // Create new ProviderCountry
            var providerCountry = new ProviderCountry
            {
                ProviderId = providerId,
                CountryId = country.Id,
                ProviderName = country.Name, // Use name from catalog
                ProviderCode = mapping.ProviderCountryName,
                ScrapedAt = DateTime.UtcNow,
                RawData = JsonSerializer.Serialize(new { fromMapping = mapping.Id, mappedTo = mapping.BetExplorerCode }),
                IsImported = false
            };

            await _providerCountryRepo.CreateAsync(providerCountry);
            createdCount++;

            // Track usage
            await _countryNameMappingRepo.TrackUsageAsync(mapping.Id, providerCountry.Id);

            // Also create CountryProvider mapping for league scanning
            var existingCountryProvider = await _countryProviderRepo.GetByCountryAndProviderAsync(country.Id, providerId);
            if (existingCountryProvider == null)
            {
                var countryProvider = new Configuration.Entities.CountryProvider
                {
                    CountryId = country.Id,
                    ProviderId = providerId,
                    ProviderCode = mapping.ProviderCountryName,
                    ProviderName = country.Name, // Use name from catalog
                    IsActive = true
                };
                await _countryProviderRepo.AddAsync(countryProvider);
                _logger.LogInformation("✓ Created CountryProvider mapping from manual mapping: {CountryName} ↔ Provider {ProviderCode}",
                    country.Name, provider.Code);
            }

            _logger.LogInformation("✓ Created ProviderCountry from mapping: {ProviderCountryName} → {CountryName}",
                mapping.ProviderCountryName, country.Name);
        }

        _logger.LogInformation("ApplyCountryMappings completed for provider {ProviderName}: {Created} entries created",
            provider.Name, createdCount);

        return createdCount;
    }

    /// <summary>
    /// Backfills provider_leagues from resolved unmatched_leagues.
    /// Creates provider_leagues entries for all resolved (mapped) unmatched leagues
    /// that don't yet have a corresponding provider_leagues record.
    /// </summary>
    public async Task<(int Created, int Updated)> BackfillProviderLeaguesFromResolvedAsync(Guid providerId)
    {
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        _logger.LogInformation("Starting backfill of provider_leagues from resolved unmatched_leagues for provider {ProviderName}",
            provider.Name);

        // Get all resolved (mapped) unmatched leagues for this provider
        var resolvedUnmatchedLeagues = await _unmatchedLeagueRepo.GetResolvedAsMappedByProviderAsync(providerId);

        int createdCount = 0;
        int updatedCount = 0;

        foreach (var unmatchedLeague in resolvedUnmatchedLeagues)
        {
            if (!unmatchedLeague.ResolvedLeagueId.HasValue)
            {
                continue;
            }

            var providerSlug = unmatchedLeague.ProviderSlug
                ?? unmatchedLeague.ProviderLeagueName.ToLowerInvariant().Replace(" ", "-");

            // Check if provider_leagues record already exists
            var existingProviderLeague = await _providerLeagueRepo.GetByProviderSlugAsync(providerId, providerSlug);

            if (existingProviderLeague == null)
            {
                // Create new provider_leagues record
                var providerLeague = new ProviderLeague
                {
                    ProviderId = providerId,
                    ProviderName = unmatchedLeague.ProviderLeagueName,
                    ProviderSlug = providerSlug,
                    CountryCode = unmatchedLeague.CountryCode,
                    LeagueId = unmatchedLeague.ResolvedLeagueId.Value,
                    IsImported = true,
                    ScrapedAt = DateTime.UtcNow
                };
                await _providerLeagueRepo.CreateAsync(providerLeague);
                createdCount++;

                _logger.LogDebug("✓ Created provider_leagues: {LeagueName} → LeagueId {LeagueId}",
                    unmatchedLeague.ProviderLeagueName, unmatchedLeague.ResolvedLeagueId.Value);
            }
            else if (!existingProviderLeague.LeagueId.HasValue ||
                     existingProviderLeague.LeagueId.Value != unmatchedLeague.ResolvedLeagueId.Value)
            {
                // Update existing record with league_id (or fix incorrect league_id)
                var oldLeagueId = existingProviderLeague.LeagueId;
                existingProviderLeague.LeagueId = unmatchedLeague.ResolvedLeagueId.Value;
                existingProviderLeague.IsImported = true;
                await _providerLeagueRepo.UpdateAsync(existingProviderLeague);
                updatedCount++;

                _logger.LogDebug("✓ Updated provider_leagues: {LeagueName} → LeagueId {LeagueId} (was: {OldLeagueId})",
                    unmatchedLeague.ProviderLeagueName, unmatchedLeague.ResolvedLeagueId.Value, oldLeagueId);
            }
        }

        _logger.LogInformation("BackfillProviderLeagues completed for provider {ProviderName}: {Created} created, {Updated} updated",
            provider.Name, createdCount, updatedCount);

        return (createdCount, updatedCount);
    }

    public async Task<(int Created, int Updated)> BackfillProviderCountriesFromResolvedAsync(Guid providerId)
    {
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        _logger.LogInformation("Starting backfill of provider_countries from resolved unmatched_countries for provider {ProviderName}",
            provider.Name);

        // Get all resolved (mapped) unmatched countries for this provider
        var resolvedUnmatchedCountries = await _unmatchedCountryRepo.GetResolvedAsMappedByProviderAsync(providerId);

        int createdCount = 0;
        int updatedCount = 0;

        foreach (var unmatchedCountry in resolvedUnmatchedCountries)
        {
            if (!unmatchedCountry.ResolvedCountryId.HasValue)
            {
                continue;
            }

            var providerCode = unmatchedCountry.ProviderSlug
                ?? unmatchedCountry.ProviderCountryName.ToLowerInvariant().Replace(" ", "-");

            // Check if provider_countries record already exists BY CODE
            var existingByCode = await _providerCountryRepo.GetByProviderCodeAsync(providerId, providerCode);

            // Check if provider_countries record already exists BY COUNTRY_ID (prevent duplicates)
            var existingByCountryId = await _providerCountryRepo.GetByProviderAndCountryAsync(
                providerId, unmatchedCountry.ResolvedCountryId.Value);

            if (existingByCode == null && existingByCountryId == null)
            {
                // Create new provider_countries record - no existing record found
                var providerCountry = new ProviderCountry
                {
                    ProviderId = providerId,
                    ProviderName = unmatchedCountry.ProviderCountryName,
                    ProviderCode = providerCode,
                    CountryId = unmatchedCountry.ResolvedCountryId.Value,
                    IsImported = true,
                    ScrapedAt = DateTime.UtcNow
                };
                await _providerCountryRepo.CreateAsync(providerCountry);
                createdCount++;

                _logger.LogDebug("✓ Created provider_countries: {CountryName} → CountryId {CountryId}",
                    unmatchedCountry.ProviderCountryName, unmatchedCountry.ResolvedCountryId.Value);
            }
            else if (existingByCountryId != null)
            {
                // Already exists record for this country - skip to prevent duplicate
                _logger.LogDebug("⏭️ Skipping: {CountryName} already exists with code {ExistingCode}",
                    unmatchedCountry.ProviderCountryName, existingByCountryId.ProviderCode);
            }
            else if (existingByCode != null && !existingByCode.CountryId.HasValue)
            {
                // Update existing record (by code) with country_id
                existingByCode.CountryId = unmatchedCountry.ResolvedCountryId.Value;
                existingByCode.IsImported = true;
                await _providerCountryRepo.UpdateAsync(existingByCode);
                updatedCount++;

                _logger.LogDebug("✓ Updated provider_countries: {CountryName} → CountryId {CountryId}",
                    unmatchedCountry.ProviderCountryName, unmatchedCountry.ResolvedCountryId.Value);
            }
        }

        _logger.LogInformation("BackfillProviderCountries completed for provider {ProviderName}: {Created} created, {Updated} updated",
            provider.Name, createdCount, updatedCount);

        return (createdCount, updatedCount);
    }

    /// <summary>
    /// Internal method: Scans both countries AND leagues in a single pass using an existing job.
    /// Optimized for Betano where both come from a single HTTP request.
    /// </summary>
    public async Task ScanCountriesAndLeaguesInternalAsync(Guid providerId, Guid jobId)
    {
        _logger.LogInformation("Starting combined countries+leagues scan for provider {ProviderId}, job {JobId}",
            providerId, jobId);

        // Load sync job and update status
        var syncJob = await _syncJobRepo.GetByIdAsync(jobId);
        if (syncJob == null)
        {
            throw new ArgumentException($"Sync job {jobId} not found");
        }

        syncJob.Status = SyncJobStatus.Running;
        syncJob.StartedAt = DateTime.UtcNow;
        await _syncJobRepo.UpdateAsync(syncJob);

        try
        {
            // Validate provider exists and is Betano
            var provider = await _dataProviderRepo.GetByIdAsync(providerId);
            if (provider == null)
            {
                throw new ArgumentException($"Provider {providerId} not found");
            }

            if (provider.Code.ToLowerInvariant() != "betano")
            {
                throw new InvalidOperationException($"Combined scan is only supported for Betano provider, got {provider.Code}");
            }

            // Get default sport (Football)
            var sports = await _sportRepo.GetAllAsync();
            var defaultSport = sports.FirstOrDefault(s => s.Name == "Football") ?? sports.First();

            // === SINGLE HTTP REQUEST ===
            _logger.LogInformation("Calling GetFullDataAsync for single HTTP request to Betano...");
            var fullDataResult = await _betanoFullDataProvider.GetFullDataAsync("football");
            if (!fullDataResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to get full data from Betano: {fullDataResult.Error}");
            }

            var betanoData = fullDataResult.Value;
            _logger.LogInformation("Got {RegionCount} regions and {LeagueCount} leagues from Betano in single request",
                betanoData.Regions.Count, betanoData.Leagues.Count);

            // === PHASE 1: PROCESS COUNTRIES ===
            int countriesNew = 0;
            int countriesUpdated = 0;
            var processedCountries = new Dictionary<string, Configuration.Entities.Country>(); // regionCode → Country

            foreach (var region in betanoData.Regions)
            {
                // Check for inactive mapping (non-countries like Copa Libertadores)
                var providerCodeLower = provider.Code.ToLowerInvariant();
                var existingInactiveMapping = await _countryNameMappingRepo.FindAnyMappingAsync(
                    providerCodeLower, region.Code);

                if (existingInactiveMapping != null && !existingInactiveMapping.IsActive)
                {
                    _logger.LogDebug("Skipping non-country region: {Name} ({Code}) - has inactive mapping",
                        region.Name, region.Code);
                    continue;
                }

                // Try to match country to BetExplorer catalog
                Configuration.Entities.Country? configCountry = null;

                // Step 1: Manual country name mapping
                var countryMapping = await _countryNameMappingRepo.FindMappingAsync(providerCodeLower, region.Code);
                if (countryMapping != null)
                {
                    if (!countryMapping.IsActive)
                    {
                        _logger.LogDebug("Skipping inactive mapping for {CountryName} ({CountryCode})",
                            region.Name, region.Code);
                        continue;
                    }
                    configCountry = await _countryRepo.GetByCodeAsync(countryMapping.BetExplorerCode);
                    if (configCountry != null)
                    {
                        _logger.LogDebug("Country found via mapping: {Name} → {BetExplorerCode}",
                            region.Name, countryMapping.BetExplorerCode);
                    }
                }

                // Step 2: Try by region code
                if (configCountry == null)
                {
                    configCountry = await _countryRepo.GetByCodeAsync(region.Code);
                }

                if (configCountry != null)
                {
                    processedCountries[region.Code] = configCountry;

                    // Create/update ProviderCountry cache
                    var existing = await _providerCountryRepo.GetByProviderNameAsync(providerId, region.Name);
                    if (existing == null)
                    {
                        var providerCountry = new ProviderCountry
                        {
                            ProviderId = providerId,
                            CountryId = configCountry.Id,
                            ProviderCode = region.Code,
                            ProviderName = region.Name,
                            ScrapedAt = DateTime.UtcNow,
                            RawData = JsonSerializer.Serialize(region),
                            IsImported = false
                        };
                        await _providerCountryRepo.CreateAsync(providerCountry);
                        countriesNew++;
                    }
                    else
                    {
                        existing.CountryId = configCountry.Id;
                        existing.ScrapedAt = DateTime.UtcNow;
                        await _providerCountryRepo.UpdateAsync(existing);
                        countriesUpdated++;
                    }

                    // Create/update CountryProvider mapping
                    var existingCp = await _countryProviderRepo.GetByCountryAndProviderAsync(configCountry.Id, providerId);
                    if (existingCp == null)
                    {
                        var countryProvider = new Configuration.Entities.CountryProvider
                        {
                            CountryId = configCountry.Id,
                            ProviderId = providerId,
                            ProviderCode = region.Code,
                            ProviderName = region.Name,
                            IsActive = true
                        };
                        await _countryProviderRepo.AddAsync(countryProvider);

                        // Auto-activate country
                        if (!configCountry.IsActive)
                        {
                            configCountry.IsActive = true;
                            await _countryRepo.UpdateAsync(configCountry);
                            _logger.LogInformation("✓ Auto-activated country {CountryName}", configCountry.Name);
                        }
                    }
                }
                else
                {
                    // Create CountryNameMapping for manual review
                    var existingAnyMapping = await _countryNameMappingRepo.FindAnyMappingAsync(providerCodeLower, region.Code);
                    if (existingAnyMapping == null)
                    {
                        var newMapping = new CountryNameMapping
                        {
                            ProviderCode = providerCodeLower,
                            ProviderCountryName = region.Code,
                            BetExplorerCode = "",
                            IsActive = false,
                            Priority = 100,
                            Notes = $"Auto-created: '{region.Name}' from Betano full scan"
                        };
                        await _countryNameMappingRepo.CreateAsync(newMapping);
                        _logger.LogInformation("📝 Created CountryNameMapping for: {Name} ({Code})",
                            region.Name, region.Code);
                    }
                }
            }

            _logger.LogInformation("Countries phase complete: {New} new, {Updated} updated, {Mapped} mapped to config",
                countriesNew, countriesUpdated, processedCountries.Count);

            // === PHASE 2: PROCESS LEAGUES ===
            int leaguesNew = 0;
            int leaguesUpdated = 0;
            int leaguesUnmatched = 0;

            // Group leagues by country code for efficient processing
            var leaguesByCountry = betanoData.Leagues
                .GroupBy(l => l.CountryCode ?? "unknown")
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (countryCode, leagues) in leaguesByCountry)
            {
                // Skip if country wasn't matched
                if (!processedCountries.TryGetValue(countryCode, out var country))
                {
                    _logger.LogDebug("Skipping {Count} leagues for unmatched country {CountryCode}",
                        leagues.Count, countryCode);
                    leaguesUnmatched += leagues.Count;
                    continue;
                }

                foreach (var league in leagues)
                {
                    // Convert LeagueAvailability to LeagueMetadata for enrichment service
                    var leagueMetadata = new Scrapers.LeagueMetadata
                    {
                        Name = league.ProviderLeagueName,
                        Slug = league.ProviderUrl?.Replace("https://www.betano.cz", "").TrimEnd('/') ?? "",
                        ProviderLeagueId = league.ProviderLeagueId
                    };

                    // === ALWAYS save to provider_leagues cache first ===
                    var existingProviderLeague = await _providerLeagueRepo.GetByProviderSlugAsync(
                        providerId, leagueMetadata.Slug);

                    ProviderLeague providerLeague;
                    if (existingProviderLeague == null)
                    {
                        providerLeague = new ProviderLeague
                        {
                            ProviderId = providerId,
                            ProviderName = league.ProviderLeagueName,
                            ProviderSlug = leagueMetadata.Slug,
                            CountryCode = country.Code,
                            ScrapedAt = DateTime.UtcNow,
                            RawData = JsonSerializer.Serialize(league),
                            IsImported = false
                        };
                        await _providerLeagueRepo.CreateAsync(providerLeague);
                        _logger.LogDebug("✓ Cached league: {LeagueName} [{Country}]", league.ProviderLeagueName, country.Name);
                    }
                    else
                    {
                        providerLeague = existingProviderLeague;
                        providerLeague.ProviderName = league.ProviderLeagueName;
                        providerLeague.ScrapedAt = DateTime.UtcNow;
                        providerLeague.RawData = JsonSerializer.Serialize(league);
                        await _providerLeagueRepo.UpdateAsync(providerLeague);
                    }

                    // === Now try to match with BetExplorer and create mappings ===
                    var configLeague = await _enrichmentService.FindOrCreateLeagueFromBetExplorerAsync(
                        leagueMetadata, country, provider.Code, defaultSport.Id);

                    if (configLeague != null)
                    {
                        // Update provider_league with LeagueId reference
                        providerLeague.LeagueId = configLeague.Id;
                        providerLeague.IsImported = true;
                        await _providerLeagueRepo.UpdateAsync(providerLeague);

                        // Create/update LeagueProvider mapping
                        var existingMapping = await _leagueProviderRepo.GetByLeagueAndProviderAsync(
                            configLeague.Id, providerId);

                        if (existingMapping == null)
                        {
                            var leagueProvider = new Configuration.Entities.LeagueProvider
                            {
                                LeagueId = configLeague.Id,
                                ProviderId = providerId,
                                ProviderSlug = leagueMetadata.Slug,
                                ProviderName = league.ProviderLeagueName,
                                IsActive = true
                            };
                            await _leagueProviderRepo.AddAsync(leagueProvider);
                            leaguesNew++;
                        }
                        else
                        {
                            existingMapping.ProviderSlug = leagueMetadata.Slug;
                            existingMapping.ProviderName = league.ProviderLeagueName;
                            existingMapping.IsActive = true;
                            await _leagueProviderRepo.UpdateAsync(existingMapping);
                            leaguesUpdated++;
                        }
                    }
                    else
                    {
                        // Save to unmatched_leagues
                        var existingUnmatched = await _unmatchedLeagueRepo.FindExistingAsync(
                            providerId, league.ProviderLeagueName, country.Code);

                        if (existingUnmatched == null)
                        {
                            var unmatchedLeague = new UnmatchedLeague
                            {
                                ProviderId = providerId,
                                ProviderLeagueId = league.ProviderLeagueId,
                                ProviderLeagueName = league.ProviderLeagueName,
                                ProviderSlug = leagueMetadata.Slug,
                                CountryCode = country.Code,
                                CountryName = country.Name,
                                ScrapedAt = DateTime.UtcNow
                            };
                            await _unmatchedLeagueRepo.CreateAsync(unmatchedLeague);
                        }
                        leaguesUnmatched++;
                    }
                }
            }

            _logger.LogInformation("Leagues phase complete: {New} new mappings, {Updated} updated, {Unmatched} unmatched",
                leaguesNew, leaguesUpdated, leaguesUnmatched);

            // Update job as completed
            syncJob.Status = SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                countriesNew,
                countriesUpdated,
                countriesMapped = processedCountries.Count,
                leaguesNew,
                leaguesUpdated,
                leaguesUnmatched
            });
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("Combined scan completed for Betano. Countries: {CountriesNew}+{CountriesUpdated}, Leagues: {LeaguesNew}+{LeaguesUpdated}, Unmatched: {Unmatched}",
                countriesNew, countriesUpdated, leaguesNew, leaguesUpdated, leaguesUnmatched);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Combined scan failed for provider {ProviderId}", providerId);
            syncJob.Status = SyncJobStatus.Failed;
            syncJob.ErrorMessage = ex.Message;
            syncJob.CompletedAt = DateTime.UtcNow;
            await _syncJobRepo.UpdateAsync(syncJob);
            throw;
        }
    }

    // Helper methods
    private (int startYear, int? endYear) ParseSeasonName(string seasonName)
    {
        // Examples: "2024-2025", "2024", "2024/2025"
        if (string.IsNullOrWhiteSpace(seasonName))
        {
            return (DateTime.UtcNow.Year, null);
        }

        // Try parsing formats like "2024-2025" or "2024/2025"
        var parts = seasonName.Split(new[] { '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
        {
            return (start, end);
        }

        // Try single year format
        if (int.TryParse(seasonName, out int year))
        {
            return (year, year);
        }

        // Fallback to current year
        return (DateTime.UtcNow.Year, null);
    }

    private bool IsCurrentSeason(int startYear, int? endYear)
    {
        var now = DateTime.UtcNow;
        var currentYear = now.Year;

        // If it's a split season (e.g., 2024-2025), check if we're in that range
        if (endYear.HasValue && startYear != endYear.Value)
        {
            // Season is current if current year is start year or end year
            return currentYear == startYear || currentYear == endYear.Value;
        }

        // For single year seasons, check if it's current year
        return currentYear == startYear;
    }

    /// <summary>
    /// Helper class for deserialized ScanCapabilities JSON
    /// </summary>
    private class ScanCapabilitiesDto
    {
        public bool CanScanCountries { get; set; } = true;
        public bool CanScanLeagues { get; set; } = true;
        public bool CanScanSeasons { get; set; } = true;
    }

    /// <summary>
    /// Parses the ScanCapabilities JSON string from DataProvider.
    /// Returns null if the string is empty or invalid.
    /// </summary>
    private ScanCapabilitiesDto? ParseScanCapabilities(string? scanCapabilitiesJson)
    {
        if (string.IsNullOrWhiteSpace(scanCapabilitiesJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ScanCapabilitiesDto>(scanCapabilitiesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse ScanCapabilities JSON: {Json}", scanCapabilitiesJson);
            return null;
        }
    }

    /// <summary>
    /// Detects duplicate ProviderCountry entries for a provider (same CountryId, different codes)
    /// </summary>
    private async Task<List<DuplicateCountryGroup>> DetectCountryDuplicatesAsync(Guid providerId)
    {
        var allCountries = await _providerCountryRepo.GetByProviderIdAsync(providerId);

        // Group by CountryId (only matched entries)
        var duplicates = allCountries
            .Where(c => c.CountryId != null)
            .GroupBy(c => c.CountryId)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateCountryGroup
            {
                CountryId = g.Key!.Value,
                Variants = g.Select(c => new DuplicateVariant
                {
                    Id = c.Id,
                    ProviderCode = c.ProviderCode,
                    ProviderName = c.ProviderName,
                    ScrapedAt = c.ScrapedAt
                }).ToList()
            })
            .ToList();

        return duplicates;
    }

    private record DuplicateCountryGroup
    {
        public Guid CountryId { get; init; }
        public List<DuplicateVariant> Variants { get; init; } = new();
    }

    private record DuplicateVariant
    {
        public Guid Id { get; init; }
        public string ProviderCode { get; init; } = string.Empty;
        public string ProviderName { get; init; } = string.Empty;
        public DateTime? ScrapedAt { get; init; }
    }
}
