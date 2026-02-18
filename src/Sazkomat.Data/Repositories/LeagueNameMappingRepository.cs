using Microsoft.EntityFrameworkCore;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;
using Sazkomat.Data.Helpers;

namespace Sazkomat.Data.Repositories;

public class LeagueNameMappingRepository : ILeagueNameMappingRepository
{
    private readonly DataDbContext _context;

    public LeagueNameMappingRepository(DataDbContext context)
    {
        _context = context;
    }

    public async Task<LeagueNameMapping?> GetByIdAsync(Guid id)
    {
        return await _context.LeagueNameMappings.FindAsync(id);
    }

    public async Task<List<LeagueNameMapping>> GetAllAsync()
    {
        return await _context.LeagueNameMappings
            .OrderBy(m => m.ProviderCode)
            .ThenBy(m => m.CountryCode)
            .ThenBy(m => m.Priority)
            .ToListAsync();
    }

    public async Task<List<LeagueNameMapping>> GetActiveByProviderAsync(string providerCode)
    {
        return await _context.LeagueNameMappings
            .Where(m => m.ProviderCode == providerCode && m.IsActive)
            .OrderBy(m => m.Priority)
            .ToListAsync();
    }

    public async Task<LeagueNameMapping?> FindMappingAsync(
        string providerCode,
        string countryCode,
        string providerLeagueName)
    {
        return await _context.LeagueNameMappings
            .Where(m => m.ProviderCode == providerCode
                     && m.CountryCode == countryCode
                     && m.ProviderLeagueName == providerLeagueName
                     && m.IsActive)
            .OrderBy(m => m.Priority)
            .FirstOrDefaultAsync();
    }

    public async Task<LeagueNameMapping?> FindMappingWithFallbackAsync(
        string providerCode,
        string countryCode,
        string providerLeagueName)
    {
        var normalized = LeagueNameNormalizer.Normalize(providerLeagueName);

        // 1. First try provider-specific mapping (highest priority)
        var specific = await _context.LeagueNameMappings
            .Where(m => m.ProviderCode == providerCode
                     && m.CountryCode == countryCode
                     && m.NormalizedProviderLeagueName == normalized
                     && m.IsActive)
            .OrderBy(m => m.Priority)
            .FirstOrDefaultAsync();

        if (specific != null)
            return specific;

        // 2. Fallback to global rule (ProviderCode = "*")
        return await _context.LeagueNameMappings
            .Where(m => m.ProviderCode == LeagueNameMapping.GlobalProviderCode
                     && m.CountryCode == countryCode
                     && m.NormalizedProviderLeagueName == normalized
                     && m.IsActive)
            .OrderBy(m => m.Priority)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Guid>> FindAffectedUnmatchedLeagueIdsAsync(
        string normalizedLeagueName,
        string countryCode)
    {
        // Find all unmatched leagues with the same normalized name and country
        // that are either unresolved or resolved as Mapped (to show in preview)
        return await _context.UnmatchedLeagues
            .Where(ul => ul.CountryCode.ToLower() == countryCode.ToLower())
            .ToListAsync()
            .ContinueWith(t => t.Result
                .Where(ul => LeagueNameNormalizer.Normalize(ul.ProviderLeagueName) == normalizedLeagueName)
                .Select(ul => ul.Id)
                .ToList());
    }

    public async Task<LeagueNameMapping> CreateAsync(LeagueNameMapping mapping)
    {
        // Auto-compute normalized name
        mapping.NormalizedProviderLeagueName = LeagueNameNormalizer.Normalize(mapping.ProviderLeagueName);

        _context.LeagueNameMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<LeagueNameMapping> UpdateAsync(LeagueNameMapping mapping)
    {
        // Auto-compute normalized name in case ProviderLeagueName changed
        mapping.NormalizedProviderLeagueName = LeagueNameNormalizer.Normalize(mapping.ProviderLeagueName);
        mapping.UpdatedAt = DateTime.UtcNow;

        _context.LeagueNameMappings.Update(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task DeleteAsync(Guid id)
    {
        var mapping = await GetByIdAsync(id);
        if (mapping != null)
        {
            _context.LeagueNameMappings.Remove(mapping);
            await _context.SaveChangesAsync();
        }
    }

    public async Task TrackUsageAsync(Guid mappingId, Guid providerLeagueId)
    {
        var mapping = await GetByIdAsync(mappingId);
        if (mapping != null)
        {
            mapping.LastUsedAt = DateTime.UtcNow;
            mapping.UsageCount++;
            mapping.LastProviderLeagueId = providerLeagueId;
            mapping.UpdatedAt = DateTime.UtcNow;

            _context.LeagueNameMappings.Update(mapping);
            await _context.SaveChangesAsync();
        }
    }
}
