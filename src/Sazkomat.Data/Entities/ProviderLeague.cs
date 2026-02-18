using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;

namespace Sazkomat.Data.Entities;

/// <summary>
/// Cache of league data scanned from a provider.
/// Used to preview available leagues before importing them into the configuration schema.
/// </summary>
public class ProviderLeague : Entity
{
    public Guid ProviderId { get; set; }
    public Guid? ProviderCountryId { get; set; }  // Nullable - betting providers don't have ProviderCountry, they use configuration countries
    public string? CountryCode { get; set; }  // ISO country code (for betting providers) - used during import to link to configuration.countries
    public string ProviderSlug { get; set; } = string.Empty;  // Provider's URL slug (e.g., "premier-league")
    public string ProviderName { get; set; } = string.Empty;  // Provider's display name
    public string? DisplayName { get; set; }
    public int Priority { get; set; } = 5;
    public bool IsBettable { get; set; } = true;
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public string? RawData { get; set; }  // JSONB - full provider response

    // Mapping tracking
    public MappingStatus MappingStatus { get; set; } = MappingStatus.Unmapped;

    // Import tracking
    public bool IsImported { get; set; } = false;
    public Guid? LeagueId { get; set; }  // FK after import
    public DateTime? ImportedAt { get; set; }

    // Navigation properties
    public DataProvider Provider { get; set; } = null!;
    public ProviderCountry ProviderCountry { get; set; } = null!;
    public League? League { get; set; }
    public ICollection<ProviderSeason> ProviderSeasons { get; set; } = new List<ProviderSeason>();
}
