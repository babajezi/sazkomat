using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Services;

/// <summary>
/// Service for Redis caching operations
/// </summary>
public interface IRedisService
{
    /// <summary>
    /// Sets provider logo in cache
    /// </summary>
    Task SetProviderLogoAsync(Guid providerId, LogoSize size, byte[] imageData, TimeSpan? ttl = null);

    /// <summary>
    /// Gets provider logo from cache
    /// Returns null if not found
    /// </summary>
    Task<byte[]?> GetProviderLogoAsync(Guid providerId, LogoSize size);

    /// <summary>
    /// Invalidates all cached logos for a provider (all 3 sizes)
    /// </summary>
    Task InvalidateProviderLogosAsync(Guid providerId);

    /// <summary>
    /// Checks if Redis connection is available
    /// </summary>
    bool IsConnected();
}
