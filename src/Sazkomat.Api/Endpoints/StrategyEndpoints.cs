using Sazkomat.Strategy.Models;
using Sazkomat.Strategy.Services;

namespace Sazkomat.Api.Endpoints;

public static class StrategyEndpoints
{
    public static void MapStrategyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/strategies")
            .WithTags("Strategies");

        // GET /api/strategies/types — Available strategies with parameter definitions
        group.MapGet("/types", (StrategyService service) =>
        {
            return Results.Ok(service.GetAvailableStrategies());
        })
        .WithName("GetStrategyTypes")
        .Produces<List<StrategyInfo>>(200);

        // POST /api/strategies/screen — Phase 1: Screen leagues
        group.MapPost("/screen", async (ScreenRequest request, StrategyService service) =>
        {
            var result = await service.ScreenAsync(request.Spec, request.Name);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ScreenStrategies")
        .Produces<ScreeningResult>(200);

        // POST /api/strategies/simulate — Phase 2: Run backtest
        group.MapPost("/simulate", async (StrategySimulationSpec spec, StrategyService service) =>
        {
            var result = await service.SimulateAsync(spec);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("SimulateStrategy")
        .Produces<SimulationResult>(200);

        // GET /api/strategies/screenings — List saved screenings
        group.MapGet("/screenings", async (StrategyService service) =>
        {
            var screenings = await service.GetScreeningsAsync();
            return Results.Ok(screenings);
        })
        .WithName("GetScreenings")
        .Produces<List<ScreeningListDto>>(200);

        // GET /api/strategies/screenings/{id} — Get screening detail
        group.MapGet("/screenings/{id:guid}", async (Guid id, StrategyService service) =>
        {
            var result = await service.GetScreeningAsync(id);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("GetScreeningDetail")
        .Produces<ScreeningDetailDto>(200);

        // DELETE /api/strategies/screenings/{id} — Delete screening
        group.MapDelete("/screenings/{id:guid}", async (Guid id, StrategyService service) =>
        {
            var result = await service.DeleteScreeningAsync(id);
            return result.IsSuccess
                ? Results.Ok(new { message = "Screening deleted" })
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("DeleteScreening")
        .Produces(200);
    }
}

// DTOs

public class ScreenRequest
{
    public StrategySimulationSpec Spec { get; set; } = new();
    public string? Name { get; set; }
}
