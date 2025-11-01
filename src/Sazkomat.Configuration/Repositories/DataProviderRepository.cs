using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class DataProviderRepository : IDataProviderRepository
{
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<DataProviderRepository> _logger;

    public DataProviderRepository(ConfigurationDbContext context, ILogger<DataProviderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<DataProvider>> GetAllAsync(bool? onlyActive = null)
    {
        var query = _context.DataProviders.AsQueryable();

        if (onlyActive.HasValue)
        {
            query = query.Where(p => p.IsActive == onlyActive.Value);
        }

        return await query
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<DataProvider?> GetByIdAsync(Guid id)
    {
        return await _context.DataProviders.FindAsync(id);
    }

    public async Task<DataProvider?> GetByCodeAsync(string code)
    {
        return await _context.DataProviders
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task AddAsync(DataProvider provider)
    {
        _logger.LogDebug("Adding data provider {ProviderName} ({ProviderCode}) to database", provider.Name, provider.Code);
        await _context.DataProviders.AddAsync(provider);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully added data provider {ProviderId} to database", provider.Id);
    }

    public async Task UpdateAsync(DataProvider provider)
    {
        _logger.LogDebug("Updating data provider {ProviderId} in database", provider.Id);
        _context.DataProviders.Update(provider);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully updated data provider {ProviderId} in database", provider.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting data provider {ProviderId} from database", id);
        var provider = await GetByIdAsync(id);
        if (provider != null)
        {
            _context.DataProviders.Remove(provider);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Successfully deleted data provider {ProviderId} from database", id);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent data provider {ProviderId}", id);
        }
    }
}
