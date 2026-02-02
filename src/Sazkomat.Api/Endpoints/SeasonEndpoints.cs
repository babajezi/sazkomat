using Microsoft.AspNetCore.Mvc;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.Api.Endpoints;

public static class SeasonEndpoints
{
    public static void MapSeasonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config/seasons")
            .WithTags("Seasons")
            .WithOpenApi();

        // GET /api/config/seasons
        group.MapGet("/", async (ISeasonRepository seasonRepository) =>
        {
            var seasons = await seasonRepository.GetAllAsync();
            var response = seasons.Select(s => new
            {
                id = s.Id,
                name = s.Name,
                startYear = s.StartYear,
                endYear = s.EndYear,
                createdAt = s.CreatedAt
            });
            return Results.Ok(response);
        })
        .WithName("GetAllSeasons")
        .Produces(200);

        // GET /api/config/seasons/available?leagueId={guid}
        group.MapGet("/available", async (
            [FromQuery] Guid leagueId,
            ILeagueSeasonRepository leagueSeasonRepository,
            ISeasonRepository seasonRepository,
            IScraperRecipeRepository recipeRepository) =>
        {
            var leagueSeasons = await leagueSeasonRepository.GetByLeagueIdAsync(leagueId);

            var seasonIds = leagueSeasons
                .Where(ls => ls.IsAvailableOnBetExplorer)
                .Select(ls => ls.SeasonId)
                .ToList();

            // Pre-load recipe names for all used recipes
            var recipeIds = leagueSeasons
                .Where(ls => ls.LastSuccessfulRecipeId.HasValue)
                .Select(ls => ls.LastSuccessfulRecipeId!.Value)
                .Distinct()
                .ToList();
            var recipeNames = new Dictionary<Guid, string>();
            foreach (var recipeId in recipeIds)
            {
                var recipe = await recipeRepository.GetByIdAsync(recipeId);
                if (recipe != null)
                {
                    recipeNames[recipeId] = recipe.Name;
                }
            }

            var seasons = new List<object>();
            foreach (var seasonId in seasonIds)
            {
                var season = await seasonRepository.GetByIdAsync(seasonId);
                if (season != null)
                {
                    var leagueSeason = leagueSeasons.First(ls => ls.SeasonId == seasonId);
                    var recipeName = leagueSeason.LastSuccessfulRecipeId.HasValue &&
                                     recipeNames.TryGetValue(leagueSeason.LastSuccessfulRecipeId.Value, out var name)
                        ? name : null;

                    seasons.Add(new
                    {
                        id = season.Id,
                        name = season.Name,
                        startYear = season.StartYear,
                        endYear = season.EndYear,
                        hasData = leagueSeason.HasData,
                        noDataReason = leagueSeason.NoDataReason?.ToString(),
                        noDataNote = leagueSeason.NoDataNote,
                        hasOdds = leagueSeason.HasOdds,
                        roundsCount = leagueSeason.RoundsCount,
                        matchesCount = leagueSeason.MatchesCount,
                        lastScrapedAt = leagueSeason.LastScrapedAt,
                        lastSuccessfulRecipeName = recipeName
                    });
                }
            }

            return Results.Ok(seasons.OrderByDescending(s => ((dynamic)s).startYear));
        })
        .WithName("GetAvailableSeasonsForLeague")
        .Produces(200);

        // GET /api/config/league-seasons?leagueId={guid}
        group.MapGet("/league-seasons", async (
            [FromQuery] Guid? leagueId,
            ILeagueSeasonRepository leagueSeasonRepository,
            ISeasonRepository seasonRepository,
            ILeagueRepository leagueRepository,
            IScraperRecipeRepository recipeRepository) =>
        {
            List<dynamic> leagueSeasons;

            // Helper to get recipe names
            var recipeNameCache = new Dictionary<Guid, string>();
            async Task<string?> GetRecipeName(Guid? recipeId)
            {
                if (!recipeId.HasValue) return null;
                if (recipeNameCache.TryGetValue(recipeId.Value, out var cached)) return cached;
                var recipe = await recipeRepository.GetByIdAsync(recipeId.Value);
                if (recipe != null)
                {
                    recipeNameCache[recipeId.Value] = recipe.Name;
                    return recipe.Name;
                }
                return null;
            }

            if (leagueId.HasValue)
            {
                var seasons = await leagueSeasonRepository.GetByLeagueIdAsync(leagueId.Value);
                leagueSeasons = new List<dynamic>();

                foreach (var ls in seasons)
                {
                    var season = await seasonRepository.GetByIdAsync(ls.SeasonId);
                    if (season != null)
                    {
                        leagueSeasons.Add(new
                        {
                            id = ls.Id,
                            leagueId = ls.LeagueId,
                            seasonId = ls.SeasonId,
                            seasonName = season.Name,
                            startYear = season.StartYear,
                            endYear = season.EndYear,
                            isAvailableOnBetExplorer = ls.IsAvailableOnBetExplorer,
                            hasData = ls.HasData,
                            noDataReason = ls.NoDataReason?.ToString(),
                            noDataNote = ls.NoDataNote,
                            hasOdds = ls.HasOdds,
                            roundsCount = ls.RoundsCount,
                            matchesCount = ls.MatchesCount,
                            lastScrapedAt = ls.LastScrapedAt,
                            syncEnabled = ls.SyncEnabled,
                            isCurrent = ls.IsCurrent,
                            syncMode = ls.SyncMode.ToString(),
                            lastDataSyncAt = ls.LastDataSyncAt,
                            lastSuccessfulRecipeName = await GetRecipeName(ls.LastSuccessfulRecipeId)
                        });
                    }
                }
            }
            else
            {
                var allLeagueSeasons = await leagueSeasonRepository.GetAllAsync();
                leagueSeasons = new List<dynamic>();

                foreach (var ls in allLeagueSeasons)
                {
                    var season = await seasonRepository.GetByIdAsync(ls.SeasonId);
                    var league = await leagueRepository.GetByIdAsync(ls.LeagueId);

                    if (season != null && league != null)
                    {
                        leagueSeasons.Add(new
                        {
                            id = ls.Id,
                            leagueId = ls.LeagueId,
                            leagueName = league.Name,
                            seasonId = ls.SeasonId,
                            seasonName = season.Name,
                            startYear = season.StartYear,
                            endYear = season.EndYear,
                            isAvailableOnBetExplorer = ls.IsAvailableOnBetExplorer,
                            hasData = ls.HasData,
                            noDataReason = ls.NoDataReason?.ToString(),
                            noDataNote = ls.NoDataNote,
                            hasOdds = ls.HasOdds,
                            roundsCount = ls.RoundsCount,
                            matchesCount = ls.MatchesCount,
                            lastScrapedAt = ls.LastScrapedAt,
                            syncEnabled = ls.SyncEnabled,
                            isCurrent = ls.IsCurrent,
                            syncMode = ls.SyncMode.ToString(),
                            lastDataSyncAt = ls.LastDataSyncAt,
                            lastSuccessfulRecipeName = await GetRecipeName(ls.LastSuccessfulRecipeId)
                        });
                    }
                }
            }

            return Results.Ok(leagueSeasons.OrderByDescending(ls => ls.startYear));
        })
        .WithName("GetLeagueSeasons")
        .Produces(200);

        // PATCH /api/config/seasons/league-seasons/{id}/sync-enabled
        group.MapPatch("/league-seasons/{id}/sync-enabled", async (
            Guid id,
            [FromBody] UpdateSyncEnabledRequest request,
            ILeagueSeasonRepository leagueSeasonRepository) =>
        {
            await leagueSeasonRepository.UpdateSyncEnabledAsync(id, request.Enabled);
            return Results.NoContent();
        })
        .WithName("UpdateSyncEnabled")
        .WithSummary("Enable or disable sync for a league season")
        .Produces(204)
        .Produces(404);
    }
}

public record UpdateSyncEnabledRequest(bool Enabled);
