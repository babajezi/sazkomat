using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class LogSettingsRepository : ILogSettingsRepository
{
    private readonly ConfigurationDbContext _dbContext;

    public LogSettingsRepository(ConfigurationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<LogSettings>> GetAllAsync()
    {
        return await _dbContext.LogSettings
            .OrderBy(ls => ls.Category)
            .ThenBy(ls => ls.SubCategory)
            .ToListAsync();
    }

    public async Task<LogSettings?> GetByIdAsync(Guid id)
    {
        return await _dbContext.LogSettings.FindAsync(id);
    }

    public async Task<LogSettings?> GetByCategoryAndSubCategoryAsync(string category, string subCategory)
    {
        return await _dbContext.LogSettings
            .FirstOrDefaultAsync(ls => ls.Category == category && ls.SubCategory == subCategory);
    }

    public async Task<IEnumerable<LogSettings>> GetByCategoryAsync(string category)
    {
        return await _dbContext.LogSettings
            .Where(ls => ls.Category == category)
            .OrderBy(ls => ls.SubCategory)
            .ToListAsync();
    }

    public async Task<LogSettings> CreateAsync(LogSettings logSettings)
    {
        _dbContext.LogSettings.Add(logSettings);
        await _dbContext.SaveChangesAsync();
        return logSettings;
    }

    public async Task<LogSettings> UpdateAsync(LogSettings logSettings)
    {
        _dbContext.LogSettings.Update(logSettings);
        await _dbContext.SaveChangesAsync();
        return logSettings;
    }

    public async Task DeleteAsync(Guid id)
    {
        var logSettings = await GetByIdAsync(id);
        if (logSettings != null)
        {
            _dbContext.LogSettings.Remove(logSettings);
            await _dbContext.SaveChangesAsync();
        }
    }
}
