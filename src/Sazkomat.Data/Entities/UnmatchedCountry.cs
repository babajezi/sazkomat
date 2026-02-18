using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;

namespace Sazkomat.Data.Entities;

/// <summary>
/// Tracks countries from betting providers that could not be automatically matched to BetExplorer.
/// Used as a queue for manual review and mapping.
/// </summary>
public class UnmatchedCountry : Entity
{
    /// <summary>
    /// The betting provider that reported this country (Betano, Fortuna, etc.)
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Provider's internal country ID (if available)
    /// </summary>
    public string? ProviderCountryId { get; set; }

    /// <summary>
    /// Country name as reported by the provider (e.g., "Anglie", "Německo")
    /// </summary>
    public string ProviderCountryName { get; set; } = string.Empty;

    /// <summary>
    /// Provider's URL slug for the country
    /// </summary>
    public string? ProviderSlug { get; set; }

    /// <summary>
    /// When this country was scraped from the provider
    /// </summary>
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

    // Resolution tracking

    /// <summary>
    /// Whether this unmatched country has been resolved (either mapped or ignored)
    /// </summary>
    public bool IsResolved { get; set; } = false;

    /// <summary>
    /// How this country was resolved (Mapped to existing country, Ignored, or Unavailable)
    /// </summary>
    public ResolutionType? ResolutionType { get; set; }

    /// <summary>
    /// The country this was resolved to (if ResolutionType == Mapped)
    /// </summary>
    public Guid? ResolvedCountryId { get; set; }

    /// <summary>
    /// When this country was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Optional notes about the resolution (e.g., why it was ignored)
    /// </summary>
    public string? ResolutionNotes { get; set; }

    // Navigation properties
    public DataProvider Provider { get; set; } = null!;
    public Country? ResolvedCountry { get; set; }
}
