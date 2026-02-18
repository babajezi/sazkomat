using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Repositories;

public class ProviderLeagueRepository : IProviderLeagueRepository
{
    private readonly DataDbContext _context;
    private readonly ILogger<ProviderLeagueRepository> _logger;

    public ProviderLeagueRepository(DataDbContext context, ILogger<ProviderLeagueRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProviderLeague>> GetAllAsync()
    {
        return await _context.ProviderLeagues
            .OrderByDescending(pl => pl.ScrapedAt)
            .ToListAsync();
    }

    public async Task<ProviderLeague?> GetByIdAsync(Guid id)
    {
        return await _context.ProviderLeagues
            .FirstOrDefaultAsync(pl => pl.Id == id);
    }

    public async Task<List<ProviderLeague>> GetByProviderIdAsync(Guid providerId)
    {
        return await _context.ProviderLeagues
            .Where(pl => pl.ProviderId == providerId)
            .OrderByDescending(pl => pl.ScrapedAt)
            .ToListAsync();
    }

    public async Task<List<ProviderLeague>> GetByProviderCountryIdAsync(Guid providerCountryId)
    {
        return await _context.ProviderLeagues
            .Where(pl => pl.ProviderCountryId == providerCountryId)
            .OrderBy(pl => pl.Priority)
            .ThenBy(pl => pl.ProviderName)
            .ToListAsync();
    }

    public async Task<ProviderLeague?> GetByProviderSlugAsync(Guid providerId, string providerSlug)
    {
        return await _context.ProviderLeagues
            .Where(pl => pl.ProviderId == providerId && pl.ProviderSlug == providerSlug)
            .OrderByDescending(pl => pl.ScrapedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ProviderLeague>> GetUnimportedAsync(Guid providerId)
    {
        return await _context.ProviderLeagues
            .Where(pl => pl.ProviderId == providerId && !pl.IsImported)
            .OrderBy(pl => pl.Priority)
            .ThenBy(pl => pl.ProviderName)
            .ToListAsync();
    }

    public async Task<ProviderLeague> CreateAsync(ProviderLeague providerLeague)
    {
        _context.ProviderLeagues.Add(providerLeague);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created ProviderLeague {Id} for provider {ProviderId}",
            providerLeague.Id, providerLeague.ProviderId);
        return providerLeague;
    }

    public async Task<ProviderLeague> UpdateAsync(ProviderLeague providerLeague)
    {
        providerLeague.UpdatedAt = DateTime.UtcNow;
        _context.ProviderLeagues.Update(providerLeague);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated ProviderLeague {Id}", providerLeague.Id);
        return providerLeague;
    }

    public async Task DeleteAsync(Guid id)
    {
        var providerLeague = await GetByIdAsync(id);
        if (providerLeague != null)
        {
            _context.ProviderLeagues.Remove(providerLeague);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted ProviderLeague {Id}", id);
        }
    }
}
