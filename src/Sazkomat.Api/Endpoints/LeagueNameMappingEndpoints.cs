using Microsoft.AspNetCore.Mvc;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.Api.Endpoints;

public static class LeagueNameMappingEndpoints
{
    public static void MapLeagueNameMappingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mappings")
            .WithTags("League Name Mappings")
            .WithDescription("Endpoints for managing manual league name mappings");

        // GET /api/mappings - Get all mappings with optional filters
        group.MapGet("/", async (
            [FromQuery] string? providerCode,
            [FromQuery] string? countryCode,
            [FromQuery] bool? isActive,
            ILeagueNameMappingRepository repository) =>
        {
            var mappings = await repository.GetAllAsync();

            // Apply filters
            if (!string.IsNullOrEmpty(providerCode))
            {
                mappings = mappings.Where(m =>
                    m.ProviderCode.Equals(providerCode, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(countryCode))
            {
                mappings = mappings.Where(m =>
                    m.CountryCode.Equals(countryCode, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (isActive.HasValue)
            {
                mappings = mappings.Where(m => m.IsActive == isActive.Value).ToList();
            }

            return Results.Ok(mappings);
        })
        .WithName("GetLeagueNameMappings")
        .Produces<List<LeagueNameMapping>>(200);

        // GET /api/mappings/{id} - Get mapping by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            ILeagueNameMappingRepository repository) =>
        {
            var mapping = await repository.GetByIdAsync(id);

            if (mapping == null)
            {
                return Results.NotFound(new { error = $"Mapping with ID {id} not found" });
            }

            return Results.Ok(mapping);
        })
        .WithName("GetLeagueNameMappingById")
        .Produces<LeagueNameMapping>(200)
        .Produces(404);

        // POST /api/mappings - Create new mapping
        group.MapPost("/", async (
            [FromBody] CreateLeagueNameMappingRequest request,
            ILeagueNameMappingRepository repository) =>
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.ProviderCode))
            {
                return Results.BadRequest(new { error = "Provider code is required" });
            }

            if (string.IsNullOrWhiteSpace(request.CountryCode))
            {
                return Results.BadRequest(new { error = "Country code is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ProviderLeagueName))
            {
                return Results.BadRequest(new { error = "Provider league name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.BetExplorerSlug))
            {
                return Results.BadRequest(new { error = "BetExplorer slug is required" });
            }

            var mapping = new LeagueNameMapping
            {
                Id = Guid.NewGuid(),
                ProviderCode = request.ProviderCode.ToLowerInvariant(),
                CountryCode = request.CountryCode.ToLowerInvariant(),
                ProviderLeagueName = request.ProviderLeagueName,
                BetExplorerSlug = request.BetExplorerSlug,
                IsActive = request.IsActive ?? true,
                Notes = request.Notes,
                Priority = request.Priority ?? 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await repository.CreateAsync(mapping);

            return Results.Created($"/api/mappings/{created.Id}", created);
        })
        .WithName("CreateLeagueNameMapping")
        .Produces<LeagueNameMapping>(201)
        .Produces(400);

        // PATCH /api/mappings/{id} - Update existing mapping
        group.MapPatch("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateLeagueNameMappingRequest request,
            ILeagueNameMappingRepository repository) =>
        {
            var mapping = await repository.GetByIdAsync(id);

            if (mapping == null)
            {
                return Results.NotFound(new { error = $"Mapping with ID {id} not found" });
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.ProviderLeagueName))
            {
                mapping.ProviderLeagueName = request.ProviderLeagueName;
            }

            if (!string.IsNullOrWhiteSpace(request.BetExplorerSlug))
            {
                mapping.BetExplorerSlug = request.BetExplorerSlug;
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
        .WithName("UpdateLeagueNameMapping")
        .Produces<LeagueNameMapping>(200)
        .Produces(404);

        // DELETE /api/mappings/{id} - Delete mapping
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ILeagueNameMappingRepository repository) =>
        {
            var mapping = await repository.GetByIdAsync(id);

            if (mapping == null)
            {
                return Results.NotFound(new { error = $"Mapping with ID {id} not found" });
            }

            await repository.DeleteAsync(id);

            return Results.NoContent();
        })
        .WithName("DeleteLeagueNameMapping")
        .Produces(204)
        .Produces(404);
    }
}

// DTOs
public record CreateLeagueNameMappingRequest(
    string ProviderCode,
    string CountryCode,
    string ProviderLeagueName,
    string BetExplorerSlug,
    bool? IsActive = true,
    string? Notes = null,
    int? Priority = 0
);

public record UpdateLeagueNameMappingRequest(
    string? ProviderLeagueName = null,
    string? BetExplorerSlug = null,
    bool? IsActive = null,
    string? Notes = null,
    int? Priority = null
);
