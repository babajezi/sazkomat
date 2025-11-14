using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;

namespace Sazkomat.Api.Endpoints;

public static class ProviderCacheEndpoints
{
    public static void MapProviderCacheEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/provider-cache")
            .WithTags("Provider Cache")
            .WithOpenApi();

        // Get cached countries
        group.MapGet("/countries", async (
            [FromQuery] Guid providerId,
            DataImportDbContext context) =>
        {
            var countries = await context.ProviderCountries
                .Where(pc => pc.ProviderId == providerId)
                .OrderByDescending(pc => pc.ScrapedAt)
                .Select(pc => new
                {
                    id = pc.Id.ToString(),
                    providerId = pc.ProviderId.ToString(),
                    providerCode = pc.ProviderCode,
                    providerName = pc.ProviderName,
                    isoCode = pc.IsoCode,
                    flagEmoji = pc.FlagEmoji,
                    data = pc.RawData,
                    scannedAt = pc.ScrapedAt,
                    createdAt = pc.CreatedAt,
                    isImported = pc.IsImported,
                    countryId = pc.CountryId != null ? pc.CountryId.ToString() : null,
                    importedAt = pc.ImportedAt
                })
                .ToListAsync();

            return Results.Ok(countries);
        })
        .WithName("GetCachedCountries")
        .Produces(200);

        // Get cached leagues
        group.MapGet("/leagues", async (
            [FromQuery] Guid providerId,
            DataImportDbContext context) =>
        {
            var leagues = await context.ProviderLeagues
                .Where(pl => pl.ProviderId == providerId)
                .OrderByDescending(pl => pl.ScrapedAt)
                .Select(pl => new
                {
                    id = pl.Id.ToString(),
                    providerId = pl.ProviderId.ToString(),
                    providerCountryId = pl.ProviderCountryId != null ? pl.ProviderCountryId.ToString() : null,
                    countryCode = pl.CountryCode ?? (pl.ProviderCountry != null ? pl.ProviderCountry.ProviderCode : null),
                    providerSlug = pl.ProviderSlug,
                    providerName = pl.ProviderName,
                    displayName = pl.DisplayName,
                    sportCode = "football", // Default for now, can be enhanced later
                    priority = pl.Priority,
                    isBettable = pl.IsBettable,
                    mappingStatus = pl.MappingStatus.ToString(),
                    data = pl.RawData,
                    scannedAt = pl.ScrapedAt,
                    createdAt = pl.CreatedAt,
                    isImported = pl.IsImported,
                    leagueId = pl.LeagueId != null ? pl.LeagueId.ToString() : null,
                    importedAt = pl.ImportedAt
                })
                .ToListAsync();

            return Results.Ok(leagues);
        })
        .WithName("GetCachedLeagues")
        .Produces(200);

        // Get cached seasons
        group.MapGet("/seasons", async (
            [FromQuery] Guid providerId,
            DataImportDbContext context) =>
        {
            var seasons = await context.ProviderSeasons
                .Where(ps => ps.ProviderId == providerId)
                .OrderByDescending(ps => ps.ScrapedAt)
                .Select(ps => new
                {
                    id = ps.Id.ToString(),
                    providerId = ps.ProviderId.ToString(),
                    providerLeagueId = ps.ProviderLeagueId.ToString(),
                    providerLeagueSlug = ps.ProviderLeague.ProviderSlug,
                    seasonName = ps.SeasonName,
                    startYear = ps.StartYear,
                    endYear = ps.EndYear,
                    isCurrentSeason = ps.IsCurrentSeason,
                    data = ps.RawData,
                    scannedAt = ps.ScrapedAt,
                    createdAt = ps.CreatedAt,
                    isImported = ps.IsImported,
                    seasonId = ps.SeasonId != null ? ps.SeasonId.ToString() : null,
                    importedAt = ps.ImportedAt
                })
                .ToListAsync();

            return Results.Ok(seasons);
        })
        .WithName("GetCachedSeasons")
        .Produces(200);

        // Delete cached countries
        group.MapDelete("/countries", async (
            [FromBody] DeleteCacheRequest request,
            DataImportDbContext context) =>
        {
            var ids = request.Ids.Select(Guid.Parse).ToList();
            var countries = await context.ProviderCountries
                .Where(pc => ids.Contains(pc.Id))
                .ToListAsync();

            if (!countries.Any())
            {
                return Results.NotFound(new { error = "No countries found to delete" });
            }

            context.ProviderCountries.RemoveRange(countries);
            await context.SaveChangesAsync();

            return Results.Ok(new { deleted = countries.Count, message = $"Deleted {countries.Count} countries from cache" });
        })
        .WithName("DeleteCachedCountries")
        .Produces(200)
        .Produces(404);

        // Delete cached leagues
        group.MapDelete("/leagues", async (
            [FromBody] DeleteCacheRequest request,
            DataImportDbContext context) =>
        {
            var ids = request.Ids.Select(Guid.Parse).ToList();
            var leagues = await context.ProviderLeagues
                .Where(pl => ids.Contains(pl.Id))
                .ToListAsync();

            if (!leagues.Any())
            {
                return Results.NotFound(new { error = "No leagues found to delete" });
            }

            context.ProviderLeagues.RemoveRange(leagues);
            await context.SaveChangesAsync();

            return Results.Ok(new { deleted = leagues.Count, message = $"Deleted {leagues.Count} leagues from cache" });
        })
        .WithName("DeleteCachedLeagues")
        .Produces(200)
        .Produces(404);

        // Delete cached seasons
        group.MapDelete("/seasons", async (
            [FromBody] DeleteCacheRequest request,
            DataImportDbContext context) =>
        {
            var ids = request.Ids.Select(Guid.Parse).ToList();
            var seasons = await context.ProviderSeasons
                .Where(ps => ids.Contains(ps.Id))
                .ToListAsync();

            if (!seasons.Any())
            {
                return Results.NotFound(new { error = "No seasons found to delete" });
            }

            context.ProviderSeasons.RemoveRange(seasons);
            await context.SaveChangesAsync();

            return Results.Ok(new { deleted = seasons.Count, message = $"Deleted {seasons.Count} seasons from cache" });
        })
        .WithName("DeleteCachedSeasons")
        .Produces(200)
        .Produces(404);

        // Apply manual mapping to a league
        group.MapPatch("/leagues/{id}/apply-mapping", async (
            Guid id,
            [FromBody] ApplyMappingRequest request,
            DataImportDbContext context) =>
        {
            // Get the ProviderLeague
            var providerLeague = await context.ProviderLeagues
                .Include(pl => pl.Provider)
                .FirstOrDefaultAsync(pl => pl.Id == id);

            if (providerLeague == null)
            {
                return Results.NotFound(new { error = $"ProviderLeague {id} not found" });
            }

            // Get the LeagueNameMapping
            var mapping = await context.LeagueNameMappings
                .FirstOrDefaultAsync(m => m.Id == Guid.Parse(request.MappingId));

            if (mapping == null)
            {
                return Results.NotFound(new { error = $"Mapping {request.MappingId} not found" });
            }

            // Validate mapping matches provider and league
            if (!mapping.ProviderCode.Equals(providerLeague.Provider.Code, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = $"Mapping provider '{mapping.ProviderCode}' does not match league provider '{providerLeague.Provider.Code}'" });
            }

            if (!string.IsNullOrEmpty(providerLeague.CountryCode) &&
                !mapping.CountryCode.Equals(providerLeague.CountryCode, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = $"Mapping country '{mapping.CountryCode}' does not match league country '{providerLeague.CountryCode}'" });
            }

            // Apply the mapping
            providerLeague.ProviderSlug = mapping.BetExplorerSlug;
            providerLeague.MappingStatus = DataImport.Entities.MappingStatus.ManualMapped;

            await context.SaveChangesAsync();

            return Results.Ok(new
            {
                id = providerLeague.Id.ToString(),
                providerSlug = providerLeague.ProviderSlug,
                mappingStatus = providerLeague.MappingStatus.ToString(),
                message = $"Manual mapping applied: {providerLeague.ProviderName} → {mapping.BetExplorerSlug}"
            });
        })
        .WithName("ApplyManualMapping")
        .Produces(200)
        .Produces(400)
        .Produces(404);
    }
}

// Request DTO for delete operations
public record DeleteCacheRequest(List<string> Ids);

// Request DTO for apply mapping operation
public record ApplyMappingRequest(string MappingId);
