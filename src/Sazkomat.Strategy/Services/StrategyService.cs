using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using Sazkomat.Core.Common;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;
using Sazkomat.Strategy.Engine;
using Sazkomat.Strategy.Models;

namespace Sazkomat.Strategy.Services;

public class StrategyService
{
    private readonly string _connectionString;
    private readonly DataDbContext _context;
    private readonly Dictionary<string, IStrategyExecutor> _executors;
    private readonly ILogger<StrategyService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StrategyService(
        string connectionString,
        DataDbContext context,
        IEnumerable<IStrategyExecutor> executors,
        ILogger<StrategyService> logger)
    {
        _connectionString = connectionString;
        _context = context;
        _executors = executors.ToDictionary(e => e.Type);
        _logger = logger;
    }

    public List<StrategyInfo> GetAvailableStrategies()
    {
        return _executors.Values.Select(e => new StrategyInfo
        {
            Type = e.Type,
            Name = e.Name,
            Description = e.Description,
            Parameters = e.GetParameterDefinitions()
        }).ToList();
    }

    public async Task<Result<ScreeningResult>> ScreenAsync(StrategySimulationSpec spec, string? name = null)
    {
        if (!_executors.TryGetValue(spec.StrategyType, out var executor))
            return Result<ScreeningResult>.Failure($"Unknown strategy type: {spec.StrategyType}");

        _logger.LogInformation("Starting screening for strategy {StrategyType}", spec.StrategyType);

        var rounds = await LoadRoundsAsync(spec);
        _logger.LogInformation("Loaded {Count} rounds for screening", rounds.Count);

        var result = executor.Screen(rounds, spec.Parameters);

        // Auto-save screening result
        var screening = new StrategyScreening
        {
            Name = name ?? $"{executor.Name} — {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
            StrategyType = spec.StrategyType,
            ParametersJson = JsonSerializer.Serialize(spec, JsonOptions),
            ResultJson = JsonSerializer.Serialize(result, JsonOptions),
            RoundsAnalyzed = result.TotalRounds,
            CalculatedAt = DateTime.UtcNow
        };

        _context.StrategyScreenings.Add(screening);
        await _context.SaveChangesAsync();

        result.Id = screening.Id;

        _logger.LogInformation("Screening complete: {Leagues} leagues, {Rounds} rounds, saved as {Id}",
            result.TotalLeagues, result.TotalRounds, screening.Id);

        return Result<ScreeningResult>.Success(result);
    }

    public async Task<Result<SimulationResult>> SimulateAsync(StrategySimulationSpec spec)
    {
        if (!_executors.TryGetValue(spec.StrategyType, out var executor))
            return Result<SimulationResult>.Failure($"Unknown strategy type: {spec.StrategyType}");

        _logger.LogInformation("Starting simulation for strategy {StrategyType}", spec.StrategyType);

        var rounds = await LoadRoundsAsync(spec);
        _logger.LogInformation("Loaded {Count} rounds for simulation", rounds.Count);

        var result = executor.Simulate(rounds, spec.Parameters);

        _logger.LogInformation("Simulation complete: {Rounds} rounds, ROI {Roi}%",
            result.Summary.TotalRounds, result.Summary.Roi);

        return Result<SimulationResult>.Success(result);
    }

    public async Task<List<ScreeningListDto>> GetScreeningsAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"SELECT id, name, strategy_type, rounds_analyzed, calculated_at, created_at
                    FROM data_import.strategy_screenings
                    ORDER BY created_at DESC";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var result = new List<ScreeningListDto>();
        while (await reader.ReadAsync())
        {
            result.Add(new ScreeningListDto
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                StrategyType = reader.GetString(2),
                RoundsAnalyzed = reader.GetInt32(3),
                CalculatedAt = reader.GetDateTime(4),
                CreatedAt = reader.GetDateTime(5)
            });
        }
        return result;
    }

    public async Task<Result<ScreeningDetailDto>> GetScreeningAsync(Guid id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"SELECT id, name, strategy_type, parameters_json, result_json,
                           rounds_analyzed, calculated_at, created_at
                    FROM data_import.strategy_screenings
                    WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Result<ScreeningDetailDto>.Failure("Screening not found");

        return Result<ScreeningDetailDto>.Success(new ScreeningDetailDto
        {
            Id = reader.GetGuid(0),
            Name = reader.GetString(1),
            StrategyType = reader.GetString(2),
            Spec = JsonSerializer.Deserialize<StrategySimulationSpec>(reader.GetString(3), JsonOptions),
            Result = JsonSerializer.Deserialize<ScreeningResult>(reader.GetString(4), JsonOptions),
            RoundsAnalyzed = reader.GetInt32(5),
            CalculatedAt = reader.GetDateTime(6),
            CreatedAt = reader.GetDateTime(7)
        });
    }

    public async Task<Result> DeleteScreeningAsync(Guid id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = "DELETE FROM data_import.strategy_screenings WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0
            ? Result.Success()
            : Result.Failure("Screening not found");
    }

    private async Task<List<RoundData>> LoadRoundsAsync(StrategySimulationSpec spec)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var conditions = new List<string> { "r.matches_count >= @minMatches" };
        var parameters = new List<NpgsqlParameter>
        {
            new("minMatches", spec.MinMatches)
        };

        if (spec.RequireOdds == true)
        {
            conditions.Add("r.cumulative_odds_home > 1");
        }

        if (spec.LeagueIds is { Count: > 0 })
        {
            conditions.Add("r.league_id = ANY(@leagueIds)");
            parameters.Add(new("leagueIds", spec.LeagueIds.ToArray()));
        }

        if (spec.CountryIds is { Count: > 0 })
        {
            conditions.Add("l.country_id = ANY(@countryIds)");
            parameters.Add(new("countryIds", spec.CountryIds.ToArray()));
        }

        if (spec.SeasonNames is { Count: > 0 })
        {
            conditions.Add("s.name = ANY(@seasonNames)");
            parameters.Add(new("seasonNames", spec.SeasonNames.ToArray()));
        }

        if (spec.StartYear.HasValue)
        {
            conditions.Add("s.start_year >= @startYear");
            parameters.Add(new("startYear", spec.StartYear.Value));
        }

        var whereClause = string.Join(" AND ", conditions);

        var sql = $@"
            SELECT r.league_id, l.name AS league_name, c.name AS country_name,
                   s.name AS season_name, r.group_name, r.round_number,
                   r.matches_count, r.home_wins, r.draws, r.away_wins,
                   r.cumulative_odds_home, r.cumulative_odds_draw, r.cumulative_odds_away
            FROM data_import.rounds r
            JOIN configuration.leagues l ON l.id = r.league_id
            JOIN configuration.countries c ON c.id = l.country_id
            JOIN configuration.seasons s ON s.id = r.season_id
            WHERE {whereClause}
            ORDER BY r.league_id, r.group_name, s.name ASC, r.round_number";

        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var p in parameters)
            cmd.Parameters.Add(p);

        await using var reader = await cmd.ExecuteReaderAsync();

        var rounds = new List<RoundData>();
        while (await reader.ReadAsync())
        {
            rounds.Add(new RoundData
            {
                LeagueId = reader.GetGuid(0),
                LeagueName = reader.GetString(1),
                CountryName = reader.GetString(2),
                SeasonName = reader.GetString(3),
                GroupName = reader.IsDBNull(4) ? null : reader.GetString(4),
                RoundNumber = reader.GetInt32(5),
                MatchesCount = reader.GetInt32(6),
                HomeWins = reader.GetInt32(7),
                Draws = reader.GetInt32(8),
                AwayWins = reader.GetInt32(9),
                CumulativeOddsHome = reader.GetDecimal(10),
                CumulativeOddsDraw = reader.GetDecimal(11),
                CumulativeOddsAway = reader.GetDecimal(12)
            });
        }

        return rounds;
    }
}

// DTOs for screening persistence

public class ScreeningListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StrategyType { get; set; } = string.Empty;
    public int RoundsAnalyzed { get; set; }
    public DateTime CalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ScreeningDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StrategyType { get; set; } = string.Empty;
    public StrategySimulationSpec? Spec { get; set; }
    public ScreeningResult? Result { get; set; }
    public int RoundsAnalyzed { get; set; }
    public DateTime CalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
