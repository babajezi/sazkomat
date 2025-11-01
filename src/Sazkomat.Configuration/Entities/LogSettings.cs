using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

/// <summary>
/// Configuration for logging per provider/service
/// Allows dynamic control of log levels and file paths
/// </summary>
public class LogSettings : Entity
{
    /// <summary>
    /// Category of logging (e.g., "BettingProvider", "Scraper", "Sync", "Import")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Sub-category - typically provider code (e.g., "betano", "betexplorer") or service name
    /// </summary>
    public string SubCategory { get; set; } = string.Empty;

    /// <summary>
    /// Path pattern for log files (e.g., "logs/providers/{provider}/sync-.log")
    /// {provider} will be replaced with SubCategory value
    /// </summary>
    public string LogPath { get; set; } = string.Empty;

    /// <summary>
    /// Minimum log level: Verbose, Debug, Information, Warning, Error, Fatal
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Whether logging is enabled for this category/subcategory
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Number of days to retain log files (0 = infinite)
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Maximum file size in bytes before rolling (0 = no limit)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 104857600; // 100 MB default

    /// <summary>
    /// Custom output template for this logger (null = use default)
    /// </summary>
    public string? OutputTemplate { get; set; }

    /// <summary>
    /// Optional description of what this log configuration is for
    /// </summary>
    public string? Description { get; set; }
}
