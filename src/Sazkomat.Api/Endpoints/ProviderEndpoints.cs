using Microsoft.AspNetCore.Mvc;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;

namespace Sazkomat.Api.Endpoints;

public static class ProviderEndpoints
{
    public static void MapProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config/providers")
            .WithTags("Providers")
            .WithOpenApi();

        // ========== DataProvider endpoints ==========

        // GET /api/config/providers
        group.MapGet("/", async (
            IDataProviderRepository repository,
            [FromQuery] bool? onlyActive) =>
        {
            var providers = await repository.GetAllAsync(onlyActive);
            return Results.Ok(providers);
        })
        .WithName("GetDataProviders")
        .Produces(200);

        // GET /api/config/providers/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IDataProviderRepository repository) =>
        {
            var provider = await repository.GetByIdAsync(id);
            if (provider == null)
            {
                return Results.NotFound(new { error = $"Data provider with ID {id} not found" });
            }
            return Results.Ok(provider);
        })
        .WithName("GetDataProvider")
        .Produces(200)
        .Produces(404);

        // POST /api/config/providers
        group.MapPost("/", async (
            [FromBody] CreateDataProviderRequest request,
            IProviderService service) =>
        {
            var result = await service.CreateDataProviderAsync(request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Created($"/api/config/providers/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateDataProvider")
        .Produces(201)
        .Produces(400);

        // PATCH /api/config/providers/{id}
        group.MapPatch("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateDataProviderRequest request,
            IProviderService service) =>
        {
            var result = await service.UpdateDataProviderAsync(id, request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(result.Value);
        })
        .WithName("UpdateDataProvider")
        .Produces(200)
        .Produces(400);

        // DELETE /api/config/providers/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IProviderService service) =>
        {
            var result = await service.DeleteDataProviderAsync(id);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.NoContent();
        })
        .WithName("DeleteDataProvider")
        .Produces(204)
        .Produces(400);

        // ========== LeagueProvider endpoints ==========

        // GET /api/config/providers/league-mappings
        group.MapGet("/league-mappings", async (
            ILeagueProviderRepository repository,
            [FromQuery] Guid? leagueId,
            [FromQuery] Guid? providerId) =>
        {
            if (leagueId.HasValue && providerId.HasValue)
            {
                var mapping = await repository.GetByLeagueAndProviderAsync(leagueId.Value, providerId.Value);
                return mapping != null ? Results.Ok(new[] { mapping }) : Results.Ok(Array.Empty<object>());
            }
            else if (leagueId.HasValue)
            {
                var mappings = await repository.GetByLeagueIdAsync(leagueId.Value);
                return Results.Ok(mappings);
            }
            else
            {
                var mappings = await repository.GetAllAsync();
                return Results.Ok(mappings);
            }
        })
        .WithName("GetLeagueProviderMappings")
        .Produces(200);

        // GET /api/config/providers/league-mappings/{id}
        group.MapGet("/league-mappings/{id:guid}", async (
            Guid id,
            ILeagueProviderRepository repository) =>
        {
            var mapping = await repository.GetByIdAsync(id);
            if (mapping == null)
            {
                return Results.NotFound(new { error = $"League-provider mapping with ID {id} not found" });
            }
            return Results.Ok(mapping);
        })
        .WithName("GetLeagueProviderMapping")
        .Produces(200)
        .Produces(404);

        // POST /api/config/providers/league-mappings
        group.MapPost("/league-mappings", async (
            [FromBody] CreateLeagueProviderRequest request,
            IProviderService service) =>
        {
            var result = await service.CreateLeagueProviderAsync(request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Created($"/api/config/providers/league-mappings/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateLeagueProviderMapping")
        .Produces(201)
        .Produces(400);

        // PATCH /api/config/providers/league-mappings/{id}
        group.MapPatch("/league-mappings/{id:guid}", async (
            Guid id,
            [FromBody] UpdateLeagueProviderRequest request,
            IProviderService service) =>
        {
            var result = await service.UpdateLeagueProviderAsync(id, request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(result.Value);
        })
        .WithName("UpdateLeagueProviderMapping")
        .Produces(200)
        .Produces(400);

        // POST /api/config/providers/league-mappings/{id}/activate
        group.MapPost("/league-mappings/{id:guid}/activate", async (
            Guid id,
            IProviderService service) =>
        {
            var result = await service.ActivateLeagueProviderAsync(id);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(result.Value);
        })
        .WithName("ActivateLeagueProviderMapping")
        .Produces(200)
        .Produces(400);

        // DELETE /api/config/providers/league-mappings/{id}
        group.MapDelete("/league-mappings/{id:guid}", async (
            Guid id,
            IProviderService service) =>
        {
            var result = await service.DeleteLeagueProviderAsync(id);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.NoContent();
        })
        .WithName("DeleteLeagueProviderMapping")
        .Produces(204)
        .Produces(400);

        // ========== CountryProvider endpoints ==========

        // GET /api/config/providers/country-mappings
        group.MapGet("/country-mappings", async (
            ICountryProviderRepository repository,
            [FromQuery] Guid? countryId,
            [FromQuery] Guid? providerId) =>
        {
            if (countryId.HasValue && providerId.HasValue)
            {
                var mapping = await repository.GetByCountryAndProviderAsync(countryId.Value, providerId.Value);
                return mapping != null ? Results.Ok(new[] { mapping }) : Results.Ok(Array.Empty<object>());
            }
            else if (countryId.HasValue)
            {
                var mappings = await repository.GetByCountryIdAsync(countryId.Value);
                return Results.Ok(mappings);
            }
            else
            {
                var mappings = await repository.GetAllAsync();
                return Results.Ok(mappings);
            }
        })
        .WithName("GetCountryProviderMappings")
        .Produces(200);

        // GET /api/config/providers/country-mappings/{id}
        group.MapGet("/country-mappings/{id:guid}", async (
            Guid id,
            ICountryProviderRepository repository) =>
        {
            var mapping = await repository.GetByIdAsync(id);
            if (mapping == null)
            {
                return Results.NotFound(new { error = $"Country-provider mapping with ID {id} not found" });
            }
            return Results.Ok(mapping);
        })
        .WithName("GetCountryProviderMapping")
        .Produces(200)
        .Produces(404);

        // POST /api/config/providers/country-mappings
        group.MapPost("/country-mappings", async (
            [FromBody] CreateCountryProviderRequest request,
            IProviderService service) =>
        {
            var result = await service.CreateCountryProviderAsync(request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Created($"/api/config/providers/country-mappings/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateCountryProviderMapping")
        .Produces(201)
        .Produces(400);

        // PATCH /api/config/providers/country-mappings/{id}
        group.MapPatch("/country-mappings/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCountryProviderRequest request,
            IProviderService service) =>
        {
            var result = await service.UpdateCountryProviderAsync(id, request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(result.Value);
        })
        .WithName("UpdateCountryProviderMapping")
        .Produces(200)
        .Produces(400);

        // DELETE /api/config/providers/country-mappings/{id}
        group.MapDelete("/country-mappings/{id:guid}", async (
            Guid id,
            IProviderService service) =>
        {
            var result = await service.DeleteCountryProviderAsync(id);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.NoContent();
        })
        .WithName("DeleteCountryProviderMapping")
        .Produces(204)
        .Produces(400);
    }
}
