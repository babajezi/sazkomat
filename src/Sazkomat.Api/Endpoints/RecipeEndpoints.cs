using Microsoft.AspNetCore.Mvc;
using Sazkomat.Data.Debug;
using Sazkomat.Data.Entities;
using Sazkomat.Data.Repositories;
using Sazkomat.Data.Services;
using Sazkomat.Configuration.Repositories;
using System.Text.Json;

namespace Sazkomat.Api.Endpoints;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recipes")
            .WithTags("Recipes");

        // GET /api/recipes - List all recipes
        group.MapGet("/", async (IScraperRecipeRepository repository) =>
        {
            var recipes = await repository.GetAllAsync();
            return Results.Ok(recipes.Select(r => new RecipeListDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Provider = r.Provider,
                PageType = r.PageType,
                Priority = r.Priority,
                IsActive = r.IsActive,
                TotalAttempts = r.TotalAttempts,
                SuccessfulAttempts = r.SuccessfulAttempts,
                SuccessRate = r.SuccessRate,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }));
        })
        .WithName("GetRecipes")
        .Produces<IEnumerable<RecipeListDto>>(200);

        // GET /api/recipes/{id} - Get recipe detail
        group.MapGet("/{id:guid}", async (Guid id, IScraperRecipeRepository repository) =>
        {
            var recipe = await repository.GetByIdAsync(id);
            if (recipe == null)
            {
                return Results.NotFound(new { error = "Recipe not found" });
            }

            return Results.Ok(new RecipeDetailDto
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Description = recipe.Description,
                Provider = recipe.Provider,
                PageType = recipe.PageType,
                Priority = recipe.Priority,
                IsActive = recipe.IsActive,
                ActionsJson = recipe.ActionsJson,
                RoundHeaderSelector = recipe.RoundHeaderSelector,
                GroupPatternRegex = recipe.GroupPatternRegex,
                MatchRowSelector = recipe.MatchRowSelector,
                OddsCellSelector = recipe.OddsCellSelector,
                TotalAttempts = recipe.TotalAttempts,
                SuccessfulAttempts = recipe.SuccessfulAttempts,
                SuccessRate = recipe.SuccessRate,
                CreatedAt = recipe.CreatedAt,
                UpdatedAt = recipe.UpdatedAt
            });
        })
        .WithName("GetRecipe")
        .Produces<RecipeDetailDto>(200)
        .Produces(404);

        // POST /api/recipes - Create recipe
        group.MapPost("/", async (
            [FromBody] CreateRecipeRequest request,
            IScraperRecipeRepository repository) =>
        {
            // Validate JSON
            try
            {
                JsonSerializer.Deserialize<List<DebugAction>>(request.ActionsJson);
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = $"Invalid ActionsJson: {ex.Message}" });
            }

            var recipe = new ScraperRecipe
            {
                Name = request.Name,
                Description = request.Description,
                Provider = request.Provider,
                PageType = request.PageType,
                Priority = request.Priority,
                IsActive = request.IsActive,
                ActionsJson = request.ActionsJson,
                RoundHeaderSelector = request.RoundHeaderSelector,
                GroupPatternRegex = request.GroupPatternRegex,
                MatchRowSelector = request.MatchRowSelector,
                OddsCellSelector = request.OddsCellSelector
            };

            var created = await repository.CreateAsync(recipe);

            return Results.Created($"/api/recipes/{created.Id}", new { id = created.Id });
        })
        .WithName("CreateRecipe")
        .Produces(201)
        .Produces(400);

        // PUT /api/recipes/{id} - Update recipe
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateRecipeRequest request,
            IScraperRecipeRepository repository) =>
        {
            var recipe = await repository.GetByIdAsync(id);
            if (recipe == null)
            {
                return Results.NotFound(new { error = "Recipe not found" });
            }

            // Validate JSON if provided
            if (request.ActionsJson != null)
            {
                try
                {
                    JsonSerializer.Deserialize<List<DebugAction>>(request.ActionsJson);
                }
                catch (JsonException ex)
                {
                    return Results.BadRequest(new { error = $"Invalid ActionsJson: {ex.Message}" });
                }
            }

            // Update fields
            if (request.Name != null) recipe.Name = request.Name;
            if (request.Description != null) recipe.Description = request.Description;
            if (request.Provider != null) recipe.Provider = request.Provider;
            if (request.PageType != null) recipe.PageType = request.PageType;
            if (request.Priority.HasValue) recipe.Priority = request.Priority.Value;
            if (request.IsActive.HasValue) recipe.IsActive = request.IsActive.Value;
            if (request.ActionsJson != null) recipe.ActionsJson = request.ActionsJson;
            if (request.RoundHeaderSelector != null) recipe.RoundHeaderSelector = request.RoundHeaderSelector;
            if (request.GroupPatternRegex != null) recipe.GroupPatternRegex = request.GroupPatternRegex;
            if (request.MatchRowSelector != null) recipe.MatchRowSelector = request.MatchRowSelector;
            if (request.OddsCellSelector != null) recipe.OddsCellSelector = request.OddsCellSelector;

            await repository.UpdateAsync(recipe);

            return Results.Ok(new { message = "Recipe updated" });
        })
        .WithName("UpdateRecipe")
        .Produces(200)
        .Produces(400)
        .Produces(404);

        // DELETE /api/recipes/{id} - Delete recipe
        group.MapDelete("/{id:guid}", async (Guid id, IScraperRecipeRepository repository) =>
        {
            var recipe = await repository.GetByIdAsync(id);
            if (recipe == null)
            {
                return Results.NotFound(new { error = "Recipe not found" });
            }

            await repository.DeleteAsync(id);

            return Results.Ok(new { message = "Recipe deleted" });
        })
        .WithName("DeleteRecipe")
        .Produces(200)
        .Produces(404);

        // GET /api/recipes/stats - Get recipe statistics
        group.MapGet("/stats", async (IScraperRecipeRepository repository) =>
        {
            var stats = await repository.GetStatsAsync();
            return Results.Ok(stats);
        })
        .WithName("GetRecipeStats")
        .Produces<IEnumerable<RecipeStats>>(200);

        // POST /api/recipes/{id}/test - Test recipe on a specific league/season
        group.MapPost("/{id:guid}/test", async (
            Guid id,
            [FromBody] TestRecipeRequest request,
            IScraperRecipeRepository recipeRepository,
            ILeagueRepository leagueRepository,
            RecipeExecutorService recipeExecutor,
            ILogger<Program> logger) =>
        {
            var recipe = await recipeRepository.GetByIdAsync(id);
            if (recipe == null)
            {
                return Results.NotFound(new { error = "Recipe not found" });
            }

            var league = await leagueRepository.GetByIdAsync(request.LeagueId);
            if (league == null)
            {
                return Results.NotFound(new { error = "League not found" });
            }

            var countrySlug = league.Country?.Code?.ToLowerInvariant() ?? "unknown";
            var baseUrl = $"https://www.betexplorer.com/football/{countrySlug}/{league.BetExplorerSlug}/";

            var variables = new Dictionary<string, string>
            {
                ["baseUrl"] = baseUrl,
                ["season"] = request.Season
            };

            await using var debugService = new ScraperDebugService(
                logger as ILogger<ScraperDebugService> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ScraperDebugService>.Instance);

            var execResult = await recipeExecutor.ExecuteRecipeAsync(debugService, recipe, variables);

            if (!execResult.Success)
            {
                return Results.Ok(new TestRecipeResponse
                {
                    Success = false,
                    Error = execResult.ErrorReason,
                    Logs = execResult.Logs,
                    DurationMs = execResult.DurationMs
                });
            }

            var scrapeResult = recipeExecutor.ParseHtmlWithRules(
                execResult.Html!, recipe, league.Id, request.Season);

            return Results.Ok(new TestRecipeResponse
            {
                Success = scrapeResult.IsSuccess && scrapeResult.Rounds.Count > 0,
                RoundsFound = scrapeResult.Rounds.Count,
                TotalMatches = scrapeResult.Rounds.Sum(r => r.MatchesCount),
                TotalRoundHeadersFound = scrapeResult.TotalRoundHeadersFound,
                TotalMatchRowsFound = scrapeResult.TotalMatchRowsFound,
                FailureReason = scrapeResult.FailureReason?.ToString(),
                RoundsSample = scrapeResult.Rounds.Take(5).Select(r => new RoundSampleDto
                {
                    RoundNumber = r.RoundNumber,
                    GroupName = r.GroupName,
                    MatchesCount = r.MatchesCount,
                    SummaryResult = r.SummaryResult
                }).ToList(),
                HtmlLength = execResult.Html?.Length ?? 0,
                Logs = execResult.Logs,
                DurationMs = execResult.DurationMs,
                Error = scrapeResult.IsSuccess ? null : scrapeResult.ErrorMessage
            });
        })
        .WithName("TestRecipe")
        .Produces<TestRecipeResponse>(200)
        .Produces(404);

        // GET /api/recipes/by-provider/{provider}/{pageType} - Get recipes by provider/page type
        group.MapGet("/by-provider/{provider}/{pageType}", async (
            string provider,
            string pageType,
            IScraperRecipeRepository repository) =>
        {
            var recipes = await repository.GetByProviderAndPageTypeAsync(provider, pageType);
            return Results.Ok(recipes.Select(r => new RecipeListDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Provider = r.Provider,
                PageType = r.PageType,
                Priority = r.Priority,
                IsActive = r.IsActive,
                TotalAttempts = r.TotalAttempts,
                SuccessfulAttempts = r.SuccessfulAttempts,
                SuccessRate = r.SuccessRate,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }));
        })
        .WithName("GetRecipesByProvider")
        .Produces<IEnumerable<RecipeListDto>>(200);
    }
}

// DTOs
public record RecipeListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string Provider { get; init; } = "";
    public string PageType { get; init; } = "";
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public int TotalAttempts { get; init; }
    public int SuccessfulAttempts { get; init; }
    public decimal SuccessRate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record RecipeDetailDto : RecipeListDto
{
    public string ActionsJson { get; init; } = "[]";
    public string RoundHeaderSelector { get; init; } = "";
    public string? GroupPatternRegex { get; init; }
    public string MatchRowSelector { get; init; } = "";
    public string? OddsCellSelector { get; init; }
}

public record CreateRecipeRequest
{
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string Provider { get; init; } = "betexplorer";
    public string PageType { get; init; } = "results";
    public int Priority { get; init; } = 100;
    public bool IsActive { get; init; } = true;
    public string ActionsJson { get; init; } = "[]";
    public string RoundHeaderSelector { get; init; } = ".//th[contains(text(), 'Round')]";
    public string? GroupPatternRegex { get; init; }
    public string MatchRowSelector { get; init; } = ".//tr[td[contains(@class, 'h-text-left')]]";
    public string? OddsCellSelector { get; init; }
}

public record UpdateRecipeRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Provider { get; init; }
    public string? PageType { get; init; }
    public int? Priority { get; init; }
    public bool? IsActive { get; init; }
    public string? ActionsJson { get; init; }
    public string? RoundHeaderSelector { get; init; }
    public string? GroupPatternRegex { get; init; }
    public string? MatchRowSelector { get; init; }
    public string? OddsCellSelector { get; init; }
}

public record TestRecipeRequest
{
    public Guid LeagueId { get; init; }
    public string Season { get; init; } = "";
}

public record TestRecipeResponse
{
    public bool Success { get; init; }
    public int RoundsFound { get; init; }
    public int TotalMatches { get; init; }
    /// <summary>
    /// Total round headers found on page (may differ from RoundsFound if rounds have no matches)
    /// </summary>
    public int TotalRoundHeadersFound { get; init; }
    /// <summary>
    /// Total match rows found on page (even if not assigned to rounds)
    /// </summary>
    public int TotalMatchRowsFound { get; init; }
    /// <summary>
    /// The failure reason from ScrapeResult (NoRoundsFound, NoResults, etc.)
    /// </summary>
    public string? FailureReason { get; init; }
    public List<RoundSampleDto> RoundsSample { get; init; } = new();
    public int HtmlLength { get; init; }
    public List<string> Logs { get; init; } = new();
    public long DurationMs { get; init; }
    public string? Error { get; init; }
}

public record RoundSampleDto
{
    public int RoundNumber { get; init; }
    public string? GroupName { get; init; }
    public int MatchesCount { get; init; }
    public string SummaryResult { get; init; } = "";
}
