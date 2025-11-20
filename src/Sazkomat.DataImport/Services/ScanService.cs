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
    private readonly ICountryNameMappingRepository _countryNameMappingRepo;
    private readonly IEnumerable<ICountryScraper> _countryScrapers;
    private readonly IEnumerable<ILeagueMetadataScraper> _leagueScrapers;
    private readonly IEnumerable<ISeasonScraper> _seasonScrapers;
    private readonly IBetExplorerEnrichmentService _enrichmentService;
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
        ICountryNameMappingRepository countryNameMappingRepo,
        IEnumerable<ICountryScraper> countryScrapers,
        IEnumerable<ILeagueMetadataScraper> leagueScrapers,
        IEnumerable<ISeasonScraper> seasonScrapers,
        IBetExplorerEnrichmentService enrichmentService,
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
        _countryNameMappingRepo = countryNameMappingRepo;
        _countryScrapers = countryScrapers;
        _leagueScrapers = leagueScrapers;
        _seasonScrapers = seasonScrapers;
        _enrichmentService = enrichmentService;
        _logger = logger;
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

            // Scrape countries from provider
            var scrapedCountries = await countryScraper.ScrapeCountriesAsync(defaultSport);
            _logger.LogInformation("Scraped {Count} countries from provider {ProviderId}",
                scrapedCountries.Count, providerId);

            int newCount = 0;
            int updatedCount = 0;

            foreach (var country in scrapedCountries)
            {
                // Check if already exists
                var existing = await _providerCountryRepo.GetByProviderCodeAsync(providerId, country.Code);

                if (existing == null)
                {
                    // Create new
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
                    // Update existing
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

                // For BETTING PROVIDERS ONLY: Create CountryProvider mappings
                // This allows betting providers to scan leagues after country scan
                if (provider.Type == Configuration.Entities.ProviderType.BettingProvider)
                {
                    Configuration.Entities.Country? configCountry = null;

                    // STEP 1: Try manual country name mapping (highest priority)
                    var countryMapping = await _countryNameMappingRepo.FindMappingAsync(
                        provider.Code.ToLowerInvariant(),
                        country.Code);

                    if (countryMapping != null)
                    {
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
                        // Check if CountryProvider mapping already exists
                        var existingMapping = await _countryProviderRepo.GetByCountryAndProviderAsync(
                            configCountry.Id, providerId);

                        if (existingMapping == null)
                        {
                            // Create new CountryProvider mapping
                            var countryProvider = new Configuration.Entities.CountryProvider
                            {
                                CountryId = configCountry.Id,
                                ProviderId = providerId,
                                ProviderCode = country.Code,
                                ProviderName = country.Name,
                                IsActive = true
                            };
                            await _countryProviderRepo.AddAsync(countryProvider);

                            _logger.LogInformation("✓ Created CountryProvider mapping: {CountryName} ({CountryCode}) ↔ Provider {ProviderCode}",
                                configCountry.Name, configCountry.Code, provider.Code);
                        }
                        else
                        {
                            // Update existing mapping
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
                        _logger.LogWarning("⚠ No matching country found in configuration for {ProviderName} '{ProviderCode}' - mapping not created. Add manual CountryNameMapping or create country first.",
                            country.Name, country.Code);
                    }
                }
            }

            // Update sync job as completed
            syncJob.Status = SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                total = scrapedCountries.Count,
                @new = newCount,
                updated = updatedCount
            });
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("Country scan completed. New: {New}, Updated: {Updated}",
                newCount, updatedCount);
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

            if (provider.Type == Configuration.Entities.ProviderType.BettingProvider)
            {
                // Betting providers: use countries with active CountryProvider mapping
                if (countryIds != null && countryIds.Any())
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

                    // ENRICHMENT FLOW: For betting providers, enrich with BetExplorer data
                    // For BetExplorer provider, use scraped data directly
                    List<LeagueMetadata> leaguesToCache;
                    int skippedCount = 0;

                    if (provider.Code.Equals("betexplorer", StringComparison.OrdinalIgnoreCase))
                    {
                        // BetExplorer: Use scraped data directly (no enrichment needed)
                        leaguesToCache = scrapedLeagues;
                        _logger.LogDebug("Using direct BetExplorer data for {Count} leagues", scrapedLeagues.Count);
                    }
                    else
                    {
                        // Betting providers (Betano, Fortuna): Enrich with BetExplorer data
                        _logger.LogInformation("Enriching {Count} leagues from {Provider} with BetExplorer data",
                            scrapedLeagues.Count, provider.Name);

                        leaguesToCache = new List<LeagueMetadata>();
                        foreach (var league in scrapedLeagues)
                        {
                            var enriched = await _enrichmentService.EnrichLeagueAsync(league, country, provider.Code);
                            if (enriched != null)
                            {
                                leaguesToCache.Add(enriched);
                            }
                            else
                            {
                                skippedCount++;
                                _logger.LogDebug("Skipping league '{League}' - not found on BetExplorer", league.Name);
                            }
                        }

                        _logger.LogInformation("Enrichment complete: {Enriched} leagues found on BetExplorer, {Skipped} skipped",
                            leaguesToCache.Count, skippedCount);
                    }

                    // Cache the leagues (either direct BetExplorer or enriched betting provider data)
                    foreach (var league in leaguesToCache)
                    {
                        // Check if already exists
                        var existing = await _providerLeagueRepo.GetByProviderSlugAsync(providerId, league.Slug);

                        if (existing == null)
                        {
                            // Create new
                            var providerLeague = new ProviderLeague
                            {
                                ProviderId = providerId,
                                ProviderCountryId = null,  // Betting providers don't have ProviderCountry
                                ProviderSlug = league.Slug,
                                ProviderName = league.Name,
                                DisplayName = league.DisplayName,
                                CountryCode = league.CountryCode ?? country.Code,
                                Priority = league.Priority,
                                IsBettable = league.IsBettable,
                                ScrapedAt = DateTime.UtcNow,
                                RawData = JsonSerializer.Serialize(league),
                                IsImported = false
                            };
                            await _providerLeagueRepo.CreateAsync(providerLeague);
                            _logger.LogInformation("✓ Added league to cache: {Name} [{Country}]",
                                league.DisplayName ?? league.Name, country.Name);
                            newCount++;
                        }
                        else
                        {
                            // Update existing
                            existing.ProviderName = league.Name;
                            existing.DisplayName = league.DisplayName;
                            existing.CountryCode = league.CountryCode ?? country.Code;
                            existing.Priority = league.Priority;
                            existing.IsBettable = league.IsBettable;
                            existing.ScrapedAt = DateTime.UtcNow;
                            existing.RawData = JsonSerializer.Serialize(league);
                            await _providerLeagueRepo.UpdateAsync(existing);
                            _logger.LogInformation("↻ Updated league in cache: {Name} [{Country}]",
                                league.DisplayName ?? league.Name, country.Name);
                            updatedCount++;
                        }
                    }

                    // Auto-activate country if leagues were found for betting providers
                    // Countries start inactive and are activated when betting providers have leagues in them
                    if (provider.Type == Configuration.Entities.ProviderType.BettingProvider && leaguesToCache.Any())
                    {
                        if (!country.IsActive)
                        {
                            country.IsActive = true;
                            await _countryRepo.UpdateAsync(country);
                            _logger.LogInformation("Auto-activated country {CountryId} ({CountryName}) - betting provider {Provider} has {Count} leagues",
                                country.Id, country.Name, provider.Name, leaguesToCache.Count);

                            // Create CountryProvider mapping for newly activated country
                            // This ensures the country will be scanned in future league scans
                            var existingMapping = await _countryProviderRepo.GetByCountryAndProviderAsync(country.Id, providerId);
                            if (existingMapping == null)
                            {
                                var countryProvider = new Configuration.Entities.CountryProvider
                                {
                                    CountryId = country.Id,
                                    ProviderId = providerId,
                                    ProviderCode = country.Code,  // For betting providers, use standard country code
                                    ProviderName = country.Name,  // For betting providers, use standard country name
                                    IsActive = true
                                };
                                await _countryProviderRepo.AddAsync(countryProvider);
                                _logger.LogInformation("Created CountryProvider mapping for auto-activated country {CountryId} ({CountryName}) and provider {ProviderId} ({ProviderName})",
                                    country.Id, country.Name, providerId, provider.Name);
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

            // Update sync job as completed
            syncJob.Status = SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                countries = countriesToScan.Count,
                total = totalScraped,
                @new = newCount,
                updated = updatedCount
            });
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("League scan completed. New: {New}, Updated: {Updated}",
                newCount, updatedCount);
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

            // Get provider leagues to scan (need corresponding League entities)
            List<ProviderLeague> providerLeagues;
            if (leagueIds != null && leagueIds.Any())
            {
                providerLeagues = new List<ProviderLeague>();
                foreach (var leagueId in leagueIds)
                {
                    var pl = await _providerLeagueRepo.GetByIdAsync(leagueId);
                    if (pl != null) providerLeagues.Add(pl);
                }
            }
            else
            {
                providerLeagues = await _providerLeagueRepo.GetByProviderIdAsync(providerId);
            }

            int newCount = 0;
            int updatedCount = 0;
            int totalScraped = 0;

            foreach (var providerLeague in providerLeagues)
            {
                try
                {
                    // Need to get the corresponding League entity from Configuration schema
                    Configuration.Entities.League? league = null;
                    if (providerLeague.LeagueId.HasValue)
                    {
                        league = await _leagueRepo.GetByIdAsync(providerLeague.LeagueId.Value);
                    }
                    else if (!string.IsNullOrEmpty(providerLeague.ProviderSlug))
                    {
                        // Try to find by slug using LeagueProvider mapping
                        var allLeagues = await _leagueRepo.GetAllAsync();
                        // This is simplified - in real scenario we'd query LeagueProvider table
                        league = allLeagues.FirstOrDefault(l =>
                            !string.IsNullOrEmpty(l.BetExplorerSlug) &&
                            l.BetExplorerSlug == providerLeague.ProviderSlug);
                    }

                    if (league == null)
                    {
                        _logger.LogWarning("No matching League found for ProviderLeague {ProviderLeagueId} ({LeagueName})",
                            providerLeague.Id, providerLeague.ProviderName);
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
                    _logger.LogWarning(ex, "Failed to scrape seasons for league {LeagueSlug}",
                        providerLeague.ProviderSlug);
                }
            }

            // Update sync job as completed
            syncJob.Status = SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                leagues = providerLeagues.Count,
                total = totalScraped,
                @new = newCount,
                updated = updatedCount
            });
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("Season scan completed. New: {New}, Updated: {Updated}",
                newCount, updatedCount);
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
}
