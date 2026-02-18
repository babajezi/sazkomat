using Sazkomat.Core.Entities;

namespace Sazkomat.Data.Entities;

public class AnalyticalView : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SpecJson { get; set; } = "{}";
    public string? Tags { get; set; }
    public bool IsFavorite { get; set; }
    public int ExecutionCount { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public int? LastExecutionMs { get; set; }
}
