using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Repositories;

public interface ICountryNameMappingRepository
{
    Task<CountryNameMapping?> GetByIdAsync(Guid id);
    Task<List<CountryNameMapping>> GetAllAsync();
    Task<List<CountryNameMapping>> GetActiveByProviderAsync(string providerCode);
    Task<CountryNameMapping?> FindMappingAsync(string providerCode, string providerCountryName);
    /// <summary>
    /// Find any mapping for the given provider and country name, including inactive ones.
    /// </summary>
    Task<CountryNameMapping?> FindAnyMappingAsync(string providerCode, string providerCountryName);
    Task<CountryNameMapping> CreateAsync(CountryNameMapping mapping);
    Task<CountryNameMapping> UpdateAsync(CountryNameMapping mapping);
    Task DeleteAsync(Guid id);
    Task TrackUsageAsync(Guid mappingId, Guid providerCountryId);

    /// <summary>
    /// Find a mapping by pattern matching the input text against all active mappings for the provider.
    /// Special cases are checked first, then regular mappings by priority.
    /// </summary>
    /// <param name="providerCode">The provider code (e.g., "tipsport", "chance")</param>
    /// <param name="inputText">The input text to match against (e.g., "1. anglická liga")</param>
    /// <returns>The matching mapping, or null if no match found</returns>
    Task<CountryNameMapping?> FindByPatternAsync(string providerCode, string inputText);
}
