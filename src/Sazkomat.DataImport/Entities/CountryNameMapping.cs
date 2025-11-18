using Sazkomat.Core.Entities;

namespace Sazkomat.DataImport.Entities;

/// <summary>
/// Maps provider-specific country names/codes to standardized BetExplorer country codes.
/// Used for resolving country naming differences between providers (e.g., Betano "czechia" → BetExplorer "czech-republic").
/// </summary>
public class CountryNameMapping : Entity
{
    /// <summary>
    /// Provider code (e.g., "betano", "fortuna") - lowercase
    /// </summary>
    public string ProviderCode { get; set; } = string.Empty;

    /// <summary>
    /// Country name or code as it appears in the provider's system
    /// Examples: "czechia", "Česko", "CAF"
    /// </summary>
    public string ProviderCountryName { get; set; } = string.Empty;

    /// <summary>
    /// Standardized BetExplorer country code to map to
    /// Examples: "czech-republic", "africa"
    /// </summary>
    public string BetExplorerCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether this mapping is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority for this mapping (lower number = higher priority)
    /// Used when multiple mappings exist for the same provider+country combination
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Optional notes about this mapping
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When this mapping was last used during import
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// How many times this mapping has been used
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// ID of the last ProviderCountry that used this mapping
    /// </summary>
    public Guid? LastProviderCountryId { get; set; }
}
