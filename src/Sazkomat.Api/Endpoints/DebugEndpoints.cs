using Microsoft.AspNetCore.Mvc;
using Sazkomat.Data.Debug;

namespace Sazkomat.Api.Endpoints;

public static class DebugEndpoints
{
    public static void MapDebugEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/debug")
            .WithTags("Debug")
            .WithOpenApi();

        // POST /api/debug/scraper/execute
        group.MapPost("/scraper/execute", async (
            ScraperDebugService debugService,
            [FromBody] DebugRequest request) =>
        {
            var result = await debugService.ExecuteAsync(request);
            return Results.Ok(result);
        })
        .WithName("ExecuteScraperDebug")
        .WithSummary("Execute scraper debug actions step-by-step with detailed logging")
        .WithDescription(@"
Execute a sequence of browser actions for debugging web scraping.

**Available action types:**
- `navigate`: Navigate to URL
- `wait`: Wait for specified milliseconds
- `waitForSelector`: Wait for element to appear
- `waitForLoadState`: Wait for page load state (load/networkidle/domcontentloaded)
- `click`: Click on element
- `type`: Type text into input
- `select`: Select value from dropdown
- `screenshot`: Take screenshot (saved to /app/debug/)
- `logElements`: Log matching elements with attributes
- `extractHtml`: Extract HTML from element or page
- `evaluate`: Execute JavaScript
- `scroll`: Scroll page (top/bottom/up/down)

**Example request:**
```json
{
  ""actions"": [
    {""type"": ""navigate"", ""url"": ""https://example.com""},
    {""type"": ""waitForLoadState"", ""state"": ""networkidle""},
    {""type"": ""logElements"", ""selector"": ""select"", ""attributes"": [""id"", ""class""]},
    {""type"": ""screenshot"", ""name"": ""test""}
  ]
}
```
")
        .Produces<DebugSessionResult>(200)
        .Produces(400);

        // GET /api/debug/screenshots
        group.MapGet("/screenshots", () =>
        {
            var debugDir = "/app/debug";
            if (!Directory.Exists(debugDir))
            {
                return Results.Ok(new { files = Array.Empty<string>() });
            }

            var files = Directory.GetFiles(debugDir, "*.png")
                .Select(f => new
                {
                    name = Path.GetFileName(f),
                    path = f,
                    size = new FileInfo(f).Length,
                    created = File.GetCreationTime(f)
                })
                .OrderByDescending(f => f.created)
                .ToList();

            return Results.Ok(new { files });
        })
        .WithName("ListDebugScreenshots")
        .WithSummary("List available debug screenshots")
        .Produces(200);

        // GET /api/debug/screenshots/{name}
        group.MapGet("/screenshots/{name}", (string name) =>
        {
            var path = Path.Combine("/app/debug", name);

            if (!File.Exists(path))
            {
                return Results.NotFound(new { error = $"Screenshot not found: {name}" });
            }

            var bytes = File.ReadAllBytes(path);
            return Results.File(bytes, "image/png", name);
        })
        .WithName("GetDebugScreenshot")
        .WithSummary("Download a debug screenshot by name")
        .Produces(200)
        .Produces(404);

        // DELETE /api/debug/screenshots
        group.MapDelete("/screenshots", () =>
        {
            var debugDir = "/app/debug";
            if (!Directory.Exists(debugDir))
            {
                return Results.Ok(new { deleted = 0 });
            }

            var files = Directory.GetFiles(debugDir, "*.png");
            foreach (var file in files)
            {
                File.Delete(file);
            }

            return Results.Ok(new { deleted = files.Length });
        })
        .WithName("ClearDebugScreenshots")
        .WithSummary("Delete all debug screenshots")
        .Produces(200);
    }
}
