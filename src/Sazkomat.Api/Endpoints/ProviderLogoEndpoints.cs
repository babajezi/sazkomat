using Microsoft.AspNetCore.Mvc;
using Sazkomat.Configuration.Services;
using Sazkomat.Core.Entities;

namespace Sazkomat.Api.Endpoints;

public static class ProviderLogoEndpoints
{
    public static void MapProviderLogoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config/providers")
            .WithTags("Provider Logos")
            .WithOpenApi();

        // POST /api/config/providers/{id}/logo
        group.MapPost("/{id:guid}/logo", async (
            Guid id,
            IFormFile file,
            IProviderLogoService service) =>
        {
            var result = await service.UploadAndProcessLogoAsync(id, file);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(new { message = "Logo uploaded and processed successfully" });
        })
        .WithName("UploadProviderLogo")
        .Produces(200)
        .Produces(400)
        .DisableAntiforgery(); // Required for file uploads

        // GET /api/config/providers/{id}/logo?size=sm|md|lg
        group.MapGet("/{id:guid}/logo", async (
            Guid id,
            [FromQuery] string? size,
            IProviderLogoService service) =>
        {
            // Parse size parameter (default to Medium)
            if (!Enum.TryParse<LogoSize>(size, true, out var logoSize))
            {
                logoSize = LogoSize.Medium;
            }

            var result = await service.GetLogoAsync(id, logoSize);

            if (!result.IsSuccess)
            {
                return Results.Problem(result.Error);
            }

            if (result.Value == null)
            {
                return Results.NotFound(new { error = "Logo not found" });
            }

            // Detect content type - check if SVG exists
            var svgPath = service.GetSvgLogoPath(id);
            var contentType = File.Exists(svgPath) ? "image/svg+xml" : "image/webp";
            var fileExtension = contentType == "image/svg+xml" ? "svg" : "webp";

            // Return image with appropriate headers
            return Results.File(
                result.Value,
                contentType,
                enableRangeProcessing: true,
                lastModified: DateTimeOffset.UtcNow,
                entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{id}-{fileExtension}\""));
        })
        .WithName("GetProviderLogo")
        .Produces(200)
        .Produces(404)
        .Produces(500);

        // DELETE /api/config/providers/{id}/logo
        group.MapDelete("/{id:guid}/logo", async (
            Guid id,
            IProviderLogoService service) =>
        {
            var result = await service.DeleteLogoAsync(id);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(new { message = "Logo deleted successfully" });
        })
        .WithName("DeleteProviderLogo")
        .Produces(200)
        .Produces(400);
    }
}
