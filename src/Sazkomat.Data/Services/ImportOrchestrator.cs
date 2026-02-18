using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;
using Sazkomat.Core.Common;
using Sazkomat.Data.DTOs;
using Sazkomat.Data.Entities;
using Sazkomat.Data.Repositories;
using Sazkomat.Data.Scrapers;
using System.Text.Json;

namespace Sazkomat.Data.Services;

public class ImportOrchestrator : IImportOrchestrator
{
    private readonly ILeagueRepository _leagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IRoundRepository _roundRepository;
    private readonly IImportJobRepository _importJobRepository;
    private readonly ISeasonService _seasonService;
    private readonly ISeasonScraper _seasonScraper;
    private readonly IDataProviderRepository _dataProviderRepository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportOrchestrator> _logger;

    public ImportOrchestrator(
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        IRoundRepository roundRepository,
        IImportJobRepository importJobRepository,
        ISeasonService seasonService,
        ISeasonScraper seasonScraper,
        IDataProviderRepository dataProviderRepository,
        IServiceScopeFactory scopeFactory,
        ILogger<ImportOrchestrator> logger)
    {
        _leagueRepository = leagueRepository;
        _seasonRepository = seasonRepository;
        _roundRepository = roundRepository;
        _importJobRepository = importJobRepository;
        _seasonService = seasonService;
        _seasonScraper = seasonScraper;
        _dataProviderRepository = dataProviderRepository;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Result<ImportJob>> StartHistoricalImportAsync(HistoricalImportRequest request)
    {
        try
        {
            // Validate that at least one league is provided
            if (request.LeagueIds == null || !request.LeagueIds.Any())
            {
                return Result<ImportJob>.Failure("At least one league must be provided");
            }

            // Validate that at least one season is provided (unless ImportAllHistorical is true)
            if (!request.ImportAllHistorical && (request.Seasons == null || !request.Seasons.Any()))
            {
                return Result<ImportJob>.Failure("At least one season must be provided when not using ImportAllHistorical");
            }

            // Validate all leagues exist and are enabled
            var leagues = new List<Configuration.Entities.League>();
            foreach (var leagueId in request.LeagueIds)
            {
                var league = await _leagueRepository.GetByIdAsync(leagueId);
                if (league == null)
                {
                    return Result<ImportJob>.Failure($"League with ID {leagueId} not found");
                }

                if (!league.IsActive)
                {
                    return Result<ImportJob>.Failure($"League '{league.Name}' is not active");
                }

                leagues.Add(league);
            }

            // Create import jobs for each league
            var jobs = new List<ImportJob>();

            foreach (var league in leagues)
            {
                List<string> seasonsToImport;

                // If ImportAllHistorical, scrape available seasons and exclude current
                if (request.ImportAllHistorical)
                {
                    _logger.LogInformation(
                        "Scraping available seasons for {LeagueName} (ImportAllHistorical mode)",
                        league.Name);

                    var allSeasons = await _seasonScraper.ScrapeAvailableSeasonsAsync(league);
                    if (!allSeasons.Any())
                    {
                        return Result<ImportJob>.Failure($"No seasons found for league '{league.Name}'");
                    }

                    // Get BetExplorer provider to check current season patterns
                    var betExplorerProvider = await _dataProviderRepository.GetByIdAsync(
                        Guid.Parse("a0000000-0000-0000-0000-000000000001"));

                    var currentSeasonPatterns = new List<string>();
                    if (betExplorerProvider != null && !string.IsNullOrEmpty(betExplorerProvider.CurrentSeasonPatterns))
                    {
                        try
                        {
                            currentSeasonPatterns = JsonSerializer.Deserialize<List<string>>(
                                betExplorerProvider.CurrentSeasonPatterns) ?? new List<string>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse CurrentSeasonPatterns, using fallback");
                        }
                    }

                    // Filter out current seasons using provider patterns
                    var excludedSeasons = new List<string>();
                    seasonsToImport = allSeasons.Where(season =>
                    {
                        var isCurrent = currentSeasonPatterns.Any(pattern =>
                            season.Contains(pattern, StringComparison.OrdinalIgnoreCase));
                        if (isCurrent)
                        {
                            excludedSeasons.Add(season);
                        }
                        return !isCurrent;
                    }).ToList();

                    _logger.LogInformation(
                        "Found {TotalSeasons} seasons for {LeagueName}, will import {HistoricalCount} historical seasons (excluded current: {ExcludedSeasons})",
                        allSeasons.Count, league.Name, seasonsToImport.Count, string.Join(", ", excludedSeasons));
                }
                else
                {
                    seasonsToImport = request.Seasons!; // Already validated above
                }

                // Convert season names to season IDs
                var seasonIds = new List<Guid>();
                foreach (var seasonName in seasonsToImport)
                {
                    var leagueSeasonResult = await _seasonService.GetOrCreateLeagueSeasonAsync(league.Id, seasonName);
                    if (leagueSeasonResult.IsSuccess && leagueSeasonResult.Value != null)
                    {
                        seasonIds.Add(leagueSeasonResult.Value.SeasonId);
                    }
                    else
                    {
                        return Result<ImportJob>.Failure($"Failed to create season '{seasonName}': {leagueSeasonResult.Error}");
                    }
                }

                var job = new ImportJob
                {
                    LeagueId = league.Id,
                    Type = ImportJobType.Historical,
                    Status = ImportJobStatus.Pending,
                    SeasonIds = seasonIds,
                    IncludeWithoutOdds = request.IncludeWithoutOdds,
                    StartedAt = DateTime.UtcNow,
                    Progress = new ImportProgressData
                    {
                        TotalSeasons = seasonIds.Count,
                        ProcessedSeasonIds = new List<Guid>(),
                        ProcessedRounds = 0,
                        Errors = new List<string>()
                    }
                };

                var createdJob = await _importJobRepository.CreateAsync(job);
                jobs.Add(createdJob);

                // Start background import for this job (fire-and-forget)
                // Create a new scope to avoid disposed dependencies
                var jobId = createdJob.Id;
                _ = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    await ExecuteImportJobInScopeAsync(scope.ServiceProvider, jobId);
                });

                _logger.LogInformation(
                    "Created import job {JobId} for league {LeagueName} with {SeasonCount} seasons",
                    createdJob.Id, league.Name, seasonsToImport.Count);
            }

            // Return the first job (or we could return all jobs)
            return Result<ImportJob>.Success(jobs.First());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting historical import");
            return Result<ImportJob>.Failure($"Error starting import: {ex.Message}");
        }
    }

    public async Task<ImportJob?> GetJobStatusAsync(Guid jobId)
    {
        return await _importJobRepository.GetByIdAsync(jobId);
    }

    public async Task<ImportStatsResponse?> GetImportStatsAsync(Guid leagueId)
    {
        var rounds = await _roundRepository.GetByLeagueAsync(leagueId);

        if (!rounds.Any())
        {
            return null;
        }

        // Group by SeasonId and get count
        var roundsBySeasonId = rounds
            .GroupBy(r => r.SeasonId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Load season entities
        var seasonIds = roundsBySeasonId.Keys.ToList();
        var seasons = new Dictionary<Guid, string>();
        foreach (var seasonId in seasonIds)
        {
            var season = await _seasonRepository.GetByIdAsync(seasonId);
            if (season != null)
            {
                seasons[seasonId] = season.Name;
            }
        }

        // Convert to season name dictionary
        var roundsBySeason = roundsBySeasonId
            .Where(kvp => seasons.ContainsKey(kvp.Key))
            .ToDictionary(kvp => seasons[kvp.Key], kvp => kvp.Value);

        var orderedSeasonNames = roundsBySeason.Keys.OrderBy(s => s).ToList();

        return new ImportStatsResponse(
            TotalRounds: rounds.Count,
            TotalSeasons: roundsBySeason.Count,
            OldestSeason: orderedSeasonNames.FirstOrDefault(),
            NewestSeason: orderedSeasonNames.LastOrDefault(),
            RoundsBySeason: roundsBySeason
        );
    }

    private async Task ExecuteImportJobInScopeAsync(IServiceProvider serviceProvider, Guid jobId)
    {
        // Resolve scoped dependencies
        var importJobRepository = serviceProvider.GetRequiredService<IImportJobRepository>();
        var leagueRepository = serviceProvider.GetRequiredService<ILeagueRepository>();
        var seasonRepository = serviceProvider.GetRequiredService<ISeasonRepository>();
        var leagueSeasonRepository = serviceProvider.GetRequiredService<ILeagueSeasonRepository>();
        var roundRepository = serviceProvider.GetRequiredService<IRoundRepository>();
        var scraperFactory = serviceProvider.GetRequiredService<ScraperFactory>();
        var logger = serviceProvider.GetRequiredService<ILogger<ImportOrchestrator>>();

        // Load job from database
        var job = await importJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            logger.LogError("Import job {JobId} not found", jobId);
            return;
        }

        await ExecuteImportJobAsync(job, importJobRepository, leagueRepository, seasonRepository, leagueSeasonRepository, roundRepository, scraperFactory, logger);
    }

    private async Task ExecuteImportJobAsync(
        ImportJob job,
        IImportJobRepository importJobRepository,
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        ILeagueSeasonRepository leagueSeasonRepository,
        IRoundRepository roundRepository,
        ScraperFactory scraperFactory,
        ILogger<ImportOrchestrator> logger)
    {
        try
        {
            logger.LogInformation("Starting execution of import job {JobId}", job.Id);

            // Update job status to Running
            job.Status = ImportJobStatus.Running;
            await importJobRepository.UpdateAsync(job);

            // Get the league
            var league = await leagueRepository.GetByIdAsync(job.LeagueId);
            if (league == null)
            {
                throw new Exception($"League {job.LeagueId} not found");
            }

            // Get the appropriate scraper
            var scraper = scraperFactory.GetScraper(league.Sport);

            var hasErrors = false;

            // Process each season
            foreach (var seasonId in job.SeasonIds)
            {
                // Load season entity outside try/catch so it's available in error handling
                var season = await seasonRepository.GetByIdAsync(seasonId);
                if (season == null)
                {
                    logger.LogError("Season {SeasonId} not found", seasonId);
                    continue;
                }

                var seasonName = season.Name;

                try
                {
                    logger.LogInformation(
                        "Scraping {LeagueName} season {SeasonName}...",
                        league.Name, seasonName);

                    job.Progress.CurrentSeasonId = seasonId;
                    await importJobRepository.UpdateAsync(job);

                    // Scrape the season
                    logger.LogInformation("Calling scraper for {LeagueName} {SeasonName}...", league.Name, seasonName);
                    var scrapeResult = await scraper.ScrapeSeasonAsync(league, seasonName);
                    var rounds = scrapeResult.Rounds;
                    logger.LogInformation("Scraper returned {RoundCount} rounds for {LeagueName} {SeasonName}",
                        rounds.Count, league.Name, seasonName);

                    // Save rounds to database
                    logger.LogInformation("Saving {RoundCount} rounds to database...", rounds.Count);
                    var savedCount = 0;
                    var skippedCount = 0;

                    foreach (var round in rounds)
                    {
                        // Set the SeasonId for the round
                        round.SeasonId = seasonId;

                        // Check if round already exists
                        var existingRound = await roundRepository.GetByLeagueSeasonRoundAsync(
                            league.Id, seasonId, round.RoundNumber);

                        if (existingRound == null)
                        {
                            // Create new round
                            logger.LogInformation(
                                "[IMPORT] Saving NEW Round {RoundNumber}/{TotalRounds}: {Summary} ({MatchCount} matches, odds: {OddsStatus})",
                                round.RoundNumber, rounds.Count, round.SummaryResult, round.MatchesCount, round.OddsComplete);

                            await roundRepository.CreateAsync(round);
                            job.Progress.ProcessedRounds++;
                            savedCount++;

                            logger.LogInformation("[IMPORT] ✓ Round {RoundNumber} saved to database", round.RoundNumber);
                        }
                        else
                        {
                            // Update existing round with new data (handles postponed matches)
                            logger.LogInformation(
                                "[IMPORT] ⟳ Round {RoundNumber} already exists (old: {OldMatches} matches, new: {NewMatches} matches)",
                                round.RoundNumber, existingRound.MatchesCount, round.MatchesCount);

                            // Delete old round (with cascade delete of matches)
                            await roundRepository.DeleteAsync(existingRound.Id);

                            // Create new round with updated data
                            await roundRepository.CreateAsync(round);
                            savedCount++;

                            logger.LogInformation(
                                "[IMPORT] ✓ Round {RoundNumber} UPDATED with {MatchCount} matches (was {OldMatchCount})",
                                round.RoundNumber, round.MatchesCount, existingRound.MatchesCount);
                        }
                    }

                    logger.LogInformation("Database save complete: {SavedCount} saved, {SkippedCount} skipped",
                        savedCount, skippedCount);

                    job.Progress.ProcessedSeasonIds.Add(seasonId);
                    await importJobRepository.UpdateAsync(job);

                    // Update LeagueSeason metadata
                    var roundsCount = rounds.Count;
                    var matchesCount = rounds.Sum(r => r.MatchesCount);
                    var hasOdds = rounds.Any(r => r.OddsComplete == "Yes");
                    await leagueSeasonRepository.UpdateMetadataAsync(league.Id, seasonId, roundsCount, matchesCount, hasOdds);

                    logger.LogInformation(
                        "Successfully processed {LeagueName} season {SeasonName}: {TotalRounds} rounds ({SavedCount} new, {SkippedCount} existing)",
                        league.Name, seasonName, rounds.Count, savedCount, skippedCount);
                }
                catch (Exception ex)
                {
                    hasErrors = true;
                    var errorMessage = $"Error scraping season {seasonName}: {ex.Message}";
                    job.Progress.Errors.Add(errorMessage);
                    logger.LogError(ex, "Error scraping {LeagueName} season {SeasonName}", league.Name, seasonName);

                    // Continue with other seasons (partial success)
                    continue;
                }
            }

            // Update final job status
            job.Status = hasErrors ? ImportJobStatus.PartialSuccess : ImportJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.Progress.CurrentSeasonId = null;
            await importJobRepository.UpdateAsync(job);

            logger.LogInformation(
                "Completed import job {JobId} with status {Status}. " +
                "Processed {ProcessedSeasons}/{TotalSeasons} seasons, {ProcessedRounds} rounds, {ErrorCount} errors",
                job.Id, job.Status, job.Progress.ProcessedSeasonIds.Count,
                job.Progress.TotalSeasons, job.Progress.ProcessedRounds, job.Progress.Errors.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error executing import job {JobId}", job.Id);

            job.Status = ImportJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.Progress.Errors.Add($"Fatal error: {ex.Message}");
            await importJobRepository.UpdateAsync(job);
        }
    }

    public async Task<DashboardStatsResponse> GetDashboardStatsAsync()
    {
        // Get all rounds
        var allRounds = await _roundRepository.GetAllAsync();

        // Get all import jobs
        var allJobs = await _importJobRepository.GetAllAsync();

        // Calculate overall stats
        var leagueIds = allRounds.Select(r => r.LeagueId).Distinct().ToList();
        var seasonIds = allRounds.Select(r => r.SeasonId).Distinct().ToList();
        var totalMatches = allRounds.Sum(r => r.MatchesCount);

        var overallStats = new OverallStats(
            TotalLeagues: leagueIds.Count,
            TotalRounds: allRounds.Count,
            TotalSeasons: seasonIds.Count,
            TotalMatches: totalMatches
        );

        // Calculate match results stats
        var totalHomeWins = allRounds.Sum(r => r.HomeWins);
        var totalDraws = allRounds.Sum(r => r.Draws);
        var totalAwayWins = allRounds.Sum(r => r.AwayWins);
        var totalGames = totalHomeWins + totalDraws + totalAwayWins;

        var resultsStats = new MatchResultsStats(
            HomeWins: totalHomeWins,
            Draws: totalDraws,
            AwayWins: totalAwayWins,
            HomeWinPercentage: totalGames > 0 ? Math.Round((decimal)totalHomeWins / totalGames * 100, 2) : 0,
            DrawPercentage: totalGames > 0 ? Math.Round((decimal)totalDraws / totalGames * 100, 2) : 0,
            AwayWinPercentage: totalGames > 0 ? Math.Round((decimal)totalAwayWins / totalGames * 100, 2) : 0
        );

        // Get top leagues by rounds count
        var leagueRounds = allRounds
            .GroupBy(r => r.LeagueId)
            .Select(g => new
            {
                LeagueId = g.Key,
                RoundsCount = g.Count(),
                SeasonsCount = g.Select(r => r.SeasonId).Distinct().Count(),
                MatchesCount = g.Sum(r => r.MatchesCount),
                LastImport = g.Max(r => r.ScrapedAt)
            })
            .OrderByDescending(x => x.RoundsCount)
            .Take(10)
            .ToList();

        var topLeagues = new List<LeagueStats>();
        foreach (var lr in leagueRounds)
        {
            var league = await _leagueRepository.GetByIdAsync(lr.LeagueId);
            if (league != null)
            {
                topLeagues.Add(new LeagueStats(
                    LeagueId: league.Id,
                    LeagueName: league.Name,
                    CountryName: league.Country?.Name ?? "Unknown",
                    CountryFlag: league.Country?.FlagEmoji ?? "",
                    SportName: league.Sport?.Name ?? "Unknown",
                    RoundsCount: lr.RoundsCount,
                    SeasonsCount: lr.SeasonsCount,
                    MatchesCount: lr.MatchesCount,
                    LastImport: lr.LastImport
                ));
            }
        }

        // Season breakdown
        var seasonGrouping = allRounds
            .GroupBy(r => r.SeasonId)
            .Select(g => new
            {
                SeasonId = g.Key,
                RoundsCount = g.Count(),
                MatchesCount = g.Sum(r => r.MatchesCount),
                LeaguesCount = g.Select(r => r.LeagueId).Distinct().Count()
            })
            .ToList();

        var seasonStats = new List<SeasonStats>();
        foreach (var sg in seasonGrouping)
        {
            var season = await _seasonRepository.GetByIdAsync(sg.SeasonId);
            if (season != null)
            {
                seasonStats.Add(new SeasonStats(
                    Season: season.Name,
                    RoundsCount: sg.RoundsCount,
                    MatchesCount: sg.MatchesCount,
                    LeaguesCount: sg.LeaguesCount
                ));
            }
        }
        seasonStats = seasonStats.OrderByDescending(s => s.Season).Take(10).ToList();

        // Recent import jobs
        var recentJobs = new List<RecentImportJob>();
        var latestJobs = allJobs.OrderByDescending(j => j.StartedAt).Take(10).ToList();

        foreach (var job in latestJobs)
        {
            var league = await _leagueRepository.GetByIdAsync(job.LeagueId);
            recentJobs.Add(new RecentImportJob(
                JobId: job.Id,
                LeagueId: job.LeagueId,
                LeagueName: league?.Name ?? "Unknown",
                Status: job.Status.ToString(),
                StartedAt: job.StartedAt,
                CompletedAt: job.CompletedAt,
                ProcessedRounds: job.Progress.ProcessedRounds,
                TotalSeasons: job.Progress.TotalSeasons
            ));
        }

        return new DashboardStatsResponse(
            Overall: overallStats,
            Results: resultsStats,
            TopLeagues: topLeagues,
            SeasonBreakdown: seasonStats,
            RecentJobs: recentJobs
        );
    }
}
