using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sazkomat.Data.Services;

namespace Sazkomat.Api.Services;

public class RecurringSyncScheduler : IHostedService
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecurringSyncScheduler> _logger;

    public RecurringSyncScheduler(
        IRecurringJobManager recurringJobManager,
        IConfiguration configuration,
        ILogger<RecurringSyncScheduler> logger)
    {
        _recurringJobManager = recurringJobManager;
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Recurring Sync Scheduler");

        try
        {
            // Optional: Schedule recurring live sync job
            // This can be enabled/disabled via configuration
            var enableLiveSync = _configuration.GetValue<bool>("Hangfire:EnableRecurringLiveSync", false);

            // Schedule current season detection job - runs daily at 6:00 UTC
            // This automatically updates SyncMode based on:
            // - Current year for single-year seasons (2026 = Current in year 2026)
            // - Next season having data for split seasons (2025-2026 = Historical if 2026-2027 has data)
            _recurringJobManager.AddOrUpdate<ISeasonSyncService>(
                "detect-current-seasons",
                service => service.DetectAndMarkCurrentSeasonsAsync(
                    Guid.Parse("a0000000-0000-0000-0000-000000000001")), // BetExplorer provider
                Cron.Daily(6, 0), // Every day at 6:00 UTC
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            _logger.LogInformation("Scheduled recurring current season detection job (daily at 6:00 UTC)");

            if (enableLiveSync)
            {
                var cronExpression = _configuration.GetValue<string>("Hangfire:LiveSyncCronExpression")
                    ?? Cron.Hourly(); // Default to hourly

                _logger.LogInformation("Scheduling recurring live sync job with cron: {Cron}", cronExpression);

                // Schedule a recurring job for live sync
                // This will sync all active leagues every hour (or based on cron expression)
                _recurringJobManager.AddOrUpdate<ILiveSyncService>(
                    "recurring-live-sync",
                    service => service.LiveSyncRoundsAsync(
                        Guid.Parse("a0000000-0000-0000-0000-000000000001"), // Default provider
                        null, // All leagues
                        false), // Don't force refresh
                    cronExpression,
                    new RecurringJobOptions
                    {
                        TimeZone = TimeZoneInfo.Utc
                    });

                _logger.LogInformation("Recurring live sync job scheduled successfully");
            }
            else
            {
                _logger.LogInformation("Recurring live sync is disabled in configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule recurring jobs");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Recurring Sync Scheduler");

        try
        {
            // Optional: Remove recurring jobs when stopping
            // _recurringJobManager.RemoveIfExists("recurring-live-sync");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping Recurring Sync Scheduler");
        }

        return Task.CompletedTask;
    }
}
