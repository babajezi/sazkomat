using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;

namespace Sazkomat.DataImport.Entities;

/// <summary>
/// Tracks leagues from betting providers that could not be automatically matched to BetExplorer.
/// Used as a queue for manual review and mapping.
/// </summary>
public class UnmatchedLeague : Entity
{
    /// <summary>
    /// The betting provider that reported this league (Betano, Fortuna, etc.)
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Provider's internal league ID (if available)
    /// </summary>
    public string? ProviderLeagueId { get; set; }

    /// <summary>
    /// League name as reported by the provider
    /// </summary>
    public string ProviderLeagueName { get; set; } = string.Empty;

    /// <summary>
    /// Provider's URL slug for the league
    /// </summary>
    public string? ProviderSlug { get; set; }

    /// <summary>
    /// Country code (ISO or provider-specific)
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Country name as reported by the provider
    /// </summary>
    public string? CountryName { get; set; }

    /// <summary>
    /// When this league was scraped from the provider
    /// </summary>
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

    // Resolution tracking

    /// <summary>
    /// Whether this unmatched league has been resolved (either mapped or ignored)
    /// </summary>
    public bool IsResolved { get; set; } = false;

    /// <summary>
    /// How this league was resolved (Mapped to existing league, or Ignored)
    /// </summary>
    public ResolutionType? ResolutionType { get; set; }

    /// <summary>
    /// The league this was resolved to (if ResolutionType == Mapped)
    /// </summary>
    public Guid? ResolvedLeagueId { get; set; }

    /// <summary>
    /// When this league was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Optional notes about the resolution (e.g., why it was ignored)
    /// </summary>
    public string? ResolutionNotes { get; set; }

    // Navigation properties
    public DataProvider Provider { get; set; } = null!;
    public League? ResolvedLeague { get; set; }
}

/// <summary>
/// How an unmatched league was resolved
/// </summary>
public enum ResolutionType
{
    /// <summary>
    /// League was manually mapped to an existing BetExplorer league
    /// </summary>
    Mapped = 1,

    /// <summary>
    /// League was deliberately ignored (user decided not to import, e.g., women's league, youth)
    /// </summary>
    Ignored = 2,

    /// <summary>
    /// League is not available in BetExplorer (technically unsupported by the data source)
    /// </summary>
    Unavailable = 3,

    /// <summary>
    /// League was manually mapped via LeagueNameMapping table (legacy compatibility)
    /// </summary>
    ManuallyMapped = 4
}
