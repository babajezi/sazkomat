using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface ILogSettingsRepository
{
    Task<IEnumerable<LogSettings>> GetAllAsync();
    Task<LogSettings?> GetByIdAsync(Guid id);
    Task<LogSettings?> GetByCategoryAndSubCategoryAsync(string category, string subCategory);
    Task<IEnumerable<LogSettings>> GetByCategoryAsync(string category);
    Task<LogSettings> CreateAsync(LogSettings logSettings);
    Task<LogSettings> UpdateAsync(LogSettings logSettings);
    Task DeleteAsync(Guid id);
}
