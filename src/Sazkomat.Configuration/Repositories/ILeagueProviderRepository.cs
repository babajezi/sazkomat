using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface ILeagueProviderRepository
{
    Task<IEnumerable<LeagueProvider>> GetAllAsync();
    Task<LeagueProvider?> GetByIdAsync(Guid id);
    Task<IEnumerable<LeagueProvider>> GetByLeagueIdAsync(Guid leagueId);
    Task<LeagueProvider?> GetByLeagueAndProviderAsync(Guid leagueId, Guid providerId);
    Task<LeagueProvider?> GetByProviderAndSlugAsync(Guid providerId, string providerSlug);
    Task<LeagueProvider?> GetActiveByLeagueIdAsync(Guid leagueId);
    Task AddAsync(LeagueProvider leagueProvider);
    Task UpdateAsync(LeagueProvider leagueProvider);
    Task DeleteAsync(Guid id);
}
