using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public class ProviderCountryRepository : IProviderCountryRepository
{
    private readonly DataImportDbContext _context;
    private readonly ILogger<ProviderCountryRepository> _logger;

    public ProviderCountryRepository(DataImportDbContext context, ILogger<ProviderCountryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProviderCountry>> GetAllAsync()
    {
        return await _context.ProviderCountries
            .OrderByDescending(pc => pc.ScrapedAt)
            .ToListAsync();
    }

    public async Task<ProviderCountry?> GetByIdAsync(Guid id)
    {
        return await _context.ProviderCountries
            .FirstOrDefaultAsync(pc => pc.Id == id);
    }

    public async Task<List<ProviderCountry>> GetByProviderIdAsync(Guid providerId)
    {
        return await _context.ProviderCountries
            .Where(pc => pc.ProviderId == providerId)
            .OrderByDescending(pc => pc.ScrapedAt)
            .ToListAsync();
    }

    public async Task<ProviderCountry?> GetByProviderCodeAsync(Guid providerId, string providerCode)
    {
        return await _context.ProviderCountries
            .Where(pc => pc.ProviderId == providerId && pc.ProviderCode == providerCode)
            .OrderByDescending(pc => pc.ScrapedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ProviderCountry>> GetUnimportedAsync(Guid providerId)
    {
        return await _context.ProviderCountries
            .Where(pc => pc.ProviderId == providerId && !pc.IsImported)
            .OrderBy(pc => pc.ProviderName)
            .ToListAsync();
    }

    public async Task<ProviderCountry> CreateAsync(ProviderCountry providerCountry)
    {
        _context.ProviderCountries.Add(providerCountry);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created ProviderCountry {Id} for provider {ProviderId}",
            providerCountry.Id, providerCountry.ProviderId);
        return providerCountry;
    }

    public async Task<ProviderCountry> UpdateAsync(ProviderCountry providerCountry)
    {
        providerCountry.UpdatedAt = DateTime.UtcNow;
        _context.ProviderCountries.Update(providerCountry);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated ProviderCountry {Id}", providerCountry.Id);
        return providerCountry;
    }

    public async Task DeleteAsync(Guid id)
    {
        var providerCountry = await GetByIdAsync(id);
        if (providerCountry != null)
        {
            _context.ProviderCountries.Remove(providerCountry);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted ProviderCountry {Id}", id);
        }
    }
}
