namespace Sazkomat.Configuration.DTOs;

public record CreateLeagueProviderRequest(
    Guid LeagueId,
    Guid ProviderId,
    string ProviderSlug,
    string? ProviderName = null,
    bool IsActive = false
);
