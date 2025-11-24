namespace Sazkomat.Configuration.DTOs;

public record CreateLeagueRequest(
    Guid SportId,
    Guid CountryId,
    string Name,
    string? NameCs = null,
    string BetExplorerSlug = "",
    bool IsBettable = true,
    int Priority = 5,
    string? Notes = null
);
