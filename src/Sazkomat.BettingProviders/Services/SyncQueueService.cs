using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Sazkomat.BettingProviders.Services;

/// <summary>
/// Redis-based queue service for managing betting provider sync operations
/// Prevents concurrent syncs and provides status tracking
/// </summary>
public class SyncQueueService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SyncQueueService> _logger;
    private const int LockExpirySeconds = 300; // 5 minutes

    public SyncQueueService(
        IConnectionMultiplexer redis,
        ILogger<SyncQueueService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Try to acquire a lock for syncing a specific provider/sport combination
    /// </summary>
    public async Task<bool> TryAcquireLockAsync(string providerCode, string sportCode)
    {
        try
        {
            var db = _redis.GetDatabase();
            var lockKey = GetLockKey(providerCode, sportCode);

            // Try to set the lock with expiry
            var acquired = await db.StringSetAsync(
                lockKey,
                DateTimeOffset.UtcNow.ToString("O"),
                TimeSpan.FromSeconds(LockExpirySeconds),
                When.NotExists
            );

            if (acquired)
            {
                _logger.LogInformation("Acquired sync lock for {Provider}/{Sport}", providerCode, sportCode);

                // Set sync status
                await SetSyncStatusAsync(providerCode, sportCode, "running");
            }
            else
            {
                _logger.LogWarning("Failed to acquire sync lock for {Provider}/{Sport} - already running", providerCode, sportCode);
            }

            return acquired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring sync lock for {Provider}/{Sport}", providerCode, sportCode);
            // If Redis is unavailable, allow the operation (fail open)
            return true;
        }
    }

    /// <summary>
    /// Release the sync lock
    /// </summary>
    public async Task ReleaseLockAsync(string providerCode, string sportCode)
    {
        try
        {
            var db = _redis.GetDatabase();
            var lockKey = GetLockKey(providerCode, sportCode);

            await db.KeyDeleteAsync(lockKey);
            _logger.LogInformation("Released sync lock for {Provider}/{Sport}", providerCode, sportCode);

            // Clear sync status
            await SetSyncStatusAsync(providerCode, sportCode, "idle");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing sync lock for {Provider}/{Sport}", providerCode, sportCode);
        }
    }

    /// <summary>
    /// Get current sync status for a provider
    /// </summary>
    public async Task<string> GetSyncStatusAsync(string providerCode, string? sportCode = null)
    {
        try
        {
            var db = _redis.GetDatabase();
            var statusKey = GetStatusKey(providerCode, sportCode);

            var status = await db.StringGetAsync(statusKey);
            return status.HasValue ? status.ToString() : "idle";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status for {Provider}", providerCode);
            return "unknown";
        }
    }

    /// <summary>
    /// Set sync status (running, idle, error)
    /// </summary>
    private async Task SetSyncStatusAsync(string providerCode, string sportCode, string status)
    {
        try
        {
            var db = _redis.GetDatabase();
            var statusKey = GetStatusKey(providerCode, sportCode);

            await db.StringSetAsync(
                statusKey,
                status,
                TimeSpan.FromSeconds(LockExpirySeconds + 60) // Keep status slightly longer than lock
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting sync status for {Provider}/{Sport}", providerCode, sportCode);
        }
    }

    private string GetLockKey(string providerCode, string sportCode)
        => $"sync:lock:{providerCode}:{sportCode}";

    private string GetStatusKey(string providerCode, string? sportCode)
        => sportCode != null
            ? $"sync:status:{providerCode}:{sportCode}"
            : $"sync:status:{providerCode}";
}
