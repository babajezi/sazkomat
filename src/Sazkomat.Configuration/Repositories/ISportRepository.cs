using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface ISportRepository
{
    Task<List<Sport>> GetAllAsync();
    Task<Sport?> GetByIdAsync(Guid id);
    Task<Sport> CreateAsync(Sport sport);
    Task<Sport> UpdateAsync(Sport sport);
    Task DeleteAsync(Guid id);
}
