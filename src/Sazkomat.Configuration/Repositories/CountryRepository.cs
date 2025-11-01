using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class CountryRepository : ICountryRepository
{
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<CountryRepository> _logger;

    public CountryRepository(ConfigurationDbContext context, ILogger<CountryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Country>> GetAllAsync()
    {
        return await _context.Countries
            .Include(c => c.CountryProviders)
                .ThenInclude(cp => cp.Provider)
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Country?> GetByIdAsync(Guid id)
    {
        return await _context.Countries
            .Include(c => c.Leagues)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Country?> GetByCodeAsync(string code)
    {
        return await _context.Countries
            .FirstOrDefaultAsync(c => c.Code == code);
    }

    public async Task<Country> AddAsync(Country country)
    {
        _context.Countries.Add(country);
        await _context.SaveChangesAsync();
        return country;
    }

    public async Task<Country> CreateAsync(Country country)
    {
        _logger.LogDebug("Creating country {CountryName} ({CountryCode}) in database", country.Name, country.Code);
        _context.Countries.Add(country);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully created country {CountryId} in database", country.Id);
        return country;
    }

    public async Task<Country> UpdateAsync(Country country)
    {
        _logger.LogDebug("Updating country {CountryId} in database", country.Id);
        _context.Countries.Update(country);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully updated country {CountryId} in database", country.Id);
        return country;
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting country {CountryId} from database", id);
        var country = await _context.Countries.FindAsync(id);
        if (country != null)
        {
            _context.Countries.Remove(country);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Successfully deleted country {CountryId} from database", id);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent country {CountryId}", id);
        }
    }
}
