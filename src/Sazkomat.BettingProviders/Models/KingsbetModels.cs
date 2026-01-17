using System.Text.Json.Serialization;

namespace Sazkomat.BettingProviders.Models;

/// <summary>
/// Root response from Kingsbet/Altenar GetSportMenu API
/// </summary>
public class KingsbetSportMenuResponse
{
    [JsonPropertyName("sports")]
    public List<KingsbetSport> Sports { get; set; } = new();

    [JsonPropertyName("categories")]
    public List<KingsbetCategory> Categories { get; set; } = new();

    [JsonPropertyName("champs")]
    public List<KingsbetChampionship> Championships { get; set; } = new();
}

/// <summary>
/// Sport info from Kingsbet (e.g., Fotbal = 66, Hokej = 70)
/// </summary>
public class KingsbetSport
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("typeId")]
    public int TypeId { get; set; }

    [JsonPropertyName("iconName")]
    public string? IconName { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("hasLiveEvents")]
    public bool HasLiveEvents { get; set; }

    /// <summary>
    /// List of category IDs that belong to this sport
    /// </summary>
    [JsonPropertyName("catIds")]
    public List<int> CategoryIds { get; set; } = new();
}

/// <summary>
/// Category (country/region) from Kingsbet
/// Categories contain championship IDs (champIds)
/// </summary>
public class KingsbetCategory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-3 country code (e.g., CZE, ENG, FRA)
    /// Empty for international categories like "Evropa", "Svět"
    /// </summary>
    [JsonPropertyName("iso")]
    public string? Iso { get; set; }

    [JsonPropertyName("eventsCount")]
    public int EventsCount { get; set; }

    [JsonPropertyName("hasLiveEvents")]
    public bool HasLiveEvents { get; set; }

    /// <summary>
    /// List of championship (league) IDs in this category
    /// </summary>
    [JsonPropertyName("champIds")]
    public List<int> ChampionshipIds { get; set; } = new();
}

/// <summary>
/// Championship (league) from Kingsbet
/// </summary>
public class KingsbetChampionship
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("eventsCount")]
    public int EventsCount { get; set; }

    [JsonPropertyName("hasLiveEvents")]
    public bool HasLiveEvents { get; set; }

    /// <summary>
    /// Category ID this championship belongs to (not in API, resolved via lookup)
    /// </summary>
    [JsonIgnore]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Category name (resolved via lookup)
    /// </summary>
    [JsonIgnore]
    public string? CategoryName { get; set; }

    /// <summary>
    /// ISO code from parent category (resolved via lookup)
    /// </summary>
    [JsonIgnore]
    public string? CategoryIso { get; set; }
}
