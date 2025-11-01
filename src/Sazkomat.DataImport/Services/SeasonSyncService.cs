using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Core.Common;
using Sazkomat.DataImport.DTOs;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using System.Text.Json;

namespace Sazkomat.DataImport.Services;

public class SeasonSyncService : ISeasonSyncService
{
    private readonly IDataProviderRepository _providerRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ILeagueSeasonRepository _leagueSeasonRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IRoundRepository _roundRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly ScraperFactory _scraperFactory;
    private readonly ILogger<SeasonSyncService> _logger;

    public SeasonSyncService(
        IDataProviderRepository providerRepository,
        ILeagueRepository leagueRepository,
        ILeagueSeasonRepository leagueSeasonRepository,
        ISeasonRepository seasonRepository,
        IRoundRepository roundRepository,
        IMatchRepository matchRepository,
        ScraperFactory scraperFactory,
        ILogger<SeasonSyncService> logger)
    {
        _providerRepository = providerRepository;
        _leagueRepository = leagueRepository;
        _leagueSeasonRepository = leagueSeasonRepository;
        _seasonRepository = seasonRepository;
        _roundRepository = roundRepository;
        _matchRepository = matchRepository;
        _scraperFactory = scraperFactory;
        _logger = logger;
    }

    public async Task<Result> DetectAndMarkCurrentSeasonsAsync(Guid providerId)
    {
        try
        {
            _logger.LogInformation("Detecting current seasons for provider {ProviderId}", providerId);

            // Get provider
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result.Failure("Provider not found");
            }

            // Parse current season patterns
            var patterns = JsonSerializer.Deserialize<string[]>(provider.CurrentSeasonPatterns);
            if (patterns == null || patterns.Length == 0)
            {
                return Result.Failure("No current season patterns defined for provider");
            }

            _logger.LogInformation("Current season patterns: {Patterns}", string.Join(", ", patterns));

            // Get all active leagues
            var leagues = await _leagueRepository.GetAllAsync();
            var activeLeagues = leagues.Where(l => l.IsActive).ToList();

            _logger.LogInformation("Found {Count} active leagues", activeLeagues.Count);

            var updatedCount = 0;

            // For each active league, check its seasons
            foreach (var league in activeLeagues)
            {
                var leagueSeasons = await _leagueSeasonRepository.GetByLeagueIdAsync(league.Id, includeRelations: true);

                foreach (var leagueSeason in leagueSeasons)
                {
                    var season = leagueSeason.Season;
                    if (season == null) continue;

                    // Check if season name matches any of the patterns
                    var isCurrent = patterns.Contains(season.Name, StringComparer.OrdinalIgnoreCase);

                    // Update IsCurrent and SyncMode
                    var newSyncMode = isCurrent ? SyncMode.Current : SyncMode.Historical;

                    // Only update if values changed
                    if (leagueSeason.IsCurrent != isCurrent || leagueSeason.SyncMode != newSyncMode)
                    {
                        await _leagueSeasonRepository.UpdateIsCurrentAsync(
                            leagueSeason.Id,
                            isCurrent,
                            newSyncMode);

                        updatedCount++;

                        _logger.LogInformation(
                            "Marked {League} - {Season} as {Status}",
                            league.DisplayName,
                            season.Name,
                            isCurrent ? "CURRENT" : "Historical");
                    }
                }
            }

            _logger.LogInformation("Updated {Count} league seasons", updatedCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting current seasons");
            return Result.Failure($"Error detecting current seasons: {ex.Message}");
        }
    }

    public async Task<Result<SyncResponse>> SyncSeasonDataAsync(
        Guid providerId,
        Guid leagueId,
        Guid seasonId,
        bool forceUpdate = false)
    {
        try
        {
            _logger.LogInformation(
                "Syncing season data for provider {ProviderId}, league {LeagueId}, season {SeasonId}",
                providerId, leagueId, seasonId);

            // Get league season
            var leagueSeason = await _leagueSeasonRepository.GetByLeagueAndSeasonAsync(leagueId, seasonId);
            if (leagueSeason == null)
            {
                return Result<SyncResponse>.Failure("League season not found");
            }

            // Check if we should skip this season
            if (!forceUpdate &&
                leagueSeason.SyncMode == SyncMode.Historical &&
                leagueSeason.HasData)
            {
                _logger.LogInformation(
                    "Skipping historical season that already has data: {League} - {Season}",
                    leagueId, seasonId);

                return Result<SyncResponse>.Success(new SyncResponse
                {
                    Success = true,
                    Message = "Season already synced (Historical mode)",
                    Statistics = new SyncStatistics
                    {
                        Skipped = 1
                    }
                });
            }

            // Get provider and league
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result<SyncResponse>.Failure("Provider not found");
            }

            var league = await _leagueRepository.GetByIdAsync(leagueId);
            if (league == null)
            {
                return Result<SyncResponse>.Failure("League not found");
            }

            var season = await _seasonRepository.GetByIdAsync(seasonId);
            if (season == null)
            {
                return Result<SyncResponse>.Failure("Season not found");
            }

            // Get scraper
            var scraper = _scraperFactory.GetScraper(league.Sport);

            _logger.LogInformation(
                "Scraping {LeagueName} season {SeasonName}...",
                league.DisplayName, season.Name);

            // Scrape the season
            var rounds = await scraper.ScrapeSeasonAsync(league, season.Name);

            _logger.LogInformation(
                "Scraped {RoundCount} rounds for {LeagueName} {SeasonName}",
                rounds.Count, league.DisplayName, season.Name);

            // Save rounds to database
            var savedCount = 0;
            var updatedCount = 0;
            var totalMatches = 0;
            var hasOdds = false;

            foreach (var round in rounds)
            {
                // Set metadata
                round.SeasonId = seasonId;
                round.ProviderId = providerId;

                // Check if round exists
                var existingRound = await _roundRepository.GetByLeagueSeasonRoundAsync(
                    leagueId, seasonId, round.RoundNumber);

                if (existingRound == null)
                {
                    await _roundRepository.CreateAsync(round);
                    savedCount++;
                }
                else
                {
                    // Update existing round (for current seasons)
                    existingRound.MatchesCount = round.MatchesCount;
                    existingRound.HomeWins = round.HomeWins;
                    existingRound.Draws = round.Draws;
                    existingRound.AwayWins = round.AwayWins;
                    existingRound.CumulativeOddsHome = round.CumulativeOddsHome;
                    existingRound.CumulativeOddsDraw = round.CumulativeOddsDraw;
                    existingRound.CumulativeOddsAway = round.CumulativeOddsAway;
                    existingRound.SummaryResult = round.SummaryResult;
                    existingRound.OddsComplete = round.OddsComplete;
                    existingRound.ScrapedAt = DateTime.UtcNow;
                    existingRound.StartDate = round.StartDate;
                    existingRound.EndDate = round.EndDate;

                    await _roundRepository.UpdateAsync(existingRound);
                    updatedCount++;
                }

                totalMatches += round.MatchesCount;
                if (round.OddsComplete == "Yes")
                {
                    hasOdds = true;
                }
            }

            // Update league season metadata
            leagueSeason.HasData = true;
            leagueSeason.RoundsCount = rounds.Count;
            leagueSeason.MatchesCount = totalMatches;
            leagueSeason.HasOdds = hasOdds;
            leagueSeason.LastDataSyncAt = DateTime.UtcNow;
            leagueSeason.LastScrapedAt = DateTime.UtcNow;

            await _leagueSeasonRepository.UpdateAsync(leagueSeason);

            _logger.LogInformation(
                "Season sync completed: {Saved} new, {Updated} updated rounds, {TotalMatches} matches",
                savedCount, updatedCount, totalMatches);

            return Result<SyncResponse>.Success(new SyncResponse
            {
                Success = true,
                Message = $"Synced {rounds.Count} rounds ({totalMatches} matches)",
                Statistics = new SyncStatistics
                {
                    TotalProcessed = rounds.Count,
                    Created = savedCount,
                    Updated = updatedCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing season data");
            return Result<SyncResponse>.Failure($"Error syncing season data: {ex.Message}");
        }
    }

    public async Task<Result<SyncResponse>> SyncAllMarkedSeasonsDataAsync(Guid providerId)
    {
        try
        {
            _logger.LogInformation("Syncing all marked seasons for provider {ProviderId}", providerId);

            // Get all sync-enabled league seasons
            var syncEnabledSeasons = await _leagueSeasonRepository.GetSyncEnabledAsync();

            _logger.LogInformation("Found {Count} seasons marked for sync", syncEnabledSeasons.Count);

            if (!syncEnabledSeasons.Any())
            {
                return Result<SyncResponse>.Success(new SyncResponse
                {
                    Success = true,
                    Message = "No seasons marked for synchronization",
                    Statistics = new SyncStatistics()
                });
            }

            // Aggregate statistics
            var totalCreated = 0;
            var totalUpdated = 0;
            var totalSkipped = 0;
            var totalFailed = 0;
            var errors = new List<string>();

            // Process each season
            foreach (var leagueSeason in syncEnabledSeasons)
            {
                try
                {
                    var result = await SyncSeasonDataAsync(
                        providerId,
                        leagueSeason.LeagueId,
                        leagueSeason.SeasonId,
                        forceUpdate: false);

                    if (result.IsSuccess && result.Value != null)
                    {
                        totalCreated += result.Value.Statistics.Created;
                        totalUpdated += result.Value.Statistics.Updated;
                        totalSkipped += result.Value.Statistics.Skipped;
                    }
                    else
                    {
                        totalFailed++;
                        var errorMsg = $"{leagueSeason.League?.DisplayName} - {leagueSeason.Season?.Name}: {result.Error}";
                        errors.Add(errorMsg);
                        _logger.LogError("Failed to sync season: {Error}", errorMsg);
                    }
                }
                catch (Exception ex)
                {
                    totalFailed++;
                    var errorMsg = $"{leagueSeason.League?.DisplayName} - {leagueSeason.Season?.Name}: {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogError(ex, "Error syncing season");
                }
            }

            var message = $"Synced {syncEnabledSeasons.Count} seasons: {totalCreated} created, {totalUpdated} updated, {totalSkipped} skipped, {totalFailed} failed";

            _logger.LogInformation(message);

            return Result<SyncResponse>.Success(new SyncResponse
            {
                Success = totalFailed == 0,
                Message = message,
                Statistics = new SyncStatistics
                {
                    TotalProcessed = syncEnabledSeasons.Count,
                    Created = totalCreated,
                    Updated = totalUpdated,
                    Skipped = totalSkipped,
                    Errors = totalFailed,
                    ErrorMessages = errors
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all marked seasons");
            return Result<SyncResponse>.Failure($"Error syncing all marked seasons: {ex.Message}");
        }
    }
}
