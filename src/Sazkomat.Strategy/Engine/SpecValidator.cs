using Sazkomat.Core.Common;
using Sazkomat.Strategy.Models;

namespace Sazkomat.Strategy.Engine;

public static class SpecValidator
{
    private static readonly HashSet<string> ValidDataSources = new(StringComparer.OrdinalIgnoreCase)
        { "matches", "rounds" };

    private static readonly HashSet<string> ValidGroupByDimensions = new(StringComparer.OrdinalIgnoreCase)
        { "league", "country", "season", "result", "month", "year", "oddsRange", "homeTeam", "awayTeam", "round", "group" };

    private static readonly HashSet<string> ValidMetricTypes = new(StringComparer.OrdinalIgnoreCase)
        { "count", "resultPercentage", "average", "sum", "roi", "impliedProbability", "valueGap", "goalAverage", "min", "max", "stddev" };

    private static readonly HashSet<string> ValidSortDirections = new(StringComparer.OrdinalIgnoreCase)
        { "asc", "desc" };

    private static readonly HashSet<string> ValidComparisonOperators = new()
        { "=", "!=", "<", ">", "<=", ">=" };

    public static readonly HashSet<string> WhitelistedMatchColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "home_score", "away_score", "home_odds", "draw_odds", "away_odds"
    };

    public static readonly HashSet<string> WhitelistedRoundColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "round_number", "matches_count", "home_wins", "draws", "away_wins",
        "cumulative_odds_home", "cumulative_odds_draw", "cumulative_odds_away"
    };

    public static readonly HashSet<string> WhitelistedOddsColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "home_odds", "draw_odds", "away_odds"
    };

    public static Result Validate(ViewSpec spec)
    {
        if (!ValidDataSources.Contains(spec.DataSource))
            return Result.Failure($"Invalid dataSource: '{spec.DataSource}'. Must be one of: {string.Join(", ", ValidDataSources)}");

        if (spec.Metrics.Count == 0)
            return Result.Failure("At least one metric is required.");

        foreach (var metric in spec.Metrics)
        {
            if (!ValidMetricTypes.Contains(metric.Type))
                return Result.Failure($"Invalid metric type: '{metric.Type}'. Must be one of: {string.Join(", ", ValidMetricTypes)}");

            var requiresColumn = metric.Type is "average" or "sum" or "min" or "max" or "stddev" or "impliedProbability";
            if (requiresColumn && string.IsNullOrEmpty(metric.Column))
                return Result.Failure($"Metric type '{metric.Type}' requires a column.");

            if (!string.IsNullOrEmpty(metric.Column) && !IsWhitelistedColumn(metric.Column, spec.DataSource))
                return Result.Failure($"Column '{metric.Column}' is not whitelisted for data source '{spec.DataSource}'.");

            var requiresResult = metric.Type is "resultPercentage" or "roi" or "valueGap";
            if (requiresResult && string.IsNullOrEmpty(metric.Result))
                return Result.Failure($"Metric type '{metric.Type}' requires a result parameter (H, D, or A).");

            if (!string.IsNullOrEmpty(metric.Result) && metric.Result is not ("H" or "D" or "A"))
                return Result.Failure($"Invalid result value: '{metric.Result}'. Must be H, D, or A.");
        }

        if (spec.GroupBy != null)
        {
            foreach (var dim in spec.GroupBy)
            {
                if (!ValidGroupByDimensions.Contains(dim))
                    return Result.Failure($"Invalid groupBy dimension: '{dim}'. Must be one of: {string.Join(", ", ValidGroupByDimensions)}");
            }
        }

        if (spec.Sort != null)
        {
            if (!ValidSortDirections.Contains(spec.Sort.Direction))
                return Result.Failure($"Invalid sort direction: '{spec.Sort.Direction}'. Must be 'asc' or 'desc'.");
        }

        if (spec.Limit is < 1 or > 10000)
            return Result.Failure("Limit must be between 1 and 10000.");

        if (spec.Filters?.FieldComparisons != null)
        {
            foreach (var comp in spec.Filters.FieldComparisons)
            {
                if (!ValidComparisonOperators.Contains(comp.Operator))
                    return Result.Failure($"Invalid comparison operator: '{comp.Operator}'.");

                if (!IsWhitelistedColumn(comp.Left, spec.DataSource))
                    return Result.Failure($"Field comparison left column '{comp.Left}' is not whitelisted.");

                if (!IsWhitelistedColumn(comp.Right, spec.DataSource))
                    return Result.Failure($"Field comparison right column '{comp.Right}' is not whitelisted.");
            }
        }

        if (spec.Filters?.OddsRange != null)
        {
            if (!WhitelistedOddsColumns.Contains(spec.Filters.OddsRange.Column))
                return Result.Failure($"Odds range column '{spec.Filters.OddsRange.Column}' is not a valid odds column.");
        }

        return Result.Success();
    }

    private static bool IsWhitelistedColumn(string column, string dataSource)
    {
        return dataSource.Equals("matches", StringComparison.OrdinalIgnoreCase)
            ? WhitelistedMatchColumns.Contains(column)
            : WhitelistedRoundColumns.Contains(column);
    }
}
