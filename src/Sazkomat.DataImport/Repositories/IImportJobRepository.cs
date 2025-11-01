using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface IImportJobRepository
{
    Task<List<ImportJob>> GetAllAsync();
    Task<ImportJob?> GetByIdAsync(Guid id);
    Task<List<ImportJob>> GetByLeagueIdAsync(Guid leagueId);
    Task<ImportJob> CreateAsync(ImportJob job);
    Task<ImportJob> UpdateAsync(ImportJob job);
}
