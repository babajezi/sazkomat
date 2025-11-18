using Microsoft.AspNetCore.Mvc;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.Api.Endpoints;

public static class CountryNameMappingEndpoints
{
    public static void MapCountryNameMappingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/country-mappings")
            .WithTags("Country Name Mappings")
            .WithDescription("Endpoints for managing manual country name mappings");

        // GET /api/country-mappings - Get all mappings with optional filters
        group.MapGet("/", async (
            [FromQuery] string? providerCode,
            [FromQuery] bool? isActive,
            ICountryNameMappingRepository repository) =>
        {
            var mappings = await repository.GetAllAsync();

            // Apply filters
            if (!string.IsNullOrEmpty(providerCode))
            {
                mappings = mappings.Where(m =>
                    m.ProviderCode.Equals(providerCode, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (isActive.HasValue)
            {
                mappings = mappings.Where(m => m.IsActive == isActive.Value).ToList();
            }

            return Results.Ok(mappings);
        })
        .WithName("GetCountryNameMappings")
        .Produces<List<CountryNameMapping>>(200);

        // GET /api/country-mappings/{id} - Get mapping by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            ICountryNameMappingRepository repository) =>
        {
            var mapping = await repository.GetByIdAsync(id);

            if (mapping == null)
            {
                return Results.NotFound(new { error = $"Mapping with ID {id} not found" });
            }

            return Results.Ok(mapping);
        })
        .WithName("GetCountryNameMappingById")
        .Produces<CountryNameMapping>(200)
        .Produces(404);

        // POST /api/country-mappings - Create new mapping
        group.MapPost("/", async (
            [FromBody] CreateCountryNameMappingRequest request,
            ICountryNameMappingRepository repository) =>
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.ProviderCode))
            {
                return Results.BadRequest(new { error = "Provider code is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ProviderCountryName))
            {
                return Results.BadRequest(new { error = "Provider country name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.BetExplorerCode))
            {
                return Results.BadRequest(new { error = "BetExplorer code is required" });
            }

            var mapping = new CountryNameMapping
            {
                Id = Guid.NewGuid(),
                ProviderCode = request.ProviderCode.ToLowerInvariant(),
                ProviderCountryName = request.ProviderCountryName,
                BetExplorerCode = request.BetExplorerCode.ToLowerInvariant(),
                IsActive = request.IsActive ?? true,
                Notes = request.Notes,
                Priority = request.Priority ?? 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await repository.CreateAsync(mapping);

            return Results.Created($"/api/country-mappings/{created.Id}", created);
        })
        .WithName("CreateCountryNameMapping")
        .Produces<CountryNameMapping>(201)
        .Produces(400);

        // PATCH /api/country-mappings/{id} - Update existing mapping
        group.MapPatch("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCountryNameMappingRequest request,
            ICountryNameMappingRepository repository) =>
        {
            var mapping = await repository.GetByIdAsync(id);

            if (mapping == null)
            {
                return Results.NotFound(new { error = $"Mapping with ID {id} not found" });
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.ProviderCountryName))
            {
                mapping.ProviderCountryName = request.ProviderCountryName;
            }

            if (!string.IsNullOrWhiteSpace(request.BetExplorerCode))
            {
                mapping.BetExplorerCode = request.BetExplorerCode.ToLowerInvariant();
            }

            if (request.IsActive.HasValue)
            {
                mapping.IsActive = request.IsActive.Value;
            }

            if (request.Notes != null)
            {
                mapping.Notes = request.Notes;
            }

            if (request.Priority.HasValue)
            {
                mapping.Priority = request.Priority.Value;
            }

            var updated = await repository.UpdateAsync(mapping);

            return Results.Ok(updated);
        })
        .WithName("UpdateCountryNameMapping")
        .Produces<CountryNameMapping>(200)
        .Produces(404);

        // DELETE /api/country-mappings/{id} - Delete mapping
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICountryNameMappingRepository repository) =>
        {
            var mapping = await repository.GetByIdAsync(id);

            if (mapping == null)
            {
                return Results.NotFound(new { error = $"Mapping with ID {id} not found" });
            }

            await repository.DeleteAsync(id);

            return Results.NoContent();
        })
        .WithName("DeleteCountryNameMapping")
        .Produces(204)
        .Produces(404);

        // POST /api/country-mappings/{id}/toggle - Toggle IsActive status
        group.MapPost("/{id:guid}/toggle", async (
            Guid id,
            ICountryNameMappingRepository repository) =>
        {
            var mapping = await repository.GetByIdAsync(id);

            if (mapping == null)
            {
                return Results.NotFound(new { error = $"Mapping with ID {id} not found" });
            }

            mapping.IsActive = !mapping.IsActive;
            mapping.UpdatedAt = DateTime.UtcNow;

            var updated = await repository.UpdateAsync(mapping);

            return Results.Ok(updated);
        })
        .WithName("ToggleCountryNameMappingActive")
        .Produces<CountryNameMapping>(200)
        .Produces(404);
    }
}

// DTOs
public record CreateCountryNameMappingRequest(
    string ProviderCode,
    string ProviderCountryName,
    string BetExplorerCode,
    bool? IsActive = true,
    string? Notes = null,
    int? Priority = 0
);

public record UpdateCountryNameMappingRequest(
    string? ProviderCountryName = null,
    string? BetExplorerCode = null,
    bool? IsActive = null,
    string? Notes = null,
    int? Priority = null
);
