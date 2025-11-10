using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public class ProviderSeasonRepository : IProviderSeasonRepository
{
    private readonly DataImportDbContext _context;
    private readonly ILogger<ProviderSeasonRepository> _logger;

    public ProviderSeasonRepository(DataImportDbContext context, ILogger<ProviderSeasonRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProviderSeason>> GetAllAsync()
    {
        return await _context.ProviderSeasons
            .OrderByDescending(ps => ps.ScrapedAt)
            .ToListAsync();
    }

    public async Task<ProviderSeason?> GetByIdAsync(Guid id)
    {
        return await _context.ProviderSeasons
            .FirstOrDefaultAsync(ps => ps.Id == id);
    }

    public async Task<List<ProviderSeason>> GetByProviderIdAsync(Guid providerId)
    {
        return await _context.ProviderSeasons
            .Where(ps => ps.ProviderId == providerId)
            .OrderByDescending(ps => ps.StartYear)
            .ThenByDescending(ps => ps.EndYear)
            .ToListAsync();
    }

    public async Task<List<ProviderSeason>> GetByProviderLeagueIdAsync(Guid providerLeagueId)
    {
        return await _context.ProviderSeasons
            .Where(ps => ps.ProviderLeagueId == providerLeagueId)
            .OrderByDescending(ps => ps.StartYear)
            .ThenByDescending(ps => ps.EndYear)
            .ToListAsync();
    }

    public async Task<ProviderSeason?> GetBySeasonNameAsync(Guid providerLeagueId, string seasonName)
    {
        return await _context.ProviderSeasons
            .Where(ps => ps.ProviderLeagueId == providerLeagueId && ps.SeasonName == seasonName)
            .OrderByDescending(ps => ps.ScrapedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ProviderSeason>> GetUnimportedAsync(Guid providerId)
    {
        return await _context.ProviderSeasons
            .Where(ps => ps.ProviderId == providerId && !ps.IsImported)
            .OrderByDescending(ps => ps.StartYear)
            .ThenByDescending(ps => ps.EndYear)
            .ToListAsync();
    }

    public async Task<List<ProviderSeason>> GetCurrentSeasonsAsync(Guid providerId)
    {
        return await _context.ProviderSeasons
            .Where(ps => ps.ProviderId == providerId && ps.IsCurrentSeason)
            .ToListAsync();
    }

    public async Task<ProviderSeason> CreateAsync(ProviderSeason providerSeason)
    {
        _context.ProviderSeasons.Add(providerSeason);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created ProviderSeason {Id} for provider {ProviderId}",
            providerSeason.Id, providerSeason.ProviderId);
        return providerSeason;
    }

    public async Task<ProviderSeason> UpdateAsync(ProviderSeason providerSeason)
    {
        providerSeason.UpdatedAt = DateTime.UtcNow;
        _context.ProviderSeasons.Update(providerSeason);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated ProviderSeason {Id}", providerSeason.Id);
        return providerSeason;
    }

    public async Task DeleteAsync(Guid id)
    {
        var providerSeason = await GetByIdAsync(id);
        if (providerSeason != null)
        {
            _context.ProviderSeasons.Remove(providerSeason);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted ProviderSeason {Id}", id);
        }
    }
}
