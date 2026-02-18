using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Repositories;

public class SyncJobRepository : ISyncJobRepository
{
    private readonly DataDbContext _context;
    private readonly ILogger<SyncJobRepository> _logger;

    public SyncJobRepository(DataDbContext context, ILogger<SyncJobRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SyncJob>> GetAllAsync()
    {
        return await _context.SyncJobs
            .OrderByDescending(sj => sj.StartedAt)
            .ToListAsync();
    }

    public async Task<SyncJob?> GetByIdAsync(Guid id)
    {
        return await _context.SyncJobs
            .FirstOrDefaultAsync(sj => sj.Id == id);
    }

    public async Task<List<SyncJob>> GetByStatusAsync(SyncJobStatus status)
    {
        return await _context.SyncJobs
            .Where(sj => sj.Status == status)
            .OrderBy(sj => sj.Priority)
            .ThenBy(sj => sj.StartedAt)
            .ToListAsync();
    }

    public async Task<List<SyncJob>> GetPendingJobsAsync()
    {
        return await _context.SyncJobs
            .Where(sj => sj.Status == SyncJobStatus.Pending)
            .OrderBy(sj => sj.Priority)
            .ThenBy(sj => sj.StartedAt)
            .ToListAsync();
    }

    public async Task<List<SyncJob>> GetRunningJobsAsync()
    {
        return await _context.SyncJobs
            .Where(sj => sj.Status == SyncJobStatus.Running)
            .ToListAsync();
    }

    public async Task<SyncJob?> GetNextPendingJobAsync()
    {
        return await _context.SyncJobs
            .Where(sj => sj.Status == SyncJobStatus.Pending)
            .OrderBy(sj => sj.Priority)
            .ThenBy(sj => sj.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<SyncJob>> GetFailedJobsAsync()
    {
        return await _context.SyncJobs
            .Where(sj => sj.Status == SyncJobStatus.Failed)
            .OrderByDescending(sj => sj.CompletedAt)
            .ToListAsync();
    }

    public async Task<List<SyncJob>> GetByProviderIdAsync(Guid providerId)
    {
        return await _context.SyncJobs
            .Where(sj => sj.ProviderId == providerId)
            .OrderByDescending(sj => sj.StartedAt)
            .ToListAsync();
    }

    public async Task<List<SyncJob>> GetRecentJobsAsync(Guid providerId, int count)
    {
        return await _context.SyncJobs
            .Where(sj => sj.ProviderId == providerId)
            .OrderByDescending(sj => sj.CreatedAt)  // Use CreatedAt instead of StartedAt to include Pending jobs
            .Take(count)
            .ToListAsync();
    }

    public async Task<SyncJob> CreateAsync(SyncJob syncJob)
    {
        _context.SyncJobs.Add(syncJob);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created SyncJob {Id} of type {Type}/{EntityType}",
            syncJob.Id, syncJob.Type, syncJob.EntityType);
        return syncJob;
    }

    public async Task<SyncJob> UpdateAsync(SyncJob syncJob)
    {
        syncJob.UpdatedAt = DateTime.UtcNow;
        _context.SyncJobs.Update(syncJob);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated SyncJob {Id} to status {Status}",
            syncJob.Id, syncJob.Status);
        return syncJob;
    }

    public async Task DeleteAsync(Guid id)
    {
        var syncJob = await GetByIdAsync(id);
        if (syncJob != null)
        {
            _context.SyncJobs.Remove(syncJob);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted SyncJob {Id}", id);
        }
    }
}
