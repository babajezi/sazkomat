using System.Diagnostics;
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

        var builder = new AnalyticsSqlBuilder(spec);
        var (sql, parameters) = builder.Build();

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

    private static string MapPostgresType(Type type)
    {
        if (type == typeof(int) || type == typeof(long)) return "number";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "number";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "date";
        if (type == typeof(bool)) return "boolean";
        return "string";
    }
}
