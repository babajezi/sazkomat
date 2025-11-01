namespace Sazkomat.Configuration.DTOs;

public record UpdateLeagueProviderRequest(
    string? ProviderSlug = null,
    string? ProviderName = null,
    bool? IsActive = null
);
