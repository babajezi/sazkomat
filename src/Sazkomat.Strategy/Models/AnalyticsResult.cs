namespace Sazkomat.Strategy.Models;

public class AnalyticsResult
{
    public List<ColumnDefinition> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int ExecutionMs { get; set; }
    public ViewSpec Spec { get; set; } = new();
}

public class ColumnDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? Alias { get; set; }
}

public class DistinctValueItem
{
    public string Value { get; set; } = string.Empty;
    public long Count { get; set; }
}
