using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface ISyncJobRepository
{
    Task<List<SyncJob>> GetAllAsync();
    Task<SyncJob?> GetByIdAsync(Guid id);
    Task<List<SyncJob>> GetByStatusAsync(SyncJobStatus status);
    Task<List<SyncJob>> GetPendingJobsAsync();
    Task<List<SyncJob>> GetRunningJobsAsync();
    Task<SyncJob?> GetNextPendingJobAsync();
    Task<List<SyncJob>> GetFailedJobsAsync();
    Task<List<SyncJob>> GetByProviderIdAsync(Guid providerId);
    Task<List<SyncJob>> GetRecentJobsAsync(Guid providerId, int count);
    Task<SyncJob> CreateAsync(SyncJob syncJob);
    Task<SyncJob> UpdateAsync(SyncJob syncJob);
    Task DeleteAsync(Guid id);
}
