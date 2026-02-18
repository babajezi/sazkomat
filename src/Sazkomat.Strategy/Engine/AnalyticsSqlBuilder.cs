using Npgsql;
using Sazkomat.Strategy.Models;

namespace Sazkomat.Strategy.Engine;

public class AnalyticsSqlBuilder
{
    private readonly ViewSpec _spec;
    private readonly List<NpgsqlParameter> _parameters = new();
    private int _paramIndex;

    public AnalyticsSqlBuilder(ViewSpec spec)
    {
        _spec = spec;
    }

    public (string Sql, List<NpgsqlParameter> Parameters) Build()
    {
        var select = BuildSelect();
        var from = BuildFrom();
        var where = BuildWhere();
        var groupBy = BuildGroupBy();
        var having = BuildHaving();
        var orderBy = BuildOrderBy();
        var limit = BuildLimit();

        var sql = $"{select}\n{from}";
        if (!string.IsNullOrEmpty(where)) sql += $"\n{where}";
        if (!string.IsNullOrEmpty(groupBy)) sql += $"\n{groupBy}";
        if (!string.IsNullOrEmpty(having)) sql += $"\n{having}";
        if (!string.IsNullOrEmpty(orderBy)) sql += $"\n{orderBy}";
        if (!string.IsNullOrEmpty(limit)) sql += $"\n{limit}";

        return (sql, _parameters);
    }

    private string BuildSelect()
    {
        var columns = new List<string>();

        if (_spec.GroupBy != null)
        {
            foreach (var dim in _spec.GroupBy)
            {
                columns.Add(MapDimensionToSql(dim));
            }
        }

        foreach (var metric in _spec.Metrics)
        {
            columns.Add(BuildMetricSql(metric));
        }

        return $"SELECT {string.Join(",\n       ", columns)}";
    }

    private string BuildFrom()
    {
        if (_spec.DataSource.Equals("rounds", StringComparison.OrdinalIgnoreCase))
        {
            return """
                FROM data_import.rounds r
                JOIN configuration.leagues l ON l.id = r.league_id
                JOIN configuration.seasons s ON s.id = r.season_id
                JOIN configuration.countries c ON c.id = l.country_id
                """;
        }

        return """
            FROM data_import.matches m
            JOIN data_import.rounds r ON r.id = m.round_id
            JOIN configuration.leagues l ON l.id = r.league_id
            JOIN configuration.seasons s ON s.id = r.season_id
            JOIN configuration.countries c ON c.id = l.country_id
            """;
    }

    private string BuildWhere()
    {
        var conditions = new List<string>();

        if (_spec.Filters == null)
            return conditions.Count > 0 ? $"WHERE {string.Join("\n  AND ", conditions)}" : "";

        var f = _spec.Filters;

        if (f.LeagueIds is { Count: > 0 })
        {
            var param = AddArrayParam(f.LeagueIds.ToArray());
            conditions.Add($"r.league_id = ANY({param})");
        }

        if (f.CountryIds is { Count: > 0 })
        {
            var param = AddArrayParam(f.CountryIds.ToArray());
            conditions.Add($"l.country_id = ANY({param})");
        }

        if (f.SeasonNames is { Count: > 0 })
        {
            var param = AddArrayParam(f.SeasonNames.ToArray());
            conditions.Add($"s.name = ANY({param})");
        }

        if (f.DateRange != null)
        {
            var dateCol = IsMatchSource() ? "m.match_date" : "r.start_date";
            if (f.DateRange.From.HasValue)
            {
                var param = AddParam(f.DateRange.From.Value);
                conditions.Add($"{dateCol} >= {param}");
            }
            if (f.DateRange.To.HasValue)
            {
                var param = AddParam(f.DateRange.To.Value);
                conditions.Add($"{dateCol} <= {param}");
            }
        }

        if (f.Results is { Count: > 0 } && IsMatchSource())
        {
            var param = AddArrayParam(f.Results.ToArray());
            conditions.Add($"m.result = ANY({param})");
        }

        if (f.HasOdds == true && IsMatchSource())
        {
            conditions.Add("m.home_odds IS NOT NULL AND m.draw_odds IS NOT NULL AND m.away_odds IS NOT NULL");
        }

        if (f.OddsRange != null && IsMatchSource())
        {
            var col = MapOddsColumn(f.OddsRange.Column);
            if (f.OddsRange.Min.HasValue)
            {
                var param = AddParam(f.OddsRange.Min.Value);
                conditions.Add($"{col} >= {param}");
            }
            if (f.OddsRange.Max.HasValue)
            {
                var param = AddParam(f.OddsRange.Max.Value);
                conditions.Add($"{col} <= {param}");
            }
        }

        if (f.FieldComparisons is { Count: > 0 })
        {
            foreach (var comp in f.FieldComparisons)
            {
                var left = MapWhitelistedColumn(comp.Left);
                var right = MapWhitelistedColumn(comp.Right);
                conditions.Add($"{left} {comp.Operator} {right}");
            }
        }

        return conditions.Count > 0 ? $"WHERE {string.Join("\n  AND ", conditions)}" : "";
    }

    private string BuildGroupBy()
    {
        if (_spec.GroupBy is not { Count: > 0 })
            return "";

        var columns = _spec.GroupBy.Select(MapDimensionToGroupBySql).ToList();
        return $"GROUP BY {string.Join(", ", columns)}";
    }

    private string BuildHaving()
    {
        if (_spec.Filters?.MinMatches is > 0 && _spec.GroupBy is { Count: > 0 })
        {
            var param = AddParam(_spec.Filters.MinMatches.Value);
            return $"HAVING COUNT(*) >= {param}";
        }
        return "";
    }

    private string BuildOrderBy()
    {
        if (_spec.Sort == null)
            return "";

        var direction = _spec.Sort.Direction.Equals("asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        // Try to match sort column to a metric alias or dimension
        var sortCol = ResolveColumnReference(_spec.Sort.Column);
        return $"ORDER BY {sortCol} {direction}";
    }

    private string BuildLimit()
    {
        if (_spec.Limit is > 0)
        {
            var param = AddParam(_spec.Limit.Value);
            return $"LIMIT {param}";
        }
        return "LIMIT 1000"; // default safety limit
    }

    private string BuildMetricSql(MetricSpec metric)
    {
        var alias = metric.Alias ?? $"{metric.Type}_{metric.Column ?? metric.Result ?? "all"}";
        var safeAlias = SanitizeAlias(alias);

        var sql = metric.Type.ToLowerInvariant() switch
        {
            "count" => "COUNT(*)",
            "resultpercentage" => BuildResultPercentageSql(metric),
            "average" => $"AVG({MapWhitelistedColumn(metric.Column!)})",
            "sum" => $"SUM({MapWhitelistedColumn(metric.Column!)})",
            "min" => $"MIN({MapWhitelistedColumn(metric.Column!)})",
            "max" => $"MAX({MapWhitelistedColumn(metric.Column!)})",
            "stddev" => $"STDDEV({MapWhitelistedColumn(metric.Column!)})",
            "roi" => BuildRoiSql(metric),
            "impliedprobability" => $"AVG(1.0 / NULLIF({MapWhitelistedColumn(metric.Column!)}, 0))",
            "valuegap" => BuildValueGapSql(metric),
            "goalaverage" => IsMatchSource()
                ? "AVG(m.home_score + m.away_score)"
                : "AVG(r.home_wins + r.draws + r.away_wins)", // approximation for rounds
            _ => "COUNT(*)"
        };

        return $"{sql} AS {safeAlias}";
    }

    private string BuildResultPercentageSql(MetricSpec metric)
    {
        var resultParam = AddParam(metric.Result!);
        return IsMatchSource()
            ? $"ROUND(100.0 * COUNT(*) FILTER (WHERE m.result = {resultParam}) / NULLIF(COUNT(*), 0), 2)"
            : $"ROUND(100.0 * SUM(CASE WHEN '{metric.Result}' = 'H' THEN r.home_wins WHEN '{metric.Result}' = 'D' THEN r.draws ELSE r.away_wins END) / NULLIF(SUM(r.matches_count), 0), 2)";
    }

    private string BuildRoiSql(MetricSpec metric)
    {
        var oddsCol = MapOddsColumn(metric.Column ?? "home_odds");
        var resultParam = AddParam(metric.Result!);
        return $"ROUND((SUM(CASE WHEN m.result = {resultParam} THEN {oddsCol} ELSE 0 END) - COUNT(*)) / NULLIF(COUNT(*), 0) * 100, 2)";
    }

    private string BuildValueGapSql(MetricSpec metric)
    {
        var oddsCol = MapOddsColumn(metric.Column ?? "home_odds");
        var resultParam = AddParam(metric.Result!);
        return $"ROUND(AVG(CASE WHEN m.result = {resultParam} THEN 1 ELSE 0 END) - AVG(1.0 / NULLIF({oddsCol}, 0)), 4)";
    }

    private string MapDimensionToSql(string dimension)
    {
        return dimension.ToLowerInvariant() switch
        {
            "league" => "l.name AS league",
            "country" => "c.name AS country",
            "season" => "s.name AS season",
            "result" => IsMatchSource() ? "m.result AS result" : "'N/A' AS result",
            "month" => IsMatchSource()
                ? "TO_CHAR(m.match_date, 'YYYY-MM') AS month"
                : "TO_CHAR(r.start_date, 'YYYY-MM') AS month",
            "year" => IsMatchSource()
                ? "EXTRACT(YEAR FROM m.match_date)::int AS year"
                : "EXTRACT(YEAR FROM r.start_date)::int AS year",
            "oddsrange" => IsMatchSource()
                ? """
                  CASE
                      WHEN m.home_odds < 1.5 THEN '<1.50'
                      WHEN m.home_odds < 2.0 THEN '1.50-1.99'
                      WHEN m.home_odds < 3.0 THEN '2.00-2.99'
                      WHEN m.home_odds < 5.0 THEN '3.00-4.99'
                      ELSE '5.00+'
                  END AS odds_range
                  """
                : "'N/A' AS odds_range",
            "hometeam" => IsMatchSource() ? "m.home_team AS home_team" : "'N/A' AS home_team",
            "awayteam" => IsMatchSource() ? "m.away_team AS away_team" : "'N/A' AS away_team",
            "round" => "r.round_number AS round_number",
            "group" => "r.group_name AS group_name",
            _ => $"'unknown' AS {dimension}"
        };
    }

    private string MapDimensionToGroupBySql(string dimension)
    {
        return dimension.ToLowerInvariant() switch
        {
            "league" => "l.name",
            "country" => "c.name",
            "season" => "s.name",
            "result" => IsMatchSource() ? "m.result" : "'N/A'",
            "month" => IsMatchSource()
                ? "TO_CHAR(m.match_date, 'YYYY-MM')"
                : "TO_CHAR(r.start_date, 'YYYY-MM')",
            "year" => IsMatchSource()
                ? "EXTRACT(YEAR FROM m.match_date)::int"
                : "EXTRACT(YEAR FROM r.start_date)::int",
            "oddsrange" => IsMatchSource()
                ? """
                  CASE
                      WHEN m.home_odds < 1.5 THEN '<1.50'
                      WHEN m.home_odds < 2.0 THEN '1.50-1.99'
                      WHEN m.home_odds < 3.0 THEN '2.00-2.99'
                      WHEN m.home_odds < 5.0 THEN '3.00-4.99'
                      ELSE '5.00+'
                  END
                  """
                : "'N/A'",
            "hometeam" => IsMatchSource() ? "m.home_team" : "'N/A'",
            "awayteam" => IsMatchSource() ? "m.away_team" : "'N/A'",
            "round" => "r.round_number",
            "group" => "r.group_name",
            _ => $"1"
        };
    }

    private string MapWhitelistedColumn(string column)
    {
        var prefix = IsMatchSource() ? "m" : "r";
        return column.ToLowerInvariant() switch
        {
            "home_score" => "m.home_score",
            "away_score" => "m.away_score",
            "home_odds" => "m.home_odds",
            "draw_odds" => "m.draw_odds",
            "away_odds" => "m.away_odds",
            "round_number" => "r.round_number",
            "matches_count" => "r.matches_count",
            "home_wins" => "r.home_wins",
            "draws" => "r.draws",
            "away_wins" => "r.away_wins",
            "cumulative_odds_home" => "r.cumulative_odds_home",
            "cumulative_odds_draw" => "r.cumulative_odds_draw",
            "cumulative_odds_away" => "r.cumulative_odds_away",
            _ => $"{prefix}.{column}" // should never hit due to validation
        };
    }

    private string MapOddsColumn(string column)
    {
        return column.ToLowerInvariant() switch
        {
            "home_odds" => "m.home_odds",
            "draw_odds" => "m.draw_odds",
            "away_odds" => "m.away_odds",
            _ => "m.home_odds"
        };
    }

    private string ResolveColumnReference(string column)
    {
        // Check if it matches a metric alias
        foreach (var metric in _spec.Metrics)
        {
            var alias = metric.Alias ?? $"{metric.Type}_{metric.Column ?? metric.Result ?? "all"}";
            if (alias.Equals(column, StringComparison.OrdinalIgnoreCase))
                return SanitizeAlias(alias);
        }

        // Check if it matches a dimension
        if (_spec.GroupBy != null && _spec.GroupBy.Contains(column, StringComparer.OrdinalIgnoreCase))
            return MapDimensionToGroupBySql(column);

        // Ordinal reference (1-based)
        if (int.TryParse(column, out var ordinal) && ordinal > 0)
            return ordinal.ToString();

        // Default: try as alias
        return SanitizeAlias(column);
    }

    private bool IsMatchSource() =>
        _spec.DataSource.Equals("matches", StringComparison.OrdinalIgnoreCase);

    private string AddParam(object value)
    {
        var name = $"@p{_paramIndex++}";
        _parameters.Add(new NpgsqlParameter(name, value));
        return name;
    }

    private string AddArrayParam(Guid[] values)
    {
        var name = $"@p{_paramIndex++}";
        _parameters.Add(new NpgsqlParameter(name, values));
        return name;
    }

    private string AddArrayParam(string[] values)
    {
        var name = $"@p{_paramIndex++}";
        _parameters.Add(new NpgsqlParameter(name, values));
        return name;
    }

    private static string SanitizeAlias(string alias)
    {
        // Only allow alphanumeric and underscore
        return new string(alias.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
    }
}
