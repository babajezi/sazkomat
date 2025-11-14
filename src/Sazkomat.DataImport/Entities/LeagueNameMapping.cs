using Sazkomat.Core.Entities;

namespace Sazkomat.DataImport.Entities;

/// <summary>
/// Manual league name mapping for enrichment process.
/// Maps provider-specific league names to BetExplorer slugs when automatic matching fails.
/// </summary>
public class LeagueNameMapping : Entity
{
    /// <summary>
    /// Provider code (betano, fortuna, etc.)
    /// </summary>
    public string ProviderCode { get; set; } = string.Empty;

    /// <summary>
    /// Country ISO code (cz, sk, gb, etc.)
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// League name as it appears in the provider's system
    /// </summary>
    public string ProviderLeagueName { get; set; } = string.Empty;

    /// <summary>
    /// Corresponding BetExplorer league slug
    /// </summary>
    public string BetExplorerSlug { get; set; } = string.Empty;

    /// <summary>
    /// Whether this mapping is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional notes about this mapping
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Priority for this mapping (lower = higher priority)
    /// Useful when multiple mappings could match
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// When this mapping was last used successfully in enrichment
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Total number of times this mapping has been used
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// ID of the last provider league that used this mapping
    /// </summary>
    public Guid? LastProviderLeagueId { get; set; }
}
