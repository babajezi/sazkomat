using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;
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
            DataImportDbContext context,
            ConfigurationDbContext configContext) =>
        {
            var currentYear = DateTime.UtcNow.Year;

            // First, load seasons with provider leagues from DataImport context
            var rawSeasons = await (
                from ps in context.ProviderSeasons
                join pl in context.ProviderLeagues on ps.ProviderLeagueId equals pl.Id into plJoin
                from pl in plJoin.DefaultIfEmpty()
                where ps.ProviderId == providerId
                // Filter out future seasons (only show current season and older)
                && ps.StartYear <= currentYear
                select new
                {
                    id = ps.Id,
                    providerId = ps.ProviderId,
                    providerLeagueId = ps.ProviderLeagueId,
                    providerSlug = pl != null ? pl.ProviderSlug : null,
                    providerLeagueName = pl != null ? (pl.DisplayName ?? pl.ProviderName) : "Unknown",
                    providerCountryCode = pl != null ? pl.CountryCode : "unknown",
                    seasonName = ps.SeasonName,
                    startYear = ps.StartYear,
                    endYear = ps.EndYear,
                    isCurrentSeason = ps.IsCurrentSeason,
                    data = ps.RawData,
                    scrapedAt = ps.ScrapedAt,
                    createdAt = ps.CreatedAt,
                    isImported = ps.IsImported,
                    seasonId = ps.SeasonId,
                    importedAt = ps.ImportedAt
                }
            ).ToListAsync();

            // Get all slugs to lookup
            var slugs = rawSeasons.Where(s => s.providerSlug != null).Select(s => s.providerSlug!).Distinct().ToList();

            // Load matching leagues from configuration context
            // Use GroupBy to handle potential duplicate slugs (e.g., "super-league" in multiple countries)
            var leaguesBySlug = await configContext.Leagues
                .Include(l => l.Country)
                .Where(l => slugs.Contains(l.BetExplorerSlug))
                .ToListAsync();

            // Group by slug and take the first match (or could be enhanced to match by country)
            var leaguesBySlugDict = leaguesBySlug
                .GroupBy(l => l.BetExplorerSlug)
                .ToDictionary(g => g.Key, g => g.First());

            // Enrich and transform
            var seasons = rawSeasons.Select(s =>
            {
                var league = s.providerSlug != null && leaguesBySlugDict.TryGetValue(s.providerSlug, out var l) ? l : null;
                var country = league?.Country;

                return new
                {
                    id = s.id.ToString(),
                    providerId = s.providerId.ToString(),
                    providerLeagueId = s.providerLeagueId.ToString(),
                    providerLeagueSlug = s.providerSlug,
                    // Use configuration data with fallback to provider data
                    leagueName = league?.Name ?? s.providerLeagueName,
                    leagueSlug = league?.BetExplorerSlug ?? s.providerSlug ?? "unknown",
                    countryCode = country?.Code?.ToLower() ?? s.providerCountryCode ?? "unknown",
                    countryName = country?.Name ?? s.providerCountryCode ?? "Unknown",
                    countrySlug = country?.Code?.ToLower() ?? s.providerCountryCode ?? "unknown",
                    seasonName = s.seasonName,
                    startYear = s.startYear,
                    endYear = s.endYear,
                    isCurrentSeason = s.isCurrentSeason,
                    data = s.data,
                    scannedAt = s.scrapedAt,
                    createdAt = s.createdAt,
                    isImported = s.isImported,
                    seasonId = s.seasonId?.ToString(),
                    importedAt = s.importedAt
                };
            })
            .OrderBy(s => s.countryName)
            .ThenBy(s => s.leagueName)
            .ThenByDescending(s => s.startYear)
            .ToList();

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

        // Get mapping details for a country
        group.MapGet("/countries/{id}/mapping", async (
            Guid id,
            DataImportDbContext context,
            ConfigurationDbContext configContext) =>
        {
            var providerCountry = await context.ProviderCountries
                .FirstOrDefaultAsync(pc => pc.Id == id);

            if (providerCountry == null)
            {
                return Results.NotFound(new { error = $"ProviderCountry {id} not found" });
            }

            // Get the provider from configuration context
            var provider = await configContext.DataProviders
                .FirstOrDefaultAsync(p => p.Id == providerCountry.ProviderId);

            // Find matching BetExplorer country - first check if CountryId is already set (from backfill)
            Configuration.Entities.Country? matchedCountry = null;
            if (providerCountry.CountryId.HasValue)
            {
                matchedCountry = await configContext.Countries
                    .Include(c => c.CountryProviders)
                        .ThenInclude(cp => cp.Provider)
                    .FirstOrDefaultAsync(c => c.Id == providerCountry.CountryId.Value);
            }

            // If not found by CountryId, try by ISO code or provider code
            if (matchedCountry == null)
            {
                matchedCountry = await configContext.Countries
                    .Include(c => c.CountryProviders)
                        .ThenInclude(cp => cp.Provider)
                    .FirstOrDefaultAsync(c =>
                        c.IsoCode == providerCountry.IsoCode ||
                        c.Code.ToLower() == providerCountry.ProviderCode.ToLower());
            }

            // Find country name mappings for this provider
            var providerCode = provider?.Code ?? "";
            var nameMappings = await context.CountryNameMappings
                .Where(m => m.ProviderCode == providerCode &&
                           (m.ProviderCountryName == providerCountry.ProviderName ||
                            m.BetExplorerCode == providerCountry.ProviderCode))
                .OrderBy(m => m.Priority)
                .ToListAsync();

            // Find CountryProvider mapping if exists
            var countryProvider = matchedCountry?.CountryProviders
                .FirstOrDefault(cp => cp.ProviderId == providerCountry.ProviderId);

            return Results.Ok(new
            {
                providerCountry = new
                {
                    id = providerCountry.Id.ToString(),
                    providerId = providerCountry.ProviderId.ToString(),
                    providerCode = providerCountry.ProviderCode,
                    providerName = providerCountry.ProviderName,
                    isoCode = providerCountry.IsoCode,
                    flagEmoji = providerCountry.FlagEmoji,
                    isImported = providerCountry.IsImported,
                    countryId = providerCountry.CountryId?.ToString(),
                    scannedAt = providerCountry.ScrapedAt
                },
                matchedCountry = matchedCountry != null ? new
                {
                    id = matchedCountry.Id.ToString(),
                    name = matchedCountry.Name,
                    nameCs = matchedCountry.NameCs,
                    code = matchedCountry.Code,
                    isoCode = matchedCountry.IsoCode,
                    flagEmoji = matchedCountry.FlagEmoji,
                    isActive = matchedCountry.IsActive
                } : null,
                countryProvider = countryProvider != null ? new
                {
                    id = countryProvider.Id.ToString(),
                    providerCode = countryProvider.ProviderCode,
                    providerName = countryProvider.ProviderName,
                    isActive = countryProvider.IsActive
                } : null,
                nameMappings = nameMappings.Select(m => new
                {
                    id = m.Id.ToString(),
                    providerCountryName = m.ProviderCountryName,
                    betExplorerCode = m.BetExplorerCode,
                    isActive = m.IsActive,
                    priority = m.Priority,
                    usageCount = m.UsageCount,
                    lastUsedAt = m.LastUsedAt
                }).ToList(),
                mappingStatus = providerCountry.CountryId != null ? "Imported" :
                               countryProvider != null ? "Mapped" :
                               nameMappings.Any(m => m.IsActive) ? "HasNameMapping" : "Unmapped"
            });
        })
        .WithName("GetCountryMappingDetails")
        .Produces(200)
        .Produces(404);

        // Get mapping details for a league
        group.MapGet("/leagues/{id}/mapping", async (
            Guid id,
            DataImportDbContext context,
            ConfigurationDbContext configContext) =>
        {
            var providerLeague = await context.ProviderLeagues
                .FirstOrDefaultAsync(pl => pl.Id == id);

            if (providerLeague == null)
            {
                return Results.NotFound(new { error = $"ProviderLeague {id} not found" });
            }

            // Get the provider from configuration context
            var provider = await configContext.DataProviders
                .FirstOrDefaultAsync(p => p.Id == providerLeague.ProviderId);

            // Find matching BetExplorer league - FIRST by LeagueId, then fallback to slug
            League? matchedLeague = null;
            if (providerLeague.LeagueId.HasValue)
            {
                matchedLeague = await configContext.Leagues
                    .Include(l => l.Country)
                    .Include(l => l.LeagueProviders)
                        .ThenInclude(lp => lp.Provider)
                    .FirstOrDefaultAsync(l => l.Id == providerLeague.LeagueId.Value);
            }
            else if (!string.IsNullOrEmpty(providerLeague.ProviderSlug))
            {
                // Fallback: try to find by slug (unlikely to work for betting providers)
                matchedLeague = await configContext.Leagues
                    .Include(l => l.Country)
                    .Include(l => l.LeagueProviders)
                        .ThenInclude(lp => lp.Provider)
                    .FirstOrDefaultAsync(l => l.BetExplorerSlug == providerLeague.ProviderSlug);
            }

            // Find league name mappings for this provider and country
            var countryCode = providerLeague.CountryCode ?? "";
            var providerCode = provider?.Code ?? "";
            var nameMappings = await context.LeagueNameMappings
                .Where(m => m.ProviderCode == providerCode &&
                           (m.ProviderLeagueName == providerLeague.ProviderName ||
                            m.ProviderLeagueName == providerLeague.DisplayName ||
                            m.BetExplorerSlug == providerLeague.ProviderSlug))
                .OrderBy(m => m.Priority)
                .ToListAsync();

            // Find LeagueProvider mapping if exists
            var leagueProvider = matchedLeague?.LeagueProviders
                .FirstOrDefault(lp => lp.ProviderId == providerLeague.ProviderId);

            return Results.Ok(new
            {
                providerLeague = new
                {
                    id = providerLeague.Id.ToString(),
                    providerId = providerLeague.ProviderId.ToString(),
                    providerName = providerLeague.ProviderName,
                    displayName = providerLeague.DisplayName,
                    providerSlug = providerLeague.ProviderSlug,
                    countryCode = countryCode,
                    mappingStatus = providerLeague.MappingStatus.ToString(),
                    isImported = providerLeague.IsImported,
                    leagueId = providerLeague.LeagueId?.ToString(),
                    scannedAt = providerLeague.ScrapedAt
                },
                matchedLeague = matchedLeague != null ? new
                {
                    id = matchedLeague.Id.ToString(),
                    name = matchedLeague.Name,
                    nameCs = matchedLeague.NameCs,
                    betExplorerSlug = matchedLeague.BetExplorerSlug,
                    country = matchedLeague.Country != null ? new
                    {
                        id = matchedLeague.Country.Id.ToString(),
                        name = matchedLeague.Country.Name,
                        code = matchedLeague.Country.Code
                    } : null,
                    isActive = matchedLeague.IsActive
                } : null,
                leagueProvider = leagueProvider != null ? new
                {
                    id = leagueProvider.Id.ToString(),
                    providerLeagueId = leagueProvider.ProviderLeagueId,
                    providerName = leagueProvider.ProviderName,
                    providerSlug = leagueProvider.ProviderSlug,
                    isActive = leagueProvider.IsActive
                } : null,
                nameMappings = nameMappings.Select(m => new
                {
                    id = m.Id.ToString(),
                    providerLeagueName = m.ProviderLeagueName,
                    countryCode = m.CountryCode,
                    betExplorerSlug = m.BetExplorerSlug,
                    isActive = m.IsActive,
                    priority = m.Priority,
                    usageCount = m.UsageCount,
                    lastUsedAt = m.LastUsedAt
                }).ToList()
            });
        })
        .WithName("GetLeagueMappingDetails")
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
