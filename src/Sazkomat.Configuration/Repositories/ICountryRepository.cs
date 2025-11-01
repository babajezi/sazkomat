using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface ICountryRepository
{
    Task<List<Country>> GetAllAsync();
    Task<Country?> GetByIdAsync(Guid id);
    Task<Country?> GetByCodeAsync(string code);
    Task<Country> CreateAsync(Country country);
    Task<Country> UpdateAsync(Country country);
    Task<Country> AddAsync(Country country);
    Task DeleteAsync(Guid id);
}
