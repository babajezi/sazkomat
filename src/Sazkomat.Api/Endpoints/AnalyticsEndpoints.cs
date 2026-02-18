using System.Text.Json;
using Sazkomat.Strategy.Models;
using Sazkomat.Strategy.Services;

namespace Sazkomat.Api.Endpoints;

public static class AnalyticsEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics")
            .WithTags("Analytics");

        // POST /api/analytics/execute — Ad-hoc execution
        group.MapPost("/execute", async (ViewSpec spec, AnalyticalViewService service) =>
        {
            var result = await service.ExecuteAsync(spec);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ExecuteAnalytics")
        .Produces<AnalyticsResult>(200);

        // GET /api/analytics/metadata — Available dimensions, metrics, columns
        group.MapGet("/metadata", (AnalyticalViewService service) =>
        {
            return Results.Ok(service.GetMetadata());
        })
        .WithName("GetAnalyticsMetadata")
        .Produces<AnalyticsMetadata>(200);

        // GET /api/analytics/views — List saved views
        group.MapGet("/views", async (AnalyticalViewService service) =>
        {
            var views = await service.GetAllAsync();
            return Results.Ok(views.Select(v => new ViewListDto
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                Tags = v.Tags,
                IsFavorite = v.IsFavorite,
                ExecutionCount = v.ExecutionCount,
                LastExecutedAt = v.LastExecutedAt,
                LastExecutionMs = v.LastExecutionMs,
                VisualizationType = ExtractVisualizationType(v.SpecJson),
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            }));
        })
        .WithName("GetAnalyticsViews")
        .Produces<IEnumerable<ViewListDto>>(200);

        // GET /api/analytics/views/{id} — Get view detail
        group.MapGet("/views/{id:guid}", async (Guid id, AnalyticalViewService service) =>
        {
            var view = await service.GetByIdAsync(id);
            if (view == null)
                return Results.NotFound(new { error = "View not found" });

            return Results.Ok(new ViewDetailDto
            {
                Id = view.Id,
                Name = view.Name,
                Description = view.Description,
                SpecJson = view.SpecJson,
                Spec = JsonSerializer.Deserialize<ViewSpec>(view.SpecJson, JsonOptions),
                Tags = view.Tags,
                IsFavorite = view.IsFavorite,
                ExecutionCount = view.ExecutionCount,
                LastExecutedAt = view.LastExecutedAt,
                LastExecutionMs = view.LastExecutionMs,
                CreatedAt = view.CreatedAt,
                UpdatedAt = view.UpdatedAt
            });
        })
        .WithName("GetAnalyticsView")
        .Produces<ViewDetailDto>(200);

        // POST /api/analytics/views — Create view
        group.MapPost("/views", async (CreateViewRequest request, AnalyticalViewService service) =>
        {
            var view = await service.CreateAsync(request.Name, request.Description, request.Spec, request.Tags);
            return Results.Created($"/api/analytics/views/{view.Id}", new { id = view.Id, name = view.Name });
        })
        .WithName("CreateAnalyticsView")
        .Produces(201);

        // PUT /api/analytics/views/{id} — Update view
        group.MapPut("/views/{id:guid}", async (Guid id, UpdateViewRequest request, AnalyticalViewService service) =>
        {
            var result = await service.UpdateAsync(id, request.Name, request.Description, request.Spec, request.Tags);
            return result.IsSuccess
                ? Results.Ok(new { id = result.Value!.Id, name = result.Value.Name })
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("UpdateAnalyticsView")
        .Produces(200);

        // DELETE /api/analytics/views/{id} — Delete view
        group.MapDelete("/views/{id:guid}", async (Guid id, AnalyticalViewService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess
                ? Results.Ok(new { message = "View deleted" })
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("DeleteAnalyticsView")
        .Produces(200);

        // POST /api/analytics/views/{id}/execute — Execute saved view
        group.MapPost("/views/{id:guid}/execute", async (Guid id, AnalyticalViewService service) =>
        {
            var result = await service.ExecuteByIdAsync(id);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ExecuteAnalyticsView")
        .Produces<AnalyticsResult>(200);

        // POST /api/analytics/views/{id}/favorite — Toggle favorite
        group.MapPost("/views/{id:guid}/favorite", async (Guid id, AnalyticalViewService service) =>
        {
            var result = await service.ToggleFavoriteAsync(id);
            return result.IsSuccess
                ? Results.Ok(new { id = result.Value!.Id, isFavorite = result.Value.IsFavorite })
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("ToggleAnalyticsViewFavorite")
        .Produces(200);
    }

    private static string ExtractVisualizationType(string specJson)
    {
        try
        {
            var spec = JsonSerializer.Deserialize<ViewSpec>(specJson, JsonOptions);
            return spec?.Visualization?.Type ?? "table";
        }
        catch
        {
            return "table";
        }
    }
}

// DTOs

public class ViewListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public bool IsFavorite { get; set; }
    public int ExecutionCount { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public int? LastExecutionMs { get; set; }
    public string VisualizationType { get; set; } = "table";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ViewDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SpecJson { get; set; } = "{}";
    public ViewSpec? Spec { get; set; }
    public string? Tags { get; set; }
    public bool IsFavorite { get; set; }
    public int ExecutionCount { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public int? LastExecutionMs { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateViewRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ViewSpec Spec { get; set; } = new();
    public string? Tags { get; set; }
}

public class UpdateViewRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ViewSpec? Spec { get; set; }
    public string? Tags { get; set; }
}
