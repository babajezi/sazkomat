using Sazkomat.Configuration.DTOs;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

/// <summary>
/// Service for universal import/export of configuration entities
/// </summary>
public interface IUniversalImportExportService
{
    /// <summary>
    /// Exports selected configuration entities to JSON format
    /// </summary>
    Task<Result<ConfigurationExportDto>> ExportAsync(ExportOptionsDto options);

    /// <summary>
    /// Gets preview information about what would be exported
    /// </summary>
    Task<Result<ExportMetadataDto>> GetExportPreviewAsync(ExportOptionsDto options);

    /// <summary>
    /// Validates import data without making changes
    /// </summary>
    Task<Result<ImportResultDto>> ValidateImportAsync(ConfigurationExportDto data);

    /// <summary>
    /// Imports configuration data with specified options
    /// </summary>
    Task<Result<ImportResultDto>> ImportAsync(ConfigurationExportDto data, ImportOptionsDto options);
}
