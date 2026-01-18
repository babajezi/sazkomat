using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface ILeagueNameMappingRepository
{
    Task<LeagueNameMapping?> GetByIdAsync(Guid id);
    Task<List<LeagueNameMapping>> GetAllAsync();
    Task<List<LeagueNameMapping>> GetActiveByProviderAsync(string providerCode);
    Task<LeagueNameMapping?> FindMappingAsync(string providerCode, string countryCode, string providerLeagueName);

    /// <summary>
    /// Finds a mapping with fallback to global rules.
    /// First tries provider-specific mapping, then falls back to global (*) mapping.
    /// Uses normalized name comparison for case-insensitive, whitespace-normalized matching.
    /// </summary>
    Task<LeagueNameMapping?> FindMappingWithFallbackAsync(string providerCode, string countryCode, string providerLeagueName);

    /// <summary>
    /// Finds all unmatched leagues that would be affected by a global rule for the given normalized name and country.
    /// </summary>
    Task<List<Guid>> FindAffectedUnmatchedLeagueIdsAsync(string normalizedLeagueName, string countryCode);

    Task<LeagueNameMapping> CreateAsync(LeagueNameMapping mapping);
    Task<LeagueNameMapping> UpdateAsync(LeagueNameMapping mapping);
    Task DeleteAsync(Guid id);
    Task TrackUsageAsync(Guid mappingId, Guid providerLeagueId);
}
