using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;

namespace Sazkomat.Data.Entities;

/// <summary>
/// Cache of country data scanned from a provider.
/// Used to preview available countries before importing them into the configuration schema.
/// </summary>
public class ProviderCountry : Entity
{
    public Guid ProviderId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;  // Provider's internal code (e.g., "england", "spain")
    public string ProviderName { get; set; } = string.Empty;  // Provider's display name (e.g., "England", "Spain")
    public string? IsoCode { get; set; }
    public string? FlagEmoji { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public string? RawData { get; set; }  // JSONB - full provider response for audit/debugging

    // Import tracking
    public bool IsImported { get; set; } = false;
    public Guid? CountryId { get; set; }  // FK after import
    public DateTime? ImportedAt { get; set; }

    // Navigation properties
    public DataProvider Provider { get; set; } = null!;
    public Country? Country { get; set; }
    public ICollection<ProviderLeague> ProviderLeagues { get; set; } = new List<ProviderLeague>();
}
