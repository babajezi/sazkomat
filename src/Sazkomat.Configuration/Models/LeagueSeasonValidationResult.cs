namespace Sazkomat.Configuration.Models;

/// <summary>
/// Severity level of a validation issue.
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Warning - season can still be locked but user should be aware.
    /// </summary>
    Warning,

    /// <summary>
    /// Error - season cannot be locked until issue is resolved.
    /// </summary>
    Error
}

/// <summary>
/// A single validation issue found during league season validation.
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Unique code for the issue type.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Human-readable message describing the issue.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Severity of the issue.
    /// </summary>
    public IssueSeverity Severity { get; set; }
}

/// <summary>
/// Result of validating a league season.
/// </summary>
public class LeagueSeasonValidationResult
{
    /// <summary>
    /// Whether the season passed all validation checks (no errors or warnings).
    /// </summary>
    public bool IsValid => Issues.Count == 0;

    /// <summary>
    /// List of validation issues found.
    /// </summary>
    public List<ValidationIssue> Issues { get; set; } = [];

    /// <summary>
    /// Whether the season can be locked (no Error-level issues).
    /// </summary>
    public bool CanBeLocked => Issues.All(i => i.Severity != IssueSeverity.Error);
}
