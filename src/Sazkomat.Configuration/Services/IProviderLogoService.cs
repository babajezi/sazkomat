using Microsoft.AspNetCore.Http;
using Sazkomat.Core.Common;
using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Services;

/// <summary>
/// Service for managing provider logo uploads, processing, and retrieval
/// </summary>
public interface IProviderLogoService
{
    /// <summary>
    /// Uploads and processes a provider logo
    /// - Validates file (max 5MB, jpg/png/svg)
    /// - Generates 3 sizes (64px, 128px, 256px)
    /// - Converts to WebP format
    /// - Saves to disk
    /// - Caches in Redis
    /// - Updates provider entity
    /// </summary>
    Task<Result> UploadAndProcessLogoAsync(Guid providerId, IFormFile file);

    /// <summary>
    /// Retrieves a logo in the specified size
    /// - Checks Redis cache first
    /// - Falls back to disk if not cached
    /// - Returns null if logo doesn't exist
    /// </summary>
    Task<Result<byte[]?>> GetLogoAsync(Guid providerId, LogoSize size);

    /// <summary>
    /// Deletes a provider logo
    /// - Removes files from disk (all 3 sizes)
    /// - Invalidates Redis cache
    /// - Updates provider entity
    /// </summary>
    Task<Result> DeleteLogoAsync(Guid providerId);

    /// <summary>
    /// Gets the file path for a specific logo size
    /// </summary>
    string GetLogoPath(Guid providerId, LogoSize size);

    /// <summary>
    /// Gets the file path for an SVG logo
    /// </summary>
    string GetSvgLogoPath(Guid providerId);

    /// <summary>
    /// Gets the file name for a specific logo size (sm.webp, md.webp, lg.webp)
    /// </summary>
    string GetLogoFileName(LogoSize size);
}
