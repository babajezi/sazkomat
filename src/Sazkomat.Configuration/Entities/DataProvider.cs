using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class DataProvider : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 10;
    public ProviderType Type { get; set; } = ProviderType.Scraper;
    public string? Notes { get; set; }

    /// <summary>
    /// JSON array of season name patterns that identify current seasons
    /// Examples: ["2025", "2025-2026"] for detecting ongoing seasons
    /// </summary>
    public string CurrentSeasonPatterns { get; set; } = "[]";

    /// <summary>
    /// JSONB storing provider credentials (username, password, sessionCookies)
    /// For betting providers that require authentication
    /// </summary>
    public string? Credentials { get; set; }

    /// <summary>
    /// JSONB storing provider-specific configuration (timeout, proxy, custom settings)
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Indicates if the provider has an uploaded logo
    /// Logo files are stored in /uploads/provider-logos/{Id}/ as sm.webp, md.webp, lg.webp
    /// </summary>
    public bool HasLogo { get; set; } = false;

    /// <summary>
    /// Timestamp when the logo was last uploaded
    /// </summary>
    public DateTime? LogoUploadedAt { get; set; }

    // Navigation properties
    public ICollection<CountryProvider> CountryProviders { get; set; } = new List<CountryProvider>();
    public ICollection<LeagueProvider> LeagueProviders { get; set; } = new List<LeagueProvider>();
    public ICollection<SportProvider> SportProviders { get; set; } = new List<SportProvider>();
}
