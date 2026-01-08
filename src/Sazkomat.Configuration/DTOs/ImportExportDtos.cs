using System.Text.Json.Serialization;

namespace Sazkomat.Configuration.DTOs;

#region Root Export/Import DTOs

/// <summary>
/// Root DTO for configuration export containing all selected entities
/// </summary>
public class ConfigurationExportDto
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0.0";

    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAt { get; set; }

    [JsonPropertyName("exportedBy")]
    public string? ExportedBy { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("metadata")]
    public ExportMetadataDto Metadata { get; set; } = new();

    // Core entities (nullable - included only if selected)
    [JsonPropertyName("sports")]
    public List<SportExportDto>? Sports { get; set; }

    [JsonPropertyName("countries")]
    public List<CountryExportDto>? Countries { get; set; }

    [JsonPropertyName("providers")]
    public List<DataProviderExportDto>? Providers { get; set; }

    [JsonPropertyName("seasons")]
    public List<SeasonExportDto>? Seasons { get; set; }

    // Dependent entities
    [JsonPropertyName("leagues")]
    public List<LeagueExportDto>? Leagues { get; set; }

    // Junction tables
    [JsonPropertyName("sportProviders")]
    public List<SportProviderExportDto>? SportProviders { get; set; }

    [JsonPropertyName("countryProviders")]
    public List<CountryProviderExportDto>? CountryProviders { get; set; }

    [JsonPropertyName("leagueProviders")]
    public List<LeagueProviderExportDto>? LeagueProviders { get; set; }

    [JsonPropertyName("leagueSeasons")]
    public List<LeagueSeasonExportDto>? LeagueSeasons { get; set; }
}

/// <summary>
/// Metadata about the export
/// </summary>
public class ExportMetadataDto
{
    [JsonPropertyName("totalEntities")]
    public int TotalEntities { get; set; }

    [JsonPropertyName("includedTypes")]
    public List<string> IncludedTypes { get; set; } = new();
}

#endregion

#region Options DTOs

/// <summary>
/// Options for exporting configuration
/// </summary>
public class ExportOptionsDto
{
    public bool IncludeSports { get; set; }
    public bool IncludeCountries { get; set; }
    public bool IncludeProviders { get; set; }
    public bool IncludeSeasons { get; set; }
    public bool IncludeLeagues { get; set; }
    public bool IncludeSportProviders { get; set; }
    public bool IncludeCountryProviders { get; set; }
    public bool IncludeLeagueProviders { get; set; }
    public bool IncludeLeagueSeasons { get; set; }

    // Filters
    public bool OnlyActive { get; set; }
    public List<Guid>? SportIds { get; set; }
    public List<Guid>? CountryIds { get; set; }
}

/// <summary>
/// Options for importing configuration
/// </summary>
public class ImportOptionsDto
{
    public ImportMode Mode { get; set; } = ImportMode.SmartMatch;
    public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.Update;
}

/// <summary>
/// Import mode strategy
/// </summary>
public enum ImportMode
{
    /// <summary>
    /// Preserve original GUIDs from export (for backup restore)
    /// </summary>
    PreserveIds,

    /// <summary>
    /// Match entities by business key (Code) and remap IDs (for config sharing)
    /// </summary>
    SmartMatch
}

/// <summary>
/// Conflict resolution strategy when entity already exists
/// </summary>
public enum ConflictResolution
{
    /// <summary>
    /// Skip existing entities, only import new ones
    /// </summary>
    Skip,

    /// <summary>
    /// Update existing entities with new data
    /// </summary>
    Update,

    /// <summary>
    /// Throw error if conflict detected
    /// </summary>
    Fail
}

#endregion

#region Result DTOs

/// <summary>
/// Result of import operation
/// </summary>
public class ImportResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();

    public EntityImportResult Sports { get; set; } = new();
    public EntityImportResult Countries { get; set; } = new();
    public EntityImportResult Providers { get; set; } = new();
    public EntityImportResult Seasons { get; set; } = new();
    public EntityImportResult Leagues { get; set; } = new();
    public EntityImportResult SportProviders { get; set; } = new();
    public EntityImportResult CountryProviders { get; set; } = new();
    public EntityImportResult LeagueProviders { get; set; } = new();
    public EntityImportResult LeagueSeasons { get; set; } = new();

    public int TotalCreated => Sports.Created + Countries.Created + Providers.Created +
                                Seasons.Created + Leagues.Created + SportProviders.Created +
                                CountryProviders.Created + LeagueProviders.Created + LeagueSeasons.Created;

    public int TotalUpdated => Sports.Updated + Countries.Updated + Providers.Updated +
                                Seasons.Updated + Leagues.Updated + SportProviders.Updated +
                                CountryProviders.Updated + LeagueProviders.Updated + LeagueSeasons.Updated;

    public int TotalSkipped => Sports.Skipped + Countries.Skipped + Providers.Skipped +
                                Seasons.Skipped + Leagues.Skipped + SportProviders.Skipped +
                                CountryProviders.Skipped + LeagueProviders.Skipped + LeagueSeasons.Skipped;
}

/// <summary>
/// Result of importing a single entity type
/// </summary>
public class EntityImportResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

#endregion

#region Entity Export DTOs

/// <summary>
/// Sport export DTO
/// </summary>
public class SportExportDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 10;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Country export DTO
/// </summary>
public class CountryExportDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("flagEmoji")]
    public string? FlagEmoji { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DataProvider export DTO
/// </summary>
public class DataProviderExportDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("currentSeasonPatterns")]
    public string? CurrentSeasonPatterns { get; set; }

    [JsonPropertyName("credentials")]
    public string? Credentials { get; set; }

    [JsonPropertyName("configuration")]
    public string? Configuration { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Season export DTO
/// </summary>
public class SeasonExportDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("startYear")]
    public int StartYear { get; set; }

    [JsonPropertyName("endYear")]
    public int? EndYear { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// League export DTO
/// </summary>
public class LeagueExportDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("sportId")]
    public Guid SportId { get; set; }

    [JsonPropertyName("countryId")]
    public Guid CountryId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("betExplorerSlug")]
    public string? BetExplorerSlug { get; set; }

    [JsonPropertyName("isBettable")]
    public bool IsBettable { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// SportProvider export DTO
/// </summary>
public class SportProviderExportDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("sportId")]
    public Guid SportId { get; set; }

    [JsonPropertyName("providerId")]
    public Guid ProviderId { get; set; }

    [JsonPropertyName("providerCode")]
    public string ProviderCode { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// CountryProvider export DTO
/// </summary>
public class CountryProviderExportDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("countryId")]
    public Guid CountryId { get; set; }

    [JsonPropertyName("providerId")]
    public Guid ProviderId { get; set; }

    [JsonPropertyName("providerCode")]
    public string ProviderCode { get; set; } = string.Empty;

    [JsonPropertyName("providerName")]
    public string ProviderName { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// LeagueProvider export DTO
/// </summary>
public class LeagueProviderExportDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("leagueId")]
    public Guid LeagueId { get; set; }

    [JsonPropertyName("providerId")]
    public Guid ProviderId { get; set; }

    [JsonPropertyName("providerSlug")]
    public string ProviderSlug { get; set; } = string.Empty;

    [JsonPropertyName("providerName")]
    public string ProviderName { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("providerLeagueId")]
    public string? ProviderLeagueId { get; set; }

    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// LeagueSeason export DTO
/// </summary>
public class LeagueSeasonExportDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("leagueId")]
    public Guid LeagueId { get; set; }

    [JsonPropertyName("seasonId")]
    public Guid SeasonId { get; set; }

    [JsonPropertyName("isAvailableOnBetExplorer")]
    public bool IsAvailableOnBetExplorer { get; set; }

    [JsonPropertyName("hasData")]
    public bool HasData { get; set; }

    [JsonPropertyName("hasOdds")]
    public bool HasOdds { get; set; }

    [JsonPropertyName("lastScrapedAt")]
    public DateTime? LastScrapedAt { get; set; }

    [JsonPropertyName("roundsCount")]
    public int RoundsCount { get; set; }

    [JsonPropertyName("matchesCount")]
    public int MatchesCount { get; set; }

    [JsonPropertyName("syncEnabled")]
    public bool SyncEnabled { get; set; }

    [JsonPropertyName("isCurrent")]
    public bool IsCurrent { get; set; }

    [JsonPropertyName("syncMode")]
    public string SyncMode { get; set; } = string.Empty;

    [JsonPropertyName("lastDataSyncAt")]
    public DateTime? LastDataSyncAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

#endregion

#region Request DTOs

/// <summary>
/// Request DTO for import operation
/// </summary>
public class ImportRequestDto
{
    public ConfigurationExportDto Data { get; set; } = new();
    public ImportOptionsDto Options { get; set; } = new();
}

#endregion
