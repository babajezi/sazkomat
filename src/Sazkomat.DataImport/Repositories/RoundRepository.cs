using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public class RoundRepository : IRoundRepository
{
    private readonly DataImportDbContext _context;
    private readonly ILogger<RoundRepository> _logger;

    public RoundRepository(DataImportDbContext context, ILogger<RoundRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Round>> GetAllAsync()
    {
        return await _context.Rounds
            .ToListAsync();
    }

    public async Task<Round?> GetByIdAsync(Guid id)
    {
        return await _context.Rounds
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Round?> GetByLeagueSeasonRoundAsync(Guid leagueId, Guid seasonId, int roundNumber)
    {
        return await _context.Rounds
            .FirstOrDefaultAsync(r => r.LeagueId == leagueId && r.SeasonId == seasonId && r.RoundNumber == roundNumber);
    }

    public async Task<List<Round>> GetByLeagueAsync(Guid leagueId)
    {
        return await _context.Rounds
            .Where(r => r.LeagueId == leagueId)
            .OrderBy(r => r.SeasonId)
            .ThenBy(r => r.RoundNumber)
            .ToListAsync();
    }

    public async Task<List<Round>> GetByLeagueAndSeasonAsync(Guid leagueId, Guid seasonId)
    {
        return await _context.Rounds
            .Where(r => r.LeagueId == leagueId && r.SeasonId == seasonId)
            .OrderBy(r => r.RoundNumber)
            .ToListAsync();
    }

    public async Task<Round> CreateAsync(Round round)
    {
        _logger.LogDebug("Creating round {RoundNumber} for league {LeagueId}, season {SeasonId} in database",
            round.RoundNumber, round.LeagueId, round.SeasonId);
        _context.Rounds.Add(round);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully created round {RoundId} in database", round.Id);
        return round;
    }

    public async Task<Round> UpdateAsync(Round round)
    {
        _logger.LogDebug("Updating round {RoundId} in database", round.Id);
        _context.Rounds.Update(round);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully updated round {RoundId} in database", round.Id);
        return round;
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting round {RoundId} from database", id);
        var round = await GetByIdAsync(id);
        if (round != null)
        {
            _context.Rounds.Remove(round);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Successfully deleted round {RoundId} from database", id);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent round {RoundId}", id);
        }
    }
}
