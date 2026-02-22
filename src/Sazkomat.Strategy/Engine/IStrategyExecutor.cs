using System.Text.Json;
using Sazkomat.Strategy.Models;

namespace Sazkomat.Strategy.Engine;

public interface IStrategyExecutor
{
    string Type { get; }
    string Name { get; }
    string Description { get; }
    List<ParameterDefinition> GetParameterDefinitions();
    ScreeningResult Screen(List<RoundData> rounds, JsonElement? parameters);
    SimulationResult Simulate(List<RoundData> rounds, JsonElement? parameters);
}
