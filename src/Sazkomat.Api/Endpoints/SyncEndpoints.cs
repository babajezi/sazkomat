using Microsoft.AspNetCore.Mvc;
using Sazkomat.Configuration.Services;
using Sazkomat.Data.DTOs;
using Sazkomat.Data.Services;
using Sazkomat.BettingProviders.Services;

namespace Sazkomat.Api.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync")
            .WithTags("Synchronization")
            .WithOpenApi();

        // GET /api/sync/workflow/state
        group.MapGet("/workflow/state", async (ISyncWorkflowService workflowService) =>
        {
            var result = await workflowService.GetStateAsync();

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("GetWorkflowState")
        .WithSummary("Get current workflow state")
        .Produces(200)
        .Produces(400);

        // POST /api/sync/workflow/confirm-countries
        group.MapPost("/workflow/confirm-countries", async (ISyncWorkflowService workflowService) =>
        {
            var result = await workflowService.ConfirmCountriesAsync();

            return result.IsSuccess
                ? Results.Ok(new { message = "Countries confirmed successfully" })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ConfirmCountries")
        .WithSummary("Confirm country selection")
        .Produces(200)
        .Produces(400);

        // POST /api/sync/workflow/confirm-leagues
        group.MapPost("/workflow/confirm-leagues", async (ISyncWorkflowService workflowService) =>
        {
            var result = await workflowService.ConfirmLeaguesAsync();

            return result.IsSuccess
                ? Results.Ok(new { message = "Leagues confirmed successfully" })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ConfirmLeagues")
        .WithSummary("Confirm league selection")
        .Produces(200)
        .Produces(400);

        // POST /api/sync/workflow/reset
        group.MapPost("/workflow/reset", async (
            ISyncWorkflowService workflowService,
            ISyncService syncService) =>
        {
            // Reset workflow state
            var result = await workflowService.ResetWorkflowAsync();

            // Reset sync status (clear in-memory IsRunning lock)
            syncService.ResetSyncStatus();

            return result.IsSuccess
                ? Results.Ok(new { message = "Workflow and sync status reset successfully" })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ResetWorkflow")
        .WithSummary("Reset entire workflow and clear sync locks")
        .Produces(200)
        .Produces(400);

        // POST /api/sync/countries
        group.MapPost("/countries", async (
            ISyncService syncService,
            ISyncWorkflowService workflowService,
            [FromBody] SyncRequest request) =>
        {
            // Check if countries can be synced
            var canSyncResult = await workflowService.CanSyncCountriesAsync();
            if (!canSyncResult.IsSuccess)
            {
                return Results.BadRequest(new { error = canSyncResult.Error });
            }

            // Perform sync
            var result = await syncService.SyncCountriesAsync(request.ProviderId, request.ActivateCountries);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            // Mark as synced
            var markResult = await workflowService.MarkCountriesSyncedAsync();
            if (!markResult.IsSuccess)
            {
                return Results.BadRequest(new { error = markResult.Error });
            }

            return Results.Ok(result.Value);
        })
        .WithName("SyncCountries")
        .WithSummary("Synchronize countries from provider")
        .Produces<SyncResponse>(200)
        .Produces(400);

        // POST /api/sync/leagues
        group.MapPost("/leagues", async (
            ISyncService syncService,
            ISyncWorkflowService workflowService,
            [FromBody] SyncRequest request) =>
        {
            // Check if leagues can be synced
            var canSyncResult = await workflowService.CanSyncLeaguesAsync();
            if (!canSyncResult.IsSuccess)
            {
                return Results.BadRequest(new { error = canSyncResult.Error });
            }

            // Perform sync (will automatically filter by active countries)
            var result = await syncService.SyncLeaguesAsync(
                request.ProviderId,
                request.EntityId); // EntityId = CountryId (optional)

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            // Mark as synced
            var markResult = await workflowService.MarkLeaguesSyncedAsync();
            if (!markResult.IsSuccess)
            {
                return Results.BadRequest(new { error = markResult.Error });
            }

            return Results.Ok(result.Value);
        })
        .WithName("SyncLeagues")
        .WithSummary("Synchronize leagues from provider for active countries")
        .Produces<SyncResponse>(200)
        .Produces(400);

        // POST /api/sync/seasons
        group.MapPost("/seasons", async (
            ISyncService syncService,
            ISyncWorkflowService workflowService,
            [FromBody] SyncRequest request) =>
        {
            // Check if seasons can be synced
            var canSyncResult = await workflowService.CanSyncSeasonsAsync();
            if (!canSyncResult.IsSuccess)
            {
                return Results.BadRequest(new { error = canSyncResult.Error });
            }

            // Perform sync for all active leagues (limited to 3 years)
            var result = await syncService.SyncAllActiveSeasonsAsync(request.ProviderId);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            // Mark as synced
            var markResult = await workflowService.MarkSeasonsSyncedAsync();
            if (!markResult.IsSuccess)
            {
                return Results.BadRequest(new { error = markResult.Error });
            }

            return Results.Ok(result.Value);
        })
        .WithName("SyncSeasons")
        .WithSummary("Synchronize seasons for all active leagues (limited to 3 years)")
        .Produces<SyncResponse>(200)
        .Produces(400);

        // POST /api/sync/seasons/global
        group.MapPost("/seasons/global", async (
            ISyncService syncService,
            [FromBody] GlobalSeasonScanRequest? request) =>
        {
            // Perform global scan for all leagues with betting provider mapping
            // No year limit - shows all historical seasons
            var result = await syncService.GlobalSeasonScanAsync(request?.LeagueIds);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("GlobalSeasonScan")
        .WithSummary("Scan ALL available seasons from BetExplorer for leagues with betting provider mappings (no year limit)")
        .Produces<SyncResponse>(200)
        .Produces(400);

        // POST /api/sync/seasons/{leagueId}
        group.MapPost("/seasons/{leagueId}", async (
            ISyncService syncService,
            Guid leagueId,
            [FromBody] SyncRequest request) =>
        {
            // Perform sync for single league (will be limited to 3 years)
            var result = await syncService.SyncSeasonsAsync(
                request.ProviderId,
                leagueId);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("SyncSeasonsSingle")
        .WithSummary("Synchronize seasons for a specific league (limited to 3 years)")
        .Produces<SyncResponse>(200)
        .Produces(400);

        // GET /api/sync/status
        group.MapGet("/status", async (ISyncService syncService) =>
        {
            var status = await syncService.GetSyncStatusAsync();
            return Results.Ok(status);
        })
        .WithName("GetSyncStatus")
        .WithSummary("Get current synchronization status")
        .Produces<SyncStatusResponse>(200);

        // POST /api/sync/seasons/detect-current
        group.MapPost("/seasons/detect-current", async (
            ISeasonSyncService seasonSyncService,
            [FromBody] SyncRequest request) =>
        {
            var result = await seasonSyncService.DetectAndMarkCurrentSeasonsAsync(request.ProviderId);

            return result.IsSuccess
                ? Results.Ok(new { message = "Current seasons detected and marked successfully" })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("DetectCurrentSeasons")
        .WithSummary("Detect and mark current seasons based on provider patterns")
        .Produces(200)
        .Produces(400);

        // POST /api/sync/seasons/data
        group.MapPost("/seasons/data", async (
            ISeasonSyncService seasonSyncService,
            [FromBody] SyncRequest request) =>
        {
            var result = await seasonSyncService.SyncAllMarkedSeasonsDataAsync(request.ProviderId);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("SyncAllMarkedSeasonsData")
        .WithSummary("Sync rounds and matches data for all seasons marked with SyncEnabled=true")
        .Produces<SyncResponse>(200)
        .Produces(400);

        // POST /api/sync/seasons/data/{leagueId}/{seasonId}
        group.MapPost("/seasons/data/{leagueId}/{seasonId}", async (
            ISeasonSyncService seasonSyncService,
            Guid leagueId,
            Guid seasonId,
            [FromBody] SyncSeasonDataRequest request) =>
        {
            var result = await seasonSyncService.SyncSeasonDataAsync(
                request.ProviderId,
                leagueId,
                seasonId,
                request.ForceUpdate);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("SyncSeasonData")
        .WithSummary("Sync rounds and matches data for a single season")
        .Produces<SyncResponse>(200)
        .Produces(400);

        // POST /api/sync/multi-sport
        group.MapPost("/multi-sport", async (
            MultiSportSyncOrchestrator orchestrator,
            [FromBody] MultiSportSyncRequest request) =>
        {
            var result = request.SportCodes?.Any() == true
                ? await orchestrator.SyncSelectedSportsAsync(request.ProviderCode, request.SportCodes)
                : await orchestrator.SyncAllActiveSportsAsync(request.ProviderCode);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("SyncMultiSport")
        .WithSummary("Synchronize leagues for multiple sports from a betting provider")
        .Produces<MultiSportSyncResult>(200)
        .Produces(400);

        // POST /api/sync/league/{leagueId}/season-data
        // Syncs rounds and matches for ALL seasons of a league (fail-fast on error)
        group.MapPost("/league/{leagueId}/season-data", async (
            ISyncService syncService,
            Guid leagueId,
            SyncLeagueSeasonDataRequest? request) =>
        {
            var forceUpdate = request?.ForceUpdate ?? false;
            var result = await syncService.SyncLeagueSeasonDataAsync(leagueId, forceUpdate);

            return result.IsSuccess
                ? Results.Ok(new { jobId = result.Value, message = "Season data sync started" })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("SyncLeagueSeasonData")
        .WithSummary("Sync rounds and matches for all seasons of a league. Historical seasons with HasData=true are skipped unless forceUpdate is true. Fail-fast on error.")
        .Produces(200)
        .Produces(400);

        // POST /api/sync/league/{leagueId}/seasons-list
        // Refreshes the list of available seasons from BetExplorer (metadata only)
        group.MapPost("/league/{leagueId}/seasons-list", async (
            ISyncService syncService,
            Guid leagueId) =>
        {
            var result = await syncService.RefreshLeagueSeasonsListAsync(leagueId);

            return result.IsSuccess
                ? Results.Ok(new { jobId = result.Value, message = "Seasons list refresh started" })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("RefreshLeagueSeasonsList")
        .WithSummary("Refresh list of available seasons for a league from BetExplorer. Only creates LeagueSeason entries, does not sync data.")
        .Produces(200)
        .Produces(400);
    }
}

public record SyncSeasonDataRequest(Guid ProviderId, bool ForceUpdate = false);

public record SyncLeagueSeasonDataRequest(bool ForceUpdate = false);

public record MultiSportSyncRequest(string ProviderCode, List<string>? SportCodes = null);

public record GlobalSeasonScanRequest(List<Guid>? LeagueIds = null);
