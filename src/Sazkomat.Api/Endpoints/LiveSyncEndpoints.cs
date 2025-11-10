using Microsoft.AspNetCore.Mvc;
using Sazkomat.DataImport.Services;

namespace Sazkomat.Api.Endpoints;

public static class LiveSyncEndpoints
{
    public static void MapLiveSyncEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/livesync")
            .WithTags("LiveSync")
            .WithOpenApi();

        // Live sync rounds for all/specific leagues
        group.MapPost("/rounds", async (
            [FromBody] LiveSyncRoundsRequest request,
            ILiveSyncService liveSyncService) =>
        {
            var jobId = await liveSyncService.LiveSyncRoundsAsync(
                request.ProviderId,
                request.LeagueIds,
                request.ForceRefresh);

            return Results.Ok(new {
                jobId,
                message = request.LeagueIds != null && request.LeagueIds.Any()
                    ? $"Live sync started for {request.LeagueIds.Count} leagues"
                    : "Live sync started for all active leagues"
            });
        })
        .WithName("LiveSyncRounds")
        .Produces(200)
        .Produces(400);

        // Live sync specific round
        group.MapPost("/rounds/{roundId:guid}", async (
            Guid roundId,
            [FromBody] LiveSyncRoundRequest request,
            ILiveSyncService liveSyncService) =>
        {
            var jobId = await liveSyncService.LiveSyncRoundAsync(request.ProviderId, roundId);
            return Results.Ok(new { jobId, message = $"Live sync started for round {roundId}" });
        })
        .WithName("LiveSyncRound")
        .Produces(200)
        .Produces(400)
        .Produces(404);

        // Get live sync stats
        group.MapGet("/stats", async (
            [FromQuery] Guid providerId,
            ILiveSyncService liveSyncService) =>
        {
            var stats = await liveSyncService.GetLiveSyncStatsAsync(providerId);
            return Results.Ok(stats);
        })
        .WithName("GetLiveSyncStats")
        .Produces(200);
    }
}

public record LiveSyncRoundsRequest(Guid ProviderId, List<Guid>? LeagueIds, bool ForceRefresh = false);
public record LiveSyncRoundRequest(Guid ProviderId);
