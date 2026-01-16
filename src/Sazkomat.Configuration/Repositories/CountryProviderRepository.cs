using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class CountryProviderRepository : ICountryProviderRepository
{
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<CountryProviderRepository> _logger;

    public CountryProviderRepository(ConfigurationDbContext context, ILogger<CountryProviderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<CountryProvider>> GetAllAsync()
    {
        return await _context.CountryProviders
            .Include(cp => cp.Country)
            .Include(cp => cp.Provider)
            .ToListAsync();
    }

    public async Task<CountryProvider?> GetByIdAsync(Guid id)
    {
        return await _context.CountryProviders
            .Include(cp => cp.Country)
            .Include(cp => cp.Provider)
            .FirstOrDefaultAsync(cp => cp.Id == id);
    }

    public async Task<IEnumerable<CountryProvider>> GetByCountryIdAsync(Guid countryId)
    {
        return await _context.CountryProviders
            .Include(cp => cp.Provider)
            .Where(cp => cp.CountryId == countryId)
            .ToListAsync();
    }

    public async Task<CountryProvider?> GetByCountryAndProviderAsync(Guid countryId, Guid providerId)
    {
        return await _context.CountryProviders
            .Include(cp => cp.Country)
            .Include(cp => cp.Provider)
            .FirstOrDefaultAsync(cp => cp.CountryId == countryId && cp.ProviderId == providerId);
    }

    public async Task<List<CountryProvider>> GetByProviderIdAsync(Guid providerId)
    {
        return await _context.CountryProviders
            .Include(cp => cp.Country)
            .Where(cp => cp.ProviderId == providerId && cp.IsActive)
            .ToListAsync();
    }

    public async Task<CountryProvider?> GetActiveByCountryIdAsync(Guid countryId)
    {
        return await _context.CountryProviders
            .Include(cp => cp.Provider)
            .FirstOrDefaultAsync(cp => cp.CountryId == countryId && cp.IsActive);
    }

    public async Task AddAsync(CountryProvider countryProvider)
    {
        _logger.LogDebug("Adding country provider mapping for country {CountryId}, provider {ProviderId} to database",
            countryProvider.CountryId, countryProvider.ProviderId);
        await _context.CountryProviders.AddAsync(countryProvider);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully added country provider mapping {CountryProviderId} to database", countryProvider.Id);
    }

    public async Task UpdateAsync(CountryProvider countryProvider)
    {
        _logger.LogDebug("Updating country provider mapping {CountryProviderId} (IsActive={IsActive}) in database",
            countryProvider.Id, countryProvider.IsActive);
        _context.CountryProviders.Update(countryProvider);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully updated country provider mapping {CountryProviderId} in database", countryProvider.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting country provider mapping {CountryProviderId} from database", id);
        var countryProvider = await _context.CountryProviders.FindAsync(id);
        if (countryProvider != null)
        {
            _context.CountryProviders.Remove(countryProvider);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Successfully deleted country provider mapping {CountryProviderId} from database", id);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent country provider mapping {CountryProviderId}", id);
        }
    }

    public async Task<int> DeleteByProviderAsync(Guid providerId)
    {
        _logger.LogInformation("Deleting all country provider mappings for provider {ProviderId}", providerId);
        var mappings = await _context.CountryProviders
            .Where(cp => cp.ProviderId == providerId)
            .ToListAsync();

        if (mappings.Count > 0)
        {
            _context.CountryProviders.RemoveRange(mappings);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted {Count} country provider mappings for provider {ProviderId}",
                mappings.Count, providerId);
        }

        return mappings.Count;
    }
}
