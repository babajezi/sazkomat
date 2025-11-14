using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public class LeagueNameMappingRepository : ILeagueNameMappingRepository
{
    private readonly DataImportDbContext _context;

    public LeagueNameMappingRepository(DataImportDbContext context)
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

    public async Task<LeagueNameMapping> CreateAsync(LeagueNameMapping mapping)
    {
        _context.LeagueNameMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<LeagueNameMapping> UpdateAsync(LeagueNameMapping mapping)
    {
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
