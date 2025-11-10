using Microsoft.AspNetCore.Mvc;
using Sazkomat.DataImport.Services;

namespace Sazkomat.Api.Endpoints;

public static class ScanEndpoints
{
    public static void MapScanEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/scan")
            .WithTags("Scan")
            .WithOpenApi();

        // Scan countries from provider
        group.MapPost("/countries", async (
            [FromBody] ScanCountriesRequest request,
            IScanService scanService) =>
        {
            var jobId = await scanService.ScanCountriesAsync(request.ProviderId);
            return Results.Ok(new { jobId, message = "Country scan started" });
        })
        .WithName("ScanCountries")
        .Produces(200)
        .Produces(400);

        // Scan leagues from provider
        group.MapPost("/leagues", async (
            [FromBody] ScanLeaguesRequest request,
            IScanService scanService) =>
        {
            var jobId = await scanService.ScanLeaguesAsync(request.ProviderId, request.CountryIds);
            return Results.Ok(new { jobId, message = $"League scan started for {request.CountryIds.Count} countries" });
        })
        .WithName("ScanLeagues")
        .Produces(200)
        .Produces(400);

        // Scan seasons from provider
        group.MapPost("/seasons", async (
            [FromBody] ScanSeasonsRequest request,
            IScanService scanService) =>
        {
            var jobId = await scanService.ScanSeasonsAsync(request.ProviderId, request.LeagueIds);
            return Results.Ok(new { jobId, message = $"Season scan started for {request.LeagueIds.Count} leagues" });
        })
        .WithName("ScanSeasons")
        .Produces(200)
        .Produces(400);

    }
}

public record ScanCountriesRequest(Guid ProviderId);
public record ScanLeaguesRequest(Guid ProviderId, List<Guid> CountryIds);
public record ScanSeasonsRequest(Guid ProviderId, List<Guid> LeagueIds);
