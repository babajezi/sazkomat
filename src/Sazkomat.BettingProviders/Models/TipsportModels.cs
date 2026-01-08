using System.Text.Json.Serialization;

namespace Sazkomat.BettingProviders.Models;

/// <summary>
/// Root response from Tipsport REST API /rest/offer/v6/sports
/// </summary>
public class TipsportResponse
{
    [JsonPropertyName("data")]
    public TipsportRootNode Data { get; set; } = new();
}

/// <summary>
/// Root node containing sport hierarchy
/// </summary>
public class TipsportRootNode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("children")]
    public List<TipsportNode> Children { get; set; } = new();
}

/// <summary>
/// Generic node in Tipsport tree structure.
/// Can be: CATEGORY, SUPERSPORT, SPORT, SUPERGROUP, COMPETITION
/// </summary>
public class TipsportNode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("children")]
    public List<TipsportNode> Children { get; set; } = new();

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("superSportId")]
    public int? SuperSportId { get; set; }

    [JsonPropertyName("competitionAnnualId")]
    public int? CompetitionAnnualId { get; set; }

    [JsonPropertyName("groupId")]
    public int? GroupId { get; set; }

    [JsonPropertyName("communityStatsEnabled")]
    public bool? CommunityStatsEnabled { get; set; }

    [JsonPropertyName("tournamentTreeAvailable")]
    public bool? TournamentTreeAvailable { get; set; }

    [JsonPropertyName("treeAvailable")]
    public bool? TreeAvailable { get; set; }

    [JsonPropertyName("sportMass")]
    public string? SportMass { get; set; }

    [JsonPropertyName("sportGender")]
    public string? SportGender { get; set; }

    [JsonPropertyName("inMySelection")]
    public bool? InMySelection { get; set; }

    [JsonPropertyName("mySelectionId")]
    public int? MySelectionId { get; set; }

    [JsonPropertyName("offerIcon")]
    public TipsportOfferIcon? OfferIcon { get; set; }
}

/// <summary>
/// Icon information for sport/category
/// </summary>
public class TipsportOfferIcon
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("offerIconType")]
    public string OfferIconType { get; set; } = string.Empty;

    [JsonPropertyName("offerIconName")]
    public string OfferIconName { get; set; } = string.Empty;
}

/// <summary>
/// Tipsport node types
/// </summary>
public static class TipsportNodeType
{
    public const string Root = "ROOT";
    public const string Category = "CATEGORY";
    public const string SuperSport = "SUPERSPORT";
    public const string Sport = "SPORT";
    public const string SuperGroup = "SUPERGROUP";
    public const string Competition = "COMPETITION";
}

/// <summary>
/// Tipsport SuperSport IDs (sport categories)
/// </summary>
public static class TipsportSuperSportId
{
    public const int Football = 16;
    public const int Hockey = 17;
    public const int Basketball = 18;
    public const int Tennis = 19;
    public const int Handball = 20;
    public const int Volleyball = 21;
}

/// <summary>
/// Extracted competition (league) from Tipsport
/// </summary>
public class TipsportCompetition
{
    /// <summary>
    /// Unique competition ID (e.g., 118 for "1. anglická liga")
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Czech name of the competition (e.g., "1. anglická liga")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL path (e.g., "/vysledky/fotbal/fotbal-muzi/1-anglicka-liga-118")
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Annual competition ID for current season
    /// </summary>
    public int? CompetitionAnnualId { get; set; }

    /// <summary>
    /// SuperSport ID (16 = Football)
    /// </summary>
    public int SuperSportId { get; set; }

    /// <summary>
    /// Number of matches/events
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Parent sport category (e.g., "Fotbal - muži")
    /// </summary>
    public string? ParentSportTitle { get; set; }

    /// <summary>
    /// Parent super group title (e.g., "Házená ženy - MS")
    /// </summary>
    public string? ParentSuperGroupTitle { get; set; }

    /// <summary>
    /// Whether community stats are available
    /// </summary>
    public bool CommunityStatsEnabled { get; set; }
}
