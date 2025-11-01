using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface IDataProviderRepository
{
    Task<IEnumerable<DataProvider>> GetAllAsync(bool? onlyActive = null);
    Task<DataProvider?> GetByIdAsync(Guid id);
    Task<DataProvider?> GetByCodeAsync(string code);
    Task AddAsync(DataProvider provider);
    Task UpdateAsync(DataProvider provider);
    Task DeleteAsync(Guid id);
}
