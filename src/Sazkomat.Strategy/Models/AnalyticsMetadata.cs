namespace Sazkomat.Strategy.Models;

public class AnalyticsMetadata
{
    public List<string> DataSources { get; set; } = new();
    public List<DimensionInfo> Dimensions { get; set; } = new();
    public List<MetricInfo> MetricTypes { get; set; } = new();
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<string> Operators { get; set; } = new();
}

public class DimensionInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MetricInfo
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresColumn { get; set; }
    public bool RequiresResult { get; set; }
}

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
