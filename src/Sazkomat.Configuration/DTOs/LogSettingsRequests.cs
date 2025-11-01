namespace Sazkomat.Configuration.DTOs;

public record CreateLogSettingsRequest(
    string Category,
    string SubCategory,
    string LogPath,
    string LogLevel = "Information",
    bool IsEnabled = true,
    int RetentionDays = 30,
    long MaxFileSizeBytes = 104857600,
    string? OutputTemplate = null,
    string? Description = null);

public record UpdateLogSettingsRequest(
    string? LogPath = null,
    string? LogLevel = null,
    bool? IsEnabled = null,
    int? RetentionDays = null,
    long? MaxFileSizeBytes = null,
    string? OutputTemplate = null,
    string? Description = null);
