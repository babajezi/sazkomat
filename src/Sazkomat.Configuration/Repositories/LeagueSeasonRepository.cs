using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class LeagueSeasonRepository : ILeagueSeasonRepository
{
    private readonly ConfigurationDbContext _context;

    public LeagueSeasonRepository(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeagueSeason>> GetAllAsync()
    {
        return await _context.LeagueSeasons
            .AsNoTracking()
            .Include(ls => ls.League)
            .Include(ls => ls.Season)
            .ToListAsync();
    }

    public async Task<LeagueSeason?> GetByIdAsync(Guid id)
    {
        return await _context.LeagueSeasons
            .Include(ls => ls.League)
            .Include(ls => ls.Season)
            .FirstOrDefaultAsync(ls => ls.Id == id);
    }

    public async Task<LeagueSeason?> GetByLeagueAndSeasonAsync(Guid leagueId, Guid seasonId)
    {
        return await _context.LeagueSeasons
            .Include(ls => ls.League)
            .Include(ls => ls.Season)
            .FirstOrDefaultAsync(ls => ls.LeagueId == leagueId && ls.SeasonId == seasonId);
    }

    public async Task<List<LeagueSeason>> GetByLeagueIdAsync(Guid leagueId, bool includeRelations = false)
    {
        var query = _context.LeagueSeasons
            .AsNoTracking()
            .Where(ls => ls.LeagueId == leagueId);

        if (includeRelations)
        {
            query = query.Include(ls => ls.Season);
        }

        return await query
            .OrderByDescending(ls => ls.Season.StartYear)
            .ThenByDescending(ls => ls.Season.EndYear)
            .ToListAsync();
    }

    public async Task<List<LeagueSeason>> GetAvailableForLeagueAsync(Guid leagueId)
    {
        return await _context.LeagueSeasons
            .AsNoTracking()
            .Include(ls => ls.Season)
            .Where(ls => ls.LeagueId == leagueId && ls.IsAvailableOnBetExplorer)
            .OrderByDescending(ls => ls.Season.StartYear)
            .ThenByDescending(ls => ls.Season.EndYear)
            .ToListAsync();
    }

    public async Task<LeagueSeason> AddAsync(LeagueSeason leagueSeason)
    {
        _context.LeagueSeasons.Add(leagueSeason);
        await _context.SaveChangesAsync();
        return leagueSeason;
    }

    public async Task<LeagueSeason> CreateAsync(LeagueSeason leagueSeason)
    {
        _context.LeagueSeasons.Add(leagueSeason);
        await _context.SaveChangesAsync();
        return leagueSeason;
    }

    public async Task<LeagueSeason> UpdateAsync(LeagueSeason leagueSeason)
    {
        _context.LeagueSeasons.Update(leagueSeason);
        await _context.SaveChangesAsync();
        return leagueSeason;
    }

    public async Task DeleteAsync(Guid id)
    {
        var leagueSeason = await _context.LeagueSeasons.FindAsync(id);
        if (leagueSeason != null)
        {
            _context.LeagueSeasons.Remove(leagueSeason);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateMetadataAsync(Guid leagueId, Guid seasonId, int roundsCount, int matchesCount, bool hasOdds)
    {
        var leagueSeason = await GetByLeagueAndSeasonAsync(leagueId, seasonId);
        if (leagueSeason != null)
        {
            leagueSeason.HasData = true;
            leagueSeason.RoundsCount = roundsCount;
            leagueSeason.MatchesCount = matchesCount;
            leagueSeason.HasOdds = hasOdds;
            leagueSeason.LastScrapedAt = DateTime.UtcNow;
            await UpdateAsync(leagueSeason);
        }
    }

    public async Task<List<LeagueSeason>> GetSyncEnabledAsync()
    {
        return await _context.LeagueSeasons
            .AsNoTracking()
            .Include(ls => ls.League)
            .Include(ls => ls.Season)
            .Where(ls => ls.SyncEnabled)
            .ToListAsync();
    }

    public async Task UpdateSyncEnabledAsync(Guid leagueSeasonId, bool enabled)
    {
        var leagueSeason = await _context.LeagueSeasons.FindAsync(leagueSeasonId);
        if (leagueSeason != null)
        {
            leagueSeason.SyncEnabled = enabled;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateIsCurrentAsync(Guid leagueSeasonId, bool isCurrent, SyncMode syncMode)
    {
        var leagueSeason = await _context.LeagueSeasons.FindAsync(leagueSeasonId);
        if (leagueSeason != null)
        {
            leagueSeason.IsCurrent = isCurrent;
            leagueSeason.SyncMode = syncMode;
            await _context.SaveChangesAsync();
        }
    }
}
