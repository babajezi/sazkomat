using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface IProviderCountryRepository
{
    Task<List<ProviderCountry>> GetAllAsync();
    Task<ProviderCountry?> GetByIdAsync(Guid id);
    Task<List<ProviderCountry>> GetByProviderIdAsync(Guid providerId);
    Task<ProviderCountry?> GetByProviderCodeAsync(Guid providerId, string providerCode);
    Task<ProviderCountry?> GetByProviderNameAsync(Guid providerId, string providerName);
    Task<List<ProviderCountry>> GetUnimportedAsync(Guid providerId);
    Task<ProviderCountry> CreateAsync(ProviderCountry providerCountry);
    Task<ProviderCountry> UpdateAsync(ProviderCountry providerCountry);
    Task DeleteAsync(Guid id);
}
