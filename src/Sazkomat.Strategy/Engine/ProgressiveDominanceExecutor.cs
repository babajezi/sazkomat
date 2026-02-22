using System.Diagnostics;
using System.Text.Json;
using Sazkomat.Strategy.Models;

namespace Sazkomat.Strategy.Engine;

public class ProgressiveDominanceExecutor : IStrategyExecutor
{
    public string Type => "ProgressiveDominance";
    public string Name => "Progresivní dominance";
    public string Description => "Progresivní sázka na dominantní kola (všechny výhry domácích/hostů nebo remízy). " +
        "Sázka se zvyšuje po prohře a resetuje po výhře.";

    public List<ParameterDefinition> GetParameterDefinitions() => new()
    {
        new ParameterDefinition
        {
            Name = "dominanceType",
            Label = "Typ dominance",
            Type = "select",
            DefaultValue = "Home",
            Options = new List<SelectOption>
            {
                new() { Label = "Domácí (1)", Value = "Home" },
                new() { Label = "Remíza (X)", Value = "Draw" },
                new() { Label = "Hosté (2)", Value = "Away" }
            }
        },
        new ParameterDefinition
        {
            Name = "baseStake",
            Label = "Základní sázka (Kč)",
            Type = "number",
            DefaultValue = 100
        },
        new ParameterDefinition
        {
            Name = "increasePercent",
            Label = "Zvýšení po prohře (%)",
            Type = "number",
            DefaultValue = 50
        },
        new ParameterDefinition
        {
            Name = "tolerance",
            Label = "Tolerance",
            Type = "select",
            DefaultValue = 1,
            Options = new List<SelectOption>
            {
                new() { Label = "PERFECT (0)", Value = "0" },
                new() { Label = "NEAR-1 (±1)", Value = "1" },
                new() { Label = "NEAR-2 (±2)", Value = "2" }
            }
        }
    };

    public ScreeningResult Screen(List<RoundData> rounds, JsonElement? parameters)
    {
        var sw = Stopwatch.StartNew();
        var p = DeserializeParams(parameters);

        var leagueGroups = rounds
            .GroupBy(r => new { r.LeagueId, r.LeagueName, r.CountryName })
            .ToList();

        var leagues = new List<LeagueScreening>();

        foreach (var lg in leagueGroups)
        {
            var leagueRounds = lg.ToList();
            int perfectCount = 0, near1Count = 0, near2Count = 0;
            int roundsWithOdds = 0;
            var seasons = leagueRounds.Select(r => r.SeasonName).Distinct().Count();

            // Gap tracking (at the user-selected tolerance level)
            var gaps = new List<int>();
            int currentGap = 0;

            foreach (var round in leagueRounds)
            {
                int target = GetTargetCount(round, p.DominanceType);
                bool isPerfect = target == round.MatchesCount;
                bool isNear1 = target >= round.MatchesCount - 1;
                bool isNear2 = target >= round.MatchesCount - 2;

                if (isPerfect) perfectCount++;
                if (isNear1) near1Count++;
                if (isNear2) near2Count++;

                decimal odds = GetCumulativeOdds(round, p.DominanceType);
                if (odds > 1) roundsWithOdds++;

                // Gap calculation uses user's tolerance
                bool isWinAtTolerance = p.Tolerance switch
                {
                    0 => isPerfect,
                    1 => isNear1,
                    _ => isNear2
                };

                if (isWinAtTolerance)
                {
                    if (currentGap > 0) gaps.Add(currentGap);
                    currentGap = 0;
                }
                else
                {
                    currentGap++;
                }
            }
            if (currentGap > 0) gaps.Add(currentGap);

            int totalRounds = leagueRounds.Count;
            leagues.Add(new LeagueScreening
            {
                LeagueId = lg.Key.LeagueId,
                League = lg.Key.LeagueName,
                Country = lg.Key.CountryName,
                TotalSeasons = seasons,
                TotalRounds = totalRounds,
                PerfectCount = perfectCount,
                Near1Count = near1Count,
                Near2Count = near2Count,
                PerfectRate = totalRounds > 0 ? Math.Round(100m * perfectCount / totalRounds, 2) : 0,
                Near1Rate = totalRounds > 0 ? Math.Round(100m * near1Count / totalRounds, 2) : 0,
                Near2Rate = totalRounds > 0 ? Math.Round(100m * near2Count / totalRounds, 2) : 0,
                AvgGap = gaps.Count > 0 ? Math.Round((decimal)gaps.Average(), 1) : 0,
                MaxGap = gaps.Count > 0 ? gaps.Max() : 0,
                RoundsWithOdds = roundsWithOdds
            });
        }

        sw.Stop();
        return new ScreeningResult
        {
            Leagues = leagues.OrderByDescending(l => l.Near1Rate).ToList(),
            TotalLeagues = leagues.Count,
            TotalRounds = rounds.Count,
            ExecutionMs = (int)sw.ElapsedMilliseconds
        };
    }

    // Practical cap to prevent decimal overflow during progressive staking
    private const decimal MaxStakeCap = 1_000_000_000m;

    public SimulationResult Simulate(List<RoundData> rounds, JsonElement? parameters)
    {
        var sw = Stopwatch.StartNew();
        var p = DeserializeParams(parameters);

        // Group by (leagueId, groupName) — each group is an independent betting series
        var leagueGroups = rounds
            .GroupBy(r => new { r.LeagueId, r.LeagueName, r.CountryName, r.GroupName })
            .ToList();

        var leagueResults = new Dictionary<Guid, LeagueSimulationResult>();
        int globalMaxConsecutiveLosses = 0;
        decimal globalMaxStake = 0;
        int globalRoundsWithOdds = 0;

        foreach (var lg in leagueGroups)
        {
            var leagueRounds = lg.ToList();

            // Get or create league result (multiple groups merge into same league)
            if (!leagueResults.TryGetValue(lg.Key.LeagueId, out var leagueResult))
            {
                leagueResult = new LeagueSimulationResult
                {
                    LeagueId = lg.Key.LeagueId,
                    League = lg.Key.LeagueName,
                    Country = lg.Key.CountryName
                };
                leagueResults[lg.Key.LeagueId] = leagueResult;
            }

            // Progressive betting — stake does NOT reset between seasons
            decimal currentStake = p.BaseStake;
            int consecutiveLosses = 0;
            int groupMaxConsecutiveLosses = 0;
            decimal groupMaxStake = p.BaseStake;

            // Track per-season details
            var seasonDetails = new Dictionary<string, SeasonDetail>();

            foreach (var round in leagueRounds)
            {
                if (!seasonDetails.TryGetValue(round.SeasonName, out var sd))
                {
                    sd = new SeasonDetail { Season = round.SeasonName };
                    seasonDetails[round.SeasonName] = sd;
                }

                int targetCount = GetTargetCount(round, p.DominanceType);
                bool isWin = targetCount >= round.MatchesCount - p.Tolerance;

                sd.TotalRounds++;
                leagueResult.TotalRounds++;
                sd.TotalStaked += currentStake;
                leagueResult.TotalStaked += currentStake;

                if (currentStake > groupMaxStake) groupMaxStake = currentStake;

                if (isWin)
                {
                    sd.WinningRounds++;
                    leagueResult.WinningRounds++;

                    decimal odds = GetCumulativeOdds(round, p.DominanceType);
                    if (odds > 1 && odds < MaxStakeCap)
                    {
                        decimal won = Math.Min(currentStake * odds, MaxStakeCap);
                        sd.TotalWon += won;
                        leagueResult.TotalWon += won;
                        sd.HasOdds = true;
                        leagueResult.HasOdds = true;
                        globalRoundsWithOdds++;
                    }

                    currentStake = p.BaseStake;
                    if (consecutiveLosses > groupMaxConsecutiveLosses)
                        groupMaxConsecutiveLosses = consecutiveLosses;
                    consecutiveLosses = 0;
                }
                else
                {
                    decimal nextStake = currentStake + p.BaseStake * (p.IncreasePercent / 100m);
                    currentStake = Math.Min(nextStake, MaxStakeCap);
                    consecutiveLosses++;
                }
            }

            // Final streak
            if (consecutiveLosses > groupMaxConsecutiveLosses)
                groupMaxConsecutiveLosses = consecutiveLosses;

            if (groupMaxConsecutiveLosses > leagueResult.MaxConsecutiveLosses)
                leagueResult.MaxConsecutiveLosses = groupMaxConsecutiveLosses;
            if (groupMaxStake > leagueResult.MaxStake)
                leagueResult.MaxStake = groupMaxStake;

            if (groupMaxConsecutiveLosses > globalMaxConsecutiveLosses)
                globalMaxConsecutiveLosses = groupMaxConsecutiveLosses;
            if (groupMaxStake > globalMaxStake)
                globalMaxStake = groupMaxStake;

            // Add season details
            foreach (var sd in seasonDetails.Values)
            {
                sd.Profit = sd.TotalWon - sd.TotalStaked;
                leagueResult.Seasons.Add(sd);
            }
        }

        // Build final results
        var allLeagues = leagueResults.Values.ToList();
        foreach (var lr in allLeagues)
        {
            lr.Profit = lr.TotalWon - lr.TotalStaked;
            lr.TotalSeasons = lr.Seasons.Count;
            lr.Seasons = lr.Seasons.OrderBy(s => s.Season).ToList();
        }

        decimal totalStaked = allLeagues.Sum(l => l.TotalStaked);
        decimal totalWon = allLeagues.Sum(l => l.TotalWon);
        int totalRounds = allLeagues.Sum(l => l.TotalRounds);
        int winningRounds = allLeagues.Sum(l => l.WinningRounds);

        sw.Stop();
        return new SimulationResult
        {
            Summary = new SimulationSummary
            {
                TotalLeagueSeasons = allLeagues.Sum(l => l.TotalSeasons),
                TotalRounds = totalRounds,
                WinningRounds = winningRounds,
                WinRate = totalRounds > 0 ? Math.Round(100m * winningRounds / totalRounds, 2) : 0,
                TotalStaked = Math.Round(totalStaked, 2),
                TotalWon = Math.Round(totalWon, 2),
                Profit = Math.Round(totalWon - totalStaked, 2),
                Roi = totalStaked > 0 ? Math.Round(100m * (totalWon - totalStaked) / totalStaked, 2) : 0,
                MaxConsecutiveLosses = globalMaxConsecutiveLosses,
                MaxStake = Math.Round(globalMaxStake, 2),
                RoundsWithOdds = globalRoundsWithOdds
            },
            Leagues = allLeagues.OrderByDescending(l => l.Profit).ToList(),
            ExecutionMs = (int)sw.ElapsedMilliseconds
        };
    }

    private static int GetTargetCount(RoundData round, string dominanceType) => dominanceType switch
    {
        "Draw" => round.Draws,
        "Away" => round.AwayWins,
        _ => round.HomeWins
    };

    private static decimal GetCumulativeOdds(RoundData round, string dominanceType) => dominanceType switch
    {
        "Draw" => round.CumulativeOddsDraw,
        "Away" => round.CumulativeOddsAway,
        _ => round.CumulativeOddsHome
    };

    private static ProgressiveDominanceParams DeserializeParams(JsonElement? parameters)
    {
        if (parameters == null) return new ProgressiveDominanceParams();
        return JsonSerializer.Deserialize<ProgressiveDominanceParams>(parameters.Value.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            })
            ?? new ProgressiveDominanceParams();
    }
}

public class ProgressiveDominanceParams
{
    public string DominanceType { get; set; } = "Home";
    public decimal BaseStake { get; set; } = 100;
    public decimal IncreasePercent { get; set; } = 50;
    public int Tolerance { get; set; } = 1;
}
