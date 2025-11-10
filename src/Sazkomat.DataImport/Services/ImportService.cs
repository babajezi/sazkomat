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

    public async Task<Guid> ImportCountriesAsync(Guid providerId, List<Guid> providerCountryIds)
    {
        _logger.LogInformation("Starting country import for provider {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // Validate providerCountryIds not empty
        if (providerCountryIds == null || !providerCountryIds.Any())
        {
            throw new ArgumentException("No provider country IDs provided", nameof(providerCountryIds));
        }

        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Running,
            StartedAt = DateTime.UtcNow,
            CountryIds = providerCountryIds,
            Priority = 1
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

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
                        country.Id, providerId);

                    if (existingMapping == null)
                    {
                        var countryProvider = new CountryProvider
                        {
                            CountryId = country.Id,
                            ProviderId = providerId,
                            ProviderCode = providerCountry.ProviderCode,
                            ProviderName = providerCountry.ProviderName,
                            IsActive = true,
                            Metadata = providerCountry.RawData
                        };
                        await _countryProviderRepo.AddAsync(countryProvider);
                        _logger.LogInformation("Created CountryProvider mapping for Country {CountryId} and Provider {ProviderId}",
                            country.Id, providerId);
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

            // Update sync job as completed
            syncJob.Status = errorCount > 0 ? SyncJobStatus.Failed : SyncJobStatus.Completed;
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

            return syncJob.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country import failed");
            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;
            await _syncJobRepo.UpdateAsync(syncJob);
            throw;
        }
    }

    public async Task<Guid> ImportLeaguesAsync(Guid providerId, List<Guid> providerLeagueIds)
    {
        _logger.LogInformation("Starting league import for provider {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // Validate providerLeagueIds not empty
        if (providerLeagueIds == null || !providerLeagueIds.Any())
        {
            throw new ArgumentException("No provider league IDs provided", nameof(providerLeagueIds));
        }

        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Running,
            StartedAt = DateTime.UtcNow,
            LeagueIds = providerLeagueIds,
            Priority = 2
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        try
        {
            // Get default sport (Football)
            var sports = await _sportRepo.GetAllAsync();
            var defaultSport = sports.FirstOrDefault(s => s.Name == "Football") ?? sports.First();

            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

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
                        // Betting providers: country should be in league mapping or determined from league name
                        // This is handled after finding the league below
                        // For now, skip explicit country lookup
                    }

                    // For betting providers, import is not supported - they use cache only
                    // Only BetExplorer (scrapers) should use import workflow
                    if (!providerLeague.ProviderCountryId.HasValue || country == null)
                    {
                        _logger.LogWarning("Skipping ProviderLeague {LeagueId} - Import is only supported for scraper providers (BetExplorer), not betting providers",
                            providerLeagueId);
                        skippedCount++;
                        errors.Add($"Betting provider leagues cannot be imported - use cache workflow instead");
                        continue;
                    }

                    // Check if League with same ProviderSlug already exists (via LeagueProvider mapping)
                    var existingLeagueProvider = await _leagueProviderRepo.GetByProviderAndSlugAsync(
                        providerId, providerLeague.ProviderSlug);

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
                            Notes = $"Imported from provider {provider.Name}"
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
                            ProviderId = providerId,
                            ProviderSlug = providerLeague.ProviderSlug,
                            ProviderName = providerLeague.ProviderName,
                            IsActive = true,
                            Metadata = providerLeague.RawData
                        };
                        await _leagueProviderRepo.AddAsync(leagueProvider);
                        _logger.LogInformation("Created LeagueProvider mapping for League {LeagueId} and Provider {ProviderId}",
                            league.Id, providerId);
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

            // Update sync job
            syncJob.Status = errorCount > 0 ? SyncJobStatus.Failed : SyncJobStatus.Completed;
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

            return syncJob.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "League import failed for provider {ProviderId}", providerId);
            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;
            await _syncJobRepo.UpdateAsync(syncJob);
            throw;
        }
    }

    public async Task<Guid> ImportSeasonsAsync(Guid providerId, List<Guid> providerSeasonIds)
    {
        _logger.LogInformation("Starting season import for provider {ProviderId}", providerId);

        // Validate provider exists
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // Validate providerSeasonIds not empty
        if (providerSeasonIds == null || !providerSeasonIds.Any())
        {
            throw new ArgumentException("No provider season IDs provided", nameof(providerSeasonIds));
        }

        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Seasons,
            Status = SyncJobStatus.Running,
            StartedAt = DateTime.UtcNow,
            SeasonIds = providerSeasonIds,
            Priority = 3
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

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

            // Update sync job
            syncJob.Status = errorCount > 0 ? SyncJobStatus.Failed : SyncJobStatus.Completed;
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

            return syncJob.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Season import failed for provider {ProviderId}", providerId);
            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;
            await _syncJobRepo.UpdateAsync(syncJob);
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
