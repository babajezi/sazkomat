using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Sazkomat.Core.Common;
using Sazkomat.Strategy.Models;

namespace Sazkomat.Strategy.Engine;

public class AnalyticsEngine
{
    private readonly string _connectionString;
    private readonly ILogger<AnalyticsEngine> _logger;

    public AnalyticsEngine(string connectionString, ILogger<AnalyticsEngine> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<Result<AnalyticsResult>> ExecuteAsync(ViewSpec spec)
    {
        var validation = SpecValidator.Validate(spec);
        if (!validation.IsSuccess)
            return Result<AnalyticsResult>.Failure(validation.Error!);

        string sql;
        List<NpgsqlParameter> parameters;

        if (!string.IsNullOrWhiteSpace(spec.CustomSql))
        {
            (sql, parameters) = BuildCustomSql(spec);
        }
        else
        {
            var builder = new AnalyticsSqlBuilder(spec);
            (sql, parameters) = builder.Build();
        }

        _logger.LogDebug("Analytics SQL: {Sql}", sql);

        var sw = Stopwatch.StartNew();

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            foreach (var param in parameters)
            {
                cmd.Parameters.Add(param);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            var columns = new List<ColumnDefinition>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(new ColumnDefinition
                {
                    Name = reader.GetName(i),
                    Type = MapPostgresType(reader.GetFieldType(i)),
                    Alias = reader.GetName(i)
                });
            }

            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value;
                }
                rows.Add(row);
            }

            sw.Stop();

            var result = new AnalyticsResult
            {
                Columns = columns,
                Rows = rows,
                TotalRows = rows.Count,
                ExecutionMs = (int)sw.ElapsedMilliseconds,
                Spec = spec
            };

            _logger.LogInformation("Analytics query executed: {Rows} rows in {Ms}ms", rows.Count, sw.ElapsedMilliseconds);

            return Result<AnalyticsResult>.Success(result);
        }
        catch (PostgresException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Analytics SQL error: {Message}\nSQL: {Sql}", ex.Message, sql);
            return Result<AnalyticsResult>.Failure($"SQL error: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Analytics execution error");
            return Result<AnalyticsResult>.Failure($"Execution error: {ex.Message}");
        }
    }

    private static readonly Regex SafeColumnName = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    public async Task<Result<List<DistinctValueItem>>> GetDistinctValuesAsync(ViewSpec spec, string column)
    {
        if (string.IsNullOrWhiteSpace(spec.CustomSql))
            return Result<List<DistinctValueItem>>.Failure("Distinct values are only supported for custom SQL queries.");

        if (!SafeColumnName.IsMatch(column))
            return Result<List<DistinctValueItem>>.Failure($"Invalid column name: '{column}'.");

        var innerSql = spec.CustomSql.TrimEnd().TrimEnd(';');
        var sql = $"SELECT \"{column}\"::text AS value, COUNT(*) AS cnt FROM ({innerSql}) _sub GROUP BY \"{column}\"::text ORDER BY 1 LIMIT 200";

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var values = new List<DistinctValueItem>();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                    values.Add(new DistinctValueItem
                    {
                        Value = reader.GetString(0),
                        Count = reader.GetInt64(1)
                    });
            }

            return Result<List<DistinctValueItem>>.Success(values);
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "Distinct values SQL error: {Message}\nSQL: {Sql}", ex.Message, sql);
            return Result<List<DistinctValueItem>>.Failure($"SQL error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Distinct values execution error");
            return Result<List<DistinctValueItem>>.Failure($"Execution error: {ex.Message}");
        }
    }

    private static (string sql, List<NpgsqlParameter> parameters) BuildCustomSql(ViewSpec spec)
    {
        var limit = spec.Limit ?? 1000;
        var innerSql = spec.CustomSql!.TrimEnd().TrimEnd(';');
        var parameters = new List<NpgsqlParameter>();

        var sql = $"SELECT * FROM ({innerSql}) _sub";

        // Apply column filters as WHERE clause
        if (spec.ColumnFilters is { Count: > 0 })
        {
            var conditions = new List<string>();
            var filterIndex = 0;
            foreach (var (columnName, values) in spec.ColumnFilters)
            {
                if (values.Count == 0 || !SafeColumnName.IsMatch(columnName))
                    continue;

                var paramName = $"p_filter_{filterIndex}";
                conditions.Add($"\"{columnName}\"::text = ANY(@{paramName})");
                parameters.Add(new NpgsqlParameter(paramName, values.ToArray()));
                filterIndex++;
            }

            if (conditions.Count > 0)
                sql += " WHERE " + string.Join(" AND ", conditions);
        }

        if (spec.Sort != null && SafeColumnName.IsMatch(spec.Sort.Column))
        {
            var direction = spec.Sort.Direction.Equals("asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
            sql += $" ORDER BY \"{spec.Sort.Column}\" {direction}";
        }

        sql += $" LIMIT {limit}";

        return (sql, parameters);
    }

    private static string MapPostgresType(Type type)
    {
        if (type == typeof(int) || type == typeof(long)) return "number";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "number";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "date";
        if (type == typeof(bool)) return "boolean";
        return "string";
    }
}
