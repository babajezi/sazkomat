using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sazkomat.Core.Entities;
using StackExchange.Redis;

namespace Sazkomat.Configuration.Services;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisService> _logger;
    private readonly TimeSpan _defaultTtl;

    public RedisService(
        IConnectionMultiplexer redis,
        ILogger<RedisService> logger,
        IConfiguration configuration)
    {
        _redis = redis;
        _logger = logger;

        // Get TTL from configuration (default 7 days)
        var cacheDurationDays = configuration.GetValue<int?>("ProviderLogo:CacheDurationDays") ?? 7;
        _defaultTtl = TimeSpan.FromDays(cacheDurationDays);
    }

    public async Task SetProviderLogoAsync(Guid providerId, LogoSize size, byte[] imageData, TimeSpan? ttl = null)
    {
        if (!IsConnected())
        {
            _logger.LogWarning("Redis is not connected, skipping cache set for provider {ProviderId} size {Size}", providerId, size);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = GetCacheKey(providerId, size);
            var expiry = ttl ?? _defaultTtl;

            await db.StringSetAsync(key, imageData, expiry);

            _logger.LogDebug(
                "Cached provider logo: {ProviderId}, size: {Size}, bytes: {ByteCount}, TTL: {TTL}",
                providerId,
                size,
                imageData.Length,
                expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache provider logo for {ProviderId}, size {Size}", providerId, size);
            // Don't throw - caching is optional, continue without it
        }
    }

    public async Task<byte[]?> GetProviderLogoAsync(Guid providerId, LogoSize size)
    {
        if (!IsConnected())
        {
            _logger.LogWarning("Redis is not connected, cache miss for provider {ProviderId} size {Size}", providerId, size);
            return null;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = GetCacheKey(providerId, size);

            var cached = await db.StringGetAsync(key);

            if (cached.HasValue)
            {
                _logger.LogDebug(
                    "Cache hit for provider logo: {ProviderId}, size: {Size}, bytes: {ByteCount}",
                    providerId,
                    size,
                    ((byte[])cached!).Length);

                return (byte[])cached!;
            }

            _logger.LogDebug("Cache miss for provider logo: {ProviderId}, size: {Size}", providerId, size);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cached provider logo for {ProviderId}, size {Size}", providerId, size);
            return null; // Treat errors as cache miss
        }
    }

    public async Task InvalidateProviderLogosAsync(Guid providerId)
    {
        if (!IsConnected())
        {
            _logger.LogWarning("Redis is not connected, skipping cache invalidation for provider {ProviderId}", providerId);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();

            // Delete all 3 sizes
            var tasks = new[]
            {
                db.KeyDeleteAsync(GetCacheKey(providerId, LogoSize.Small)),
                db.KeyDeleteAsync(GetCacheKey(providerId, LogoSize.Medium)),
                db.KeyDeleteAsync(GetCacheKey(providerId, LogoSize.Large))
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation("Invalidated all cached logos for provider {ProviderId}", providerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cached logos for provider {ProviderId}", providerId);
            // Don't throw - cache invalidation failure shouldn't block operations
        }
    }

    public bool IsConnected()
    {
        try
        {
            return _redis.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    private static string GetCacheKey(Guid providerId, LogoSize size)
    {
        var sizeStr = size switch
        {
            LogoSize.Small => "sm",
            LogoSize.Medium => "md",
            LogoSize.Large => "lg",
            _ => "md"
        };

        return $"provider:logo:{providerId}:{sizeStr}";
    }
}
