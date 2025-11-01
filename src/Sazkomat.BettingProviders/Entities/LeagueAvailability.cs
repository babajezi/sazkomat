namespace Sazkomat.BettingProviders.Entities;

/// <summary>
/// Represents a league available for betting on a betting provider
/// </summary>
public class LeagueAvailability
{
    public string ProviderLeagueName { get; set; } = string.Empty;
    public string ProviderLeagueId { get; set; } = string.Empty;
    public string ProviderUrl { get; set; } = string.Empty;
    public string SportCode { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
}
