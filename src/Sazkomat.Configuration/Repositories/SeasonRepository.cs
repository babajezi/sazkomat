using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class SeasonRepository : ISeasonRepository
{
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<SeasonRepository> _logger;

    public SeasonRepository(ConfigurationDbContext context, ILogger<SeasonRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Season>> GetAllAsync()
    {
        return await _context.Seasons
            .AsNoTracking()
            .OrderByDescending(s => s.StartYear)
            .ThenByDescending(s => s.EndYear)
            .ToListAsync();
    }

    public async Task<Season?> GetByIdAsync(Guid id)
    {
        return await _context.Seasons
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Season?> GetByNameAsync(string name)
    {
        return await _context.Seasons
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<Season> GetOrCreateAsync(string name)
    {
        var existing = await GetByNameAsync(name);
        if (existing != null)
        {
            _logger.LogDebug("Season {SeasonName} already exists, returning existing {SeasonId}", name, existing.Id);
            return existing;
        }

        _logger.LogDebug("Season {SeasonName} not found, creating new season", name);
        // Parse season name to extract years (e.g., "2023-2024" or "2023")
        var season = ParseSeasonName(name);
        return await CreateAsync(season);
    }

    public async Task<Season> AddAsync(Season season)
    {
        _logger.LogDebug("Adding season {SeasonName} to database", season.Name);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully added season {SeasonId} to database", season.Id);
        return season;
    }

    public async Task<Season> CreateAsync(Season season)
    {
        _logger.LogDebug("Creating season {SeasonName} in database", season.Name);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully created season {SeasonId} in database", season.Id);
        return season;
    }

    public async Task<Season> UpdateAsync(Season season)
    {
        _logger.LogDebug("Updating season {SeasonId} in database", season.Id);
        _context.Seasons.Update(season);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully updated season {SeasonId} in database", season.Id);
        return season;
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting season {SeasonId} from database", id);
        var season = await _context.Seasons.FindAsync(id);
        if (season != null)
        {
            _context.Seasons.Remove(season);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Successfully deleted season {SeasonId} from database", id);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent season {SeasonId}", id);
        }
    }

    private Season ParseSeasonName(string name)
    {
        var season = new Season { Name = name };

        // Try to parse format "2023-2024" or "2023/2024"
        var parts = name.Split(new[] { '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0].Trim(), out var startYear))
            {
                season.StartYear = startYear;
            }

            if (int.TryParse(parts[1].Trim(), out var endYear))
            {
                season.EndYear = endYear;
            }
        }
        else if (parts.Length == 1)
        {
            // Single year season (e.g., "2023")
            if (int.TryParse(parts[0].Trim(), out var year))
            {
                season.StartYear = year;
                season.EndYear = null;
            }
        }

        return season;
    }
}
