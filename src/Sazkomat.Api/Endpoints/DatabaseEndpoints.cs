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
    }
}
