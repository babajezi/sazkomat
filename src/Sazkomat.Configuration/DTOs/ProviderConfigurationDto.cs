namespace Sazkomat.Configuration.DTOs;

/// <summary>
/// Type-safe model for deserializing provider Configuration JSONB column
/// </summary>
public class ProviderConfigurationDto
{
    /// <summary>
    /// Request timeout in milliseconds
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    /// Proxy URL for scraping requests
    /// </summary>
    public string? ProxyUrl { get; set; }

    /// <summary>
    /// List of Betano country/region IDs to exclude from country scan
    /// Example: ["188486", "123456"] for special sections like "Kluby UEFA", "FIFA", etc.
    /// </summary>
    public List<string>? ExcludedCountryIds { get; set; }

    /// <summary>
    /// List of Betano league IDs to exclude from league scan
    /// Example: ["789012", "456789"]
    /// </summary>
    public List<string>? ExcludedLeagueIds { get; set; }

    /// <summary>
    /// Provider-specific custom settings
    /// </summary>
    public Dictionary<string, string>? CustomSettings { get; set; }
}
