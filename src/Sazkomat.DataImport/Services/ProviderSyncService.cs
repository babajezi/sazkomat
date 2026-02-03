using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Core.Common;
using Sazkomat.DataImport.DTOs;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Helpers;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using Sazkomat.DataImport.Validators;
using System.Diagnostics;
using System.Text.Json;

namespace Sazkomat.DataImport.Services;

public class ProviderSyncService : ISyncService
{
    private readonly IDataProviderRepository _providerRepository;
    private readonly ISportRepository _sportRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly ILeagueSeasonRepository _leagueSeasonRepository;
    private readonly ICountryProviderRepository _countryProviderRepository;
    private readonly ILeagueProviderRepository _leagueProviderRepository;
    private readonly IEnumerable<ICountryScraper> _countryScrapers;
    private readonly IEnumerable<ILeagueMetadataScraper> _leagueScrapers;
    private readonly IEnumerable<ISeasonScraper> _seasonScrapers;
    private readonly ISeasonSyncService _seasonSyncService;
    private readonly ILeagueRoundValidator _roundValidator;
    private readonly ISyncJobRepository _syncJobRepository;
    private readonly ILogger<ProviderSyncService> _logger;

    private static SyncStatusResponse _currentStatus = new();
    private static readonly object _lock = new();

    public ProviderSyncService(
        IDataProviderRepository providerRepository,
        ISportRepository sportRepository,
        ICountryRepository countryRepository,
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        ILeagueSeasonRepository leagueSeasonRepository,
        ICountryProviderRepository countryProviderRepository,
        ILeagueProviderRepository leagueProviderRepository,
        IEnumerable<ICountryScraper> countryScrapers,
        IEnumerable<ILeagueMetadataScraper> leagueScrapers,
        IEnumerable<ISeasonScraper> seasonScrapers,
        ISeasonSyncService seasonSyncService,
        ILeagueRoundValidator roundValidator,
        ISyncJobRepository syncJobRepository,
        ILogger<ProviderSyncService> logger)
    {
        _providerRepository = providerRepository;
        _sportRepository = sportRepository;
        _countryRepository = countryRepository;
        _leagueRepository = leagueRepository;
        _seasonRepository = seasonRepository;
        _leagueSeasonRepository = leagueSeasonRepository;
        _countryProviderRepository = countryProviderRepository;
        _leagueProviderRepository = leagueProviderRepository;
        _countryScrapers = countryScrapers;
        _leagueScrapers = leagueScrapers;
        _seasonScrapers = seasonScrapers;
        _seasonSyncService = seasonSyncService;
        _roundValidator = roundValidator;
        _syncJobRepository = syncJobRepository;
        _logger = logger;
    }

    public async Task<Result<SyncResponse>> SyncCountriesAsync(Guid providerId, bool activateCountries = false)
    {
        lock (_lock)
        {
            if (_currentStatus.IsRunning)
            {
                return Result<SyncResponse>.Failure("Synchronization is already running");
            }
            _currentStatus = new SyncStatusResponse
            {
                IsRunning = true,
                CurrentSyncType = SyncType.Countries,
                StartedAt = DateTime.UtcNow
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var stats = new SyncStatistics();

        try
        {
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result<SyncResponse>.Failure("Provider not found");
            }

            var scraper = _countryScrapers.FirstOrDefault(s => s.CanHandle(provider));
            if (scraper == null)
            {
                return Result<SyncResponse>.Failure($"No scraper available for provider {provider.Name}");
            }

            _logger.LogInformation("Starting country synchronization for provider {Provider}", provider.Name);

            // Get all active sports
            var sports = await _sportRepository.GetAllAsync();
            var activeSports = sports.Where(s => s.IsActive).ToList();

            foreach (var sport in activeSports)
            {
                try
                {
                    _logger.LogInformation("Syncing countries for sport {Sport}", sport.Name);

                    var scrapedCountries = await scraper.ScrapeCountriesAsync(sport);
                    stats.TotalProcessed += scrapedCountries.Count;

                    foreach (var countryInfo in scrapedCountries)
                    {
                        try
                        {
                            // Skip international competitions (Champions League, etc.)
                            if (IsInternationalCompetition(countryInfo.Code, countryInfo.Name))
                            {
                                _logger.LogDebug("Skipping international competition: {Name} ({Code})",
                                    countryInfo.Name, countryInfo.Code);
                                stats.Skipped++;
                                continue;
                            }

                            // Normalize provider code to our DB code (Betano Czech → English)
                            var normalizedCode = await NormalizeCountryCodeAsync(countryInfo.Code, providerId);

                            // Try to find country by normalized code
                            var existingCountry = await _countryRepository.GetByCodeAsync(normalizedCode);

                            if (existingCountry == null)
                            {
                                // Country not found - create new country
                                existingCountry = new Country
                                {
                                    Code = normalizedCode,
                                    Name = countryInfo.Name,
                                    FlagEmoji = countryInfo.FlagEmoji ?? "🏳️",
                                    IsoCode = countryInfo.IsoCode ?? "",
                                    IsActive = activateCountries
                                };
                                await _countryRepository.AddAsync(existingCountry);
                                stats.Created++;
                                _logger.LogInformation("Created new country: {Name} ({Code})", existingCountry.Name, existingCountry.Code);

                                // Create CountryProvider mapping for newly created country
                                var newCountryProvider = new CountryProvider
                                {
                                    CountryId = existingCountry.Id,
                                    ProviderId = providerId,
                                    ProviderCode = countryInfo.ProviderCode ?? countryInfo.Code,
                                    ProviderName = countryInfo.Name,
                                    IsActive = true
                                };
                                await _countryProviderRepository.AddAsync(newCountryProvider);
                                stats.Created++;
                                _logger.LogDebug("Created country provider mapping for new country {CountryName}", existingCountry.Name);
                                continue;
                            }

                            _logger.LogDebug("Matched provider country {ProviderName} ({ProviderCode}) to DB country {CountryName} ({CountryCode})",
                                countryInfo.Name, countryInfo.Code, existingCountry.Name, existingCountry.Code);

                            // Update IsoCode if it changed or is empty
                            var needsIsoUpdate = false;
                            if (!string.IsNullOrEmpty(countryInfo.IsoCode) &&
                                existingCountry.IsoCode != countryInfo.IsoCode)
                            {
                                var oldIsoCode = existingCountry.IsoCode;
                                existingCountry.IsoCode = countryInfo.IsoCode;
                                needsIsoUpdate = true;
                                _logger.LogInformation("Updating IsoCode for country {Name}: '{OldCode}' -> '{NewCode}'",
                                    existingCountry.Name, oldIsoCode, countryInfo.IsoCode);
                            }

                            // Handle country activation and mapping creation
                            if (existingCountry.IsActive)
                            {
                                // Country is already active - create/update mapping
                                var countryProvider = await _countryProviderRepository.GetByCountryAndProviderAsync(
                                    existingCountry.Id, providerId);

                                if (countryProvider == null)
                                {
                                    countryProvider = new CountryProvider
                                    {
                                        CountryId = existingCountry.Id,
                                        ProviderId = providerId,
                                        ProviderCode = countryInfo.ProviderCode ?? countryInfo.Code,
                                        ProviderName = countryInfo.Name,
                                        IsActive = true
                                    };

                                    await _countryProviderRepository.AddAsync(countryProvider);
                                    stats.Created++; // Created mapping, not country
                                    _logger.LogDebug("Created country provider mapping for {CountryName}", existingCountry.Name);
                                }
                                else
                                {
                                    // Check if mapping data actually changed before updating
                                    var newProviderCode = countryInfo.ProviderCode ?? countryInfo.Code;
                                    bool mappingChanged =
                                        countryProvider.ProviderCode != newProviderCode ||
                                        countryProvider.ProviderName != countryInfo.Name ||
                                        countryProvider.IsActive != true;

                                    if (mappingChanged || needsIsoUpdate)
                                    {
                                        // Update mapping if it changed
                                        if (mappingChanged)
                                        {
                                            countryProvider.ProviderCode = newProviderCode;
                                            countryProvider.ProviderName = countryInfo.Name;
                                            countryProvider.IsActive = true;
                                            await _countryProviderRepository.UpdateAsync(countryProvider);
                                            _logger.LogDebug("Updated country provider mapping for {CountryName}", existingCountry.Name);
                                        }

                                        // Update IsoCode if needed
                                        if (needsIsoUpdate)
                                        {
                                            await _countryRepository.UpdateAsync(existingCountry);
                                        }

                                        // Count as updated only once even if both mapping and IsoCode changed
                                        stats.Updated++;
                                    }
                                    else
                                    {
                                        // No changes detected, skip
                                        stats.Skipped++;
                                        _logger.LogDebug("Skipped country provider mapping for {CountryName} (no changes)", existingCountry.Name);
                                    }
                                }
                            }
                            else if (activateCountries)
                            {
                                // Country is inactive but auto-activate is enabled
                                existingCountry.IsActive = true;
                                await _countryRepository.UpdateAsync(existingCountry);
                                stats.Updated++;

                                _logger.LogInformation("Activated country {CountryName} ({CountryCode}) during sync",
                                    existingCountry.Name, existingCountry.Code);

                                // Create CountryProvider mapping for newly activated country
                                var countryProvider = new CountryProvider
                                {
                                    CountryId = existingCountry.Id,
                                    ProviderId = providerId,
                                    ProviderCode = countryInfo.ProviderCode ?? countryInfo.Code,
                                    ProviderName = countryInfo.Name,
                                    IsActive = true
                                };

                                await _countryProviderRepository.AddAsync(countryProvider);
                                stats.Created++;
                                _logger.LogDebug("Created country provider mapping for activated country {CountryName}", existingCountry.Name);
                            }
                            else
                            {
                                // Country is inactive and auto-activate is disabled - skip mapping creation
                                // But still update IsoCode if needed
                                if (needsIsoUpdate)
                                {
                                    await _countryRepository.UpdateAsync(existingCountry);
                                    stats.Updated++;
                                    _logger.LogInformation("Updated IsoCode for inactive country {Name}", existingCountry.Name);
                                }
                                else
                                {
                                    _logger.LogDebug("Country {CountryName} is inactive and auto-activate is disabled, skipping mapping creation",
                                        existingCountry.Name);
                                    stats.Skipped++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            stats.Errors++;
                            stats.ErrorMessages.Add($"Error processing country {countryInfo.Name}: {ex.Message}");
                            _logger.LogError(ex, "Error processing country {Country}", countryInfo.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    stats.Errors++;
                    stats.ErrorMessages.Add($"Error syncing countries for sport {sport.Name}: {ex.Message}");
                    _logger.LogError(ex, "Error syncing countries for sport {Sport}", sport.Name);
                }
            }

            stopwatch.Stop();

            var response = new SyncResponse
            {
                Success = stats.Errors == 0,
                Message = $"Country sync completed. Processed {stats.TotalProcessed} countries.",
                Statistics = stats,
                Duration = stopwatch.Elapsed
            };

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
                _currentStatus.LastCompletedAt = DateTime.UtcNow;
                _currentStatus.LastResult = response;
            }

            _logger.LogInformation("Country sync completed: {Created} created, {Updated} updated, {Skipped} skipped, {Errors} errors",
                stats.Created, stats.Updated, stats.Skipped, stats.Errors);

            return Result<SyncResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
            }

            _logger.LogError(ex, "Fatal error during country synchronization");
            return Result<SyncResponse>.Failure($"Synchronization failed: {ex.Message}");
        }
    }

    public async Task<Result<SyncResponse>> SyncLeaguesAsync(Guid providerId, Guid? countryId = null)
    {
        lock (_lock)
        {
            if (_currentStatus.IsRunning)
            {
                return Result<SyncResponse>.Failure("Synchronization is already running");
            }
            _currentStatus = new SyncStatusResponse
            {
                IsRunning = true,
                CurrentSyncType = SyncType.Leagues,
                StartedAt = DateTime.UtcNow
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var stats = new SyncStatistics();

        try
        {
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result<SyncResponse>.Failure("Provider not found");
            }

            var scraper = _leagueScrapers.FirstOrDefault(s => s.CanHandle(provider));
            if (scraper == null)
            {
                return Result<SyncResponse>.Failure($"No scraper available for provider {provider.Name}");
            }

            _logger.LogInformation("Starting league synchronization for provider {Provider}", provider.Name);

            // Deserialize current season patterns from provider
            List<string> seasonPatterns;
            try
            {
                seasonPatterns = JsonSerializer.Deserialize<List<string>>(provider.CurrentSeasonPatterns) ?? new List<string>();
                if (seasonPatterns.Count == 0)
                {
                    _logger.LogWarning("No current season patterns configured for provider {Provider}, using empty list", provider.Name);
                }
                else
                {
                    _logger.LogInformation("Using current season patterns: {Patterns}", string.Join(", ", seasonPatterns));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize current season patterns for provider {Provider}, using empty list", provider.Name);
                seasonPatterns = new List<string>();
            }

            // Get active countries with provider mapping
            var countries = countryId.HasValue
                ? new List<Country> { (await _countryRepository.GetByIdAsync(countryId.Value))! }
                : (await _countryRepository.GetAllAsync()).Where(c => c.IsActive).ToList();

            // Get all active sports (only sync leagues for known sports)
            var sports = (await _sportRepository.GetAllAsync()).Where(s => s.IsActive).ToList();

            foreach (var sport in sports)
            {
                foreach (var country in countries)
                {
                    try
                    {
                        _logger.LogInformation("Syncing leagues for {Country} ({Sport})", country.Name, sport.Name);

                        // Use new method to scrape only current season leagues
                        var scrapedLeagues = await scraper.ScrapeLeaguesForCurrentSeasonAsync(sport, country, seasonPatterns);
                        stats.TotalProcessed += scrapedLeagues.Count;

                        foreach (var leagueMetadata in scrapedLeagues)
                        {
                            try
                            {
                                // Validate if league is round-based (not a cup competition)
                                try
                                {
                                    // Calculate previous season for validation
                                    if (seasonPatterns != null && seasonPatterns.Any())
                                    {
                                        var firstPattern = seasonPatterns.First();
                                        var previousSeason = SeasonHelper.GetPreviousSeasonPattern(firstPattern);

                                        // Validate league structure
                                        var isRoundBased = await _roundValidator.IsRoundBasedLeagueAsync(
                                            leagueMetadata.Slug,
                                            country.Code,
                                            previousSeason,
                                            providerId
                                        );

                                        if (!isRoundBased)
                                        {
                                            // Skip cup competitions
                                            _logger.LogInformation(
                                                "Skipping cup competition: {Country}/{League} (validated against season {Season})",
                                                country.Name, leagueMetadata.Name, previousSeason
                                            );
                                            stats.Skipped++;
                                            continue; // Skip this league
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Don't fail the entire sync if validation fails
                                    _logger.LogWarning(ex,
                                        "Failed to validate {League}, including it anyway",
                                        leagueMetadata.Name
                                    );
                                    // Continue with league creation/update
                                }

                                // Check if league provider mapping exists
                                var leagueProvider = await _leagueProviderRepository.GetByProviderAndSlugAsync(
                                    providerId, leagueMetadata.Slug);

                                League existingLeague;

                                if (leagueProvider == null)
                                {
                                    // No mapping exists - create new league and mapping
                                    var newLeague = new League
                                    {
                                        SportId = sport.Id,
                                        CountryId = country.Id,
                                        Name = leagueMetadata.Name,
                                        DisplayName = leagueMetadata.DisplayName,
                                        BetExplorerSlug = leagueMetadata.Slug,
                                        IsActive = true, // Activate immediately
                                        IsBettable = leagueMetadata.IsBettable,
                                        Priority = leagueMetadata.Priority
                                    };

                                    await _leagueRepository.AddAsync(newLeague);
                                    existingLeague = newLeague;
                                    stats.Created++;

                                    _logger.LogDebug("Created league: {League}", newLeague.DisplayName);

                                    // Create LeagueProvider mapping
                                    leagueProvider = new LeagueProvider
                                    {
                                        LeagueId = newLeague.Id,
                                        ProviderId = providerId,
                                        ProviderSlug = leagueMetadata.Slug,
                                        ProviderName = leagueMetadata.Name,
                                        IsActive = true
                                    };

                                    await _leagueProviderRepository.AddOrUpdateAsync(leagueProvider);
                                }
                                else
                                {
                                    // Mapping exists - check if data changed before updating
                                    existingLeague = leagueProvider.League!;

                                    bool leagueChanged =
                                        existingLeague.DisplayName != leagueMetadata.DisplayName ||
                                        existingLeague.Priority != leagueMetadata.Priority;

                                    bool mappingChanged =
                                        leagueProvider.ProviderName != leagueMetadata.Name;

                                    if (leagueChanged || mappingChanged)
                                    {
                                        // Update league if it changed
                                        if (leagueChanged)
                                        {
                                            existingLeague.DisplayName = leagueMetadata.DisplayName;
                                            existingLeague.Priority = leagueMetadata.Priority;
                                            await _leagueRepository.UpdateAsync(existingLeague);
                                            _logger.LogDebug("Updated league {LeagueName} - DisplayName or Priority changed", existingLeague.DisplayName);
                                        }

                                        // Update provider mapping if it changed
                                        if (mappingChanged)
                                        {
                                            leagueProvider.ProviderName = leagueMetadata.Name;
                                            await _leagueProviderRepository.UpdateAsync(leagueProvider);
                                            _logger.LogDebug("Updated league provider mapping for {LeagueName}", existingLeague.DisplayName);
                                        }

                                        stats.Updated++;
                                    }
                                    else
                                    {
                                        // No changes detected, skip
                                        stats.Skipped++;
                                        _logger.LogDebug("Skipped league {LeagueName} (no changes)", existingLeague.DisplayName);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                stats.Errors++;
                                stats.ErrorMessages.Add($"Error processing league {leagueMetadata.Name}: {ex.Message}");
                                _logger.LogError(ex, "Error processing league {League}", leagueMetadata.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        stats.Errors++;
                        stats.ErrorMessages.Add($"Error syncing leagues for {country.Name} ({sport.Name}): {ex.Message}");
                        _logger.LogError(ex, "Error syncing leagues for {Country} ({Sport})", country.Name, sport.Name);
                    }
                }
            }

            stopwatch.Stop();

            var response = new SyncResponse
            {
                Success = stats.Errors == 0,
                Message = $"League sync completed. Processed {stats.TotalProcessed} leagues.",
                Statistics = stats,
                Duration = stopwatch.Elapsed
            };

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
                _currentStatus.LastCompletedAt = DateTime.UtcNow;
                _currentStatus.LastResult = response;
            }

            _logger.LogInformation("League sync completed: {Created} created, {Updated} updated, {Errors} errors",
                stats.Created, stats.Updated, stats.Errors);

            return Result<SyncResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
            }

            _logger.LogError(ex, "Fatal error during league synchronization");
            return Result<SyncResponse>.Failure($"Synchronization failed: {ex.Message}");
        }
    }

    public async Task<Result<SyncResponse>> SyncSeasonsAsync(Guid providerId, Guid leagueId)
    {
        lock (_lock)
        {
            if (_currentStatus.IsRunning)
            {
                return Result<SyncResponse>.Failure("Synchronization is already running");
            }
            _currentStatus = new SyncStatusResponse
            {
                IsRunning = true,
                CurrentSyncType = SyncType.Seasons,
                StartedAt = DateTime.UtcNow
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var stats = new SyncStatistics();

        try
        {
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result<SyncResponse>.Failure("Provider not found");
            }

            var league = await _leagueRepository.GetByIdAsync(leagueId);
            if (league == null)
            {
                return Result<SyncResponse>.Failure("League not found");
            }

            var scraper = _seasonScrapers.FirstOrDefault(s => s.CanHandle(provider));
            if (scraper == null)
            {
                return Result<SyncResponse>.Failure($"No scraper available for provider {provider.Name}");
            }

            _logger.LogInformation("Starting season synchronization for league {League}", league.Name);

            var scrapedSeasons = await scraper.ScrapeAvailableSeasonsAsync(league);

            // Limit to last 3 seasons
            var currentYear = DateTime.UtcNow.Year;
            var limitedSeasons = scrapedSeasons
                .Where(seasonName =>
                {
                    var years = seasonName.Split('-');
                    if (years.Length >= 1 && int.TryParse(years[0], out int startYear))
                    {
                        return startYear >= currentYear - 3;
                    }
                    return false;
                })
                .ToList();

            stats.TotalProcessed = limitedSeasons.Count;
            _logger.LogInformation("Limited to {Count} seasons (last 3 years from {Total} available)",
                limitedSeasons.Count, scrapedSeasons.Count);

            foreach (var seasonName in limitedSeasons)
            {
                try
                {
                    // Parse season years
                    var years = seasonName.Split('-');
                    int startYear, endYear;

                    if (years.Length == 2 && int.TryParse(years[0], out startYear) && int.TryParse(years[1], out endYear))
                    {
                        // Two-year season (e.g., 2023-2024)
                    }
                    else if (years.Length == 1 && int.TryParse(years[0], out startYear))
                    {
                        // Single-year season (e.g., 2023)
                        endYear = startYear;
                    }
                    else
                    {
                        stats.Errors++;
                        stats.ErrorMessages.Add($"Invalid season format: {seasonName}");
                        continue;
                    }

                    // Check if season exists
                    var existingSeason = await _seasonRepository.GetByNameAsync(seasonName);

                    if (existingSeason == null)
                    {
                        // Create new season
                        var newSeason = new Season
                        {
                            Name = seasonName,
                            StartYear = startYear,
                            EndYear = endYear
                        };

                        await _seasonRepository.AddAsync(newSeason);
                        existingSeason = newSeason;
                        stats.Created++;

                        _logger.LogDebug("Created season: {Season}", seasonName);
                    }

                    // Create/update LeagueSeason mapping
                    var leagueSeason = await _leagueSeasonRepository.GetByLeagueAndSeasonAsync(
                        leagueId, existingSeason.Id);

                    if (leagueSeason == null)
                    {
                        leagueSeason = new LeagueSeason
                        {
                            LeagueId = leagueId,
                            SeasonId = existingSeason.Id,
                            IsAvailableOnBetExplorer = true,
                            HasData = false,
                            HasOdds = false
                        };

                        await _leagueSeasonRepository.AddAsync(leagueSeason);
                        stats.Created++;
                    }
                    else
                    {
                        // Check if data actually changed before updating
                        if (leagueSeason.IsAvailableOnBetExplorer != true)
                        {
                            leagueSeason.IsAvailableOnBetExplorer = true;
                            await _leagueSeasonRepository.UpdateAsync(leagueSeason);
                            stats.Updated++;
                            _logger.LogDebug("Updated league season {SeasonName} - IsAvailableOnBetExplorer changed to true", seasonName);
                        }
                        else
                        {
                            stats.Skipped++;
                            _logger.LogDebug("Skipped league season {SeasonName} (no changes)", seasonName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    stats.Errors++;
                    stats.ErrorMessages.Add($"Error processing season {seasonName}: {ex.Message}");
                    _logger.LogError(ex, "Error processing season {Season}", seasonName);
                }
            }

            stopwatch.Stop();

            var response = new SyncResponse
            {
                Success = stats.Errors == 0,
                Message = $"Season sync completed. Processed {stats.TotalProcessed} seasons.",
                Statistics = stats,
                Duration = stopwatch.Elapsed
            };

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
                _currentStatus.LastCompletedAt = DateTime.UtcNow;
                _currentStatus.LastResult = response;
            }

            _logger.LogInformation("Season sync completed: {Created} created, {Updated} updated, {Errors} errors",
                stats.Created, stats.Updated, stats.Errors);

            return Result<SyncResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
            }

            _logger.LogError(ex, "Fatal error during season synchronization");
            return Result<SyncResponse>.Failure($"Synchronization failed: {ex.Message}");
        }
    }

    public async Task<Result<SyncResponse>> SyncAllActiveSeasonsAsync(Guid providerId)
    {
        lock (_lock)
        {
            if (_currentStatus.IsRunning)
            {
                return Result<SyncResponse>.Failure("Synchronization is already running");
            }
            _currentStatus = new SyncStatusResponse
            {
                IsRunning = true,
                CurrentSyncType = SyncType.Seasons,
                StartedAt = DateTime.UtcNow
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var stats = new SyncStatistics();

        try
        {
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result<SyncResponse>.Failure("Provider not found");
            }

            _logger.LogInformation("Starting seasons synchronization for all active leagues");

            // Get all active leagues
            var activeLeagues = (await _leagueRepository.GetAllAsync(null, null, true))
                .Where(l => l.IsActive)
                .ToList();

            _logger.LogInformation("Found {Count} active leagues to sync seasons for", activeLeagues.Count);

            foreach (var league in activeLeagues)
            {
                try
                {
                    _logger.LogInformation("Syncing seasons for league {League}", league.DisplayName);

                    // Call the single-league sync method
                    var leagueResult = await SyncSeasonsAsync(providerId, league.Id);

                    if (leagueResult.IsSuccess && leagueResult.Value != null)
                    {
                        stats.TotalProcessed += leagueResult.Value.Statistics.TotalProcessed;
                        stats.Created += leagueResult.Value.Statistics.Created;
                        stats.Updated += leagueResult.Value.Statistics.Updated;
                        stats.Errors += leagueResult.Value.Statistics.Errors;
                        stats.ErrorMessages.AddRange(leagueResult.Value.Statistics.ErrorMessages);
                    }
                    else
                    {
                        stats.Errors++;
                        stats.ErrorMessages.Add($"Failed to sync seasons for {league.DisplayName}: {leagueResult.Error}");
                        _logger.LogWarning("Failed to sync seasons for {League}: {Error}", league.DisplayName, leagueResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    stats.Errors++;
                    stats.ErrorMessages.Add($"Error syncing seasons for {league.DisplayName}: {ex.Message}");
                    _logger.LogError(ex, "Error syncing seasons for league {League}", league.DisplayName);
                }
            }

            stopwatch.Stop();

            var response = new SyncResponse
            {
                Success = stats.Errors == 0,
                Message = $"Seasons sync completed for {activeLeagues.Count} active leagues. Processed {stats.TotalProcessed} seasons total.",
                Statistics = stats,
                Duration = stopwatch.Elapsed
            };

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
                _currentStatus.LastCompletedAt = DateTime.UtcNow;
                _currentStatus.LastResult = response;
            }

            _logger.LogInformation("Seasons sync completed: {Created} created, {Updated} updated, {Errors} errors across {Leagues} leagues",
                stats.Created, stats.Updated, stats.Errors, activeLeagues.Count);

            // Automatically detect and mark current seasons after sync
            _logger.LogInformation("Detecting and marking current seasons...");
            var detectResult = await _seasonSyncService.DetectAndMarkCurrentSeasonsAsync(providerId);
            if (!detectResult.IsSuccess)
            {
                _logger.LogWarning("Failed to detect current seasons: {Error}", detectResult.Error);
            }
            else
            {
                _logger.LogInformation("Current seasons detected successfully");
            }

            return Result<SyncResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
            }

            _logger.LogError(ex, "Fatal error during seasons synchronization");
            return Result<SyncResponse>.Failure($"Synchronization failed: {ex.Message}");
        }
    }

    public Task<SyncStatusResponse> GetSyncStatusAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_currentStatus);
        }
    }

    public void ResetSyncStatus()
    {
        lock (_lock)
        {
            _currentStatus = new SyncStatusResponse
            {
                IsRunning = false
            };
            _logger.LogInformation("Sync status reset - IsRunning cleared");
        }
    }

    private bool IsInternationalCompetition(string code, string name)
    {
        var internationalKeywords = new[] {
            "liga-mistru", "liga-mistrů", "liga mistru", "liga mistrů",
            "champions-league", "uefa-champions-league",
            "europa-league", "uefa-europa-league",
            "conference-league", "uefa-conference-league",
            "mezinarodni", "international", "world", "euro", "wc"
        };

        return internationalKeywords.Any(keyword =>
            code.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> NormalizeCountryCodeAsync(string providerCode, Guid providerId)
    {
        // Get provider to check type
        var provider = await _providerRepository.GetByIdAsync(providerId);
        if (provider == null)
            return providerCode.ToLowerInvariant();

        // Betano-specific mappings (Czech → English)
        if (provider.Code.Equals("betano", StringComparison.OrdinalIgnoreCase))
        {
            return providerCode.ToLowerInvariant() switch
            {
                "anglie" or "velka-britanie" or "velká-británie" => "england",
                "spanelsko" or "španělsko" => "spain",
                "nemecko" or "německo" => "germany",
                "italie" => "italy",
                "francie" => "france",
                "portugalsko" => "portugal",
                "nizozemsko" or "holandsko" => "netherlands",
                "belgie" => "belgium",
                "recko" or "řecko" => "greece",
                "turecko" => "turkey",
                "cesko" or "česko" or "ceska-republika" or "česká-republika" => "czech-republic",
                "slovensko" => "slovakia",
                "slovinsko" => "slovenia",
                "polsko" => "poland",
                "madarsko" or "maďarsko" => "hungary",
                "rumunsko" => "romania",
                "bulharsko" => "bulgaria",
                "chorvatsko" => "croatia",
                "srbsko" => "serbia",
                "norsko" => "norway",
                "svedsko" or "švédsko" => "sweden",
                "finsko" => "finland",
                "dansko" or "dánsko" => "denmark",
                "svycarsko" or "švýcarsko" => "switzerland",
                "rakousko" => "austria",
                "skotsko" => "scotland",
                "wales" => "wales",
                "irsko" => "ireland",
                "severni-irsko" => "northern-ireland",
                "argentina" => "argentina",
                "brazilie" or "brazílie" => "brazil",
                "usa" or "spojene-staty" => "usa",
                "mexiko" => "mexico",
                "japonsko" => "japan",
                "cina" or "čína" => "china",
                "jizni-korea" or "jižní-korea" => "south-korea",
                "australia" or "austrálie" => "australia",
                _ => providerCode.ToLowerInvariant()
            };
        }

        // BetExplorer and other providers - use code as-is
        return providerCode.ToLowerInvariant();
    }

    /// <summary>
    /// BetExplorer provider ID (hardcoded from seed data)
    /// </summary>
    private static readonly Guid BetExplorerProviderId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    public async Task<Result<SyncResponse>> GlobalSeasonScanAsync(List<Guid>? leagueIds = null)
    {
        lock (_lock)
        {
            if (_currentStatus.IsRunning)
            {
                return Result<SyncResponse>.Failure("Synchronization is already running");
            }
            _currentStatus = new SyncStatusResponse
            {
                IsRunning = true,
                CurrentSyncType = SyncType.Seasons,
                StartedAt = DateTime.UtcNow
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var stats = new SyncStatistics();

        try
        {
            // Get BetExplorer provider
            var betExplorer = await _providerRepository.GetByIdAsync(BetExplorerProviderId);
            if (betExplorer == null)
            {
                return Result<SyncResponse>.Failure("BetExplorer provider not found");
            }

            // Get season scraper for BetExplorer
            var scraper = _seasonScrapers.FirstOrDefault(s => s.CanHandle(betExplorer));
            if (scraper == null)
            {
                return Result<SyncResponse>.Failure("No season scraper available for BetExplorer");
            }

            // Get leagues to scan
            List<Guid> targetLeagueIds;
            if (leagueIds != null && leagueIds.Any())
            {
                targetLeagueIds = leagueIds;
                _logger.LogInformation("Global season scan: using {Count} specified leagues", targetLeagueIds.Count);
            }
            else
            {
                // Get all leagues with at least one betting provider mapping
                targetLeagueIds = await _leagueProviderRepository.GetLeagueIdsWithBettingProviderMappingAsync();
                _logger.LogInformation("Global season scan: found {Count} leagues with betting provider mappings", targetLeagueIds.Count);
            }

            if (!targetLeagueIds.Any())
            {
                return Result<SyncResponse>.Success(new SyncResponse
                {
                    Success = true,
                    Message = "No leagues found with betting provider mappings",
                    Statistics = stats,
                    Duration = stopwatch.Elapsed
                });
            }

            // Process each league
            int processedLeagues = 0;
            foreach (var leagueId in targetLeagueIds)
            {
                try
                {
                    var league = await _leagueRepository.GetByIdAsync(leagueId);
                    if (league == null || !league.IsActive)
                    {
                        _logger.LogDebug("Skipping league {LeagueId} - not found or inactive", leagueId);
                        continue;
                    }

                    _logger.LogInformation("Scanning seasons for league {League} ({LeagueId})",
                        league.DisplayName ?? league.Name, leagueId);

                    // Scrape ALL available seasons (no year limit)
                    var scrapedSeasons = await scraper.ScrapeAvailableSeasonsAsync(league);
                    stats.TotalProcessed += scrapedSeasons.Count;

                    foreach (var seasonName in scrapedSeasons)
                    {
                        try
                        {
                            // Parse season years
                            var years = seasonName.Split('-');
                            int startYear, endYear;

                            if (years.Length == 2 && int.TryParse(years[0], out startYear) && int.TryParse(years[1], out endYear))
                            {
                                // Two-year season (e.g., 2023-2024)
                            }
                            else if (years.Length == 1 && int.TryParse(years[0], out startYear))
                            {
                                // Single-year season (e.g., 2023)
                                endYear = startYear;
                            }
                            else
                            {
                                stats.Errors++;
                                stats.ErrorMessages.Add($"Invalid season format: {seasonName}");
                                continue;
                            }

                            // Get or create Season
                            var existingSeason = await _seasonRepository.GetByNameAsync(seasonName);
                            if (existingSeason == null)
                            {
                                var newSeason = new Season
                                {
                                    Name = seasonName,
                                    StartYear = startYear,
                                    EndYear = endYear
                                };
                                await _seasonRepository.AddAsync(newSeason);
                                existingSeason = newSeason;
                                stats.Created++;
                                _logger.LogDebug("Created season: {Season}", seasonName);
                            }

                            // Get or create LeagueSeason mapping
                            var leagueSeason = await _leagueSeasonRepository.GetByLeagueAndSeasonAsync(
                                leagueId, existingSeason.Id);

                            if (leagueSeason == null)
                            {
                                // Determine if this is a current season
                                var isCurrent = IsCurrentSeason(startYear, endYear);

                                leagueSeason = new LeagueSeason
                                {
                                    LeagueId = leagueId,
                                    SeasonId = existingSeason.Id,
                                    IsAvailableOnBetExplorer = true,
                                    HasData = false,
                                    HasOdds = false,
                                    IsCurrent = isCurrent,
                                    SyncMode = isCurrent ? SyncMode.Current : SyncMode.Historical
                                };
                                await _leagueSeasonRepository.AddAsync(leagueSeason);
                                stats.Created++;
                            }
                            else if (!leagueSeason.IsAvailableOnBetExplorer)
                            {
                                leagueSeason.IsAvailableOnBetExplorer = true;
                                await _leagueSeasonRepository.UpdateAsync(leagueSeason);
                                stats.Updated++;
                                _logger.LogDebug("Updated league season {SeasonName} - IsAvailableOnBetExplorer set to true", seasonName);
                            }
                            else
                            {
                                stats.Skipped++;
                            }
                        }
                        catch (Exception ex)
                        {
                            stats.Errors++;
                            stats.ErrorMessages.Add($"Error processing season {seasonName} for league {league.Name}: {ex.Message}");
                            _logger.LogError(ex, "Error processing season {Season} for league {League}", seasonName, league.Name);
                        }
                    }

                    processedLeagues++;
                }
                catch (Exception ex)
                {
                    stats.Errors++;
                    stats.ErrorMessages.Add($"Error processing league {leagueId}: {ex.Message}");
                    _logger.LogError(ex, "Error processing league {LeagueId}", leagueId);
                }
            }

            stopwatch.Stop();

            var response = new SyncResponse
            {
                Success = stats.Errors == 0,
                Message = $"Global season scan completed. Processed {processedLeagues} leagues, found {stats.TotalProcessed} seasons.",
                Statistics = stats,
                Duration = stopwatch.Elapsed
            };

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
                _currentStatus.LastCompletedAt = DateTime.UtcNow;
                _currentStatus.LastResult = response;
            }

            _logger.LogInformation("Global season scan completed: {Leagues} leagues, {Created} created, {Updated} updated, {Skipped} skipped, {Errors} errors",
                processedLeagues, stats.Created, stats.Updated, stats.Skipped, stats.Errors);

            return Result<SyncResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            lock (_lock)
            {
                _currentStatus.IsRunning = false;
            }

            _logger.LogError(ex, "Fatal error during global season scan");
            return Result<SyncResponse>.Failure($"Global season scan failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes season data (rounds, matches) for all seasons of a specific league.
    /// Historical seasons with HasData=true are automatically skipped unless forceUpdate is true.
    /// Fail-fast: stops on first error.
    /// </summary>
    public async Task<Result<Guid>> SyncLeagueSeasonDataAsync(Guid leagueId, bool forceUpdate = false)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate league exists
            var league = await _leagueRepository.GetByIdAsync(leagueId);
            if (league == null)
            {
                return Result<Guid>.Failure("League not found");
            }

            // Check if league has betting provider mapping
            var leagueProviders = await _leagueProviderRepository.GetByLeagueIdAsync(leagueId);
            var hasBettingProvider = leagueProviders.Any(lp =>
                lp.Provider?.Type == ProviderType.BettingProvider && lp.IsActive);

            if (!hasBettingProvider)
            {
                return Result<Guid>.Failure("League does not have active betting provider mapping");
            }

            // Create SyncJob for tracking
            var syncJob = new SyncJob
            {
                Id = Guid.NewGuid(),
                ProviderId = BetExplorerProviderId,
                Type = SyncJobType.Scan,
                EntityType = SyncEntityType.Seasons,
                Status = SyncJobStatus.Running,
                StartedAt = DateTime.UtcNow,
                LeagueIds = new List<Guid> { leagueId }
            };
            await _syncJobRepository.CreateAsync(syncJob);

            _logger.LogInformation("Starting season data sync for league {LeagueName} ({LeagueId}), JobId: {JobId}",
                league.DisplayName, leagueId, syncJob.Id);

            // Get all seasons for this league
            var leagueSeasons = await _leagueSeasonRepository.GetByLeagueIdAsync(leagueId, includeRelations: true);
            if (!leagueSeasons.Any())
            {
                syncJob.Status = SyncJobStatus.Failed;
                syncJob.CompletedAt = DateTime.UtcNow;
                syncJob.ErrorMessage = "No seasons found for this league. Run 'Refresh seznam' first.";
                await _syncJobRepository.UpdateAsync(syncJob);
                return Result<Guid>.Failure("No seasons found for this league. Run 'Refresh seznam' first.");
            }

            var stats = new SyncStatistics();
            var processedSeasons = 0;

            // Filter only completed historical seasons
            // - Skip current seasons (incomplete rounds)
            // - Skip future seasons (haven't started yet)
            var currentYear = DateTime.UtcNow.Year;
            var completedSeasons = leagueSeasons
                .Where(ls => ls.SyncMode == SyncMode.Historical
                             && ls.Season != null
                             && (ls.Season.EndYear ?? ls.Season.StartYear) < currentYear)
                .OrderByDescending(ls => ls.Season?.StartYear)
                .ToList();

            // Log skipped seasons
            var skippedSeasons = leagueSeasons.Except(completedSeasons).ToList();
            if (skippedSeasons.Any())
            {
                _logger.LogInformation("Skipping {Count} season(s) (current/future): {Seasons}",
                    skippedSeasons.Count,
                    string.Join(", ", skippedSeasons.Select(s => s.Season?.Name ?? "unknown")));
            }

            if (!completedSeasons.Any())
            {
                _logger.LogInformation("No completed historical seasons to sync for league {LeagueName}",
                    league.DisplayName);
                // Mark job as completed with 0 items
                syncJob.Status = SyncJobStatus.Completed;
                syncJob.CompletedAt = DateTime.UtcNow;
                syncJob.ProgressData = JsonSerializer.Serialize(new { total = 0, processed = 0, message = "No completed historical seasons to sync" });
                await _syncJobRepository.UpdateAsync(syncJob);
                return Result<Guid>.Success(syncJob.Id);
            }

            var random = new Random();
            var isFirstSeason = true;

            foreach (var leagueSeason in completedSeasons)
            {
                try
                {
                    // Random delay between seasons (1-2 seconds) to avoid Playwright resource contention
                    if (!isFirstSeason)
                    {
                        var delayMs = random.Next(1000, 2001);
                        _logger.LogDebug("Waiting {DelayMs}ms before next season sync", delayMs);
                        await Task.Delay(delayMs);
                    }
                    isFirstSeason = false;

                    _logger.LogInformation("Syncing season {SeasonName} for {LeagueName}",
                        leagueSeason.Season?.Name ?? "unknown", league.DisplayName);

                    // Call existing SyncSeasonDataAsync which handles:
                    // - Skipping historical seasons with HasData=true (unless forceUpdate)
                    // - Setting HasData=true after successful sync
                    var result = await _seasonSyncService.SyncSeasonDataAsync(
                        BetExplorerProviderId,
                        leagueId,
                        leagueSeason.SeasonId,
                        forceUpdate: forceUpdate);

                    if (result.IsSuccess && result.Value != null)
                    {
                        stats.TotalProcessed++;
                        stats.Created += result.Value.Statistics.Created;
                        stats.Updated += result.Value.Statistics.Updated;
                        stats.Skipped += result.Value.Statistics.Skipped;
                        processedSeasons++;

                        _logger.LogInformation("Season {SeasonName} synced: {Created} created, {Updated} updated, {Skipped} skipped",
                            leagueSeason.Season?.Name,
                            result.Value.Statistics.Created,
                            result.Value.Statistics.Updated,
                            result.Value.Statistics.Skipped);
                    }
                    else
                    {
                        // FAIL-FAST: Stop on first error
                        stats.Errors++;
                        var errorMsg = result.Error ?? "Unknown error";
                        stats.ErrorMessages.Add($"{leagueSeason.Season?.Name}: {errorMsg}");

                        _logger.LogError("Failed to sync season {SeasonName}: {Error}. Stopping sync (fail-fast).",
                            leagueSeason.Season?.Name, errorMsg);

                        // Mark job as failed
                        syncJob.Status = SyncJobStatus.Failed;
                        syncJob.CompletedAt = DateTime.UtcNow;
                        syncJob.ErrorMessage = errorMsg;
                        await _syncJobRepository.UpdateAsync(syncJob);

                        stopwatch.Stop();
                        return Result<Guid>.Failure($"Season sync failed at {leagueSeason.Season?.Name}: {errorMsg}");
                    }
                }
                catch (Exception ex)
                {
                    // FAIL-FAST: Stop on first exception
                    stats.Errors++;
                    stats.ErrorMessages.Add($"{leagueSeason.Season?.Name}: {ex.Message}");

                    _logger.LogError(ex, "Exception syncing season {SeasonName}. Stopping sync (fail-fast).",
                        leagueSeason.Season?.Name);

                    // Mark job as failed
                    syncJob.Status = SyncJobStatus.Failed;
                    syncJob.CompletedAt = DateTime.UtcNow;
                    syncJob.ErrorMessage = ex.Message;
                    await _syncJobRepository.UpdateAsync(syncJob);

                    stopwatch.Stop();
                    return Result<Guid>.Failure($"Season sync failed at {leagueSeason.Season?.Name}: {ex.Message}");
                }
            }

            stopwatch.Stop();

            // Mark job as completed
            syncJob.Status = SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                total = completedSeasons.Count,
                processed = processedSeasons,
                created = stats.Created,
                updated = stats.Updated,
                skipped = stats.Skipped,
                errors = stats.Errors
            });
            await _syncJobRepository.UpdateAsync(syncJob);

            _logger.LogInformation(
                "League season data sync completed for {LeagueName}: {Processed} seasons, {Created} rounds created, {Updated} updated, {Skipped} skipped in {Duration}",
                league.DisplayName, processedSeasons, stats.Created, stats.Updated, stats.Skipped, stopwatch.Elapsed);

            return Result<Guid>.Success(syncJob.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Fatal error during league season data sync for {LeagueId}", leagueId);
            return Result<Guid>.Failure($"League season data sync failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes the list of available seasons for a league from BetExplorer.
    /// Only creates LeagueSeason entries, does not sync rounds/matches.
    /// </summary>
    public async Task<Result<Guid>> RefreshLeagueSeasonsListAsync(Guid leagueId)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate league exists
            var league = await _leagueRepository.GetByIdAsync(leagueId);
            if (league == null)
            {
                return Result<Guid>.Failure("League not found");
            }

            // Check if league has betting provider mapping
            var leagueProviders = await _leagueProviderRepository.GetByLeagueIdAsync(leagueId);
            var hasBettingProvider = leagueProviders.Any(lp =>
                lp.Provider?.Type == ProviderType.BettingProvider && lp.IsActive);

            if (!hasBettingProvider)
            {
                return Result<Guid>.Failure("League does not have active betting provider mapping");
            }

            _logger.LogInformation("Starting seasons list refresh for league {LeagueName} ({LeagueId})",
                league.DisplayName, leagueId);

            // Get BetExplorer provider and scraper
            var betExplorer = await _providerRepository.GetByIdAsync(BetExplorerProviderId);
            if (betExplorer == null)
            {
                return Result<Guid>.Failure("BetExplorer provider not found");
            }

            var scraper = _seasonScrapers.FirstOrDefault(s => s.CanHandle(betExplorer));
            if (scraper == null)
            {
                return Result<Guid>.Failure("No season scraper available for BetExplorer");
            }

            // Scrape available seasons (no year limit)
            var scrapedSeasons = await scraper.ScrapeAvailableSeasonsAsync(league);

            _logger.LogInformation("Found {Count} seasons for {LeagueName}: {Seasons}",
                scrapedSeasons.Count, league.DisplayName, string.Join(", ", scrapedSeasons.Take(5)));

            var stats = new SyncStatistics();

            foreach (var seasonName in scrapedSeasons)
            {
                try
                {
                    // Parse season years
                    var years = seasonName.Split('-');
                    int startYear, endYear;

                    if (years.Length == 2 && int.TryParse(years[0], out startYear) && int.TryParse(years[1], out endYear))
                    {
                        // Format: 2023-2024
                    }
                    else if (years.Length == 1 && int.TryParse(years[0], out startYear))
                    {
                        // Format: 2024 (single year for MLS, Allsvenskan, etc.)
                        endYear = startYear;
                    }
                    else
                    {
                        _logger.LogWarning("Could not parse season name: {SeasonName}", seasonName);
                        stats.Errors++;
                        stats.ErrorMessages.Add($"Invalid season format: {seasonName}");
                        continue;
                    }

                    stats.TotalProcessed++;

                    // Get or create Season entity
                    var existingSeason = await _seasonRepository.GetByNameAsync(seasonName);
                    if (existingSeason == null)
                    {
                        existingSeason = new Season
                        {
                            Name = seasonName,
                            StartYear = startYear,
                            EndYear = endYear
                        };
                        await _seasonRepository.AddAsync(existingSeason);
                        _logger.LogDebug("Created new season: {SeasonName}", seasonName);
                    }

                    // Get or create LeagueSeason mapping
                    var leagueSeason = await _leagueSeasonRepository.GetByLeagueAndSeasonAsync(leagueId, existingSeason.Id);
                    if (leagueSeason == null)
                    {
                        leagueSeason = new LeagueSeason
                        {
                            LeagueId = leagueId,
                            SeasonId = existingSeason.Id,
                            IsAvailableOnBetExplorer = true,
                            HasData = false,
                            HasOdds = false
                        };
                        await _leagueSeasonRepository.AddAsync(leagueSeason);
                        stats.Created++;
                        _logger.LogDebug("Created league season mapping for {SeasonName}", seasonName);
                    }
                    else if (!leagueSeason.IsAvailableOnBetExplorer)
                    {
                        leagueSeason.IsAvailableOnBetExplorer = true;
                        await _leagueSeasonRepository.UpdateAsync(leagueSeason);
                        stats.Updated++;
                        _logger.LogDebug("Updated league season {SeasonName} - IsAvailableOnBetExplorer set to true", seasonName);
                    }
                    else
                    {
                        stats.Skipped++;
                    }
                }
                catch (Exception ex)
                {
                    // FAIL-FAST: Stop on first error
                    stats.Errors++;
                    stats.ErrorMessages.Add($"{seasonName}: {ex.Message}");

                    _logger.LogError(ex, "Failed to process season {SeasonName}. Stopping refresh (fail-fast).", seasonName);

                    stopwatch.Stop();
                    return Result<Guid>.Failure($"Season list refresh failed at {seasonName}: {ex.Message}");
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Seasons list refresh completed for {LeagueName}: {Total} processed, {Created} created, {Updated} updated, {Skipped} skipped in {Duration}",
                league.DisplayName, stats.TotalProcessed, stats.Created, stats.Updated, stats.Skipped, stopwatch.Elapsed);

            // Return a dummy GUID since we're not creating a SyncJob for now
            return Result<Guid>.Success(Guid.NewGuid());
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Fatal error during seasons list refresh for {LeagueId}", leagueId);
            return Result<Guid>.Failure($"Seasons list refresh failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines if a season is current based on year.
    /// A season is current if the current year falls within its range.
    /// </summary>
    private bool IsCurrentSeason(int startYear, int endYear)
    {
        var currentYear = DateTime.UtcNow.Year;

        // If it's a split season (e.g., 2025-2026), check if we're in that range
        if (startYear != endYear)
        {
            return currentYear == startYear || currentYear == endYear;
        }

        // For single year seasons (e.g., 2026), check if it's current year
        return currentYear == startYear;
    }
}
