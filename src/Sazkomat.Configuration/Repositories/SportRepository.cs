using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class SportRepository : ISportRepository
{
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<SportRepository> _logger;

    public SportRepository(ConfigurationDbContext context, ILogger<SportRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Sport>> GetAllAsync()
    {
        return await _context.Sports
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Sport?> GetByIdAsync(Guid id)
    {
        return await _context.Sports
            .Include(s => s.Leagues)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Sport> CreateAsync(Sport sport)
    {
        _logger.LogDebug("Creating sport {SportName} in database", sport.Name);
        _context.Sports.Add(sport);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully created sport {SportId} in database", sport.Id);
        return sport;
    }

    public async Task<Sport> UpdateAsync(Sport sport)
    {
        _logger.LogDebug("Updating sport {SportId} in database", sport.Id);
        _context.Sports.Update(sport);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully updated sport {SportId} in database", sport.Id);
        return sport;
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting sport {SportId} from database", id);
        var sport = await _context.Sports.FindAsync(id);
        if (sport != null)
        {
            _context.Sports.Remove(sport);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Successfully deleted sport {SportId} from database", id);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent sport {SportId}", id);
        }
    }
}
