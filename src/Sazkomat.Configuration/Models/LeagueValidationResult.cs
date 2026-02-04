namespace Sazkomat.Configuration.Models;

/// <summary>
/// Result of validating a single league season within a league validation.
/// </summary>
public class SeasonValidationResultItem
{
    /// <summary>
    /// The ID of the league season.
    /// </summary>
    public Guid SeasonId { get; set; }

    /// <summary>
    /// Name of the season (e.g., "2020-2021").
    /// </summary>
    public required string SeasonName { get; set; }

    /// <summary>
    /// Whether the season passed all validation checks (no errors or warnings).
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Whether the season can be locked (no Error-level issues).
    /// </summary>
    public bool CanBeLocked { get; set; }

    /// <summary>
    /// List of validation issues found for this season.
    /// </summary>
    public List<ValidationIssue> Issues { get; set; } = [];
}

/// <summary>
/// Result of validating all historical seasons in a league.
/// </summary>
public class LeagueValidationResult
{
    /// <summary>
    /// Total number of historical seasons validated.
    /// </summary>
    public int TotalSeasons { get; set; }

    /// <summary>
    /// Number of seasons that passed validation without any issues.
    /// </summary>
    public int ValidSeasons { get; set; }

    /// <summary>
    /// Number of seasons with warnings (but no errors).
    /// </summary>
    public int SeasonsWithWarnings { get; set; }

    /// <summary>
    /// Number of seasons with errors.
    /// </summary>
    public int SeasonsWithErrors { get; set; }

    /// <summary>
    /// Number of seasons that can be locked (no errors).
    /// </summary>
    public int CanLockCount { get; set; }

    /// <summary>
    /// Number of seasons already locked.
    /// </summary>
    public int AlreadyLockedCount { get; set; }

    /// <summary>
    /// Detailed validation results for each season.
    /// </summary>
    public List<SeasonValidationResultItem> SeasonResults { get; set; } = [];
}
