using Sazkomat.Core.Entities;

namespace Sazkomat.Data.Entities;

public class StrategyScreening : Entity
{
    public string Name { get; set; } = string.Empty;
    public string StrategyType { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public int RoundsAnalyzed { get; set; }
    public DateTime CalculatedAt { get; set; }
}
