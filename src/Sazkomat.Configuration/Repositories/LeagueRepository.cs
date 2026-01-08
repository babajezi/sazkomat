using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class LeagueRepository : ILeagueRepository
{
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<LeagueRepository> _logger;

    public LeagueRepository(ConfigurationDbContext context, ILogger<LeagueRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<League>> GetAllAsync(Guid? sportId = null, Guid? countryId = null, bool? onlyEnabled = null, bool includeRelations = false)
    {
        var query = _context.Leagues
            .AsNoTracking()
            .AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(l => l.Sport)
                .Include(l => l.Country)
                .Include(l => l.LeagueProviders)
                    .ThenInclude(lp => lp.Provider);
        }

        if (sportId.HasValue)
        {
            query = query.Where(l => l.SportId == sportId.Value);
        }

        if (countryId.HasValue)
        {
            query = query.Where(l => l.CountryId == countryId.Value);
        }

        if (onlyEnabled.HasValue && onlyEnabled.Value)
        {
            query = query.Where(l => l.IsActive);
        }

        return await query.ToListAsync();
    }

    public async Task<League?> GetByIdAsync(Guid id)
    {
        return await _context.Leagues
            .Include(l => l.Sport)
            .Include(l => l.Country)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<League?> GetBySlugAsync(string slug)
    {
        return await _context.Leagues
            .FirstOrDefaultAsync(l => l.BetExplorerSlug == slug);
    }

    public async Task<League?> GetByBetExplorerSlugAsync(string betExplorerSlug)
    {
        return await _context.Leagues
            .Include(l => l.Country)
            .FirstOrDefaultAsync(l => l.BetExplorerSlug == betExplorerSlug);
    }

    public async Task<List<League>> GetByCountryIdAsync(Guid countryId)
    {
        return await _context.Leagues
            .Where(l => l.CountryId == countryId)
            .ToListAsync();
    }

    public async Task<League> AddAsync(League league)
    {
        _context.Leagues.Add(league);
        await _context.SaveChangesAsync();
        return league;
    }

    public async Task<League> CreateAsync(League league)
    {
        _logger.LogDebug("Creating league {LeagueName} in database", league.Name);
        _context.Leagues.Add(league);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully created league {LeagueId} in database", league.Id);
        return league;
    }

    public async Task<League> UpdateAsync(League league)
    {
        _logger.LogDebug("Updating league {LeagueId} in database", league.Id);
        _context.Leagues.Update(league);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully updated league {LeagueId} in database", league.Id);
        return league;
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting league {LeagueId} from database", id);
        var league = await _context.Leagues.FindAsync(id);
        if (league != null)
        {
            _context.Leagues.Remove(league);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Successfully deleted league {LeagueId} from database", id);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent league {LeagueId}", id);
        }
    }
}
