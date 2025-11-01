namespace Sazkomat.DataImport.DTOs;

public record HistoricalImportRequest(
    List<Guid> LeagueIds,
    List<string>? Seasons,                  // Nullable - not required when ImportAllHistorical is true
    bool IncludeWithoutOdds = true,
    bool ImportAllHistorical = false        // Import all historical seasons (except current)
);
