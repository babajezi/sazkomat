using Microsoft.Extensions.Logging;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.DataImport.Services;

public class SyncJobProcessor : ISyncJobProcessor
{
    private readonly ISyncJobRepository _syncJobRepo;
    private readonly IScanService _scanService;
    private readonly IImportService _importService;
    private readonly ILiveSyncService _liveSyncService;
    private readonly ILogger<SyncJobProcessor> _logger;

    public SyncJobProcessor(
        ISyncJobRepository syncJobRepo,
        IScanService scanService,
        IImportService importService,
        ILiveSyncService liveSyncService,
        ILogger<SyncJobProcessor> logger)
    {
        _syncJobRepo = syncJobRepo;
        _scanService = scanService;
        _importService = importService;
        _liveSyncService = liveSyncService;
        _logger = logger;
    }

    public async Task ProcessScanJobAsync(Guid jobId)
    {
        _logger.LogInformation("Processing scan job {JobId}", jobId);

        var job = await _syncJobRepo.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Scan job {JobId} not found", jobId);
            return;
        }

        if (job.Status != SyncJobStatus.Pending)
        {
            _logger.LogWarning("Scan job {JobId} is not in Pending status, current status: {Status}",
                jobId, job.Status);
            return;
        }

        // Note: ScanService internal methods will handle status updates (Pending -> Running -> Completed/PartiallyCompleted/Failed)
        try
        {
            // Process based on entity type using internal methods that expect the job already created
            switch (job.EntityType)
            {
                case SyncEntityType.Countries:
                    await _scanService.ScanCountriesInternalAsync(job.ProviderId, job.Id);
                    break;

                case SyncEntityType.Leagues:
                    // Pass countryIds (can be empty - method handles loading all countries from provider cache)
                    await _scanService.ScanLeaguesInternalAsync(job.ProviderId, job.CountryIds ?? new List<Guid>(), job.Id);
                    break;

                case SyncEntityType.Seasons:
                    // Always call - method handles empty leagueIds by loading all active leagues
                    await _scanService.ScanSeasonsInternalAsync(
                        job.ProviderId,
                        job.LeagueIds ?? new List<Guid>(),
                        job.Id);
                    break;

                case SyncEntityType.CountriesAndLeagues:
                    // Combined scan for Betano - single HTTP request for both countries and leagues
                    await _scanService.ScanCountriesAndLeaguesInternalAsync(job.ProviderId, job.Id);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported entity type for scan: {job.EntityType}");
            }

            // Internal methods handle status updates, so we just verify completion
            job = await _syncJobRepo.GetByIdAsync(jobId);
            if (job != null && job.Status != SyncJobStatus.Completed && job.Status != SyncJobStatus.PartiallyCompleted && job.Status != SyncJobStatus.Failed)
            {
                // If for some reason the job is still in Running status, mark as completed
                job.Status = SyncJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                await _syncJobRepo.UpdateAsync(job);
            }

            _logger.LogInformation("Scan job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan job {JobId} failed", jobId);

            job.Status = SyncJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;

            // Resilient status update with retry logic to prevent jobs stuck in Running status
            bool statusUpdated = false;
            for (int attempt = 1; attempt <= 3 && !statusUpdated; attempt++)
            {
                try
                {
                    await _syncJobRepo.UpdateAsync(job);
                    statusUpdated = true;
                    _logger.LogInformation("Successfully updated job {JobId} to Failed status", job.Id);
                }
                catch (Exception updateEx)
                {
                    if (attempt == 3)
                    {
                        _logger.LogCritical(updateEx,
                            "CRITICAL: Failed to update job {JobId} to Failed status after 3 attempts. " +
                            "Job will remain in Running status. Original error: {OriginalError}",
                            job.Id, ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning(updateEx,
                            "Attempt {Attempt}/3 to update job status failed, retrying...", attempt);
                        await Task.Delay(500 * attempt); // Exponential backoff
                    }
                }
            }

            throw;
        }
    }

    public async Task ProcessImportJobAsync(Guid jobId)
    {
        _logger.LogInformation("Processing import job {JobId}", jobId);

        var job = await _syncJobRepo.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Import job {JobId} not found", jobId);
            return;
        }

        if (job.Status != SyncJobStatus.Pending)
        {
            _logger.LogWarning("Import job {JobId} is not in Pending status, current status: {Status}",
                jobId, job.Status);
            return;
        }

        // Note: ImportService internal methods will handle status updates (Pending -> Running -> Completed/PartiallyCompleted/Failed)
        try
        {
            // Process based on entity type using internal methods that expect the job already created
            switch (job.EntityType)
            {
                case SyncEntityType.Countries:
                    if (job.CountryIds != null && job.CountryIds.Any())
                    {
                        await _importService.ImportCountriesFromCacheInternalAsync(job.Id, job.CountryIds);
                    }
                    break;

                case SyncEntityType.Leagues:
                    if (job.LeagueIds != null && job.LeagueIds.Any())
                    {
                        await _importService.ImportLeaguesFromCacheInternalAsync(job.Id, job.LeagueIds);
                    }
                    break;

                case SyncEntityType.Seasons:
                    if (job.SeasonIds != null && job.SeasonIds.Any())
                    {
                        await _importService.ImportSeasonsFromCacheInternalAsync(job.Id, job.SeasonIds);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported entity type for import: {job.EntityType}");
            }

            // Internal methods handle status updates, so we just verify completion
            job = await _syncJobRepo.GetByIdAsync(jobId);
            if (job != null && job.Status != SyncJobStatus.Completed && job.Status != SyncJobStatus.PartiallyCompleted && job.Status != SyncJobStatus.Failed)
            {
                // If for some reason the job is still in Running status, mark as completed
                job.Status = SyncJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                await _syncJobRepo.UpdateAsync(job);
            }

            _logger.LogInformation("Import job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import job {JobId} failed", jobId);

            job.Status = SyncJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;

            // Resilient status update with retry logic to prevent jobs stuck in Running status
            bool statusUpdated = false;
            for (int attempt = 1; attempt <= 3 && !statusUpdated; attempt++)
            {
                try
                {
                    await _syncJobRepo.UpdateAsync(job);
                    statusUpdated = true;
                    _logger.LogInformation("Successfully updated job {JobId} to Failed status", job.Id);
                }
                catch (Exception updateEx)
                {
                    if (attempt == 3)
                    {
                        _logger.LogCritical(updateEx,
                            "CRITICAL: Failed to update job {JobId} to Failed status after 3 attempts. " +
                            "Job will remain in Running status. Original error: {OriginalError}",
                            job.Id, ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning(updateEx,
                            "Attempt {Attempt}/3 to update job status failed, retrying...", attempt);
                        await Task.Delay(500 * attempt); // Exponential backoff
                    }
                }
            }

            throw;
        }
    }

    public async Task ProcessLiveSyncJobAsync(Guid jobId)
    {
        _logger.LogInformation("Processing live sync job {JobId}", jobId);

        var job = await _syncJobRepo.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Live sync job {JobId} not found", jobId);
            return;
        }

        if (job.Status != SyncJobStatus.Pending)
        {
            _logger.LogWarning("Live sync job {JobId} is not in Pending status, current status: {Status}",
                jobId, job.Status);
            return;
        }

        // Note: LiveSyncService internal methods will handle status updates (Pending -> Running -> Completed/PartiallyCompleted/Failed)
        try
        {
            // Only Rounds are supported for live sync
            if (job.EntityType != SyncEntityType.Rounds)
            {
                throw new InvalidOperationException($"Unsupported entity type for live sync: {job.EntityType}");
            }

            // Use internal method to avoid duplicate job creation
            await _liveSyncService.LiveSyncRoundsInternalAsync(
                job.Id,
                job.ProviderId,
                job.LeagueIds,
                false);

            // Internal methods handle status updates, so we just verify completion
            job = await _syncJobRepo.GetByIdAsync(jobId);
            if (job != null && job.Status != SyncJobStatus.Completed && job.Status != SyncJobStatus.PartiallyCompleted && job.Status != SyncJobStatus.Failed)
            {
                // If for some reason the job is still in Running status, mark as completed
                job.Status = SyncJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                await _syncJobRepo.UpdateAsync(job);
            }

            _logger.LogInformation("Live sync job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Live sync job {JobId} failed", jobId);

            job.Status = SyncJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;

            // Resilient status update with retry logic to prevent jobs stuck in Running status
            bool statusUpdated = false;
            for (int attempt = 1; attempt <= 3 && !statusUpdated; attempt++)
            {
                try
                {
                    await _syncJobRepo.UpdateAsync(job);
                    statusUpdated = true;
                    _logger.LogInformation("Successfully updated job {JobId} to Failed status", job.Id);
                }
                catch (Exception updateEx)
                {
                    if (attempt == 3)
                    {
                        _logger.LogCritical(updateEx,
                            "CRITICAL: Failed to update job {JobId} to Failed status after 3 attempts. " +
                            "Job will remain in Running status. Original error: {OriginalError}",
                            job.Id, ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning(updateEx,
                            "Attempt {Attempt}/3 to update job status failed, retrying...", attempt);
                        await Task.Delay(500 * attempt); // Exponential backoff
                    }
                }
            }

            throw;
        }
    }

    public async Task<SyncJob?> GetJobStatusAsync(Guid jobId)
    {
        return await _syncJobRepo.GetByIdAsync(jobId);
    }

    public async Task<List<SyncJob>> GetRecentJobsAsync(Guid providerId, int count = 20)
    {
        return await _syncJobRepo.GetRecentJobsAsync(providerId, count);
    }

    public async Task<bool> CancelJobAsync(Guid jobId)
    {
        _logger.LogInformation("Attempting to cancel job {JobId}", jobId);

        var job = await _syncJobRepo.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return false;
        }

        // Only running or pending jobs can be cancelled
        if (job.Status != SyncJobStatus.Running && job.Status != SyncJobStatus.Pending)
        {
            _logger.LogWarning("Job {JobId} cannot be cancelled. Current status: {Status}",
                jobId, job.Status);
            return false;
        }

        // Update job status to Failed with cancellation message
        job.Status = SyncJobStatus.Failed;
        job.CompletedAt = DateTime.UtcNow;
        job.ErrorMessage = "Job cancelled by user";
        await _syncJobRepo.UpdateAsync(job);

        _logger.LogInformation("Job {JobId} cancelled successfully", jobId);
        return true;
    }
}
