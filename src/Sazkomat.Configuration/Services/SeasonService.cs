using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

public class SeasonService : ISeasonService
{
    private readonly ISeasonRepository _seasonRepository;
    private readonly ILeagueSeasonRepository _leagueSeasonRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ILogger<SeasonService> _logger;

    public SeasonService(
        ISeasonRepository seasonRepository,
        ILeagueSeasonRepository leagueSeasonRepository,
        ILeagueRepository leagueRepository,
        ILogger<SeasonService> logger)
    {
        _seasonRepository = seasonRepository;
        _leagueSeasonRepository = leagueSeasonRepository;
        _leagueRepository = leagueRepository;
        _logger = logger;
    }

    public async Task<Result<List<LeagueSeason>>> GetAvailableSeasonsForLeagueAsync(Guid leagueId)
    {
        try
        {
            var leagueSeasons = await _leagueSeasonRepository.GetAvailableForLeagueAsync(leagueId);
            return Result<List<LeagueSeason>>.Success(leagueSeasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available seasons for league {LeagueId}", leagueId);
            return Result<List<LeagueSeason>>.Failure($"Error getting available seasons: {ex.Message}");
        }
    }

    public async Task<Result> UpdateLeagueSeasonStatsAsync(Guid leagueId, Guid seasonId, int roundsCount, int matchesCount, bool hasOdds)
    {
        try
        {
            await _leagueSeasonRepository.UpdateMetadataAsync(leagueId, seasonId, roundsCount, matchesCount, hasOdds);
            _logger.LogInformation("Updated LeagueSeason stats for League {LeagueId}, Season {SeasonId}: {RoundsCount} rounds, {MatchesCount} matches",
                leagueId, seasonId, roundsCount, matchesCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating LeagueSeason stats");
            return Result.Failure($"Error updating stats: {ex.Message}");
        }
    }

    public async Task<Result<LeagueSeason>> GetOrCreateLeagueSeasonAsync(Guid leagueId, string seasonName)
    {
        try
        {
            // Get or create season
            var season = await _seasonRepository.GetOrCreateAsync(seasonName);

            // Check if LeagueSeason exists
            var existing = await _leagueSeasonRepository.GetByLeagueAndSeasonAsync(leagueId, season.Id);

            if (existing != null)
            {
                return Result<LeagueSeason>.Success(existing);
            }

            // Create new LeagueSeason
            var leagueSeason = new LeagueSeason
            {
                LeagueId = leagueId,
                SeasonId = season.Id,
                IsAvailableOnBetExplorer = false, // Unknown until synced
                HasData = false,
                HasOdds = false
            };

            leagueSeason = await _leagueSeasonRepository.CreateAsync(leagueSeason);
            return Result<LeagueSeason>.Success(leagueSeason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating LeagueSeason");
            return Result<LeagueSeason>.Failure($"Error: {ex.Message}");
        }
    }
}
