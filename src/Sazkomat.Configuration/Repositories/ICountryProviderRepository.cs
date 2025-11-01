using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface ICountryProviderRepository
{
    Task<IEnumerable<CountryProvider>> GetAllAsync();
    Task<CountryProvider?> GetByIdAsync(Guid id);
    Task<IEnumerable<CountryProvider>> GetByCountryIdAsync(Guid countryId);
    Task<CountryProvider?> GetByCountryAndProviderAsync(Guid countryId, Guid providerId);
    Task<CountryProvider?> GetActiveByCountryIdAsync(Guid countryId);
    Task AddAsync(CountryProvider countryProvider);
    Task UpdateAsync(CountryProvider countryProvider);
    Task DeleteAsync(Guid id);
}
