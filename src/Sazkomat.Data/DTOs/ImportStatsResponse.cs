namespace Sazkomat.Data.DTOs;

public record ImportStatsResponse(
    int TotalRounds,
    int TotalSeasons,
    string? OldestSeason,
    string? NewestSeason,
    Dictionary<string, int> RoundsBySeason
);
