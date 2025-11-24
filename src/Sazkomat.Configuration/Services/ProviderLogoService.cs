using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Core.Common;
using Sazkomat.Core.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Sazkomat.Configuration.Services;

public class ProviderLogoService : IProviderLogoService
{
    private readonly IDataProviderRepository _providerRepository;
    private readonly IRedisService _redisService;
    private readonly ILogger<ProviderLogoService> _logger;
    private readonly string _uploadPath;
    private readonly long _maxFileSizeBytes;
    private readonly string[] _allowedExtensions;
    private readonly int _webpQuality;

    public ProviderLogoService(
        IDataProviderRepository providerRepository,
        IRedisService redisService,
        ILogger<ProviderLogoService> logger,
        IConfiguration configuration)
    {
        _providerRepository = providerRepository;
        _redisService = redisService;
        _logger = logger;

        // Load configuration
        var providerLogoConfig = configuration.GetSection("ProviderLogo");
        _uploadPath = providerLogoConfig["UploadPath"] ?? "/uploads/provider-logos";
        _maxFileSizeBytes = (providerLogoConfig.GetValue<int?>("MaxFileSizeMB") ?? 5) * 1024 * 1024;
        _allowedExtensions = providerLogoConfig.GetSection("AllowedExtensions").Get<string[]>()
            ?? new[] { ".jpg", ".jpeg", ".png", ".svg" };
        _webpQuality = providerLogoConfig.GetValue<int?>("WebPQuality") ?? 85;

        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
            _logger.LogInformation("Created provider logos upload directory: {UploadPath}", _uploadPath);
        }
    }

    public async Task<Result> UploadAndProcessLogoAsync(Guid providerId, IFormFile file)
    {
        try
        {
            // Validate provider exists
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result.Failure($"Provider with ID {providerId} not found");
            }

            // Validate file
            var validation = ValidateFile(file);
            if (!validation.IsSuccess)
            {
                return validation;
            }

            // Create provider directory
            var providerDir = Path.Combine(_uploadPath, providerId.ToString());
            if (!Directory.Exists(providerDir))
            {
                Directory.CreateDirectory(providerDir);
            }

            // Delete old logos if exist
            if (provider.HasLogo)
            {
                await DeleteLogoFilesAsync(providerId);
            }

            // Detect SVG files - they don't need processing
            var isSvg = Path.GetExtension(file.FileName).Equals(".svg", StringComparison.OrdinalIgnoreCase);

            if (isSvg)
            {
                // SVG - just copy the original file
                var svgPath = GetSvgLogoPath(providerId);
                using (var stream = file.OpenReadStream())
                {
                    using (var fileStream = File.Create(svgPath))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }

                _logger.LogDebug("Saved SVG logo for provider {ProviderId} at {Path}", providerId, svgPath);
            }
            else
            {
                // Raster image - process and save in 3 sizes
                using (var stream = file.OpenReadStream())
                {
                    using (var image = await Image.LoadAsync(stream))
                    {
                        // Small (64px)
                        await SaveResizedImageAsync(image, providerId, LogoSize.Small);

                        // Medium (128px)
                        await SaveResizedImageAsync(image, providerId, LogoSize.Medium);

                        // Large (256px)
                        await SaveResizedImageAsync(image, providerId, LogoSize.Large);
                    }
                }
            }

            // Cache all 3 sizes in Redis
            await CacheLogoSizesAsync(providerId);

            // Update provider entity
            provider.HasLogo = true;
            provider.LogoUploadedAt = DateTime.UtcNow;
            await _providerRepository.UpdateAsync(provider);

            _logger.LogInformation(
                "Successfully uploaded and processed logo for provider {ProviderId} ({ProviderName})",
                providerId,
                provider.Name);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload and process logo for provider {ProviderId}", providerId);
            return Result.Failure($"Failed to process logo: {ex.Message}");
        }
    }

    public async Task<Result<byte[]?>> GetLogoAsync(Guid providerId, LogoSize size)
    {
        try
        {
            // Check if SVG exists first (SVG is not cached and doesn't have sizes)
            var svgPath = GetSvgLogoPath(providerId);
            if (File.Exists(svgPath))
            {
                var svgBytes = await File.ReadAllBytesAsync(svgPath);
                return Result<byte[]?>.Success(svgBytes);
            }

            // Check Redis cache first for raster images
            var cachedLogo = await _redisService.GetProviderLogoAsync(providerId, size);
            if (cachedLogo != null)
            {
                return Result<byte[]?>.Success(cachedLogo);
            }

            // Cache miss - load from disk
            var logoPath = GetLogoPath(providerId, size);

            if (!File.Exists(logoPath))
            {
                return Result<byte[]?>.Success(null);
            }

            var bytes = await File.ReadAllBytesAsync(logoPath);

            // Cache in Redis for future requests
            await _redisService.SetProviderLogoAsync(providerId, size, bytes);

            return Result<byte[]?>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve logo for provider {ProviderId}, size {Size}",
                providerId,
                size);
            return Result<byte[]?>.Failure($"Failed to retrieve logo: {ex.Message}");
        }
    }

    public async Task<Result> DeleteLogoAsync(Guid providerId)
    {
        try
        {
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result.Failure($"Provider with ID {providerId} not found");
            }

            if (!provider.HasLogo)
            {
                return Result.Failure("Provider does not have a logo");
            }

            // Delete logo files
            await DeleteLogoFilesAsync(providerId);

            // Invalidate Redis cache (all 3 sizes)
            await _redisService.InvalidateProviderLogosAsync(providerId);

            // Update provider entity
            provider.HasLogo = false;
            provider.LogoUploadedAt = null;
            await _providerRepository.UpdateAsync(provider);

            _logger.LogInformation(
                "Successfully deleted logo for provider {ProviderId} ({ProviderName})",
                providerId,
                provider.Name);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete logo for provider {ProviderId}", providerId);
            return Result.Failure($"Failed to delete logo: {ex.Message}");
        }
    }

    public string GetLogoPath(Guid providerId, LogoSize size)
    {
        var providerDir = Path.Combine(_uploadPath, providerId.ToString());
        var fileName = GetLogoFileName(size);
        return Path.Combine(providerDir, fileName);
    }

    public string GetSvgLogoPath(Guid providerId)
    {
        var providerDir = Path.Combine(_uploadPath, providerId.ToString());
        return Path.Combine(providerDir, "logo.svg");
    }

    public string GetLogoFileName(LogoSize size)
    {
        return size switch
        {
            LogoSize.Small => "sm.webp",
            LogoSize.Medium => "md.webp",
            LogoSize.Large => "lg.webp",
            _ => throw new ArgumentException($"Invalid logo size: {size}", nameof(size))
        };
    }

    private Result ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Result.Failure("File is required");
        }

        if (file.Length > _maxFileSizeBytes)
        {
            var maxSizeMB = _maxFileSizeBytes / (1024 * 1024);
            return Result.Failure($"File size exceeds maximum allowed size of {maxSizeMB}MB");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            var allowed = string.Join(", ", _allowedExtensions);
            return Result.Failure($"Invalid file type. Allowed types: {allowed}");
        }

        return Result.Success();
    }

    private async Task SaveResizedImageAsync(Image image, Guid providerId, LogoSize size)
    {
        var targetSize = (int)size;
        var clone = image.Clone(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(targetSize, targetSize),
                Mode = ResizeMode.Max // Preserve aspect ratio, fit within bounds
            });
        });

        var logoPath = GetLogoPath(providerId, size);
        var encoder = new WebpEncoder { Quality = _webpQuality };

        await clone.SaveAsync(logoPath, encoder);

        _logger.LogDebug(
            "Saved {Size}px logo for provider {ProviderId} at {Path}",
            targetSize,
            providerId,
            logoPath);
    }

    private async Task DeleteLogoFilesAsync(Guid providerId)
    {
        // Delete SVG if exists
        var svgPath = GetSvgLogoPath(providerId);
        if (File.Exists(svgPath))
        {
            File.Delete(svgPath);
        }

        // Delete raster images (all sizes)
        foreach (LogoSize size in Enum.GetValues(typeof(LogoSize)))
        {
            var logoPath = GetLogoPath(providerId, size);
            if (File.Exists(logoPath))
            {
                File.Delete(logoPath);
                await Task.CompletedTask; // For async consistency
            }
        }

        // Delete provider directory if empty
        var providerDir = Path.Combine(_uploadPath, providerId.ToString());
        if (Directory.Exists(providerDir) && !Directory.EnumerateFileSystemEntries(providerDir).Any())
        {
            Directory.Delete(providerDir);
        }
    }

    private async Task CacheLogoSizesAsync(Guid providerId)
    {
        try
        {
            // Cache all 3 sizes in Redis
            foreach (LogoSize size in Enum.GetValues(typeof(LogoSize)))
            {
                var logoPath = GetLogoPath(providerId, size);
                if (File.Exists(logoPath))
                {
                    var bytes = await File.ReadAllBytesAsync(logoPath);
                    await _redisService.SetProviderLogoAsync(providerId, size, bytes);
                }
            }

            _logger.LogDebug("Cached all logo sizes for provider {ProviderId} in Redis", providerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache logos in Redis for provider {ProviderId}", providerId);
            // Don't throw - caching failure shouldn't block the operation
        }
    }
}
