using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface ICountryNameMappingRepository
{
    Task<CountryNameMapping?> GetByIdAsync(Guid id);
    Task<List<CountryNameMapping>> GetAllAsync();
    Task<List<CountryNameMapping>> GetActiveByProviderAsync(string providerCode);
    Task<CountryNameMapping?> FindMappingAsync(string providerCode, string providerCountryName);
    Task<CountryNameMapping> CreateAsync(CountryNameMapping mapping);
    Task<CountryNameMapping> UpdateAsync(CountryNameMapping mapping);
    Task DeleteAsync(Guid id);
    Task TrackUsageAsync(Guid mappingId, Guid providerCountryId);
}
