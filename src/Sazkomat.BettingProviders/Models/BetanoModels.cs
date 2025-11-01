using System.Text.Json.Serialization;

namespace Sazkomat.BettingProviders.Models;

/// <summary>
/// Root response from Betano window["initial_state"] JSON
/// </summary>
public class BetanoResponse
{
    [JsonPropertyName("data")]
    public BetanoData Data { get; set; } = new();
}

/// <summary>
/// Data container with top leagues and region groups
/// </summary>
public class BetanoData
{
    [JsonPropertyName("topLeagues")]
    public List<BetanoTopLeague> TopLeagues { get; set; } = new();

    [JsonPropertyName("regionGroups")]
    public List<BetanoRegionGroup> RegionGroups { get; set; } = new();
}

/// <summary>
/// Featured league (promoted in UI)
/// </summary>
public class BetanoTopLeague
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("regionName")]
    public string RegionName { get; set; } = string.Empty;

    [JsonPropertyName("regionCode")]
    public string RegionCode { get; set; } = string.Empty;

    [JsonPropertyName("regionId")]
    public string RegionId { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("flagUrl")]
    public string? FlagUrl { get; set; }
}

/// <summary>
/// Region group (category like "EVROPA – HLAVNÍ SOUTĚŽE")
/// </summary>
public class BetanoRegionGroup
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("expanded")]
    public bool Expanded { get; set; }

    [JsonPropertyName("regions")]
    public List<BetanoRegion> Regions { get; set; } = new();
}

/// <summary>
/// Region (country or special category like Champions League)
/// </summary>
public class BetanoRegion
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("regionCode")]
    public string RegionCode { get; set; } = string.Empty;

    [JsonPropertyName("flagUrl")]
    public string? FlagUrl { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("expanded")]
    public bool? Expanded { get; set; }

    [JsonPropertyName("hideLeagues")]
    public bool? HideLeagues { get; set; }

    [JsonPropertyName("leagues")]
    public List<BetanoLeague> Leagues { get; set; } = new();
}

/// <summary>
/// League info
/// </summary>
public class BetanoLeague
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("regionName")]
    public string? RegionName { get; set; }

    [JsonPropertyName("regionCode")]
    public string? RegionCode { get; set; }

    [JsonPropertyName("regionId")]
    public string? RegionId { get; set; }

    [JsonPropertyName("flagUrl")]
    public string? FlagUrl { get; set; }
}
