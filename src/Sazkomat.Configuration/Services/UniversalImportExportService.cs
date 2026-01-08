using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

/// <summary>
/// Universal service for importing and exporting configuration entities
/// </summary>
public class UniversalImportExportService : IUniversalImportExportService
{
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<UniversalImportExportService> _logger;

    public UniversalImportExportService(
        ConfigurationDbContext context,
        ILogger<UniversalImportExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Export

    public async Task<Result<ConfigurationExportDto>> ExportAsync(ExportOptionsDto options)
    {
        try
        {
            _logger.LogInformation("Starting configuration export");

            var export = new ConfigurationExportDto
            {
                ExportedAt = DateTime.UtcNow,
                Description = "Sazkomat configuration export"
            };

            // Validate dependencies
            var validationResult = ValidateExportDependencies(options);
            if (!validationResult.IsSuccess)
            {
                return Result<ConfigurationExportDto>.Failure(validationResult.Error);
            }

            // Export Level 1: Core entities (parallel)
            if (options.IncludeSports)
            {
                export.Sports = await ExportSportsAsync(options);
            }

            if (options.IncludeCountries)
            {
                export.Countries = await ExportCountriesAsync(options);
            }

            if (options.IncludeProviders)
            {
                export.Providers = await ExportProvidersAsync(options);
            }

            if (options.IncludeSeasons)
            {
                export.Seasons = await ExportSeasonsAsync(options);
            }

            // Export Level 2: Dependent entities
            if (options.IncludeLeagues)
            {
                export.Leagues = await ExportLeaguesAsync(options);
            }

            // Export Level 3+: Junction tables
            if (options.IncludeSportProviders)
            {
                export.SportProviders = await ExportSportProvidersAsync(options);
            }

            if (options.IncludeCountryProviders)
            {
                export.CountryProviders = await ExportCountryProvidersAsync(options);
            }

            if (options.IncludeLeagueProviders)
            {
                export.LeagueProviders = await ExportLeagueProvidersAsync(options);
            }

            if (options.IncludeLeagueSeasons)
            {
                export.LeagueSeasons = await ExportLeagueSeasonsAsync(options);
            }

            // Set metadata
            export.Metadata = BuildMetadata(export);

            _logger.LogInformation("Export completed successfully. Total entities: {Count}", export.Metadata.TotalEntities);

            return Result<ConfigurationExportDto>.Success(export);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration export");
            return Result<ConfigurationExportDto>.Failure($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<ExportMetadataDto>> GetExportPreviewAsync(ExportOptionsDto options)
    {
        try
        {
            var metadata = new ExportMetadataDto();
            var includedTypes = new List<string>();
            int totalCount = 0;

            if (options.IncludeSports)
            {
                var count = await _context.Sports.CountAsync(s => !options.OnlyActive || s.IsActive);
                totalCount += count;
                includedTypes.Add($"sports ({count})");
            }

            if (options.IncludeCountries)
            {
                var count = await _context.Countries.CountAsync(c => !options.OnlyActive || c.IsActive);
                totalCount += count;
                includedTypes.Add($"countries ({count})");
            }

            if (options.IncludeProviders)
            {
                var count = await _context.DataProviders.CountAsync(p => !options.OnlyActive || p.IsActive);
                totalCount += count;
                includedTypes.Add($"providers ({count})");
            }

            if (options.IncludeSeasons)
            {
                var count = await _context.Seasons.CountAsync();
                totalCount += count;
                includedTypes.Add($"seasons ({count})");
            }

            if (options.IncludeLeagues)
            {
                var query = _context.Leagues.AsQueryable();
                if (options.OnlyActive) query = query.Where(l => l.IsActive);
                if (options.SportIds?.Any() == true) query = query.Where(l => options.SportIds.Contains(l.SportId));
                if (options.CountryIds?.Any() == true) query = query.Where(l => options.CountryIds.Contains(l.CountryId));

                var count = await query.CountAsync();
                totalCount += count;
                includedTypes.Add($"leagues ({count})");
            }

            metadata.TotalEntities = totalCount;
            metadata.IncludedTypes = includedTypes;

            return Result<ExportMetadataDto>.Success(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting export preview");
            return Result<ExportMetadataDto>.Failure($"Preview failed: {ex.Message}");
        }
    }

    #region Export Helper Methods

    private async Task<List<SportExportDto>> ExportSportsAsync(ExportOptionsDto options)
    {
        var query = _context.Sports.AsQueryable();

        if (options.OnlyActive)
            query = query.Where(s => s.IsActive);

        return await query.Select(s => new SportExportDto
        {
            Id = s.Id,
            Name = s.Name,
            Code = s.Code,
            IsActive = s.IsActive,
            Priority = s.Priority,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToListAsync();
    }

    private async Task<List<CountryExportDto>> ExportCountriesAsync(ExportOptionsDto options)
    {
        var query = _context.Countries.AsQueryable();

        if (options.OnlyActive)
            query = query.Where(c => c.IsActive);

        return await query.Select(c => new CountryExportDto
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            FlagEmoji = c.FlagEmoji,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToListAsync();
    }

    private async Task<List<DataProviderExportDto>> ExportProvidersAsync(ExportOptionsDto options)
    {
        var query = _context.DataProviders.AsQueryable();

        if (options.OnlyActive)
            query = query.Where(p => p.IsActive);

        return await query.Select(p => new DataProviderExportDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            BaseUrl = p.BaseUrl,
            IsActive = p.IsActive,
            Priority = p.Priority,
            Type = p.Type.ToString(),
            CurrentSeasonPatterns = p.CurrentSeasonPatterns,
            Credentials = p.Credentials,
            Configuration = p.Configuration,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToListAsync();
    }

    private async Task<List<SeasonExportDto>> ExportSeasonsAsync(ExportOptionsDto options)
    {
        return await _context.Seasons.Select(s => new SeasonExportDto
        {
            Id = s.Id,
            Name = s.Name,
            StartYear = s.StartYear,
            EndYear = s.EndYear,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToListAsync();
    }

    private async Task<List<LeagueExportDto>> ExportLeaguesAsync(ExportOptionsDto options)
    {
        var query = _context.Leagues.AsQueryable();

        if (options.OnlyActive)
            query = query.Where(l => l.IsActive);

        if (options.SportIds?.Any() == true)
            query = query.Where(l => options.SportIds.Contains(l.SportId));

        if (options.CountryIds?.Any() == true)
            query = query.Where(l => options.CountryIds.Contains(l.CountryId));

        return await query.Select(l => new LeagueExportDto
        {
            Id = l.Id,
            SportId = l.SportId,
            CountryId = l.CountryId,
            Name = l.Name,
            DisplayName = l.DisplayName,
            BetExplorerSlug = l.BetExplorerSlug,
            IsBettable = l.IsBettable,
            IsActive = l.IsActive,
            Priority = l.Priority,
            Notes = l.Notes,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        }).ToListAsync();
    }

    private async Task<List<SportProviderExportDto>> ExportSportProvidersAsync(ExportOptionsDto options)
    {
        var query = _context.SportProviders.AsQueryable();

        if (options.OnlyActive)
            query = query.Where(sp => sp.IsActive);

        return await query.Select(sp => new SportProviderExportDto
        {
            Id = sp.Id,
            SportId = sp.SportId,
            ProviderId = sp.ProviderId,
            ProviderCode = sp.ProviderCode,
            IsActive = sp.IsActive,
            Metadata = sp.Metadata,
            CreatedAt = sp.CreatedAt,
            UpdatedAt = sp.UpdatedAt
        }).ToListAsync();
    }

    private async Task<List<CountryProviderExportDto>> ExportCountryProvidersAsync(ExportOptionsDto options)
    {
        var query = _context.CountryProviders.AsQueryable();

        if (options.OnlyActive)
            query = query.Where(cp => cp.IsActive);

        return await query.Select(cp => new CountryProviderExportDto
        {
            Id = cp.Id,
            CountryId = cp.CountryId,
            ProviderId = cp.ProviderId,
            ProviderCode = cp.ProviderCode,
            ProviderName = cp.ProviderName,
            IsActive = cp.IsActive,
            Metadata = cp.Metadata,
            CreatedAt = cp.CreatedAt,
            UpdatedAt = cp.UpdatedAt
        }).ToListAsync();
    }

    private async Task<List<LeagueProviderExportDto>> ExportLeagueProvidersAsync(ExportOptionsDto options)
    {
        var query = _context.LeagueProviders.AsQueryable();

        if (options.OnlyActive)
            query = query.Where(lp => lp.IsActive);

        return await query.Select(lp => new LeagueProviderExportDto
        {
            Id = lp.Id,
            LeagueId = lp.LeagueId,
            ProviderId = lp.ProviderId,
            ProviderSlug = lp.ProviderSlug,
            ProviderName = lp.ProviderName,
            IsActive = lp.IsActive,
            ProviderLeagueId = lp.ProviderLeagueId.HasValue ? lp.ProviderLeagueId.Value.ToString() : null,
            Metadata = lp.Metadata,
            CreatedAt = lp.CreatedAt,
            UpdatedAt = lp.UpdatedAt
        }).ToListAsync();
    }

    private async Task<List<LeagueSeasonExportDto>> ExportLeagueSeasonsAsync(ExportOptionsDto options)
    {
        return await _context.LeagueSeasons.Select(ls => new LeagueSeasonExportDto
        {
            Id = ls.Id,
            LeagueId = ls.LeagueId,
            SeasonId = ls.SeasonId,
            IsAvailableOnBetExplorer = ls.IsAvailableOnBetExplorer,
            HasData = ls.HasData,
            HasOdds = ls.HasOdds,
            LastScrapedAt = ls.LastScrapedAt,
            RoundsCount = ls.RoundsCount,
            MatchesCount = ls.MatchesCount,
            SyncEnabled = ls.SyncEnabled,
            IsCurrent = ls.IsCurrent,
            SyncMode = ls.SyncMode.ToString(),
            LastDataSyncAt = ls.LastDataSyncAt,
            CreatedAt = ls.CreatedAt,
            UpdatedAt = ls.UpdatedAt
        }).ToListAsync();
    }

    private Result ValidateExportDependencies(ExportOptionsDto options)
    {
        // Leagues require Sports and Countries
        if (options.IncludeLeagues && (!options.IncludeSports || !options.IncludeCountries))
        {
            return Result.Failure("Cannot export leagues without sports and countries");
        }

        // LeagueProviders require Leagues and Providers
        if (options.IncludeLeagueProviders && (!options.IncludeLeagues || !options.IncludeProviders))
        {
            return Result.Failure("Cannot export league-provider mappings without leagues and providers");
        }

        // SportProviders require Sports and Providers
        if (options.IncludeSportProviders && (!options.IncludeSports || !options.IncludeProviders))
        {
            return Result.Failure("Cannot export sport-provider mappings without sports and providers");
        }

        // CountryProviders require Countries and Providers
        if (options.IncludeCountryProviders && (!options.IncludeCountries || !options.IncludeProviders))
        {
            return Result.Failure("Cannot export country-provider mappings without countries and providers");
        }

        // LeagueSeasons require Leagues and Seasons
        if (options.IncludeLeagueSeasons && (!options.IncludeLeagues || !options.IncludeSeasons))
        {
            return Result.Failure("Cannot export league-season mappings without leagues and seasons");
        }

        return Result.Success();
    }

    private ExportMetadataDto BuildMetadata(ConfigurationExportDto export)
    {
        var metadata = new ExportMetadataDto();
        int totalCount = 0;
        var types = new List<string>();

        if (export.Sports?.Any() == true)
        {
            totalCount += export.Sports.Count;
            types.Add("sports");
        }

        if (export.Countries?.Any() == true)
        {
            totalCount += export.Countries.Count;
            types.Add("countries");
        }

        if (export.Providers?.Any() == true)
        {
            totalCount += export.Providers.Count;
            types.Add("providers");
        }

        if (export.Seasons?.Any() == true)
        {
            totalCount += export.Seasons.Count;
            types.Add("seasons");
        }

        if (export.Leagues?.Any() == true)
        {
            totalCount += export.Leagues.Count;
            types.Add("leagues");
        }

        if (export.SportProviders?.Any() == true)
        {
            totalCount += export.SportProviders.Count;
            types.Add("sportProviders");
        }

        if (export.CountryProviders?.Any() == true)
        {
            totalCount += export.CountryProviders.Count;
            types.Add("countryProviders");
        }

        if (export.LeagueProviders?.Any() == true)
        {
            totalCount += export.LeagueProviders.Count;
            types.Add("leagueProviders");
        }

        if (export.LeagueSeasons?.Any() == true)
        {
            totalCount += export.LeagueSeasons.Count;
            types.Add("leagueSeasons");
        }

        metadata.TotalEntities = totalCount;
        metadata.IncludedTypes = types;

        return metadata;
    }

    #endregion

    #endregion

    #region Import

    public async Task<Result<ImportResultDto>> ValidateImportAsync(ConfigurationExportDto data)
    {
        try
        {
            var result = new ImportResultDto { Success = true };

            // Basic schema validation
            if (data == null)
            {
                return Result<ImportResultDto>.Failure("Import data is null");
            }

            // Check for at least one entity type
            if (data.Sports?.Any() != true &&
                data.Countries?.Any() != true &&
                data.Providers?.Any() != true &&
                data.Seasons?.Any() != true &&
                data.Leagues?.Any() != true)
            {
                return Result<ImportResultDto>.Failure("No entities to import");
            }

            // Dependency validation
            if (data.Leagues?.Any() == true)
            {
                if (data.Sports?.Any() != true || data.Countries?.Any() != true)
                {
                    result.Errors.Add("Leagues require Sports and Countries to be included in import");
                }
            }

            if (data.LeagueProviders?.Any() == true)
            {
                if (data.Leagues?.Any() != true || data.Providers?.Any() != true)
                {
                    result.Errors.Add("LeagueProviders require Leagues and Providers");
                }
            }

            if (result.Errors.Any())
            {
                result.Success = false;
                return Result<ImportResultDto>.Success(result); // Return validation errors
            }

            _logger.LogInformation("Import validation successful");
            return Result<ImportResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating import");
            return Result<ImportResultDto>.Failure($"Validation failed: {ex.Message}");
        }
    }

    public async Task<Result<ImportResultDto>> ImportAsync(ConfigurationExportDto data, ImportOptionsDto options)
    {
        var result = new ImportResultDto();
        var idMapping = new Dictionary<Guid, Guid>(); // oldId -> newId mapping

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _logger.LogInformation("Starting configuration import (Mode: {Mode})", options.Mode);

            // LEVEL 1: Core entities (independent)
            if (data.Sports?.Any() == true)
            {
                result.Sports = await ImportSportsAsync(data.Sports, options, idMapping);
            }

            if (data.Countries?.Any() == true)
            {
                result.Countries = await ImportCountriesAsync(data.Countries, options, idMapping);
            }

            if (data.Providers?.Any() == true)
            {
                result.Providers = await ImportProvidersAsync(data.Providers, options, idMapping);
            }

            if (data.Seasons?.Any() == true)
            {
                result.Seasons = await ImportSeasonsAsync(data.Seasons, options, idMapping);
            }

            await _context.SaveChangesAsync();

            // LEVEL 2: Leagues (depend on Sports + Countries)
            if (data.Leagues?.Any() == true)
            {
                RemapLeagueForeignKeys(data.Leagues, idMapping);
                result.Leagues = await ImportLeaguesAsync(data.Leagues, options, idMapping);
                await _context.SaveChangesAsync();
            }

            // LEVEL 3: Junction tables
            if (data.SportProviders?.Any() == true)
            {
                RemapSportProviderForeignKeys(data.SportProviders, idMapping);
                result.SportProviders = await ImportSportProvidersAsync(data.SportProviders, options, idMapping);
            }

            if (data.CountryProviders?.Any() == true)
            {
                RemapCountryProviderForeignKeys(data.CountryProviders, idMapping);
                result.CountryProviders = await ImportCountryProvidersAsync(data.CountryProviders, options, idMapping);
            }

            await _context.SaveChangesAsync();

            // LEVEL 4: LeagueProviders
            if (data.LeagueProviders?.Any() == true)
            {
                RemapLeagueProviderForeignKeys(data.LeagueProviders, idMapping);
                result.LeagueProviders = await ImportLeagueProvidersAsync(data.LeagueProviders, options, idMapping);
                await _context.SaveChangesAsync();
            }

            // LEVEL 5: LeagueSeasons
            if (data.LeagueSeasons?.Any() == true)
            {
                RemapLeagueSeasonForeignKeys(data.LeagueSeasons, idMapping);
                result.LeagueSeasons = await ImportLeagueSeasonsAsync(data.LeagueSeasons, options, idMapping);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            result.Success = true;

            _logger.LogInformation(
                "Import completed successfully. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
                result.TotalCreated, result.TotalUpdated, result.TotalSkipped);

            return Result<ImportResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during import");

            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Errors.Add($"Import failed: {ex.Message}");

            return Result<ImportResultDto>.Success(result); // Return result with errors
        }
    }

    #region Import Helper Methods

    private async Task<EntityImportResult> ImportSportsAsync(
        List<SportExportDto> sports,
        ImportOptionsDto options,
        Dictionary<Guid, Guid> idMapping)
    {
        var result = new EntityImportResult();

        foreach (var sportDto in sports)
        {
            try
            {
                Entities.Sport? existing = null;

                if (options.Mode == ImportMode.SmartMatch)
                {
                    existing = await _context.Sports.FirstOrDefaultAsync(s => s.Code == sportDto.Code);
                }
                else // PreserveIds
                {
                    existing = await _context.Sports.FindAsync(sportDto.Id);
                }

                if (existing != null)
                {
                    idMapping[sportDto.Id] = existing.Id;

                    if (options.ConflictResolution == ConflictResolution.Update)
                    {
                        existing.Name = sportDto.Name;
                        existing.IsActive = sportDto.IsActive;
                        existing.Priority = sportDto.Priority;
                        result.Updated++;
                    }
                    else if (options.ConflictResolution == ConflictResolution.Skip)
                    {
                        result.Skipped++;
                    }
                    else // Fail
                    {
                        throw new InvalidOperationException($"Sport '{sportDto.Code}' already exists");
                    }
                }
                else
                {
                    var newSport = new Entities.Sport
                    {
                        Id = options.Mode == ImportMode.PreserveIds ? sportDto.Id : Guid.NewGuid(),
                        Name = sportDto.Name,
                        Code = sportDto.Code,
                        IsActive = sportDto.IsActive,
                        Priority = sportDto.Priority
                    };

                    idMapping[sportDto.Id] = newSport.Id;
                    _context.Sports.Add(newSport);
                    result.Created++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Sport '{sportDto.Name}': {ex.Message}");
            }
        }

        return result;
    }

    private async Task<EntityImportResult> ImportCountriesAsync(
        List<CountryExportDto> countries,
        ImportOptionsDto options,
        Dictionary<Guid, Guid> idMapping)
    {
        var result = new EntityImportResult();

        foreach (var countryDto in countries)
        {
            try
            {
                Entities.Country? existing = null;

                if (options.Mode == ImportMode.SmartMatch)
                {
                    existing = await _context.Countries.FirstOrDefaultAsync(c => c.Code == countryDto.Code);
                }
                else
                {
                    existing = await _context.Countries.FindAsync(countryDto.Id);
                }

                if (existing != null)
                {
                    idMapping[countryDto.Id] = existing.Id;

                    if (options.ConflictResolution == ConflictResolution.Update)
                    {
                        existing.Name = countryDto.Name;
                        existing.FlagEmoji = countryDto.FlagEmoji;
                        existing.IsActive = countryDto.IsActive;
                        result.Updated++;
                    }
                    else if (options.ConflictResolution == ConflictResolution.Skip)
                    {
                        result.Skipped++;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Country '{countryDto.Code}' already exists");
                    }
                }
                else
                {
                    var newCountry = new Entities.Country
                    {
                        Id = options.Mode == ImportMode.PreserveIds ? countryDto.Id : Guid.NewGuid(),
                        Name = countryDto.Name,
                        Code = countryDto.Code,
                        FlagEmoji = countryDto.FlagEmoji,
                        IsActive = countryDto.IsActive
                    };

                    idMapping[countryDto.Id] = newCountry.Id;
                    _context.Countries.Add(newCountry);
                    result.Created++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Country '{countryDto.Name}': {ex.Message}");
            }
        }

        return result;
    }

    private async Task<EntityImportResult> ImportProvidersAsync(
        List<DataProviderExportDto> providers,
        ImportOptionsDto options,
        Dictionary<Guid, Guid> idMapping)
    {
        var result = new EntityImportResult();

        foreach (var providerDto in providers)
        {
            try
            {
                Entities.DataProvider? existing = null;

                if (options.Mode == ImportMode.SmartMatch)
                {
                    existing = await _context.DataProviders.FirstOrDefaultAsync(p => p.Code == providerDto.Code);
                }
                else
                {
                    existing = await _context.DataProviders.FindAsync(providerDto.Id);
                }

                if (existing != null)
                {
                    idMapping[providerDto.Id] = existing.Id;

                    if (options.ConflictResolution == ConflictResolution.Update)
                    {
                        existing.Name = providerDto.Name;
                        existing.BaseUrl = providerDto.BaseUrl;
                        existing.IsActive = providerDto.IsActive;
                        existing.Priority = providerDto.Priority;
                        result.Updated++;
                    }
                    else if (options.ConflictResolution == ConflictResolution.Skip)
                    {
                        result.Skipped++;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Provider '{providerDto.Code}' already exists");
                    }
                }
                else
                {
                    var newProvider = new Entities.DataProvider
                    {
                        Id = options.Mode == ImportMode.PreserveIds ? providerDto.Id : Guid.NewGuid(),
                        Name = providerDto.Name,
                        Code = providerDto.Code,
                        BaseUrl = providerDto.BaseUrl,
                        IsActive = providerDto.IsActive,
                        Priority = providerDto.Priority,
                        Type = Enum.Parse<Entities.ProviderType>(providerDto.Type),
                        CurrentSeasonPatterns = providerDto.CurrentSeasonPatterns,
                        Notes = providerDto.Notes
                    };

                    idMapping[providerDto.Id] = newProvider.Id;
                    _context.DataProviders.Add(newProvider);
                    result.Created++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Provider '{providerDto.Name}': {ex.Message}");
            }
        }

        return result;
    }

    private async Task<EntityImportResult> ImportSeasonsAsync(
        List<SeasonExportDto> seasons,
        ImportOptionsDto options,
        Dictionary<Guid, Guid> idMapping)
    {
        var result = new EntityImportResult();

        foreach (var seasonDto in seasons)
        {
            try
            {
                Entities.Season? existing = null;

                if (options.Mode == ImportMode.SmartMatch)
                {
                    existing = await _context.Seasons.FirstOrDefaultAsync(s => s.Name == seasonDto.Name);
                }
                else
                {
                    existing = await _context.Seasons.FindAsync(seasonDto.Id);
                }

                if (existing != null)
                {
                    idMapping[seasonDto.Id] = existing.Id;

                    if (options.ConflictResolution == ConflictResolution.Update)
                    {
                        existing.StartYear = seasonDto.StartYear;
                        existing.EndYear = seasonDto.EndYear;
                        result.Updated++;
                    }
                    else if (options.ConflictResolution == ConflictResolution.Skip)
                    {
                        result.Skipped++;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Season '{seasonDto.Name}' already exists");
                    }
                }
                else
                {
                    var newSeason = new Entities.Season
                    {
                        Id = options.Mode == ImportMode.PreserveIds ? seasonDto.Id : Guid.NewGuid(),
                        Name = seasonDto.Name,
                        StartYear = seasonDto.StartYear,
                        EndYear = seasonDto.EndYear
                    };

                    idMapping[seasonDto.Id] = newSeason.Id;
                    _context.Seasons.Add(newSeason);
                    result.Created++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Season '{seasonDto.Name}': {ex.Message}");
            }
        }

        return result;
    }

    private async Task<EntityImportResult> ImportLeaguesAsync(
        List<LeagueExportDto> leagues,
        ImportOptionsDto options,
        Dictionary<Guid, Guid> idMapping)
    {
        var result = new EntityImportResult();

        foreach (var leagueDto in leagues)
        {
            try
            {
                Entities.League? existing = null;

                if (options.Mode == ImportMode.SmartMatch)
                {
                    existing = await _context.Leagues.FirstOrDefaultAsync(l =>
                        l.SportId == leagueDto.SportId &&
                        l.CountryId == leagueDto.CountryId &&
                        l.Name == leagueDto.Name);
                }
                else
                {
                    existing = await _context.Leagues.FindAsync(leagueDto.Id);
                }

                if (existing != null)
                {
                    idMapping[leagueDto.Id] = existing.Id;

                    if (options.ConflictResolution == ConflictResolution.Update)
                    {
                        existing.DisplayName = leagueDto.DisplayName;
                        existing.IsBettable = leagueDto.IsBettable;
                        existing.IsActive = leagueDto.IsActive;
                        existing.Priority = leagueDto.Priority;
                        result.Updated++;
                    }
                    else if (options.ConflictResolution == ConflictResolution.Skip)
                    {
                        result.Skipped++;
                    }
                    else
                    {
                        throw new InvalidOperationException($"League '{leagueDto.Name}' already exists");
                    }
                }
                else
                {
                    var newLeague = new Entities.League
                    {
                        Id = options.Mode == ImportMode.PreserveIds ? leagueDto.Id : Guid.NewGuid(),
                        SportId = leagueDto.SportId,
                        CountryId = leagueDto.CountryId,
                        Name = leagueDto.Name,
                        DisplayName = leagueDto.DisplayName,
                        BetExplorerSlug = leagueDto.BetExplorerSlug,
                        IsBettable = leagueDto.IsBettable,
                        IsActive = leagueDto.IsActive,
                        Priority = leagueDto.Priority,
                        Notes = leagueDto.Notes
                    };

                    idMapping[leagueDto.Id] = newLeague.Id;
                    _context.Leagues.Add(newLeague);
                    result.Created++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"League '{leagueDto.Name}': {ex.Message}");
            }
        }

        return result;
    }

    // Junction table imports
    private async Task<EntityImportResult> ImportSportProvidersAsync(
        List<SportProviderExportDto> items,
        ImportOptionsDto options,
        Dictionary<Guid, Guid> idMapping)
    {
        var result = new EntityImportResult();

        foreach (var item in items)
        {
            try
            {
                var existing = await _context.SportProviders.FirstOrDefaultAsync(sp =>
                    sp.SportId == item.SportId && sp.ProviderId == item.ProviderId);

                if (existing != null)
                {
                    if (options.ConflictResolution == ConflictResolution.Update)
                    {
                        existing.ProviderCode = item.ProviderCode;
                        existing.IsActive = item.IsActive;
                        existing.Metadata = item.Metadata;
                        result.Updated++;
                    }
                    else if (options.ConflictResolution == ConflictResolution.Skip)
                    {
                        result.Skipped++;
                    }
                }
                else
                {
                    var newItem = new Entities.SportProvider
                    {
                        Id = options.Mode == ImportMode.PreserveIds ? item.Id : Guid.NewGuid(),
                        SportId = item.SportId,
                        ProviderId = item.ProviderId,
                        ProviderCode = item.ProviderCode,
                        IsActive = item.IsActive,
                        Metadata = item.Metadata
                    };

                    _context.SportProviders.Add(newItem);
                    result.Created++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"SportProvider: {ex.Message}");
            }
        }

        return result;
    }

    private async Task<EntityImportResult> ImportCountryProvidersAsync(
        List<CountryProviderExportDto> items,
        ImportOptionsDto options,
        Dictionary<Guid, Guid> idMapping)
    {
        var result = new EntityImportResult();

        foreach (var item in items)
        {
            try
            {
                var existing = await _context.CountryProviders.FirstOrDefaultAsync(cp =>
                    cp.CountryId == item.CountryId && cp.ProviderId == item.ProviderId);

                if (existing != null)
                {
                    if (options.ConflictResolution == ConflictResolution.Update)
                    {
                        existing.ProviderCode = item.ProviderCode;
                        existing.ProviderName = item.ProviderName;
                        existing.IsActive = item.IsActive;
                        existing.Metadata = item.Metadata;
                        result.Updated++;
                    }
                    else if (options.ConflictResolution == ConflictResolution.Skip)
                    {
                        result.Skipped++;
                    }
                }
                else
                {
                    var newItem = new Entities.CountryProvider
                    {
                        Id = options.Mode == ImportMode.PreserveIds ? item.Id : Guid.NewGuid(),
                        CountryId = item.CountryId,
                        ProviderId = item.ProviderId,
                        ProviderCode = item.ProviderCode,
                        ProviderName = item.ProviderName,
                        IsActive = item.IsActive,
                        Metadata = item.Metadata
                    };

                    _context.CountryProviders.Add(newItem);
                    result.Created++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"CountryProvider: {ex.Message}");
            }
        }

        return result;
    }

    private async Task<EntityImportResult> ImportLeagueProvidersAsync(
        List<LeagueProviderExportDto> items,
        ImportOptionsDto options,
        Dictionary<Guid, Guid> idMapping)
    {
        var result = new EntityImportResult();

        foreach (var item in items)
        {
            try
            {
                var existing = await _context.LeagueProviders.FirstOrDefaultAsync(lp =>
                    lp.LeagueId == item.LeagueId && lp.ProviderId == item.ProviderId);

                if (existing != null)
                {
                    if (options.ConflictResolution == ConflictResolution.Update)
                    {
                        existing.ProviderSlug = item.ProviderSlug;
                        existing.ProviderName = item.ProviderName;
                        existing.IsActive = item.IsActive;
                        existing.Metadata = item.Metadata;
                        result.Updated++;
                    }
                    else if (options.ConflictResolution == ConflictResolution.Skip)
                    {
                        result.Skipped++;
                    }
                }
                else
                {
                    var newItem = new Entities.LeagueProvider
                    {
                        Id = options.Mode == ImportMode.PreserveIds ? item.Id : Guid.NewGuid(),
                        LeagueId = item.LeagueId,
                        ProviderId = item.ProviderId,
                        ProviderSlug = item.ProviderSlug,
                        ProviderName = item.ProviderName,
                        IsActive = item.IsActive,
                        ProviderLeagueId = !string.IsNullOrEmpty(item.ProviderLeagueId) ? int.Parse(item.ProviderLeagueId) : null,
                        Metadata = item.Metadata
                    };

                    _context.LeagueProviders.Add(newItem);
                    result.Created++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"LeagueProvider: {ex.Message}");
            }
        }

        return result;
    }

    private async Task<EntityImportResult> ImportLeagueSeasonsAsync(
        List<LeagueSeasonExportDto> items,
        ImportOptionsDto options,
        Dictionary<Guid, Guid> idMapping)
    {
        var result = new EntityImportResult();

        foreach (var item in items)
        {
            try
            {
                var existing = await _context.LeagueSeasons.FirstOrDefaultAsync(ls =>
                    ls.LeagueId == item.LeagueId && ls.SeasonId == item.SeasonId);

                if (existing != null)
                {
                    if (options.ConflictResolution == ConflictResolution.Update)
                    {
                        existing.SyncEnabled = item.SyncEnabled;
                        existing.IsCurrent = item.IsCurrent;
                        result.Updated++;
                    }
                    else if (options.ConflictResolution == ConflictResolution.Skip)
                    {
                        result.Skipped++;
                    }
                }
                else
                {
                    var newItem = new Entities.LeagueSeason
                    {
                        Id = options.Mode == ImportMode.PreserveIds ? item.Id : Guid.NewGuid(),
                        LeagueId = item.LeagueId,
                        SeasonId = item.SeasonId,
                        IsAvailableOnBetExplorer = item.IsAvailableOnBetExplorer,
                        HasData = item.HasData,
                        HasOdds = item.HasOdds,
                        SyncEnabled = item.SyncEnabled,
                        IsCurrent = item.IsCurrent,
                        SyncMode = Enum.Parse<Entities.SyncMode>(item.SyncMode)
                    };

                    _context.LeagueSeasons.Add(newItem);
                    result.Created++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"LeagueSeason: {ex.Message}");
            }
        }

        return result;
    }

    // FK Remapping methods
    private void RemapLeagueForeignKeys(List<LeagueExportDto> leagues, Dictionary<Guid, Guid> idMapping)
    {
        foreach (var league in leagues)
        {
            if (idMapping.ContainsKey(league.SportId))
                league.SportId = idMapping[league.SportId];
            if (idMapping.ContainsKey(league.CountryId))
                league.CountryId = idMapping[league.CountryId];
        }
    }

    private void RemapSportProviderForeignKeys(List<SportProviderExportDto> items, Dictionary<Guid, Guid> idMapping)
    {
        foreach (var item in items)
        {
            if (idMapping.ContainsKey(item.SportId))
                item.SportId = idMapping[item.SportId];
            if (idMapping.ContainsKey(item.ProviderId))
                item.ProviderId = idMapping[item.ProviderId];
        }
    }

    private void RemapCountryProviderForeignKeys(List<CountryProviderExportDto> items, Dictionary<Guid, Guid> idMapping)
    {
        foreach (var item in items)
        {
            if (idMapping.ContainsKey(item.CountryId))
                item.CountryId = idMapping[item.CountryId];
            if (idMapping.ContainsKey(item.ProviderId))
                item.ProviderId = idMapping[item.ProviderId];
        }
    }

    private void RemapLeagueProviderForeignKeys(List<LeagueProviderExportDto> items, Dictionary<Guid, Guid> idMapping)
    {
        foreach (var item in items)
        {
            if (idMapping.ContainsKey(item.LeagueId))
                item.LeagueId = idMapping[item.LeagueId];
            if (idMapping.ContainsKey(item.ProviderId))
                item.ProviderId = idMapping[item.ProviderId];
        }
    }

    private void RemapLeagueSeasonForeignKeys(List<LeagueSeasonExportDto> items, Dictionary<Guid, Guid> idMapping)
    {
        foreach (var item in items)
        {
            if (idMapping.ContainsKey(item.LeagueId))
                item.LeagueId = idMapping[item.LeagueId];
            if (idMapping.ContainsKey(item.SeasonId))
                item.SeasonId = idMapping[item.SeasonId];
        }
    }

    #endregion

    #endregion
}
