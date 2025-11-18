using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public class CountryNameMappingRepository : ICountryNameMappingRepository
{
    private readonly DataImportDbContext _context;

    public CountryNameMappingRepository(DataImportDbContext context)
    {
        _context = context;
    }

    public async Task<CountryNameMapping?> GetByIdAsync(Guid id)
    {
        return await _context.CountryNameMappings.FindAsync(id);
    }

    public async Task<List<CountryNameMapping>> GetAllAsync()
    {
        return await _context.CountryNameMappings
            .OrderBy(m => m.ProviderCode)
            .ThenBy(m => m.Priority)
            .ToListAsync();
    }

    public async Task<List<CountryNameMapping>> GetActiveByProviderAsync(string providerCode)
    {
        return await _context.CountryNameMappings
            .Where(m => m.ProviderCode == providerCode && m.IsActive)
            .OrderBy(m => m.Priority)
            .ToListAsync();
    }

    public async Task<CountryNameMapping?> FindMappingAsync(
        string providerCode,
        string providerCountryName)
    {
        return await _context.CountryNameMappings
            .Where(m => m.ProviderCode == providerCode
                     && m.ProviderCountryName == providerCountryName
                     && m.IsActive)
            .OrderBy(m => m.Priority)
            .FirstOrDefaultAsync();
    }

    public async Task<CountryNameMapping> CreateAsync(CountryNameMapping mapping)
    {
        _context.CountryNameMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<CountryNameMapping> UpdateAsync(CountryNameMapping mapping)
    {
        mapping.UpdatedAt = DateTime.UtcNow;
        _context.CountryNameMappings.Update(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task DeleteAsync(Guid id)
    {
        var mapping = await GetByIdAsync(id);
        if (mapping != null)
        {
            _context.CountryNameMappings.Remove(mapping);
            await _context.SaveChangesAsync();
        }
    }

    public async Task TrackUsageAsync(Guid mappingId, Guid providerCountryId)
    {
        var mapping = await GetByIdAsync(mappingId);
        if (mapping != null)
        {
            mapping.LastUsedAt = DateTime.UtcNow;
            mapping.UsageCount++;
            mapping.LastProviderCountryId = providerCountryId;
            mapping.UpdatedAt = DateTime.UtcNow;

            _context.CountryNameMappings.Update(mapping);
            await _context.SaveChangesAsync();
        }
    }
}
