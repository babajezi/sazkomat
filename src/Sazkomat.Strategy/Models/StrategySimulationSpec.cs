using System.Text.Json;

namespace Sazkomat.Strategy.Models;

public class StrategySimulationSpec
{
    public string StrategyType { get; set; } = string.Empty;
    public JsonElement? Parameters { get; set; }
    public List<Guid>? LeagueIds { get; set; }
    public List<Guid>? CountryIds { get; set; }
    public List<string>? SeasonNames { get; set; }
    public bool? RequireOdds { get; set; }
    public int MinMatches { get; set; } = 4;
    public int? StartYear { get; set; }
}
