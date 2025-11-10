using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface IProviderSeasonRepository
{
    Task<List<ProviderSeason>> GetAllAsync();
    Task<ProviderSeason?> GetByIdAsync(Guid id);
    Task<List<ProviderSeason>> GetByProviderIdAsync(Guid providerId);
    Task<List<ProviderSeason>> GetByProviderLeagueIdAsync(Guid providerLeagueId);
    Task<ProviderSeason?> GetBySeasonNameAsync(Guid providerLeagueId, string seasonName);
    Task<List<ProviderSeason>> GetUnimportedAsync(Guid providerId);
    Task<List<ProviderSeason>> GetCurrentSeasonsAsync(Guid providerId);
    Task<ProviderSeason> CreateAsync(ProviderSeason providerSeason);
    Task<ProviderSeason> UpdateAsync(ProviderSeason providerSeason);
    Task DeleteAsync(Guid id);
}
