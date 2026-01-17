using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Services;

namespace Sazkomat.Api.Endpoints;

public static class ScanEndpoints
{
    public static void MapScanEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/scan")
            .WithTags("Scan")
            .WithOpenApi();

        // Scan countries from provider (async via Hangfire)
        group.MapPost("/countries", async (
            [FromBody] ScanCountriesRequest request,
            IScanService scanService,
            IBackgroundJobClient backgroundJobClient,
            ISyncJobProcessor jobProcessor) =>
        {
            try
            {
                // 1. Create job in DB (Pending status)
                var jobId = await scanService.CreateScanJobAsync(request.ProviderId, SyncEntityType.Countries);

                // 2. Enqueue to Hangfire for background processing
                var hangfireJobId = backgroundJobClient.Enqueue(() =>
                    jobProcessor.ProcessScanJobAsync(jobId));

                // 3. Return immediately
                return Results.Ok(new { jobId, hangfireJobId, message = "Country scan enqueued" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("ScanCountries")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Scan leagues from provider (async via Hangfire)
        // NOTE: Only available for BettingProvider types. Reference providers (BetExplorer) should use /api/scan/full
        group.MapPost("/leagues", async (
            [FromBody] ScanLeaguesRequest request,
            IScanService scanService,
            IBackgroundJobClient backgroundJobClient,
            ISyncJobProcessor jobProcessor,
            IDataProviderRepository providerRepo) =>
        {
            try
            {
                // Validate provider exists and is a BettingProvider
                var provider = await providerRepo.GetByIdAsync(request.ProviderId);
                if (provider == null)
                    return Results.NotFound(new { error = $"Provider {request.ProviderId} not found" });

                if (provider.Type != ProviderType.BettingProvider)
                    return Results.BadRequest(new {
                        error = $"League scan is only available for betting providers. " +
                                $"Provider '{provider.Name}' is of type '{provider.Type}'. " +
                                $"For reference providers like BetExplorer, leagues are discovered during season/round import."
                    });

                // 1. Create job in DB (Pending status)
                var jobId = await scanService.CreateScanJobAsync(
                    request.ProviderId,
                    SyncEntityType.Leagues,
                    countryIds: request.CountryIds);

                // 2. Enqueue to Hangfire for background processing
                var hangfireJobId = backgroundJobClient.Enqueue(() =>
                    jobProcessor.ProcessScanJobAsync(jobId));

                // 3. Return immediately
                return Results.Ok(new { jobId, hangfireJobId, message = $"League scan enqueued for {request.CountryIds?.Count ?? 0} countries" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("ScanLeagues")
        .Produces(200)
        .Produces(400)
        .Produces(404)
        .Produces(500);

        // Scan seasons from provider (async via Hangfire)
        group.MapPost("/seasons", async (
            [FromBody] ScanSeasonsRequest request,
            IScanService scanService,
            IBackgroundJobClient backgroundJobClient,
            ISyncJobProcessor jobProcessor) =>
        {
            try
            {
                // 1. Create job in DB (Pending status)
                var jobId = await scanService.CreateScanJobAsync(
                    request.ProviderId,
                    SyncEntityType.Seasons,
                    leagueIds: request.LeagueIds);

                // 2. Enqueue to Hangfire for background processing
                var hangfireJobId = backgroundJobClient.Enqueue(() =>
                    jobProcessor.ProcessScanJobAsync(jobId));

                // 3. Return immediately
                return Results.Ok(new { jobId, hangfireJobId, message = $"Season scan enqueued for {request.LeagueIds?.Count ?? 0} leagues" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("ScanSeasons")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Combined scan of countries AND leagues in single pass (optimized for Betano)
        group.MapPost("/full", async (
            [FromBody] ScanFullRequest request,
            IScanService scanService,
            IBackgroundJobClient backgroundJobClient,
            ISyncJobProcessor jobProcessor) =>
        {
            try
            {
                // 1. Create job in DB (Pending status) with CountriesAndLeagues type
                var jobId = await scanService.CreateScanJobAsync(request.ProviderId, SyncEntityType.CountriesAndLeagues);

                // 2. Enqueue to Hangfire for background processing
                var hangfireJobId = backgroundJobClient.Enqueue(() =>
                    jobProcessor.ProcessScanJobAsync(jobId));

                // 3. Return immediately
                return Results.Ok(new { jobId, hangfireJobId, message = "Full scan (countries + leagues) enqueued" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("ScanFull")
        .WithDescription("Scan countries AND leagues in a single pass. Optimized for betting providers like Betano where both come from one HTTP request.")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Apply country mappings to create missing ProviderCountry entries
        group.MapPost("/apply-country-mappings", async (
            [FromBody] ApplyCountryMappingsRequest request,
            IScanService scanService) =>
        {
            try
            {
                var createdCount = await scanService.ApplyCountryMappingsAsync(request.ProviderId);
                return Results.Ok(new {
                    createdCount,
                    message = $"Applied country mappings: {createdCount} entries created"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("ApplyCountryMappings")
        .WithDescription("Applies active country name mappings to create missing ProviderCountry entries without running a full scan")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Backfill provider_leagues from resolved unmatched_leagues
        group.MapPost("/backfill-provider-leagues", async (
            [FromBody] BackfillProviderLeaguesRequest request,
            IScanService scanService) =>
        {
            try
            {
                var (created, updated) = await scanService.BackfillProviderLeaguesFromResolvedAsync(request.ProviderId);
                return Results.Ok(new {
                    created,
                    updated,
                    total = created + updated,
                    message = $"Backfilled provider_leagues: {created} created, {updated} updated"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("BackfillProviderLeagues")
        .WithDescription("Backfills provider_leagues from resolved unmatched_leagues. Creates provider_leagues entries for all resolved (mapped) unmatched leagues that don't yet have a corresponding provider_leagues record.")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Backfill provider_countries from resolved unmatched_countries
        group.MapPost("/backfill-provider-countries", async (
            [FromBody] BackfillProviderCountriesRequest request,
            IScanService scanService) =>
        {
            try
            {
                var (created, updated) = await scanService.BackfillProviderCountriesFromResolvedAsync(request.ProviderId);
                return Results.Ok(new {
                    created,
                    updated,
                    total = created + updated,
                    message = $"Backfilled provider_countries: {created} created, {updated} updated"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("BackfillProviderCountries")
        .WithDescription("Backfills provider_countries from resolved unmatched_countries. Creates provider_countries entries for all resolved (mapped) unmatched countries that don't yet have a corresponding provider_countries record.")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Backfill LeagueProvider mappings from resolved unmatched_leagues
        group.MapPost("/backfill-league-providers", async (
            [FromBody] BackfillLeagueProvidersRequest request,
            IUnmatchedLeagueRepository unmatchedLeagueRepo,
            ILeagueProviderRepository leagueProviderRepo,
            ILogger<Program> logger) =>
        {
            try
            {
                var resolvedMapped = await unmatchedLeagueRepo.GetResolvedAsMappedByProviderAsync(request.ProviderId);
                int created = 0;
                int skipped = 0;

                // Track slugs we've already processed to avoid duplicates
                var processedSlugs = new HashSet<string>();

                foreach (var unmatched in resolvedMapped)
                {
                    if (!unmatched.ResolvedLeagueId.HasValue) continue;

                    var providerSlug = unmatched.ProviderSlug ?? unmatched.ProviderLeagueName.ToLowerInvariant().Replace(" ", "-");

                    // Skip if we already processed this slug in this run
                    var slugKey = $"{unmatched.ProviderId}:{providerSlug}";
                    if (processedSlugs.Contains(slugKey))
                    {
                        skipped++;
                        continue;
                    }
                    processedSlugs.Add(slugKey);

                    // Check by league+provider
                    var existingByLeague = await leagueProviderRepo.GetByLeagueAndProviderAsync(
                        unmatched.ResolvedLeagueId.Value, unmatched.ProviderId);

                    // Use AddOrUpdate to handle existing mappings gracefully
                    if (existingByLeague == null)
                    {
                        var leagueProvider = new Configuration.Entities.LeagueProvider
                        {
                            LeagueId = unmatched.ResolvedLeagueId.Value,
                            ProviderId = unmatched.ProviderId,
                            ProviderSlug = providerSlug,
                            ProviderName = unmatched.ProviderLeagueName,
                            IsActive = true
                        };
                        await leagueProviderRepo.AddOrUpdateAsync(leagueProvider);
                        created++;
                        logger.LogInformation("Created/Updated LeagueProvider mapping for league {LeagueId} -> provider {ProviderId} (slug: {Slug})",
                            unmatched.ResolvedLeagueId, unmatched.ProviderId, providerSlug);
                    }
                    else
                    {
                        skipped++;
                    }
                }

                return Results.Ok(new {
                    created,
                    skipped,
                    total = created + skipped,
                    message = $"Backfilled LeagueProvider: {created} created, {skipped} already existed"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("BackfillLeagueProviders")
        .WithDescription("Backfills LeagueProvider mappings from resolved unmatched_leagues. Creates LeagueProvider entries for all resolved (mapped) unmatched leagues that don't yet have a mapping.")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // =====================================================================
        // UNMATCHED LEAGUES - Manual mapping queue
        // =====================================================================

        // Get all unmatched leagues (with optional filters)
        group.MapGet("/unmatched-leagues", async (
            [FromQuery] bool? resolved,
            [FromQuery] Guid? providerId,
            IUnmatchedLeagueRepository unmatchedLeagueRepo,
            IDataProviderRepository providerRepo) =>
        {
            try
            {
                List<UnmatchedLeague> leagues;

                if (providerId.HasValue)
                {
                    leagues = resolved == false
                        ? await unmatchedLeagueRepo.GetUnresolvedByProviderAsync(providerId.Value)
                        : await unmatchedLeagueRepo.GetByProviderAsync(providerId.Value);
                }
                else
                {
                    leagues = resolved == false
                        ? await unmatchedLeagueRepo.GetUnresolvedAsync()
                        : await unmatchedLeagueRepo.GetAllAsync();
                }

                // Load provider names from configuration context
                var providerIds = leagues.Select(l => l.ProviderId).Distinct().ToHashSet();
                var allProviders = await providerRepo.GetAllAsync();
                var providerDict = allProviders
                    .Where(p => providerIds.Contains(p.Id))
                    .ToDictionary(p => p.Id, p => p.Name);

                return Results.Ok(leagues.Select(l => new
                {
                    l.Id,
                    l.ProviderId,
                    providerName = providerDict.TryGetValue(l.ProviderId, out var name) ? name : null,
                    l.ProviderLeagueId,
                    l.ProviderLeagueName,
                    l.ProviderSlug,
                    l.CountryCode,
                    l.CountryName,
                    l.ScrapedAt,
                    l.IsResolved,
                    resolutionType = l.ResolutionType?.ToString(),
                    l.ResolvedLeagueId,
                    l.ResolvedAt,
                    l.ResolutionNotes
                }));
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("GetScanUnmatchedLeagues")
        .WithDescription("Get list of unmatched leagues from betting providers. Use resolved=false to get only unresolved.")
        .Produces(200)
        .Produces(500);

        // Resolve unmatched league by mapping to BetExplorer slug
        group.MapPost("/unmatched-leagues/{id}/resolve", async (
            Guid id,
            [FromBody] ResolveUnmatchedLeagueRequest request,
            IUnmatchedLeagueRepository unmatchedLeagueRepo,
            ILeagueRepository leagueRepo,
            ILeagueNameMappingRepository mappingRepo,
            IDataProviderRepository providerRepo) =>
        {
            try
            {
                var unmatched = await unmatchedLeagueRepo.GetByIdAsync(id);
                if (unmatched == null)
                    return Results.NotFound(new { error = $"Unmatched league {id} not found" });

                if (unmatched.IsResolved)
                    return Results.BadRequest(new { error = "League is already resolved" });

                // Find the league by BetExplorer slug
                var league = await leagueRepo.GetByBetExplorerSlugAsync(request.BetExplorerSlug);
                if (league == null)
                    return Results.NotFound(new { error = $"No league found with BetExplorer slug '{request.BetExplorerSlug}'" });

                // Resolve the unmatched league
                await unmatchedLeagueRepo.ResolveAsMappedAsync(id, league.Id, request.Notes);

                // Get provider code for mapping
                var provider = await providerRepo.GetByIdAsync(unmatched.ProviderId);
                var providerCode = provider?.Code?.ToLowerInvariant() ?? "unknown";

                // Create league_name_mapping for future scans
                var existingMapping = await mappingRepo.FindMappingAsync(
                    providerCode,
                    unmatched.CountryCode.ToLowerInvariant(),
                    unmatched.ProviderLeagueName);

                if (existingMapping == null)
                {
                    var newMapping = new LeagueNameMapping
                    {
                        ProviderCode = providerCode,
                        CountryCode = unmatched.CountryCode.ToLowerInvariant(),
                        ProviderLeagueName = unmatched.ProviderLeagueName,
                        BetExplorerSlug = request.BetExplorerSlug,
                        IsActive = true,
                        Notes = $"Created from unmatched_leagues resolution"
                    };
                    await mappingRepo.CreateAsync(newMapping);
                }

                return Results.Ok(new
                {
                    message = $"Successfully resolved '{unmatched.ProviderLeagueName}' → '{league.Name}'",
                    leagueId = league.Id,
                    leagueName = league.Name,
                    mappingCreated = existingMapping == null
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("ScanResolveUnmatchedLeague")
        .WithDescription("Manually resolve an unmatched league by providing the BetExplorer slug")
        .Produces(200)
        .Produces(400)
        .Produces(404)
        .Produces(500);

        // Ignore unmatched league (not available in BetExplorer)
        group.MapPost("/unmatched-leagues/{id}/ignore", async (
            Guid id,
            [FromBody] IgnoreUnmatchedLeagueRequest? request,
            IUnmatchedLeagueRepository unmatchedLeagueRepo) =>
        {
            try
            {
                var unmatched = await unmatchedLeagueRepo.GetByIdAsync(id);
                if (unmatched == null)
                    return Results.NotFound(new { error = $"Unmatched league {id} not found" });

                if (unmatched.IsResolved)
                    return Results.BadRequest(new { error = "League is already resolved" });

                await unmatchedLeagueRepo.ResolveAsIgnoredAsync(id, request?.Notes);

                return Results.Ok(new
                {
                    message = $"League '{unmatched.ProviderLeagueName}' marked as ignored",
                    notes = request?.Notes
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("ScanIgnoreUnmatchedLeague")
        .WithDescription("Mark an unmatched league as ignored (not available in BetExplorer)")
        .Produces(200)
        .Produces(400)
        .Produces(404)
        .Produces(500);

        // Delete unmatched league
        group.MapDelete("/unmatched-leagues/{id}", async (
            Guid id,
            IUnmatchedLeagueRepository unmatchedLeagueRepo) =>
        {
            try
            {
                var unmatched = await unmatchedLeagueRepo.GetByIdAsync(id);
                if (unmatched == null)
                    return Results.NotFound(new { error = $"Unmatched league {id} not found" });

                await unmatchedLeagueRepo.DeleteAsync(id);

                return Results.Ok(new { message = $"Deleted unmatched league '{unmatched.ProviderLeagueName}'" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error");
            }
        })
        .WithName("ScanDeleteUnmatchedLeague")
        .WithDescription("Delete an unmatched league from the queue")
        .Produces(200)
        .Produces(404)
        .Produces(500);

    }
}

public record ScanCountriesRequest(Guid ProviderId);
public record ScanLeaguesRequest(Guid ProviderId, List<Guid> CountryIds);
public record ScanSeasonsRequest(Guid ProviderId, List<Guid> LeagueIds);
public record ScanFullRequest(Guid ProviderId);
public record ApplyCountryMappingsRequest(Guid ProviderId);
public record BackfillProviderLeaguesRequest(Guid ProviderId);
public record BackfillProviderCountriesRequest(Guid ProviderId);
public record BackfillLeagueProvidersRequest(Guid ProviderId);
public record ResolveUnmatchedLeagueRequest(string BetExplorerSlug, string? Notes = null);
public record IgnoreUnmatchedLeagueRequest(string? Notes = null);
