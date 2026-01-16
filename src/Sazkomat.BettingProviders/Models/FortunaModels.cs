using System.Text.Json.Serialization;

namespace Sazkomat.BettingProviders.Models;

/// <summary>
/// Root response from Fortuna page data extraction
/// </summary>
public class FortunaData
{
    /// <summary>
    /// List of country/region groups (e.g., "Anglie", "Německo", etc.)
    /// </summary>
    public List<FortunaCountryGroup> CountryGroups { get; set; } = new();

    /// <summary>
    /// Favorites/featured leagues (to be filtered out)
    /// </summary>
    public List<FortunaFavoriteLeague> Favorites { get; set; } = new();
}

/// <summary>
/// A country/region group containing leagues
/// </summary>
public class FortunaCountryGroup
{
    /// <summary>
    /// Country/region name (e.g., "Anglie", "Německo")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Country code extracted from URL or derived from name
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// URL path for the country (e.g., "/sazeni/fotbal/anglie")
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Leagues within this country
    /// </summary>
    public List<FortunaLeague> Leagues { get; set; } = new();

    /// <summary>
    /// Whether this is an excluded group (Mezinárodní, eSport, Exhibice)
    /// </summary>
    public bool IsExcluded { get; set; } = false;
}

/// <summary>
/// Individual league info
/// </summary>
public class FortunaLeague
{
    /// <summary>
    /// League name (e.g., "Premier League", "Championship")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// League URL path (e.g., "/sazeni/fotbal/anglie/premier-league")
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// League ID extracted from URL or data attribute
    /// </summary>
    public string? LeagueId { get; set; }

    /// <summary>
    /// Number of available matches (if shown)
    /// </summary>
    public int? MatchCount { get; set; }

    /// <summary>
    /// Parent country code
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Parent country name
    /// </summary>
    public string? CountryName { get; set; }
}

/// <summary>
/// Favorite/featured league (to be filtered out - no country context)
/// </summary>
public class FortunaFavoriteLeague
{
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
}

/// <summary>
/// Raw DOM element data extracted from Fortuna page
/// </summary>
public class FortunaRawElement
{
    public string TagName { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public string? DataId { get; set; }
    public string? Href { get; set; }
    public string? Text { get; set; }
    public List<FortunaRawElement> Children { get; set; } = new();
}

/// <summary>
/// Result of Fortuna page exploration
/// </summary>
public class FortunaExplorationResult
{
    /// <summary>
    /// Whether JSON state was found in the page (like Betano's initial_state)
    /// </summary>
    public bool HasEmbeddedJson { get; set; }

    /// <summary>
    /// Raw JSON if found
    /// </summary>
    public string? RawJson { get; set; }

    /// <summary>
    /// Captured API URLs from network requests
    /// </summary>
    public List<string> ApiUrls { get; set; } = new();

    /// <summary>
    /// Extracted country groups from DOM
    /// </summary>
    public List<FortunaCountryGroup> ExtractedGroups { get; set; } = new();

    /// <summary>
    /// HTML sample for debugging
    /// </summary>
    public string? HtmlSample { get; set; }

    /// <summary>
    /// CSS class patterns found in the page
    /// </summary>
    public List<string> RelevantCssClasses { get; set; } = new();
}
