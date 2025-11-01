namespace Sazkomat.DataImport.DTOs;

public record AvailableSeasonsResponse(
    Guid LeagueId,
    string LeagueName,
    List<string> Seasons,
    string? CurrentSeason,
    List<string> HistoricalSeasons
);
