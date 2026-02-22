using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.Core.Common;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;
using Sazkomat.Strategy.Engine;
using Sazkomat.Strategy.Models;

namespace Sazkomat.Strategy.Services;

public class AnalyticalViewService
{
    private readonly DataDbContext _context;
    private readonly AnalyticsEngine _engine;
    private readonly ILogger<AnalyticalViewService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AnalyticalViewService(
        DataDbContext context,
        AnalyticsEngine engine,
        ILogger<AnalyticalViewService> logger)
    {
        _context = context;
        _engine = engine;
        _logger = logger;
    }

    public async Task<Result<AnalyticsResult>> ExecuteAsync(ViewSpec spec)
    {
        return await _engine.ExecuteAsync(spec);
    }

    public async Task<Result<List<DistinctValueItem>>> GetDistinctValuesAsync(ViewSpec spec, string column)
    {
        return await _engine.GetDistinctValuesAsync(spec, column);
    }

    public async Task<Result<AnalyticsResult>> ExecuteByIdAsync(Guid id)
    {
        var view = await _context.AnalyticalViews.FindAsync(id);
        if (view == null)
            return Result<AnalyticsResult>.Failure("View not found.");

        var spec = JsonSerializer.Deserialize<ViewSpec>(view.SpecJson, JsonOptions);
        if (spec == null)
            return Result<AnalyticsResult>.Failure("Invalid spec JSON.");

        var result = await _engine.ExecuteAsync(spec);

        if (result.IsSuccess)
        {
            view.ExecutionCount++;
            view.LastExecutedAt = DateTime.UtcNow;
            view.LastExecutionMs = result.Value!.ExecutionMs;
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<List<AnalyticalView>> GetAllAsync()
    {
        return await _context.AnalyticalViews
            .OrderByDescending(v => v.IsFavorite)
            .ThenByDescending(v => v.LastExecutedAt)
            .ThenBy(v => v.Name)
            .ToListAsync();
    }

    public async Task<AnalyticalView?> GetByIdAsync(Guid id)
    {
        return await _context.AnalyticalViews.FindAsync(id);
    }

    public async Task<AnalyticalView> CreateAsync(string name, string? description, ViewSpec spec, string? tags)
    {
        var view = new AnalyticalView
        {
            Name = name,
            Description = description,
            SpecJson = JsonSerializer.Serialize(spec, JsonOptions),
            Tags = tags
        };

        _context.AnalyticalViews.Add(view);
        await _context.SaveChangesAsync();
        return view;
    }

    public async Task<Result<AnalyticalView>> UpdateAsync(Guid id, string? name, string? description, ViewSpec? spec, string? tags)
    {
        var view = await _context.AnalyticalViews.FindAsync(id);
        if (view == null)
            return Result<AnalyticalView>.Failure("View not found.");

        if (name != null) view.Name = name;
        if (description != null) view.Description = description;
        if (spec != null) view.SpecJson = JsonSerializer.Serialize(spec, JsonOptions);
        if (tags != null) view.Tags = tags;

        await _context.SaveChangesAsync();
        return Result<AnalyticalView>.Success(view);
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var view = await _context.AnalyticalViews.FindAsync(id);
        if (view == null)
            return Result.Failure("View not found.");

        _context.AnalyticalViews.Remove(view);
        await _context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<AnalyticalView>> ToggleFavoriteAsync(Guid id)
    {
        var view = await _context.AnalyticalViews.FindAsync(id);
        if (view == null)
            return Result<AnalyticalView>.Failure("View not found.");

        view.IsFavorite = !view.IsFavorite;
        await _context.SaveChangesAsync();
        return Result<AnalyticalView>.Success(view);
    }

    public AnalyticsMetadata GetMetadata()
    {
        return new AnalyticsMetadata
        {
            DataSources = new List<string> { "matches", "rounds" },
            Dimensions = new List<DimensionInfo>
            {
                new() { Name = "league", Description = "Group by league name" },
                new() { Name = "country", Description = "Group by country name" },
                new() { Name = "season", Description = "Group by season name" },
                new() { Name = "result", Description = "Group by match result (H/D/A) — matches only" },
                new() { Name = "month", Description = "Group by year-month (YYYY-MM)" },
                new() { Name = "year", Description = "Group by year" },
                new() { Name = "oddsRange", Description = "Group by home odds range — matches only" },
                new() { Name = "homeTeam", Description = "Group by home team — matches only" },
                new() { Name = "awayTeam", Description = "Group by away team — matches only" },
                new() { Name = "round", Description = "Group by round number" },
                new() { Name = "group", Description = "Group by group name (for grouped leagues)" }
            },
            MetricTypes = new List<MetricInfo>
            {
                new() { Type = "count", Description = "Count of rows" },
                new() { Type = "resultPercentage", Description = "Percentage of specific result (H/D/A)", RequiresResult = true },
                new() { Type = "average", Description = "Average of a numeric column", RequiresColumn = true },
                new() { Type = "sum", Description = "Sum of a numeric column", RequiresColumn = true },
                new() { Type = "min", Description = "Minimum value of a column", RequiresColumn = true },
                new() { Type = "max", Description = "Maximum value of a column", RequiresColumn = true },
                new() { Type = "stddev", Description = "Standard deviation", RequiresColumn = true },
                new() { Type = "roi", Description = "Return on investment for flat betting on result", RequiresColumn = true, RequiresResult = true },
                new() { Type = "impliedProbability", Description = "Average implied probability from odds", RequiresColumn = true },
                new() { Type = "valueGap", Description = "Actual win rate minus implied probability", RequiresColumn = true, RequiresResult = true },
                new() { Type = "goalAverage", Description = "Average total goals per match" }
            },
            Columns = new List<ColumnInfo>
            {
                // Match columns
                new() { Name = "home_score", Table = "matches", Type = "int", Description = "Home team score" },
                new() { Name = "away_score", Table = "matches", Type = "int", Description = "Away team score" },
                new() { Name = "home_odds", Table = "matches", Type = "decimal", Description = "Home win odds" },
                new() { Name = "draw_odds", Table = "matches", Type = "decimal", Description = "Draw odds" },
                new() { Name = "away_odds", Table = "matches", Type = "decimal", Description = "Away win odds" },
                // Round columns
                new() { Name = "round_number", Table = "rounds", Type = "int", Description = "Round number" },
                new() { Name = "matches_count", Table = "rounds", Type = "int", Description = "Matches in round" },
                new() { Name = "home_wins", Table = "rounds", Type = "int", Description = "Home wins in round" },
                new() { Name = "draws", Table = "rounds", Type = "int", Description = "Draws in round" },
                new() { Name = "away_wins", Table = "rounds", Type = "int", Description = "Away wins in round" },
                new() { Name = "cumulative_odds_home", Table = "rounds", Type = "decimal", Description = "Cumulative home odds" },
                new() { Name = "cumulative_odds_draw", Table = "rounds", Type = "decimal", Description = "Cumulative draw odds" },
                new() { Name = "cumulative_odds_away", Table = "rounds", Type = "decimal", Description = "Cumulative away odds" }
            },
            Operators = new List<string> { "=", "!=", "<", ">", "<=", ">=" }
        };
    }
}
