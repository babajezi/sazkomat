namespace Sazkomat.Strategy.Models;

// === Shared DTOs ===

public class RoundData
{
    public Guid LeagueId { get; set; }
    public string LeagueName { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string SeasonName { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int RoundNumber { get; set; }
    public int MatchesCount { get; set; }
    public int HomeWins { get; set; }
    public int Draws { get; set; }
    public int AwayWins { get; set; }
    public decimal CumulativeOddsHome { get; set; }
    public decimal CumulativeOddsDraw { get; set; }
    public decimal CumulativeOddsAway { get; set; }
}

public class ParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "number"; // "number", "select", "boolean"
    public object? DefaultValue { get; set; }
    public List<SelectOption>? Options { get; set; }
}

public class SelectOption
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class StrategyInfo
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ParameterDefinition> Parameters { get; set; } = new();
}

// === Screening (Phase 1) ===

public class ScreeningResult
{
    public Guid? Id { get; set; }
    public List<LeagueScreening> Leagues { get; set; } = new();
    public int TotalLeagues { get; set; }
    public int TotalRounds { get; set; }
    public int ExecutionMs { get; set; }
}

public class LeagueScreening
{
    public Guid LeagueId { get; set; }
    public string League { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int TotalSeasons { get; set; }
    public int TotalRounds { get; set; }
    public int PerfectCount { get; set; }
    public int Near1Count { get; set; }
    public int Near2Count { get; set; }
    public decimal PerfectRate { get; set; }
    public decimal Near1Rate { get; set; }
    public decimal Near2Rate { get; set; }
    public decimal AvgGap { get; set; }
    public int MaxGap { get; set; }
    public int RoundsWithOdds { get; set; }
}

// === Simulation (Phase 2) ===

public class SimulationResult
{
    public SimulationSummary Summary { get; set; } = new();
    public List<LeagueSimulationResult> Leagues { get; set; } = new();
    public int ExecutionMs { get; set; }
}

public class SimulationSummary
{
    public int TotalLeagueSeasons { get; set; }
    public int TotalRounds { get; set; }
    public int WinningRounds { get; set; }
    public decimal WinRate { get; set; }
    public decimal TotalStaked { get; set; }
    public decimal TotalWon { get; set; }
    public decimal Profit { get; set; }
    public decimal Roi { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public decimal MaxStake { get; set; }
    public int RoundsWithOdds { get; set; }
}

public class LeagueSimulationResult
{
    public Guid LeagueId { get; set; }
    public string League { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int TotalRounds { get; set; }
    public int TotalSeasons { get; set; }
    public int WinningRounds { get; set; }
    public decimal TotalStaked { get; set; }
    public decimal TotalWon { get; set; }
    public decimal Profit { get; set; }
    public bool HasOdds { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public decimal MaxStake { get; set; }
    public List<SeasonDetail> Seasons { get; set; } = new();
}

public class SeasonDetail
{
    public string Season { get; set; } = string.Empty;
    public int TotalRounds { get; set; }
    public int WinningRounds { get; set; }
    public decimal TotalStaked { get; set; }
    public decimal TotalWon { get; set; }
    public decimal Profit { get; set; }
    public bool HasOdds { get; set; }
}
