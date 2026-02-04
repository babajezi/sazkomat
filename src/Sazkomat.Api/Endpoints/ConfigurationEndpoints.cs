using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;
using Sazkomat.BettingProviders.Services;
using Sazkomat.Configuration.Entities;
using System.Text.Json;

namespace Sazkomat.Api.Endpoints;

public static class ConfigurationEndpoints
{
    public static void MapConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config")
            .WithTags("Configuration")
            .WithOpenApi();

        // GET /api/config/sports
        group.MapGet("/sports", async (Sazkomat.Configuration.Data.ConfigurationDbContext dbContext) =>
        {
            var sports = await dbContext.Sports
                .Include(s => s.SportProviders)
                    .ThenInclude(sp => sp.Provider)
                .ToListAsync();

            // Map to DTO to avoid circular reference issues
            var sportDtos = sports.Select(s => new
            {
                s.Id,
                s.Name,
                s.Code,
                s.IsActive,
                s.CreatedAt,
                s.UpdatedAt,
                s.Leagues,
                SportProviders = s.SportProviders.Select(sp => new
                {
                    sp.Id,
                    sp.ProviderId,
                    sp.ProviderCode,
                    sp.IsActive,
                    sp.Metadata,
                    Provider = sp.Provider == null ? null : new
                    {
                        sp.Provider.Id,
                        sp.Provider.Name,
                        sp.Provider.Code,
                        sp.Provider.Type
                    }
                }).ToList()
            }).ToList();

            return Results.Ok(sportDtos);
        })
        .WithName("GetSports")
        .Produces(200);

        // PATCH /api/config/sports/{id}
        group.MapPatch("/sports/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSportRequest request,
            IConfigurationService service) =>
        {
            var result = await service.UpdateSportAsync(id, request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(result.Value);
        })
        .WithName("UpdateSport")
        .Produces(200)
        .Produces(400);

        // GET /api/config/countries
        group.MapGet("/countries", async (
            ICountryRepository repository,
            [FromQuery] Guid? sportId) =>
        {
            var countries = await repository.GetAllAsync();

            // Optional: Filter by sport if provided
            // This would require a join with leagues, skipping for simplicity

            return Results.Ok(countries);
        })
        .WithName("GetCountries")
        .Produces(200);

        // POST /api/config/countries
        group.MapPost("/countries", async (
            [FromBody] CreateCountryRequest request,
            IConfigurationService service) =>
        {
            var result = await service.CreateCountryAsync(request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Created($"/api/config/countries/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateCountry")
        .Produces(201)
        .Produces(400);

        // PATCH /api/config/countries/{id}
        group.MapPatch("/countries/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCountryRequest request,
            IConfigurationService service) =>
        {
            var result = await service.UpdateCountryAsync(id, request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(result.Value);
        })
        .WithName("UpdateCountry")
        .Produces(200)
        .Produces(400);

        // DELETE /api/config/countries/{id}
        group.MapDelete("/countries/{id:guid}", async (
            Guid id,
            IConfigurationService service) =>
        {
            var result = await service.DeleteCountryAsync(id);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.NoContent();
        })
        .WithName("DeleteCountry")
        .Produces(204)
        .Produces(400);

        // PATCH /api/config/countries/{countryId}/providers/{providerId}
        group.MapPatch("/countries/{countryId:guid}/providers/{providerId:guid}", async (
            Guid countryId,
            Guid providerId,
            [FromBody] ToggleProviderSyncRequest request,
            IConfigurationService service) =>
        {
            var result = await service.ToggleCountryProviderSyncAsync(countryId, providerId, request.IsActive);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(result.Value);
        })
        .WithName("ToggleCountryProviderSync")
        .Produces(200)
        .Produces(400);

        // GET /api/config/leagues
        group.MapGet("/leagues", async (
            ILeagueRepository repository,
            [FromQuery] Guid? sportId,
            [FromQuery] Guid? countryId,
            [FromQuery] bool? onlyEnabled,
            [FromQuery] bool includeRelations = true) =>
        {
            var leagues = await repository.GetAllAsync(sportId, countryId, onlyEnabled, includeRelations);
            return Results.Ok(leagues);
        })
        .WithName("GetLeagues")
        .Produces(200);

        // POST /api/config/leagues
        group.MapPost("/leagues", async (
            [FromBody] CreateLeagueRequest request,
            IConfigurationService service) =>
        {
            var result = await service.CreateLeagueAsync(request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Created($"/api/config/leagues/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateLeague")
        .Produces(201)
        .Produces(400);

        // PATCH /api/config/leagues/{id}
        group.MapPatch("/leagues/{id:guid}", async (
            Guid id,
            [FromBody] UpdateLeagueRequest request,
            IConfigurationService service) =>
        {
            var result = await service.UpdateLeagueAsync(id, request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(result.Value);
        })
        .WithName("UpdateLeague")
        .Produces(200)
        .Produces(400);

        // DELETE /api/config/leagues/{id}
        group.MapDelete("/leagues/{id:guid}", async (
            Guid id,
            IConfigurationService service) =>
        {
            var result = await service.DeleteLeagueAsync(id);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.NoContent();
        })
        .WithName("DeleteLeague")
        .Produces(204)
        .Produces(400);

        // PATCH /api/config/leagues/{leagueId}/providers/{providerId}
        group.MapPatch("/leagues/{leagueId:guid}/providers/{providerId:guid}", async (
            Guid leagueId,
            Guid providerId,
            [FromBody] ToggleProviderSyncRequest request,
            IConfigurationService service) =>
        {
            var result = await service.ToggleLeagueProviderSyncAsync(leagueId, providerId, request.IsActive);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(result.Value);
        })
        .WithName("ToggleLeagueProviderSync")
        .Produces(200)
        .Produces(400);

        // ===== LEAGUE VALIDATION & LOCKING =====

        // POST /api/config/leagues/{leagueId}/validate
        group.MapPost("/leagues/{leagueId:guid}/validate", async (
            Guid leagueId,
            ILeagueSeasonValidationService validationService) =>
        {
            var result = await validationService.ValidateLeagueAsync(leagueId);

            return Results.Ok(new
            {
                totalSeasons = result.TotalSeasons,
                validSeasons = result.ValidSeasons,
                seasonsWithWarnings = result.SeasonsWithWarnings,
                seasonsWithErrors = result.SeasonsWithErrors,
                canLockCount = result.CanLockCount,
                alreadyLockedCount = result.AlreadyLockedCount,
                seasonResults = result.SeasonResults.Select(sr => new
                {
                    seasonId = sr.SeasonId,
                    seasonName = sr.SeasonName,
                    isValid = sr.IsValid,
                    canBeLocked = sr.CanBeLocked,
                    issues = sr.Issues.Select(i => new
                    {
                        code = i.Code,
                        message = i.Message,
                        severity = i.Severity.ToString()
                    })
                })
            });
        })
        .WithName("ValidateLeague")
        .WithSummary("Validate all historical seasons for a league")
        .Produces(200);

        // POST /api/config/leagues/{leagueId}/lock
        group.MapPost("/leagues/{leagueId:guid}/lock", async (
            Guid leagueId,
            ILeagueSeasonValidationService validationService) =>
        {
            var lockedCount = await validationService.LockValidSeasonsAsync(leagueId);

            return Results.Ok(new
            {
                message = $"Locked {lockedCount} seasons",
                lockedCount
            });
        })
        .WithName("LockLeagueSeasons")
        .WithSummary("Lock all valid historical seasons for a league")
        .Produces(200);

        // POST /api/config/leagues/{leagueId}/unlock
        group.MapPost("/leagues/{leagueId:guid}/unlock", async (
            Guid leagueId,
            ILeagueSeasonValidationService validationService) =>
        {
            var unlockedCount = await validationService.UnlockAllSeasonsAsync(leagueId);

            return Results.Ok(new
            {
                message = $"Unlocked {unlockedCount} seasons",
                unlockedCount
            });
        })
        .WithName("UnlockLeagueSeasons")
        .WithSummary("Unlock all locked seasons for a league")
        .Produces(200);

        // ===== PROVIDER MAPPING MANAGEMENT =====

        // DELETE /api/config/country-providers/by-provider
        group.MapDelete("/country-providers/by-provider", async (
            [FromBody] DeleteByProviderRequest request,
            ICountryProviderRepository repository,
            Sazkomat.DataImport.Data.DataImportDbContext dataImportContext) =>
        {
            var deleted = await repository.DeleteByProviderAsync(request.ProviderId);

            // Also reset provider_countries.is_imported flag so they can be re-imported
            var resetCount = await dataImportContext.ProviderCountries
                .Where(pc => pc.ProviderId == request.ProviderId && pc.IsImported)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(pc => pc.IsImported, false)
                    .SetProperty(pc => pc.ImportedAt, (DateTime?)null));

            return Results.Ok(new
            {
                deleted,
                resetProviderCountries = resetCount,
                message = $"Deleted {deleted} country provider mappings, reset {resetCount} provider_countries"
            });
        })
        .WithName("DeleteCountryProvidersByProvider")
        .WithDescription("Delete all CountryProvider mappings for a specific provider")
        .Produces(200);

        // DELETE /api/config/league-providers/by-provider
        group.MapDelete("/league-providers/by-provider", async (
            [FromBody] DeleteByProviderRequest request,
            ILeagueProviderRepository repository,
            Sazkomat.DataImport.Data.DataImportDbContext dataImportContext) =>
        {
            var deleted = await repository.DeleteByProviderAsync(request.ProviderId);

            // Also reset provider_leagues.is_imported flag so they can be re-imported
            var resetCount = await dataImportContext.ProviderLeagues
                .Where(pl => pl.ProviderId == request.ProviderId && pl.IsImported)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(pl => pl.IsImported, false)
                    .SetProperty(pl => pl.ImportedAt, (DateTime?)null));

            return Results.Ok(new
            {
                deleted,
                resetProviderLeagues = resetCount,
                message = $"Deleted {deleted} league provider mappings, reset {resetCount} provider_leagues"
            });
        })
        .WithName("DeleteLeagueProvidersByProvider")
        .WithDescription("Delete all LeagueProvider mappings for a specific provider")
        .Produces(200);

        // ===== BETTING PROVIDERS ENDPOINTS =====

        // GET /api/config/providers/betting
        group.MapGet("/providers/betting", async (IDataProviderRepository repository) =>
        {
            var providers = await repository.GetAllAsync();
            var bettingProviders = providers.Where(p => p.Type == ProviderType.BettingProvider && p.IsActive);
            return Results.Ok(bettingProviders);
        })
        .WithName("GetBettingProviders")
        .Produces(200);

        // PATCH /api/config/providers/{providerId}/credentials
        group.MapPatch("/providers/{providerId:guid}/credentials", async (
            Guid providerId,
            [FromBody] UpdateProviderCredentialsRequest request,
            IProviderService service) =>
        {
            var result = await service.UpdateProviderCredentialsAsync(providerId, request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(result.Value);
        })
        .WithName("UpdateProviderCredentials")
        .Produces(200)
        .Produces(400);

        // PATCH /api/config/providers/{providerId}/configuration
        group.MapPatch("/providers/{providerId:guid}/configuration", async (
            Guid providerId,
            [FromBody] UpdateProviderConfigurationRequest request,
            IProviderService service) =>
        {
            var result = await service.UpdateProviderConfigurationAsync(providerId, request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(result.Value);
        })
        .WithName("UpdateProviderConfiguration")
        .Produces(200)
        .Produces(400);

        // GET /api/config/providers/{providerId}/sync-status
        group.MapGet("/providers/{providerId:guid}/sync-status", async (
            Guid providerId,
            [FromQuery] string? sportCode,
            BettingProviderOrchestrator orchestrator,
            IDataProviderRepository providerRepository) =>
        {
            var provider = await providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Results.NotFound(new { error = "Provider not found" });
            }

            var status = await orchestrator.GetSyncStatusAsync(provider.Code, sportCode);
            return Results.Ok(new { providerCode = provider.Code, sportCode, status });
        })
        .WithName("GetProviderSyncStatus")
        .Produces(200)
        .Produces(404);

        // POST /api/config/providers/{providerId}/sync-leagues
        group.MapPost("/providers/{providerId:guid}/sync-leagues", async (
            Guid providerId,
            [FromBody] SyncLeaguesRequest request,
            BettingProviderOrchestrator orchestrator,
            IDataProviderRepository providerRepository) =>
        {
            // Validate provider exists and is betting provider
            var provider = await providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Results.NotFound(new { error = "Provider not found" });
            }

            if (provider.Type != ProviderType.BettingProvider)
            {
                return Results.BadRequest(new { error = "Provider is not a betting provider" });
            }

            // Execute sync
            var result = await orchestrator.SyncLeagueAvailabilityAsync(provider.Code, request.SportCode);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(new { message = "League sync completed successfully" });
        })
        .WithName("SyncBettingProviderLeagues")
        .Produces(200)
        .Produces(400)
        .Produces(404);

        // POST /api/config/providers/auto-enable-betexplorer
        group.MapPost("/providers/auto-enable-betexplorer", async (
            BettingProviderOrchestrator orchestrator) =>
        {
            var result = await orchestrator.AutoEnableBetExplorerSyncAsync();

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(new { message = "BetExplorer auto-enable completed successfully" });
        })
        .WithName("AutoEnableBetExplorerSync")
        .Produces(200)
        .Produces(400);

        // GET /api/config/leagues/{leagueId}/betting-availability
        group.MapGet("/leagues/{leagueId:guid}/betting-availability", async (
            Guid leagueId,
            ILeagueProviderRepository leagueProviderRepository) =>
        {
            var providerMappings = await leagueProviderRepository.GetByLeagueIdAsync(leagueId);
            var bettingProviders = providerMappings
                .Where(m => m.Provider.Type == ProviderType.BettingProvider)
                .Select(m => new
                {
                    m.Provider.Id,
                    m.Provider.Name,
                    m.Provider.Code,
                    m.IsActive,
                    m.ProviderName,
                    m.ProviderSlug
                });

            return Results.Ok(bettingProviders);
        })
        .WithName("GetLeagueBettingAvailability")
        .Produces(200);

        // GET /api/config/sports/{sportId}/providers
        group.MapGet("/sports/{sportId:guid}/providers", async (
            Guid sportId,
            ISportRepository sportRepository) =>
        {
            var sport = await sportRepository.GetByIdAsync(sportId);
            if (sport == null)
                return Results.NotFound(new { error = $"Sport with ID {sportId} not found" });

            return Results.Ok(sport.SportProviders);
        })
        .WithName("GetSportProviders")
        .Produces(200)
        .Produces(404);

        // POST /api/config/sports/{sportId}/providers
        group.MapPost("/sports/{sportId:guid}/providers", async (
            Guid sportId,
            [FromBody] CreateSportProviderRequest request,
            ISportRepository sportRepository,
            IDataProviderRepository providerRepository,
            Sazkomat.Configuration.Data.ConfigurationDbContext dbContext) =>
        {
            // Validate sport exists
            var sport = await sportRepository.GetByIdAsync(sportId);
            if (sport == null)
                return Results.NotFound(new { error = $"Sport with ID {sportId} not found" });

            // Validate provider exists
            var provider = await providerRepository.GetByIdAsync(request.ProviderId);
            if (provider == null)
                return Results.NotFound(new { error = $"Provider with ID {request.ProviderId} not found" });

            // Check if mapping already exists
            var existing = await dbContext.SportProviders
                .FirstOrDefaultAsync(sp => sp.SportId == sportId && sp.ProviderId == request.ProviderId);
            if (existing != null)
                return Results.BadRequest(new { error = "Mapping already exists for this provider" });

            // Create new mapping directly in DbContext
            var sportProvider = new SportProvider
            {
                Id = Guid.NewGuid(),
                SportId = sportId,
                ProviderId = request.ProviderId,
                ProviderCode = request.ProviderCode,
                IsActive = request.IsActive ?? true,
                Metadata = request.Metadata
            };

            dbContext.SportProviders.Add(sportProvider);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/api/config/sports/{sportId}/providers/{request.ProviderId}", sportProvider);
        })
        .WithName("CreateSportProvider")
        .Produces(201)
        .Produces(400)
        .Produces(404);

        // PATCH /api/config/sports/{sportId}/providers/{providerId}
        group.MapPatch("/sports/{sportId:guid}/providers/{providerId:guid}", async (
            Guid sportId,
            Guid providerId,
            [FromBody] UpdateSportProviderRequest request,
            Sazkomat.Configuration.Data.ConfigurationDbContext dbContext) =>
        {
            var mapping = await dbContext.SportProviders
                .FirstOrDefaultAsync(sp => sp.SportId == sportId && sp.ProviderId == providerId);

            if (mapping == null)
                return Results.NotFound(new { error = "Mapping not found" });

            // Update mapping
            if (request.ProviderCode != null)
                mapping.ProviderCode = request.ProviderCode;
            if (request.IsActive.HasValue)
                mapping.IsActive = request.IsActive.Value;
            if (request.Metadata != null)
                mapping.Metadata = request.Metadata;

            await dbContext.SaveChangesAsync();

            return Results.Ok(mapping);
        })
        .WithName("UpdateSportProvider")
        .Produces(200)
        .Produces(404);

        // DELETE /api/config/sports/{sportId}/providers/{providerId}
        group.MapDelete("/sports/{sportId:guid}/providers/{providerId:guid}", async (
            Guid sportId,
            Guid providerId,
            Sazkomat.Configuration.Data.ConfigurationDbContext dbContext) =>
        {
            var mapping = await dbContext.SportProviders
                .FirstOrDefaultAsync(sp => sp.SportId == sportId && sp.ProviderId == providerId);

            if (mapping == null)
                return Results.NotFound(new { error = "Mapping not found" });

            dbContext.SportProviders.Remove(mapping);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { message = "Sport-Provider mapping deleted successfully" });
        })
        .WithName("DeleteSportProvider")
        .Produces(200)
        .Produces(404);

        // ===== LOG SETTINGS ENDPOINTS =====

        // GET /api/config/log-settings
        group.MapGet("/log-settings", async (ILogSettingsRepository repository) =>
        {
            var settings = await repository.GetAllAsync();
            return Results.Ok(settings);
        })
        .WithName("GetLogSettings")
        .Produces(200);

        // GET /api/config/log-settings/{id}
        group.MapGet("/log-settings/{id:guid}", async (
            Guid id,
            ILogSettingsRepository repository) =>
        {
            var setting = await repository.GetByIdAsync(id);
            if (setting == null)
                return Results.NotFound(new { error = "Log settings not found" });

            return Results.Ok(setting);
        })
        .WithName("GetLogSettingById")
        .Produces(200)
        .Produces(404);

        // POST /api/config/log-settings
        group.MapPost("/log-settings", async (
            [FromBody] CreateLogSettingsRequest request,
            ILogSettingsRepository repository) =>
        {
            // Check if settings for this category/subcategory already exist
            var existing = await repository.GetByCategoryAndSubCategoryAsync(
                request.Category, request.SubCategory);

            if (existing != null)
                return Results.BadRequest(new { error = "Log settings for this category/subcategory already exist" });

            var logSettings = new LogSettings
            {
                Id = Guid.NewGuid(),
                Category = request.Category,
                SubCategory = request.SubCategory,
                LogPath = request.LogPath,
                LogLevel = request.LogLevel,
                IsEnabled = request.IsEnabled,
                RetentionDays = request.RetentionDays,
                MaxFileSizeBytes = request.MaxFileSizeBytes,
                OutputTemplate = request.OutputTemplate,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await repository.CreateAsync(logSettings);
            return Results.Created($"/api/config/log-settings/{logSettings.Id}", logSettings);
        })
        .WithName("CreateLogSettings")
        .Produces(201)
        .Produces(400);

        // PATCH /api/config/log-settings/{id}
        group.MapPatch("/log-settings/{id:guid}", async (
            Guid id,
            [FromBody] UpdateLogSettingsRequest request,
            ILogSettingsRepository repository) =>
        {
            var logSettings = await repository.GetByIdAsync(id);
            if (logSettings == null)
                return Results.NotFound(new { error = "Log settings not found" });

            // Update only provided fields
            if (request.LogPath != null)
                logSettings.LogPath = request.LogPath;
            if (request.LogLevel != null)
                logSettings.LogLevel = request.LogLevel;
            if (request.IsEnabled.HasValue)
                logSettings.IsEnabled = request.IsEnabled.Value;
            if (request.RetentionDays.HasValue)
                logSettings.RetentionDays = request.RetentionDays.Value;
            if (request.MaxFileSizeBytes.HasValue)
                logSettings.MaxFileSizeBytes = request.MaxFileSizeBytes.Value;
            if (request.OutputTemplate != null)
                logSettings.OutputTemplate = request.OutputTemplate;
            if (request.Description != null)
                logSettings.Description = request.Description;

            await repository.UpdateAsync(logSettings);
            return Results.Ok(logSettings);
        })
        .WithName("UpdateLogSettings")
        .Produces(200)
        .Produces(404);

        // DELETE /api/config/log-settings/{id}
        group.MapDelete("/log-settings/{id:guid}", async (
            Guid id,
            ILogSettingsRepository repository) =>
        {
            var logSettings = await repository.GetByIdAsync(id);
            if (logSettings == null)
                return Results.NotFound(new { error = "Log settings not found" });

            await repository.DeleteAsync(id);
            return Results.NoContent();
        })
        .WithName("DeleteLogSettings")
        .Produces(204)
        .Produces(404);
    }
}

// DTOs
public record SyncLeaguesRequest(string SportCode);

public record CreateSportProviderRequest(
    Guid ProviderId,
    string ProviderCode,
    bool? IsActive = true,
    string? Metadata = null);

public record UpdateSportProviderRequest(
    string? ProviderCode = null,
    bool? IsActive = null,
    string? Metadata = null);

public record DeleteByProviderRequest(Guid ProviderId);
