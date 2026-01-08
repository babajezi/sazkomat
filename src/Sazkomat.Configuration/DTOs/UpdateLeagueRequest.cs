namespace Sazkomat.Configuration.DTOs;

public record UpdateLeagueRequest(
    string? Name = null,
    string? NameCs = null,
    string? BetExplorerSlug = null,
    bool? IsBettable = null,
    bool? IsActive = null,
    int? Priority = null,
    string? Notes = null
);
