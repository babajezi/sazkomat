using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Repositories;

public interface IProviderLeagueRepository
{
    Task<List<ProviderLeague>> GetAllAsync();
    Task<ProviderLeague?> GetByIdAsync(Guid id);
    Task<List<ProviderLeague>> GetByProviderIdAsync(Guid providerId);
    Task<List<ProviderLeague>> GetByProviderCountryIdAsync(Guid providerCountryId);
    Task<ProviderLeague?> GetByProviderSlugAsync(Guid providerId, string providerSlug);
    Task<List<ProviderLeague>> GetUnimportedAsync(Guid providerId);
    Task<ProviderLeague> CreateAsync(ProviderLeague providerLeague);
    Task<ProviderLeague> UpdateAsync(ProviderLeague providerLeague);
    Task DeleteAsync(Guid id);
}
