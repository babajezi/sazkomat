using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Repositories;

public interface IUnmatchedCountryRepository
{
    Task<UnmatchedCountry?> GetByIdAsync(Guid id);
    Task<List<UnmatchedCountry>> GetAllAsync();
    Task<List<UnmatchedCountry>> GetUnresolvedAsync();
    Task<List<UnmatchedCountry>> GetByProviderAsync(Guid providerId);
    Task<List<UnmatchedCountry>> GetUnresolvedByProviderAsync(Guid providerId);
    Task<List<UnmatchedCountry>> GetResolvedAsMappedByProviderAsync(Guid providerId);
    Task<UnmatchedCountry?> FindExistingAsync(Guid providerId, string providerCountryName);
    Task<UnmatchedCountry> CreateAsync(UnmatchedCountry unmatchedCountry);
    Task<UnmatchedCountry> UpdateAsync(UnmatchedCountry unmatchedCountry);
    Task DeleteAsync(Guid id);
    Task<UnmatchedCountry> ResolveAsMappedAsync(Guid id, Guid countryId, string? notes = null);
    Task<UnmatchedCountry> ResolveAsIgnoredAsync(Guid id, string? notes = null);
    Task<UnmatchedCountry> ResolveAsUnavailableAsync(Guid id, string? notes = null);
    Task UnresolveAsync(Guid id);
    Task<UnmatchedCountryStats> GetStatsAsync(Guid? providerId = null);
}

public class UnmatchedCountryStats
{
    public int Total { get; set; }
    public int Unresolved { get; set; }
    public int Mapped { get; set; }
    public int Ignored { get; set; }
    public int Unavailable { get; set; }
}
