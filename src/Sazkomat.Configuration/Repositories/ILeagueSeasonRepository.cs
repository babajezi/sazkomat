using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface ILeagueSeasonRepository
{
    Task<List<LeagueSeason>> GetAllAsync();
    Task<LeagueSeason?> GetByIdAsync(Guid id);
    Task<LeagueSeason?> GetByLeagueAndSeasonAsync(Guid leagueId, Guid seasonId);
    Task<List<LeagueSeason>> GetByLeagueIdAsync(Guid leagueId, bool includeRelations = false);
    Task<List<LeagueSeason>> GetAvailableForLeagueAsync(Guid leagueId);
    Task<LeagueSeason> AddAsync(LeagueSeason leagueSeason);
    Task<LeagueSeason> CreateAsync(LeagueSeason leagueSeason);
    Task<LeagueSeason> UpdateAsync(LeagueSeason leagueSeason);
    Task DeleteAsync(Guid id);
    Task UpdateMetadataAsync(Guid leagueId, Guid seasonId, int roundsCount, int matchesCount, bool hasOdds);
    Task<List<LeagueSeason>> GetSyncEnabledAsync();
    Task UpdateSyncEnabledAsync(Guid leagueSeasonId, bool enabled);
    Task UpdateIsCurrentAsync(Guid leagueSeasonId, bool isCurrent, SyncMode syncMode);
    Task UpdateLockStatusAsync(Guid id, bool isLocked);
    Task UpdateLastValidatedAsync(Guid id);
}
