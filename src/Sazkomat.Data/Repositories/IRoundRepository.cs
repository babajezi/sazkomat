using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Repositories;

public interface IRoundRepository
{
    Task<List<Round>> GetAllAsync();
    Task<Round?> GetByIdAsync(Guid id);
    Task<Round?> GetByLeagueSeasonRoundAsync(Guid leagueId, Guid seasonId, int roundNumber, string? groupName = null);
    Task<List<Round>> GetByLeagueAsync(Guid leagueId);
    Task<List<Round>> GetByLeagueAndSeasonAsync(Guid leagueId, Guid seasonId);
    Task<Round> CreateAsync(Round round);
    Task<Round> UpdateAsync(Round round);
    Task DeleteAsync(Guid id);
}
