using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Services;

namespace Sazkomat.Api.Endpoints;

public static class ImportExportEndpoints
{
    public static IEndpointRouteBuilder MapImportExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config")
            .WithTags("Import/Export")
            .WithOpenApi();

        // GET /api/config/export/preview
        group.MapGet("/export/preview", async (
            IUniversalImportExportService service,
            [FromQuery] bool? sports,
            [FromQuery] bool? countries,
            [FromQuery] bool? providers,
            [FromQuery] bool? seasons,
            [FromQuery] bool? leagues,
            [FromQuery] bool? sportProviders,
            [FromQuery] bool? countryProviders,
            [FromQuery] bool? leagueProviders,
            [FromQuery] bool? leagueSeasons,
            [FromQuery] bool? onlyActive) =>
        {
            var options = new ExportOptionsDto
            {
                IncludeSports = sports ?? false,
                IncludeCountries = countries ?? false,
                IncludeProviders = providers ?? false,
                IncludeSeasons = seasons ?? false,
                IncludeLeagues = leagues ?? false,
                IncludeSportProviders = sportProviders ?? false,
                IncludeCountryProviders = countryProviders ?? false,
                IncludeLeagueProviders = leagueProviders ?? false,
                IncludeLeagueSeasons = leagueSeasons ?? false,
                OnlyActive = onlyActive ?? false
            };

            var result = await service.GetExportPreviewAsync(options);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("GetExportPreview")
        .WithDescription("Gets preview of what would be exported with given options");

        // POST /api/config/export
        group.MapPost("/export", async (
            [FromBody] ExportOptionsDto options,
            IUniversalImportExportService service) =>
        {
            var result = await service.ExportAsync(options);

            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });

            // Serialize to JSON with indentation
            var json = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Return as downloadable JSON file
            var bytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"sazkomat-config-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";

            return Results.File(
                bytes,
                contentType: "application/json",
                fileDownloadName: fileName);
        })
        .WithName("ExportConfiguration")
        .WithDescription("Exports selected configuration entities to JSON file");

        // POST /api/config/import/validate
        group.MapPost("/import/validate", async (
            [FromBody] ConfigurationExportDto data,
            IUniversalImportExportService service) =>
        {
            var result = await service.ValidateImportAsync(data);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ValidateImport")
        .WithDescription("Validates import data without making any changes");

        // POST /api/config/import
        group.MapPost("/import", async (
            [FromBody] ImportRequestDto request,
            IUniversalImportExportService service) =>
        {
            var result = await service.ImportAsync(request.Data, request.Options);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ImportConfiguration")
        .WithDescription("Imports configuration data with specified options");

        return app;
    }
}
