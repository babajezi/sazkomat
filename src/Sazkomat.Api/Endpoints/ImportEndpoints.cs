using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.DTOs;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using Sazkomat.DataImport.Services;

namespace Sazkomat.Api.Endpoints;

public static class ImportEndpoints
{
    public static void MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/import")
            .WithTags("Import")
            .WithOpenApi();

        // GET /api/import/leagues/available
        group.MapGet("/leagues/available", async (
            ILeagueRepository repository,
            [FromQuery] Guid? sportId) =>
        {
            // Return only enabled leagues for import with sport and country info
            var leagues = await repository.GetAllAsync(sportId, countryId: null, onlyEnabled: true, includeRelations: true);
            return Results.Ok(leagues);
        })
        .WithName("GetAvailableLeaguesForImport")
        .Produces(200);

        // POST /api/import/historical
        group.MapPost("/historical", async (
            [FromBody] HistoricalImportRequest request,
            IImportOrchestrator orchestrator) =>
        {
            var result = await orchestrator.StartHistoricalImportAsync(request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            }

            return Results.Ok(new
            {
                jobId = result.Value!.Id,
                message = "Import job started",
                job = result.Value
            });
        })
        .WithName("StartHistoricalImport")
        .Produces(200)
        .Produces(400);

        // GET /api/import/jobs/{jobId}
        group.MapGet("/jobs/{jobId:guid}", async (
            Guid jobId,
            IImportOrchestrator orchestrator) =>
        {
            var job = await orchestrator.GetJobStatusAsync(jobId);

            if (job == null)
            {
                return Results.NotFound(new { error = "Job not found" });
            }

            return Results.Ok(job);
        })
        .WithName("GetImportJobStatus")
        .Produces(200)
        .Produces(404);

        // GET /api/import/stats
        group.MapGet("/stats", async (
            [FromQuery] Guid leagueId,
            IImportOrchestrator orchestrator) =>
        {
            var stats = await orchestrator.GetImportStatsAsync(leagueId);

            if (stats == null)
            {
                return Results.NotFound(new { error = "No import data found for this league" });
            }

            return Results.Ok(stats);
        })
        .WithName("GetImportStats")
        .Produces(200)
        .Produces(404);

        // GET /api/import/matches
        group.MapGet("/matches", async (
            [FromQuery] Guid? leagueId,
            [FromQuery] string? season,
            [FromQuery] int? roundNumber,
            [FromQuery] string? result,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? teamName,
            [FromQuery] int? skip,
            [FromQuery] int? take,
            [FromQuery] string? sortBy,
            [FromQuery] bool? sortDescending,
            IMatchRepository matchRepository,
            ILeagueRepository leagueRepository,
            ISeasonRepository seasonRepository) =>
        {
            // Convert season string to seasonId if provided
            Guid? seasonId = null;
            if (!string.IsNullOrEmpty(season))
            {
                var seasonEntity = await seasonRepository.GetByNameAsync(season);
                seasonId = seasonEntity?.Id;
            }

            var filter = new MatchFilter
            {
                LeagueId = leagueId,
                SeasonId = seasonId,
                RoundNumber = roundNumber,
                Result = result,
                DateFrom = dateFrom,
                DateTo = dateTo,
                TeamName = teamName,
                Skip = skip,
                Take = take ?? 100, // Default to 100 if not specified
                SortBy = sortBy,
                SortDescending = sortDescending ?? true // Default to descending if not specified
            };

            var matches = await matchRepository.GetAllAsync(filter);
            var totalCount = await matchRepository.GetCountAsync(filter);

            // Get league info for each unique league
            var leagueIds = matches.Select(m => m.Round.LeagueId).Distinct().ToList();
            var leagues = new Dictionary<Guid, object>();

            foreach (var lid in leagueIds)
            {
                var league = await leagueRepository.GetByIdAsync(lid);
                if (league != null)
                {
                    leagues[lid] = new
                    {
                        id = league.Id,
                        name = league.Name,
                        displayName = league.DisplayName,
                        country = league.Country?.Name,
                        sport = league.Sport?.Name
                    };
                }
            }

            // Get season info for each unique season
            var seasonIds = matches.Select(m => m.Round.SeasonId).Distinct().ToList();
            var seasonNames = new Dictionary<Guid, string>();

            foreach (var sid in seasonIds)
            {
                var seasonEntity = await seasonRepository.GetByIdAsync(sid);
                if (seasonEntity != null)
                {
                    seasonNames[sid] = seasonEntity.Name;
                }
            }

            var response = matches.Select(m => new
            {
                id = m.Id,
                homeTeam = m.HomeTeam,
                awayTeam = m.AwayTeam,
                homeScore = m.HomeScore,
                awayScore = m.AwayScore,
                result = m.Result,
                homeOdds = m.HomeOdds,
                drawOdds = m.DrawOdds,
                awayOdds = m.AwayOdds,
                matchDate = m.MatchDate,
                betExplorerUrl = m.BetExplorerUrl,
                round = new
                {
                    id = m.Round.Id,
                    season = seasonNames.ContainsKey(m.Round.SeasonId) ? seasonNames[m.Round.SeasonId] : null,
                    roundNumber = m.Round.RoundNumber,
                    leagueId = m.Round.LeagueId
                },
                league = leagues.ContainsKey(m.Round.LeagueId) ? leagues[m.Round.LeagueId] : null
            });

            return Results.Ok(new
            {
                matches = response,
                totalCount,
                skip = filter.Skip ?? 0,
                take = filter.Take ?? 100
            });
        })
        .WithName("GetMatches")
        .Produces(200);

        // GET /api/import/dashboard
        group.MapGet("/dashboard", async (IImportOrchestrator orchestrator) =>
        {
            var stats = await orchestrator.GetDashboardStatsAsync();
            return Results.Ok(stats);
        })
        .WithName("GetDashboardStats")
        .Produces(200);

        // GET /api/import/rounds
        group.MapGet("/rounds", async (
            DataImportDbContext context,
            ISeasonRepository seasonRepository,
            ILeagueRepository leagueRepository,
            string? season = null,
            Guid? leagueId = null,
            int? skip = null,
            int? take = null,
            bool sortDescending = true) =>
        {
            var query = context.Rounds
                .Include(r => r.Matches)
                .AsQueryable();

            // Convert season string to seasonId if provided
            if (!string.IsNullOrEmpty(season))
            {
                var seasonEntity = await seasonRepository.GetByNameAsync(season);
                if (seasonEntity != null)
                {
                    query = query.Where(r => r.SeasonId == seasonEntity.Id);
                }
            }

            if (leagueId.HasValue)
            {
                query = query.Where(r => r.LeagueId == leagueId.Value);
            }

            var totalCount = await query.CountAsync();

            // Load all seasons for sorting (Season navigation is ignored, it's in different schema)
            var allSeasons = await seasonRepository.GetAllAsync();
            var seasonStartYears = allSeasons.ToDictionary(s => s.Id, s => s.StartYear);

            // Load all matching rounds for in-memory sorting
            var allRounds = await query.ToListAsync();

            // Sort in memory by season start year
            IEnumerable<Round> sortedRounds = sortDescending
                ? allRounds.OrderByDescending(r => seasonStartYears.GetValueOrDefault(r.SeasonId, 0))
                           .ThenByDescending(r => r.RoundNumber)
                : allRounds.OrderBy(r => seasonStartYears.GetValueOrDefault(r.SeasonId, 0))
                           .ThenBy(r => r.RoundNumber);

            // Apply pagination in memory
            if (skip.HasValue)
            {
                sortedRounds = sortedRounds.Skip(skip.Value);
            }

            var rounds = sortedRounds.Take(take ?? 50).ToList();

            // Build season names dictionary from already loaded seasons
            var seasons = allSeasons.ToDictionary(s => s.Id, s => s.Name);

            // Load leagues for all rounds
            var leagueIds = rounds.Select(r => r.LeagueId).Distinct().ToList();
            var leagues = new Dictionary<Guid, object>();
            foreach (var lid in leagueIds)
            {
                var league = await leagueRepository.GetByIdAsync(lid);
                if (league != null)
                {
                    leagues[lid] = new
                    {
                        id = league.Id,
                        name = league.Name,
                        displayName = league.DisplayName,
                        country = league.Country?.Name,
                        countryFlagEmoji = league.Country?.FlagEmoji,
                        sport = league.Sport?.Name
                    };
                }
            }

            var response = rounds.Select(r => new
            {
                id = r.Id,
                leagueId = r.LeagueId,
                league = leagues.ContainsKey(r.LeagueId) ? leagues[r.LeagueId] : null,
                season = seasons.ContainsKey(r.SeasonId) ? seasons[r.SeasonId] : null,
                roundNumber = r.RoundNumber,
                groupName = r.GroupName,
                matchesCount = r.MatchesCount,
                homeWins = r.HomeWins,
                draws = r.Draws,
                awayWins = r.AwayWins,
                summaryResult = r.SummaryResult,
                cumulativeOddsHome = r.CumulativeOddsHome,
                cumulativeOddsDraw = r.CumulativeOddsDraw,
                cumulativeOddsAway = r.CumulativeOddsAway,
                oddsComplete = r.OddsComplete,
                scrapedAt = r.ScrapedAt,
                dataSource = r.DataSource,
                matches = r.Matches.Select(m => new
                {
                    id = m.Id,
                    homeTeam = m.HomeTeam,
                    awayTeam = m.AwayTeam,
                    homeScore = m.HomeScore,
                    awayScore = m.AwayScore,
                    result = m.Result,
                    homeOdds = m.HomeOdds,
                    drawOdds = m.DrawOdds,
                    awayOdds = m.AwayOdds,
                    matchDate = m.MatchDate,
                    betExplorerUrl = m.BetExplorerUrl
                }).ToList()
            });

            return Results.Ok(new
            {
                rounds = response,
                totalCount,
                skip = skip ?? 0,
                take = take ?? 50
            });
        })
        .WithName("GetRounds")
        .Produces(200);

        // GET /api/import/leagues/{leagueId}/seasons/available
        group.MapGet("/leagues/{leagueId:guid}/seasons/available", async (
            Guid leagueId,
            ILeagueRepository leagueRepository,
            ISeasonScraper seasonScraper) =>
        {
            // Get league
            var league = await leagueRepository.GetByIdAsync(leagueId);
            if (league == null)
            {
                return Results.NotFound(new { error = "League not found" });
            }

            // Scrape available seasons
            var seasons = await seasonScraper.ScrapeAvailableSeasonsAsync(league);

            if (!seasons.Any())
            {
                return Results.Ok(new AvailableSeasonsResponse(
                    LeagueId: leagueId,
                    LeagueName: league.Name,
                    Seasons: new List<string>(),
                    CurrentSeason: null,
                    HistoricalSeasons: new List<string>()
                ));
            }

            // Sort seasons (newest first)
            var sortedSeasons = seasons.OrderByDescending(s => s).ToList();
            var currentSeason = sortedSeasons.First();
            var historicalSeasons = sortedSeasons.Skip(1).ToList();

            return Results.Ok(new AvailableSeasonsResponse(
                LeagueId: leagueId,
                LeagueName: league.Name,
                Seasons: sortedSeasons,
                CurrentSeason: currentSeason,
                HistoricalSeasons: historicalSeasons
            ));
        })
        .WithName("GetAvailableSeasons")
        .Produces<AvailableSeasonsResponse>(200)
        .Produces(404);

        // ===== NEW IMPORT FROM CACHE ENDPOINTS =====

        // POST /api/import/countries - Import selected countries from provider cache
        group.MapPost("/countries", async (
            [FromBody] ImportCountriesRequest request,
            IImportService importService) =>
        {
            try
            {
                var result = await importService.ImportCountriesFromCacheAsync(request.ProviderId, request.ProviderCountryIds);
                return Results.Ok(new
                {
                    jobId = result.JobId,
                    total = result.Total,
                    created = result.Created,
                    updated = result.Updated,
                    imported = result.Created + result.Updated,
                    skipped = result.Skipped,
                    errors = result.Errors,
                    message = $"Import dokončen: {result.Created} vytvořeno, {result.Updated} aktualizováno, {result.Skipped} přeskočeno"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ImportCountries")
        .Produces(200)
        .Produces(400);

        // POST /api/import/leagues - Import selected leagues from provider cache
        group.MapPost("/leagues", async (
            [FromBody] ImportLeaguesRequest request,
            IImportService importService) =>
        {
            try
            {
                var result = await importService.ImportLeaguesFromCacheAsync(request.ProviderId, request.ProviderLeagueIds);
                return Results.Ok(new
                {
                    jobId = result.JobId,
                    total = result.Total,
                    created = result.Created,
                    updated = result.Updated,
                    imported = result.Created + result.Updated,
                    skipped = result.Skipped,
                    errors = result.Errors,
                    message = $"Import dokončen: {result.Created} vytvořeno, {result.Updated} aktualizováno, {result.Skipped} přeskočeno"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ImportLeagues")
        .Produces(200)
        .Produces(400);

        // POST /api/import/seasons - Import selected seasons from provider cache
        group.MapPost("/seasons", async (
            [FromBody] ImportSeasonsRequest request,
            IImportService importService) =>
        {
            try
            {
                var result = await importService.ImportSeasonsFromCacheAsync(request.ProviderId, request.ProviderSeasonIds);
                return Results.Ok(new
                {
                    jobId = result.JobId,
                    total = result.Total,
                    created = result.Created,
                    updated = result.Updated,
                    imported = result.Created + result.Updated,
                    skipped = result.Skipped,
                    errors = result.Errors,
                    message = $"Import dokončen: {result.Created} vytvořeno, {result.Updated} aktualizováno, {result.Skipped} přeskočeno"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ImportSeasons")
        .Produces(200)
        .Produces(400);

        // GET /api/import/seasons/imported - Get all seasons that have imported rounds
        group.MapGet("/seasons/imported", async (
            DataImportDbContext context,
            ISeasonRepository seasonRepository) =>
        {
            // Get all unique season IDs from rounds
            var seasonIds = await context.Rounds
                .Select(r => r.SeasonId)
                .Distinct()
                .ToListAsync();

            // Load season details
            var seasons = new List<object>();
            foreach (var seasonId in seasonIds)
            {
                var season = await seasonRepository.GetByIdAsync(seasonId);
                if (season != null)
                {
                    var roundsCount = await context.Rounds.CountAsync(r => r.SeasonId == seasonId);
                    var matchesCount = await context.Matches.CountAsync(m => m.Round.SeasonId == seasonId);

                    seasons.Add(new
                    {
                        id = season.Id,
                        name = season.Name,
                        startYear = season.StartYear,
                        endYear = season.EndYear,
                        roundsCount,
                        matchesCount
                    });
                }
            }

            return Results.Ok(seasons.OrderByDescending(s => ((dynamic)s).startYear));
        })
        .WithName("GetImportedSeasons")
        .Produces(200);

        // GET /api/import/rounds/available - Get all available round numbers for filters
        group.MapGet("/rounds/available", async (
            DataImportDbContext context,
            ISeasonRepository seasonRepository,
            [FromQuery] Guid? leagueId,
            [FromQuery] string? season) =>
        {
            var query = context.Rounds.AsQueryable();

            // Apply filters
            if (leagueId.HasValue)
            {
                query = query.Where(r => r.LeagueId == leagueId.Value);
            }

            if (!string.IsNullOrEmpty(season))
            {
                var seasonEntity = await seasonRepository.GetByNameAsync(season);
                if (seasonEntity != null)
                {
                    query = query.Where(r => r.SeasonId == seasonEntity.Id);
                }
            }

            // Get unique round numbers sorted
            var roundNumbers = await query
                .Select(r => r.RoundNumber)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            return Results.Ok(roundNumbers);
        })
        .WithName("GetAvailableRounds")
        .Produces(200);

        // GET /api/import/cache/stats - Get cache vs imported statistics
        group.MapGet("/cache/stats", async (
            [FromQuery] Guid providerId,
            IImportService importService) =>
        {
            try
            {
                var stats = await importService.GetImportStatsAsync(providerId);
                return Results.Ok(stats);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetCacheImportStats")
        .Produces(200)
        .Produces(400);
    }
}

// Request DTOs for cache import endpoints
public record ImportCountriesRequest(Guid ProviderId, List<Guid>? ProviderCountryIds);
public record ImportLeaguesRequest(Guid ProviderId, List<Guid>? ProviderLeagueIds);
public record ImportSeasonsRequest(Guid ProviderId, List<Guid>? ProviderSeasonIds);
