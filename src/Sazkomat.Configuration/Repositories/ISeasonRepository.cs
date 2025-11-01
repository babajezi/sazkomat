using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface ISeasonRepository
{
    Task<List<Season>> GetAllAsync();
    Task<Season?> GetByIdAsync(Guid id);
    Task<Season?> GetByNameAsync(string name);
    Task<Season> GetOrCreateAsync(string name);
    Task<Season> AddAsync(Season season);
    Task<Season> CreateAsync(Season season);
    Task<Season> UpdateAsync(Season season);
    Task DeleteAsync(Guid id);
}
