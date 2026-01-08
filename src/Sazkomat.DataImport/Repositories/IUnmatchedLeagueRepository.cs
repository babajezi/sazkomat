using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface IUnmatchedLeagueRepository
{
    Task<UnmatchedLeague?> GetByIdAsync(Guid id);
    Task<List<UnmatchedLeague>> GetAllAsync();
    Task<List<UnmatchedLeague>> GetUnresolvedAsync();
    Task<List<UnmatchedLeague>> GetByProviderAsync(Guid providerId);
    Task<List<UnmatchedLeague>> GetUnresolvedByProviderAsync(Guid providerId);
    Task<List<UnmatchedLeague>> GetResolvedAsMappedByProviderAsync(Guid providerId);
    Task<UnmatchedLeague?> FindExistingAsync(Guid providerId, string providerLeagueName, string countryCode);
    Task<UnmatchedLeague> CreateAsync(UnmatchedLeague unmatchedLeague);
    Task<UnmatchedLeague> UpdateAsync(UnmatchedLeague unmatchedLeague);
    Task DeleteAsync(Guid id);
    Task<UnmatchedLeague> ResolveAsMappedAsync(Guid id, Guid leagueId, string? notes = null);
    Task<UnmatchedLeague> ResolveAsIgnoredAsync(Guid id, string? notes = null);
    Task<UnmatchedLeague> ResolveAsUnavailableAsync(Guid id, string? notes = null);
}
