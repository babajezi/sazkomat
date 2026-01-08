using Sazkomat.Api.Services;

namespace Sazkomat.Api.Endpoints;

public static class DatabaseEndpoints
{
    public static void MapDatabaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/database")
            .WithTags("Database")
            .WithOpenApi();

        // DELETE /api/database/reset
        group.MapDelete("/reset", async (IDatabaseResetService service) =>
        {
            try
            {
                await service.ResetDatabaseAsync();
                return Results.Ok(new
                {
                    success = true,
                    message = "Database has been reset successfully. All data has been deleted.",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Database reset failed");
            }
        })
        .WithName("ResetDatabase")
        .WithDescription("Deletes all data from all tables. USE WITH CAUTION - This operation is irreversible!")
        .Produces(200)
        .Produces(500);

        // POST /api/database/seed
        group.MapPost("/seed", async (IDatabaseResetService service) =>
        {
            try
            {
                await service.SeedDatabaseAsync();
                return Results.Ok(new
                {
                    success = true,
                    message = "Database has been seeded successfully with initial data.",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Database seeding failed");
            }
        })
        .WithName("SeedDatabase")
        .WithDescription("Seeds the database with initial configuration data (sports, countries, leagues).")
        .Produces(200)
        .Produces(500);

        // POST /api/database/reset-and-seed
        group.MapPost("/reset-and-seed", async (IDatabaseResetService service) =>
        {
            try
            {
                await service.ResetAndSeedAsync();
                return Results.Ok(new
                {
                    success = true,
                    message = "Database has been reset and seeded successfully.",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Database reset and seed failed");
            }
        })
        .WithName("ResetAndSeedDatabase")
        .WithDescription("Deletes all data and then seeds with initial configuration. USE WITH CAUTION!")
        .Produces(200)
        .Produces(500);

        // POST /api/database/reset/all
        group.MapPost("/reset/all", async (IDatabaseResetService service) =>
        {
            var (success, message) = await service.ResetAllDataAsync();

            if (success)
            {
                return Results.Ok(new
                {
                    success = true,
                    message,
                    timestamp = DateTime.UtcNow
                });
            }

            return Results.BadRequest(new
            {
                success = false,
                error = message
            });
        })
        .WithName("ResetAllData")
        .WithDescription("Deletes all data including configuration (keeps only sports and providers). USE WITH CAUTION!")
        .Produces(200)
        .Produces(400);

        // POST /api/database/reset/data-only
        group.MapPost("/reset/data-only", async (IDatabaseResetService service) =>
        {
            var (success, message) = await service.ResetImportedDataOnlyAsync();

            if (success)
            {
                return Results.Ok(new
                {
                    success = true,
                    message,
                    timestamp = DateTime.UtcNow
                });
            }

            return Results.BadRequest(new
            {
                success = false,
                error = message
            });
        })
        .WithName("ResetImportedDataOnly")
        .WithDescription("Deletes only imported data (rounds, matches, jobs). Keeps all configuration.")
        .Produces(200)
        .Produces(400);

        // GET /api/database/counts
        group.MapGet("/counts", async (IDatabaseResetService service) =>
        {
            try
            {
                var counts = await service.GetEntityCountsAsync();
                return Results.Ok(counts);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to get entity counts");
            }
        })
        .WithName("GetEntityCounts")
        .WithDescription("Gets record counts for all resettable entities")
        .Produces<Dictionary<string, int>>(200)
        .Produces(500);

        // GET /api/database/counts/bindings
        group.MapGet("/counts/bindings", async (IDatabaseResetService service) =>
        {
            try
            {
                var counts = await service.GetBindingCountsByProviderAsync();
                return Results.Ok(counts);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to get binding counts");
            }
        })
        .WithName("GetBindingCountsByProvider")
        .WithDescription("Gets binding counts (league_providers, country_providers) grouped by provider")
        .Produces<Dictionary<string, Dictionary<string, int>>>(200)
        .Produces(500);

        // POST /api/database/reset/selective
        group.MapPost("/reset/selective", async (SelectiveResetRequest request, IDatabaseResetService service) =>
        {
            if (request.Entities == null || request.Entities.Count == 0)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    error = "No entities specified for deletion"
                });
            }

            var (success, message, deletedCounts) = await service.ResetSelectiveAsync(request.Entities);

            if (success)
            {
                return Results.Ok(new
                {
                    success = true,
                    message,
                    deletedCounts,
                    timestamp = DateTime.UtcNow
                });
            }

            return Results.BadRequest(new
            {
                success = false,
                error = message,
                deletedCounts
            });
        })
        .WithName("ResetSelective")
        .WithDescription("Selectively deletes specified entities. Valid: rounds, import_jobs, sync_jobs, provider_countries, provider_leagues, provider_seasons, country_name_mappings, league_name_mappings, leagues, countries, seasons, league_providers, league_seasons, country_providers")
        .Produces(200)
        .Produces(400);

        // POST /api/database/reset/bindings/{providerCode}
        group.MapPost("/reset/bindings/{providerCode}", async (
            string providerCode,
            ResetBindingsRequest request,
            IDatabaseResetService service) =>
        {
            if (request.BindingTypes == null || request.BindingTypes.Count == 0)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    error = "No binding types specified for deletion"
                });
            }

            var (success, message, deletedCounts) = await service.ResetBindingsForProviderAsync(
                providerCode, request.BindingTypes);

            if (success)
            {
                return Results.Ok(new
                {
                    success = true,
                    message,
                    deletedCounts,
                    timestamp = DateTime.UtcNow
                });
            }

            return Results.BadRequest(new
            {
                success = false,
                error = message,
                deletedCounts
            });
        })
        .WithName("ResetBindingsForProvider")
        .WithDescription("Deletes bindings (league_providers, country_providers) for a specific provider")
        .Produces(200)
        .Produces(400);
    }
}

public record SelectiveResetRequest(List<string> Entities);
public record ResetBindingsRequest(List<string> BindingTypes);
