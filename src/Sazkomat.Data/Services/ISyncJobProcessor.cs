using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Services;

public interface ISyncJobProcessor
{
    Task ProcessScanJobAsync(Guid jobId);
    Task ProcessImportJobAsync(Guid jobId);
    Task ProcessLiveSyncJobAsync(Guid jobId);
    Task<SyncJob?> GetJobStatusAsync(Guid jobId);
    Task<List<SyncJob>> GetRecentJobsAsync(Guid providerId, int count = 20);
    Task<bool> CancelJobAsync(Guid jobId);
}
