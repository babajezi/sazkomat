using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using System.Text.Json;

namespace Sazkomat.DataImport.Services;

public class LiveSyncService : ILiveSyncService
{
    private readonly ISyncJobRepository _syncJobRepo;
    private readonly IDataProviderRepository _dataProviderRepo;
    private readonly ILeagueRepository _leagueRepo;
    private readonly ISeasonRepository _seasonRepo;
    private readonly ILeagueSeasonRepository _leagueSeasonRepo;
    private readonly IRoundRepository _roundRepo;
    private readonly IMatchRepository _matchRepo;
    private readonly ILeagueScraper _scraper;
    private readonly ILogger<LiveSyncService> _logger;

    public LiveSyncService(
        ISyncJobRepository syncJobRepo,
        IDataProviderRepository dataProviderRepo,
        ILeagueRepository leagueRepo,
        ISeasonRepository seasonRepo,
        ILeagueSeasonRepository leagueSeasonRepo,
        IRoundRepository roundRepo,
        IMatchRepository matchRepo,
        ILeagueScraper scraper,
        ILogger<LiveSyncService> logger)
    {
        _syncJobRepo = syncJobRepo;
        _dataProviderRepo = dataProviderRepo;
        _leagueRepo = leagueRepo;
        _seasonRepo = seasonRepo;
        _leagueSeasonRepo = leagueSeasonRepo;
        _roundRepo = roundRepo;
        _matchRepo = matchRepo;
        _scraper = scraper;
        _logger = logger;
    }

    /// <summary>
    /// Public wrapper for live syncing multiple rounds. Creates a new SyncJob and returns jobId.
    /// </summary>
    public async Task<Guid> LiveSyncRoundsAsync(Guid providerId, List<Guid>? leagueIds = null, bool forceRefresh = false)
    {
        _logger.LogInformation("Starting live sync for provider {ProviderId}, leagues: {LeagueIds}, forceRefresh: {ForceRefresh}",
            providerId, leagueIds?.Count ?? 0, forceRefresh);

        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.LiveUpdate,
            EntityType = SyncEntityType.Rounds,
            Status = SyncJobStatus.Pending,
            LeagueIds = leagueIds ?? new List<Guid>(),
            Priority = 10 // High priority for live updates
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        // Delegate to internal implementation with jobId
        await LiveSyncRoundsInternalAsync(syncJob.Id, providerId, leagueIds, forceRefresh);

        return syncJob.Id;
    }

    /// <summary>
    /// Internal implementation that loads and processes an existing SyncJob.
    /// </summary>
    public async Task LiveSyncRoundsInternalAsync(Guid jobId, Guid providerId, List<Guid>? leagueIds = null, bool forceRefresh = false)
    {
        // Load the existing job
        var syncJob = await _syncJobRepo.GetByIdAsync(jobId);
        if (syncJob == null)
        {
            throw new ArgumentException($"Sync job {jobId} not found", nameof(jobId));
        }

        // Update job status to Running
        syncJob.Status = SyncJobStatus.Running;
        syncJob.StartedAt = DateTime.UtcNow;
        await _syncJobRepo.UpdateAsync(syncJob);

        try
        {
            // Get leagues to sync
            var allLeagues = await _leagueRepo.GetAllAsync();
            var leaguesToSync = leagueIds != null && leagueIds.Any()
                ? allLeagues.Where(l => leagueIds.Contains(l.Id)).ToList()
                : allLeagues.ToList();

            _logger.LogInformation("Found {Count} leagues to sync", leaguesToSync.Count);

            int totalRounds = 0;
            int totalMatches = 0;
            int roundsCreated = 0;
            int roundsUpdated = 0;
            int roundsSkipped = 0;

            foreach (var league in leaguesToSync)
            {
                try
                {
                    // Find active season for this league
                    var leagueSeasons = await _leagueSeasonRepo.GetByLeagueIdAsync(league.Id, includeRelations: true);
                    var activeSeason = leagueSeasons.FirstOrDefault(ls => ls.IsCurrent);
                    if (activeSeason == null)
                    {
                        _logger.LogWarning("No active season found for league {LeagueId} ({LeagueName})",
                            league.Id, league.Name);
                        continue;
                    }

                    var season = await _seasonRepo.GetByIdAsync(activeSeason.SeasonId);
                    if (season == null)
                    {
                        _logger.LogWarning("Season {SeasonId} not found for league {LeagueId}",
                            activeSeason.SeasonId, league.Id);
                        continue;
                    }

                    _logger.LogInformation("Live syncing league {LeagueName} for season {SeasonName}",
                        league.Name, season.Name);

                    // Scrape all rounds for the season
                    var scrapedRounds = await _scraper.ScrapeSeasonAsync(league, season.Name);
                    _logger.LogInformation("Scraped {Count} rounds for {LeagueName} - {SeasonName}",
                        scrapedRounds.Count, league.Name, season.Name);

                    // Process each scraped round
                    foreach (var scrapedRound in scrapedRounds)
                    {
                        // Set the IDs
                        scrapedRound.LeagueId = league.Id;
                        scrapedRound.SeasonId = season.Id;
                        scrapedRound.ProviderId = providerId;
                        scrapedRound.ScrapedAt = DateTime.UtcNow;

                        // Check if round already exists
                        var existingRounds = await _roundRepo.GetByLeagueAndSeasonAsync(league.Id, season.Id);
                        var existingRound = existingRounds.FirstOrDefault(r => r.RoundNumber == scrapedRound.RoundNumber);

                        if (existingRound != null && !forceRefresh)
                        {
                            _logger.LogDebug("Round {RoundNumber} already exists for {LeagueName}, skipping",
                                scrapedRound.RoundNumber, league.Name);
                            roundsSkipped++;
                            continue;
                        }

                        if (existingRound != null)
                        {
                            // Update existing round
                            existingRound.StartDate = scrapedRound.StartDate;
                            existingRound.EndDate = scrapedRound.EndDate;
                            existingRound.MatchesCount = scrapedRound.MatchesCount;
                            existingRound.HomeWins = scrapedRound.HomeWins;
                            existingRound.Draws = scrapedRound.Draws;
                            existingRound.AwayWins = scrapedRound.AwayWins;
                            existingRound.CumulativeOddsHome = scrapedRound.CumulativeOddsHome;
                            existingRound.CumulativeOddsDraw = scrapedRound.CumulativeOddsDraw;
                            existingRound.CumulativeOddsAway = scrapedRound.CumulativeOddsAway;
                            existingRound.SummaryResult = scrapedRound.SummaryResult;
                            existingRound.OddsComplete = scrapedRound.OddsComplete;
                            existingRound.ScrapedAt = DateTime.UtcNow;

                            await _roundRepo.UpdateAsync(existingRound);

                            // Delete old matches and create new ones
                            var oldMatches = await _matchRepo.GetByRoundIdAsync(existingRound.Id);
                            foreach (var oldMatch in oldMatches)
                            {
                                await _matchRepo.DeleteAsync(oldMatch.Id);
                            }

                            // Create new matches
                            foreach (var match in scrapedRound.Matches)
                            {
                                match.RoundId = existingRound.Id;
                                await _matchRepo.CreateAsync(match);
                                totalMatches++;
                            }

                            roundsUpdated++;
                            _logger.LogInformation("Updated round {RoundNumber} for {LeagueName} with {MatchCount} matches",
                                scrapedRound.RoundNumber, league.Name, scrapedRound.Matches.Count);
                        }
                        else
                        {
                            // Create new round
                            var createdRound = await _roundRepo.CreateAsync(scrapedRound);

                            // Create matches
                            foreach (var match in scrapedRound.Matches)
                            {
                                match.RoundId = createdRound.Id;
                                await _matchRepo.CreateAsync(match);
                                totalMatches++;
                            }

                            roundsCreated++;
                            _logger.LogInformation("Created round {RoundNumber} for {LeagueName} with {MatchCount} matches",
                                scrapedRound.RoundNumber, league.Name, scrapedRound.Matches.Count);
                        }

                        totalRounds++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to live sync league {LeagueId} ({LeagueName})",
                        league.Id, league.Name);
                    // Continue with next league
                }
            }

            syncJob.Status = SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                totalRounds,
                totalMatches,
                roundsCreated,
                roundsUpdated,
                roundsSkipped,
                leaguesProcessed = leaguesToSync.Count
            });
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation(
                "Live sync completed. Total rounds: {TotalRounds}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Matches: {Matches}",
                totalRounds, roundsCreated, roundsUpdated, roundsSkipped, totalMatches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Live sync failed for job {JobId}", jobId);

            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;

            // Resilient status update with retry logic to prevent jobs stuck in Running status
            bool statusUpdated = false;
            for (int attempt = 1; attempt <= 3 && !statusUpdated; attempt++)
            {
                try
                {
                    await _syncJobRepo.UpdateAsync(syncJob);
                    statusUpdated = true;
                    _logger.LogInformation("Successfully updated job {JobId} to Failed status", syncJob.Id);
                }
                catch (Exception updateEx)
                {
                    if (attempt == 3)
                    {
                        _logger.LogCritical(updateEx,
                            "CRITICAL: Failed to update job {JobId} to Failed status after 3 attempts. " +
                            "Job will remain in Running status. Original error: {OriginalError}",
                            syncJob.Id, ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning(updateEx,
                            "Attempt {Attempt}/3 to update job status failed, retrying...", attempt);
                        await Task.Delay(500 * attempt); // Exponential backoff
                    }
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Public wrapper for live syncing a specific round. Creates a new SyncJob and returns jobId.
    /// </summary>
    public async Task<Guid> LiveSyncRoundAsync(Guid providerId, Guid roundId)
    {
        _logger.LogInformation("Starting live sync for specific round {RoundId}", roundId);

        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        var round = await _roundRepo.GetByIdAsync(roundId);
        if (round == null)
        {
            throw new ArgumentException($"Round {roundId} not found", nameof(roundId));
        }

        var syncJob = new SyncJob
        {
            ProviderId = providerId,
            Type = SyncJobType.LiveUpdate,
            EntityType = SyncEntityType.Rounds,
            Status = SyncJobStatus.Pending,
            Priority = 10 // High priority for live updates
        };
        syncJob = await _syncJobRepo.CreateAsync(syncJob);

        // Delegate to internal implementation with jobId
        await LiveSyncRoundInternalAsync(syncJob.Id, providerId, roundId);

        return syncJob.Id;
    }

    /// <summary>
    /// Internal implementation that loads and processes an existing SyncJob for a specific round.
    /// </summary>
    public async Task LiveSyncRoundInternalAsync(Guid jobId, Guid providerId, Guid roundId)
    {
        // Load the existing job
        var syncJob = await _syncJobRepo.GetByIdAsync(jobId);
        if (syncJob == null)
        {
            throw new ArgumentException($"Sync job {jobId} not found", nameof(jobId));
        }

        // Update job status to Running
        syncJob.Status = SyncJobStatus.Running;
        syncJob.StartedAt = DateTime.UtcNow;
        await _syncJobRepo.UpdateAsync(syncJob);

        try
        {
            var round = await _roundRepo.GetByIdAsync(roundId);
            if (round == null)
            {
                throw new ArgumentException($"Round {roundId} not found");
            }

            var league = await _leagueRepo.GetByIdAsync(round.LeagueId);
            if (league == null)
            {
                throw new ArgumentException($"League {round.LeagueId} not found");
            }

            var season = await _seasonRepo.GetByIdAsync(round.SeasonId);
            if (season == null)
            {
                throw new ArgumentException($"Season {round.SeasonId} not found");
            }

            _logger.LogInformation("Live syncing round {RoundNumber} for {LeagueName} - {SeasonName}",
                round.RoundNumber, league.Name, season.Name);

            // Scrape all rounds for the season (scraper doesn't support single round)
            var scrapedRounds = await _scraper.ScrapeSeasonAsync(league, season.Name);
            var scrapedRound = scrapedRounds.FirstOrDefault(r => r.RoundNumber == round.RoundNumber);

            if (scrapedRound == null)
            {
                throw new InvalidOperationException($"Round {round.RoundNumber} not found in scraped data");
            }

            // Update the round
            round.StartDate = scrapedRound.StartDate;
            round.EndDate = scrapedRound.EndDate;
            round.MatchesCount = scrapedRound.MatchesCount;
            round.HomeWins = scrapedRound.HomeWins;
            round.Draws = scrapedRound.Draws;
            round.AwayWins = scrapedRound.AwayWins;
            round.CumulativeOddsHome = scrapedRound.CumulativeOddsHome;
            round.CumulativeOddsDraw = scrapedRound.CumulativeOddsDraw;
            round.CumulativeOddsAway = scrapedRound.CumulativeOddsAway;
            round.SummaryResult = scrapedRound.SummaryResult;
            round.OddsComplete = scrapedRound.OddsComplete;
            round.ScrapedAt = DateTime.UtcNow;

            await _roundRepo.UpdateAsync(round);

            // Delete old matches and create new ones
            var oldMatches = await _matchRepo.GetByRoundIdAsync(round.Id);
            foreach (var oldMatch in oldMatches)
            {
                await _matchRepo.DeleteAsync(oldMatch.Id);
            }

            // Create new matches
            int matchCount = 0;
            foreach (var match in scrapedRound.Matches)
            {
                match.RoundId = round.Id;
                await _matchRepo.CreateAsync(match);
                matchCount++;
            }

            syncJob.Status = SyncJobStatus.Completed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ProgressData = JsonSerializer.Serialize(new
            {
                roundId = round.Id,
                roundNumber = round.RoundNumber,
                matchesUpdated = matchCount
            });
            await _syncJobRepo.UpdateAsync(syncJob);

            _logger.LogInformation("Live sync completed for round {RoundNumber}. Updated {MatchCount} matches",
                round.RoundNumber, matchCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Live sync failed for round {RoundId}", roundId);

            syncJob.Status = SyncJobStatus.Failed;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message;

            // Resilient status update with retry logic to prevent jobs stuck in Running status
            bool statusUpdated = false;
            for (int attempt = 1; attempt <= 3 && !statusUpdated; attempt++)
            {
                try
                {
                    await _syncJobRepo.UpdateAsync(syncJob);
                    statusUpdated = true;
                    _logger.LogInformation("Successfully updated job {JobId} to Failed status", syncJob.Id);
                }
                catch (Exception updateEx)
                {
                    if (attempt == 3)
                    {
                        _logger.LogCritical(updateEx,
                            "CRITICAL: Failed to update job {JobId} to Failed status after 3 attempts. " +
                            "Job will remain in Running status. Original error: {OriginalError}",
                            syncJob.Id, ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning(updateEx,
                            "Attempt {Attempt}/3 to update job status failed, retrying...", attempt);
                        await Task.Delay(500 * attempt); // Exponential backoff
                    }
                }
            }

            throw;
        }
    }

    public async Task<LiveSyncStats> GetLiveSyncStatsAsync(Guid providerId)
    {
        var provider = await _dataProviderRepo.GetByIdAsync(providerId);
        if (provider == null)
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        // Get all active league-season combinations
        var allLeagueSeasons = await _leagueSeasonRepo.GetAllAsync();
        var activeLeagueSeasons = allLeagueSeasons.Where(ls => ls.IsCurrent).ToList();

        var activeLeagues = activeLeagueSeasons.Select(ls => ls.LeagueId).Distinct().Count();

        // Get all rounds for active seasons
        int totalRounds = 0;
        foreach (var leagueSeason in activeLeagueSeasons)
        {
            var rounds = await _roundRepo.GetByLeagueAndSeasonAsync(leagueSeason.LeagueId, leagueSeason.SeasonId);
            totalRounds += rounds.Count;
        }

        // Find rounds that might need update (older than 24 hours)
        var cutoffTime = DateTime.UtcNow.AddHours(-24);
        int roundsNeedingUpdate = 0;
        foreach (var leagueSeason in activeLeagueSeasons)
        {
            var rounds = await _roundRepo.GetByLeagueAndSeasonAsync(leagueSeason.LeagueId, leagueSeason.SeasonId);
            roundsNeedingUpdate += rounds.Count(r => r.ScrapedAt < cutoffTime);
        }

        // Get last sync time for this provider
        var recentJobs = await _syncJobRepo.GetRecentJobsAsync(providerId, 10);
        var lastLiveSync = recentJobs
            .Where(j => j.Type == SyncJobType.LiveUpdate && j.Status == SyncJobStatus.Completed)
            .OrderByDescending(j => j.CompletedAt)
            .FirstOrDefault();

        return new LiveSyncStats(
            ActiveLeagues: activeLeagues,
            TotalRounds: totalRounds,
            RoundsNeedingUpdate: roundsNeedingUpdate,
            LastSyncAt: lastLiveSync?.CompletedAt
        );
    }
}
