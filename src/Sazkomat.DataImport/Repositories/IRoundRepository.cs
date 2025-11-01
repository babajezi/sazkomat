using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface IRoundRepository
{
    Task<List<Round>> GetAllAsync();
    Task<Round?> GetByIdAsync(Guid id);
    Task<Round?> GetByLeagueSeasonRoundAsync(Guid leagueId, Guid seasonId, int roundNumber);
    Task<List<Round>> GetByLeagueAsync(Guid leagueId);
    Task<Round> CreateAsync(Round round);
    Task<Round> UpdateAsync(Round round);
    Task DeleteAsync(Guid id);
}
