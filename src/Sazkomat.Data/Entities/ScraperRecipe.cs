using Sazkomat.Core.Entities;

namespace Sazkomat.Data.Entities;

/// <summary>
/// Configurable scraping recipe with actions and parsing rules.
/// Enables adaptive fallback - when one recipe fails, try the next.
/// </summary>
public class ScraperRecipe : Entity
{
    /// <summary>
    /// Human-readable name, e.g., "BetExplorer Default"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this recipe does
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Provider code, e.g., "betexplorer"
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Page type this recipe handles, e.g., "results"
    /// </summary>
    public string PageType { get; set; } = string.Empty;

    /// <summary>
    /// Priority order (1 = try first, higher = try later)
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Whether this recipe is active and should be tried
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// JSON array of DebugAction objects (navigate, click, wait, extractHtml, etc.)
    /// Supports variable substitution: {baseUrl}, {season}
    /// </summary>
    public string ActionsJson { get; set; } = "[]";

    // Parsing rules (XPath/CSS selectors for HTML parsing)

    /// <summary>
    /// XPath selector to find round headers, e.g., ".//th[contains(text(), 'Round')]"
    /// </summary>
    public string RoundHeaderSelector { get; set; } = ".//th[contains(text(), 'Round')]";

    /// <summary>
    /// Regex pattern to extract group name from round header (for grouped leagues).
    /// E.g., "^(.+?)\\s*-\\s*(\\d+)\\.\\s*Round$" for "East - 1. Round"
    /// Capture groups: (1) = group name, (2) = round number
    /// </summary>
    public string? GroupPatternRegex { get; set; }

    /// <summary>
    /// XPath selector to find match rows within a round
    /// </summary>
    public string MatchRowSelector { get; set; } = ".//tr[td[contains(@class, 'h-text-left')]]";

    /// <summary>
    /// XPath/CSS selector for odds cells (optional)
    /// </summary>
    public string? OddsCellSelector { get; set; }

    /// <summary>
    /// Optional hint key that must be "true" in accumulated variables
    /// from previous recipe executions for this recipe to run.
    /// Null = no prerequisite.
    /// </summary>
    public string? RequiresHint { get; set; }

    // Statistics (denormalized for quick access)

    /// <summary>
    /// Total number of times this recipe was attempted
    /// </summary>
    public int TotalAttempts { get; set; } = 0;

    /// <summary>
    /// Number of successful attempts
    /// </summary>
    public int SuccessfulAttempts { get; set; } = 0;

    /// <summary>
    /// Success rate (SuccessfulAttempts / TotalAttempts)
    /// </summary>
    public decimal SuccessRate => TotalAttempts > 0
        ? (decimal)SuccessfulAttempts / TotalAttempts
        : 0;
}
