using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface ILeagueNameMappingRepository
{
    Task<LeagueNameMapping?> GetByIdAsync(Guid id);
    Task<List<LeagueNameMapping>> GetAllAsync();
    Task<List<LeagueNameMapping>> GetActiveByProviderAsync(string providerCode);
    Task<LeagueNameMapping?> FindMappingAsync(string providerCode, string countryCode, string providerLeagueName);
    Task<LeagueNameMapping> CreateAsync(LeagueNameMapping mapping);
    Task<LeagueNameMapping> UpdateAsync(LeagueNameMapping mapping);
    Task DeleteAsync(Guid id);
    Task TrackUsageAsync(Guid mappingId, Guid providerLeagueId);
}
