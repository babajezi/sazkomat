using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;

namespace Sazkomat.DataImport.Entities;

/// <summary>
/// Cache of season data scanned from a provider.
/// Used to preview available seasons before importing them into the configuration schema.
/// </summary>
public class ProviderSeason : Entity
{
    public Guid ProviderId { get; set; }
    public Guid ProviderLeagueId { get; set; }
    public string SeasonName { get; set; } = string.Empty;  // E.g., "2024-2025", "2024"
    public int StartYear { get; set; }
    public int? EndYear { get; set; }  // Null for single-year seasons
    public bool IsCurrentSeason { get; set; } = false;
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public string? RawData { get; set; }  // JSONB - full provider response

    // Import tracking
    public bool IsImported { get; set; } = false;
    public Guid? SeasonId { get; set; }  // FK after import
    public DateTime? ImportedAt { get; set; }

    // Navigation properties
    public DataProvider Provider { get; set; } = null!;
    public ProviderLeague ProviderLeague { get; set; } = null!;
    public Season? Season { get; set; }
}
