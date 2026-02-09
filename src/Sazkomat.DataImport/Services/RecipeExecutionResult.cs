namespace Sazkomat.DataImport.Services;

/// <summary>
/// Result of executing a scraper recipe
/// </summary>
public class RecipeExecutionResult
{
    /// <summary>
    /// Whether the recipe execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Extracted HTML content (if successful)
    /// </summary>
    public string? Html { get; set; }

    /// <summary>
    /// Execution logs for debugging
    /// </summary>
    public List<string> Logs { get; set; } = new();

    /// <summary>
    /// Error reason if failed
    /// </summary>
    public string? ErrorReason { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Variables stored during recipe execution (e.g., hints for orchestrator)
    /// </summary>
    public Dictionary<string, string> StoredVariables { get; set; } = new();

    public static RecipeExecutionResult Succeeded(string html, List<string> logs, long durationMs, Dictionary<string, string>? storedVariables = null)
    {
        return new RecipeExecutionResult
        {
            Success = true,
            Html = html,
            Logs = logs,
            DurationMs = durationMs,
            StoredVariables = storedVariables ?? new()
        };
    }

    public static RecipeExecutionResult Failed(string errorReason, List<string> logs, long durationMs, Dictionary<string, string>? storedVariables = null)
    {
        return new RecipeExecutionResult
        {
            Success = false,
            ErrorReason = errorReason,
            Logs = logs,
            DurationMs = durationMs,
            StoredVariables = storedVariables ?? new()
        };
    }
}

/// <summary>
/// Information about a recipe that was tried during sync
/// </summary>
public class TriedRecipeInfo
{
    public Guid RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string? Error { get; set; }
    public long DurationMs { get; set; }
}
