using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

public interface ISeasonService
{
    Task<Result<List<LeagueSeason>>> GetAvailableSeasonsForLeagueAsync(Guid leagueId);
    Task<Result> UpdateLeagueSeasonStatsAsync(Guid leagueId, Guid seasonId, int roundsCount, int matchesCount, bool hasOdds);
    Task<Result<LeagueSeason>> GetOrCreateLeagueSeasonAsync(Guid leagueId, string seasonName);
}
