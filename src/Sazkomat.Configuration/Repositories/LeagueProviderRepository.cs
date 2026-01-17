using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class LeagueProviderRepository : ILeagueProviderRepository
{
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<LeagueProviderRepository> _logger;

    public LeagueProviderRepository(ConfigurationDbContext context, ILogger<LeagueProviderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<LeagueProvider>> GetAllAsync()
    {
        return await _context.LeagueProviders
            .Include(lp => lp.League)
            .Include(lp => lp.Provider)
            .ToListAsync();
    }

    public async Task<LeagueProvider?> GetByIdAsync(Guid id)
    {
        return await _context.LeagueProviders
            .Include(lp => lp.League)
            .Include(lp => lp.Provider)
            .FirstOrDefaultAsync(lp => lp.Id == id);
    }

    public async Task<IEnumerable<LeagueProvider>> GetByLeagueIdAsync(Guid leagueId)
    {
        return await _context.LeagueProviders
            .Include(lp => lp.Provider)
            .Where(lp => lp.LeagueId == leagueId)
            .ToListAsync();
    }

    public async Task<LeagueProvider?> GetByLeagueAndProviderAsync(Guid leagueId, Guid providerId)
    {
        return await _context.LeagueProviders
            .Include(lp => lp.League)
            .Include(lp => lp.Provider)
            .FirstOrDefaultAsync(lp => lp.LeagueId == leagueId && lp.ProviderId == providerId);
    }

    public async Task<LeagueProvider?> GetByProviderAndSlugAsync(Guid providerId, string providerSlug)
    {
        return await _context.LeagueProviders
            .Include(lp => lp.League)
                .ThenInclude(l => l.Sport)
            .Include(lp => lp.League)
                .ThenInclude(l => l.Country)
            .Include(lp => lp.Provider)
            .FirstOrDefaultAsync(lp => lp.ProviderId == providerId && lp.ProviderSlug == providerSlug);
    }

    public async Task<LeagueProvider?> GetActiveByLeagueIdAsync(Guid leagueId)
    {
        return await _context.LeagueProviders
            .Include(lp => lp.Provider)
            .FirstOrDefaultAsync(lp => lp.LeagueId == leagueId && lp.IsActive);
    }

    public async Task AddAsync(LeagueProvider leagueProvider)
    {
        _logger.LogDebug("Adding league provider mapping for league {LeagueId}, provider {ProviderId} (Slug: {ProviderSlug}) to database",
            leagueProvider.LeagueId, leagueProvider.ProviderId, leagueProvider.ProviderSlug);
        await _context.LeagueProviders.AddAsync(leagueProvider);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully added league provider mapping {LeagueProviderId} to database", leagueProvider.Id);
    }

    public async Task<LeagueProvider> AddOrUpdateAsync(LeagueProvider leagueProvider)
    {
        var existing = await _context.LeagueProviders
            .FirstOrDefaultAsync(lp =>
                lp.ProviderId == leagueProvider.ProviderId &&
                lp.ProviderSlug == leagueProvider.ProviderSlug);

        if (existing != null)
        {
            _logger.LogDebug("Updating existing league provider mapping {LeagueProviderId} (Slug: {ProviderSlug})",
                existing.Id, existing.ProviderSlug);
            existing.LeagueId = leagueProvider.LeagueId;
            existing.ProviderName = leagueProvider.ProviderName;
            existing.IsActive = leagueProvider.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }
        else
        {
            _logger.LogDebug("Adding new league provider mapping for league {LeagueId}, provider {ProviderId} (Slug: {ProviderSlug})",
                leagueProvider.LeagueId, leagueProvider.ProviderId, leagueProvider.ProviderSlug);
            await _context.LeagueProviders.AddAsync(leagueProvider);
            await _context.SaveChangesAsync();
            return leagueProvider;
        }
    }

    public async Task UpdateAsync(LeagueProvider leagueProvider)
    {
        _logger.LogDebug("Updating league provider mapping {LeagueProviderId} (IsActive={IsActive}) in database",
            leagueProvider.Id, leagueProvider.IsActive);
        _context.LeagueProviders.Update(leagueProvider);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully updated league provider mapping {LeagueProviderId} in database", leagueProvider.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting league provider mapping {LeagueProviderId} from database", id);
        var leagueProvider = await _context.LeagueProviders.FindAsync(id);
        if (leagueProvider != null)
        {
            _context.LeagueProviders.Remove(leagueProvider);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Successfully deleted league provider mapping {LeagueProviderId} from database", id);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent league provider mapping {LeagueProviderId}", id);
        }
    }

    public async Task<List<LeagueProvider>> GetByProviderIdAsync(Guid providerId)
    {
        return await _context.LeagueProviders
            .Include(lp => lp.League)
            .Where(lp => lp.ProviderId == providerId && lp.IsActive)
            .ToListAsync();
    }

    public async Task<int> DeleteByProviderAsync(Guid providerId)
    {
        _logger.LogInformation("Deleting all league provider mappings for provider {ProviderId}", providerId);
        var mappings = await _context.LeagueProviders
            .Where(lp => lp.ProviderId == providerId)
            .ToListAsync();

        if (mappings.Count > 0)
        {
            _context.LeagueProviders.RemoveRange(mappings);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted {Count} league provider mappings for provider {ProviderId}",
                mappings.Count, providerId);
        }

        return mappings.Count;
    }
}
