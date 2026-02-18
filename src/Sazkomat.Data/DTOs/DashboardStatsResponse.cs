namespace Sazkomat.Data.DTOs;

public record DashboardStatsResponse(
    OverallStats Overall,
    MatchResultsStats Results,
    List<LeagueStats> TopLeagues,
    List<SeasonStats> SeasonBreakdown,
    List<RecentImportJob> RecentJobs
);

public record OverallStats(
    int TotalLeagues,
    int TotalRounds,
    int TotalSeasons,
    int TotalMatches
);

public record MatchResultsStats(
    int HomeWins,
    int Draws,
    int AwayWins,
    decimal HomeWinPercentage,
    decimal DrawPercentage,
    decimal AwayWinPercentage
);

public record LeagueStats(
    Guid LeagueId,
    string LeagueName,
    string CountryName,
    string CountryFlag,
    string SportName,
    int RoundsCount,
    int SeasonsCount,
    int MatchesCount,
    DateTime? LastImport
);

public record SeasonStats(
    string Season,
    int RoundsCount,
    int MatchesCount,
    int LeaguesCount
);

public record RecentImportJob(
    Guid JobId,
    Guid LeagueId,
    string LeagueName,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int ProcessedRounds,
    int TotalSeasons
);
