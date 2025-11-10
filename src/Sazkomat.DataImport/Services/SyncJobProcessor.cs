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

        // Update status to Running
        job.Status = SyncJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await _syncJobRepo.UpdateAsync(job);

        try
        {
            // Process based on entity type
            switch (job.EntityType)
            {
                case SyncEntityType.Countries:
                    await _scanService.ScanCountriesAsync(job.ProviderId);
                    break;

                case SyncEntityType.Leagues:
                    if (job.CountryIds != null && job.CountryIds.Any())
                    {
                        await _scanService.ScanLeaguesAsync(job.ProviderId, job.CountryIds);
                    }
                    else
                    {
                        _logger.LogWarning("Scan job {JobId} for leagues has no country IDs", jobId);
                    }
                    break;

                case SyncEntityType.Seasons:
                    if (job.LeagueIds != null && job.LeagueIds.Any())
                    {
                        await _scanService.ScanSeasonsAsync(job.ProviderId, job.LeagueIds);
                    }
                    else
                    {
                        _logger.LogWarning("Scan job {JobId} for seasons has no league IDs", jobId);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported entity type for scan: {job.EntityType}");
            }

            // Update status to Completed
            job.Status = SyncJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await _syncJobRepo.UpdateAsync(job);

            _logger.LogInformation("Scan job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan job {JobId} failed", jobId);

            job.Status = SyncJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;
            await _syncJobRepo.UpdateAsync(job);

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

        // Update status to Running
        job.Status = SyncJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await _syncJobRepo.UpdateAsync(job);

        try
        {
            // Process based on entity type
            switch (job.EntityType)
            {
                case SyncEntityType.Countries:
                    if (job.CountryIds != null && job.CountryIds.Any())
                    {
                        await _importService.ImportCountriesAsync(job.ProviderId, job.CountryIds);
                    }
                    break;

                case SyncEntityType.Leagues:
                    if (job.LeagueIds != null && job.LeagueIds.Any())
                    {
                        await _importService.ImportLeaguesAsync(job.ProviderId, job.LeagueIds);
                    }
                    break;

                case SyncEntityType.Seasons:
                    if (job.SeasonIds != null && job.SeasonIds.Any())
                    {
                        await _importService.ImportSeasonsAsync(job.ProviderId, job.SeasonIds);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported entity type for import: {job.EntityType}");
            }

            // Update status to Completed
            job.Status = SyncJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await _syncJobRepo.UpdateAsync(job);

            _logger.LogInformation("Import job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import job {JobId} failed", jobId);

            job.Status = SyncJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;
            await _syncJobRepo.UpdateAsync(job);

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

        // Update status to Running
        job.Status = SyncJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await _syncJobRepo.UpdateAsync(job);

        try
        {
            // Only Rounds are supported for live sync
            if (job.EntityType != SyncEntityType.Rounds)
            {
                throw new InvalidOperationException($"Unsupported entity type for live sync: {job.EntityType}");
            }

            // LiveSyncService creates its own job, so we just invoke it
            // and it will handle the job lifecycle
            await _liveSyncService.LiveSyncRoundsAsync(
                job.ProviderId,
                job.LeagueIds,
                forceRefresh: false);

            // The LiveSyncService already created a job, so we mark this one as completed
            job.Status = SyncJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await _syncJobRepo.UpdateAsync(job);

            _logger.LogInformation("Live sync job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Live sync job {JobId} failed", jobId);

            job.Status = SyncJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;
            await _syncJobRepo.UpdateAsync(job);

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
}
