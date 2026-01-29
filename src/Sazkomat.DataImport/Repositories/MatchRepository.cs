using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly DataImportDbContext _context;
    private readonly ISeasonRepository _seasonRepository;
    private readonly ILogger<MatchRepository> _logger;

    public MatchRepository(DataImportDbContext context, ISeasonRepository seasonRepository, ILogger<MatchRepository> logger)
    {
        _context = context;
        _seasonRepository = seasonRepository;
        _logger = logger;
    }

    public async Task<List<Match>> GetAllAsync(MatchFilter? filter = null)
    {
        var query = _context.Matches
            .Include(m => m.Round)
            .AsQueryable();

        query = ApplyFilter(query, filter);

        // For "round" and default sorting, we need to get season start years
        // since the Season navigation is ignored (it's in a different schema)
        var sortByRound = filter?.SortBy?.ToLower() == "round" || filter?.SortBy == null;
        var sortDescending = filter?.SortDescending ?? true;

        _logger.LogDebug("MatchRepository.GetAllAsync: sortByRound={SortByRound}, sortDescending={SortDescending}", sortByRound, sortDescending);

        if (sortByRound)
        {
            // Get all seasons from configuration repository
            var allSeasons = await _seasonRepository.GetAllAsync();
            var seasons = allSeasons.ToDictionary(s => s.Id, s => s.StartYear);

            _logger.LogDebug("MatchRepository: Loaded {SeasonCount} seasons for sorting", seasons.Count);

            // Load all matching records first (without pagination for sorting)
            var allMatches = await query.ToListAsync();

            // Sort in memory with season start year
            var sortedMatches = sortDescending
                ? allMatches.OrderByDescending(m => seasons.GetValueOrDefault(m.Round.SeasonId, 0))
                            .ThenByDescending(m => m.Round.RoundNumber)
                            .ToList()
                : allMatches.OrderBy(m => seasons.GetValueOrDefault(m.Round.SeasonId, 0))
                            .ThenBy(m => m.Round.RoundNumber)
                            .ToList();

            // Apply pagination in memory
            if (filter?.Skip.HasValue == true)
            {
                sortedMatches = sortedMatches.Skip(filter.Skip.Value).ToList();
            }

            if (filter?.Take.HasValue == true)
            {
                sortedMatches = sortedMatches.Take(filter.Take.Value).ToList();
            }

            return sortedMatches;
        }

        // Apply sorting for non-round sorts
        if (filter?.SortBy != null)
        {
            query = filter.SortBy.ToLower() switch
            {
                "date" => sortDescending
                    ? query.OrderByDescending(m => m.MatchDate)
                    : query.OrderBy(m => m.MatchDate),
                _ => sortDescending
                    ? query.OrderByDescending(m => m.CreatedAt)
                    : query.OrderBy(m => m.CreatedAt)
            };
        }

        // Apply pagination
        if (filter?.Skip.HasValue == true)
        {
            query = query.Skip(filter.Skip.Value);
        }

        if (filter?.Take.HasValue == true)
        {
            query = query.Take(filter.Take.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<Match?> GetByIdAsync(Guid id)
    {
        return await _context.Matches
            .Include(m => m.Round)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Match>> GetByRoundIdAsync(Guid roundId)
    {
        return await _context.Matches
            .Where(m => m.RoundId == roundId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(MatchFilter? filter = null)
    {
        var query = _context.Matches.AsQueryable();
        query = ApplyFilter(query, filter);
        return await query.CountAsync();
    }

    public async Task<Match> CreateAsync(Match match)
    {
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();
        return match;
    }

    public async Task<Match> UpdateAsync(Match match)
    {
        match.UpdatedAt = DateTime.UtcNow;
        _context.Matches.Update(match);
        await _context.SaveChangesAsync();
        return match;
    }

    public async Task DeleteAsync(Guid id)
    {
        var match = await GetByIdAsync(id);
        if (match != null)
        {
            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();
        }
    }

    private IQueryable<Match> ApplyFilter(IQueryable<Match> query, MatchFilter? filter)
    {
        if (filter == null) return query;

        if (filter.LeagueId.HasValue)
        {
            query = query.Where(m => m.Round.LeagueId == filter.LeagueId.Value);
        }

        if (filter.SeasonId.HasValue)
        {
            query = query.Where(m => m.Round.SeasonId == filter.SeasonId.Value);
        }

        if (filter.RoundNumber.HasValue)
        {
            query = query.Where(m => m.Round.RoundNumber == filter.RoundNumber.Value);
        }

        if (!string.IsNullOrEmpty(filter.Result))
        {
            query = query.Where(m => m.Result == filter.Result);
        }

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(m => m.MatchDate >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            query = query.Where(m => m.MatchDate <= filter.DateTo.Value);
        }

        if (!string.IsNullOrEmpty(filter.TeamName))
        {
            var teamName = filter.TeamName.ToLower();
            query = query.Where(m => m.HomeTeam.ToLower().Contains(teamName) ||
                                     m.AwayTeam.ToLower().Contains(teamName));
        }

        return query;
    }
}
