using System.Text.Json.Serialization;

namespace Sazkomat.Data.Debug;

/// <summary>
/// Request for debug scraper execution
/// </summary>
public class DebugRequest
{
    [JsonPropertyName("actions")]
    public List<DebugAction> Actions { get; set; } = new();
}

/// <summary>
/// Result of entire debug session
/// </summary>
public class DebugSessionResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("totalDurationMs")]
    public long TotalDurationMs { get; set; }

    [JsonPropertyName("results")]
    public List<DebugStepResult> Results { get; set; } = new();

    [JsonPropertyName("logs")]
    public List<string> Logs { get; set; } = new();

    [JsonPropertyName("storedVariables")]
    public Dictionary<string, string> StoredVariables { get; set; } = new();
}

/// <summary>
/// Result of single debug step
/// </summary>
public class DebugStepResult
{
    [JsonPropertyName("step")]
    public int Step { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("details")]
    public object? Details { get; set; }
}

/// <summary>
/// Details for navigate action
/// </summary>
public class NavigateDetails
{
    [JsonPropertyName("finalUrl")]
    public string FinalUrl { get; set; } = "";
}

/// <summary>
/// Details for logElements action
/// </summary>
public class LogElementsDetails
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("elements")]
    public List<ElementInfo> Elements { get; set; } = new();
}

/// <summary>
/// Info about single element
/// </summary>
public class ElementInfo
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "";

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("class")]
    public string? Class { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, string>? Attributes { get; set; }
}

/// <summary>
/// Details for screenshot action
/// </summary>
public class ScreenshotDetails
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

/// <summary>
/// Details for extractHtml action
/// </summary>
public class ExtractHtmlDetails
{
    [JsonPropertyName("html")]
    public string Html { get; set; } = "";

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("truncated")]
    public bool Truncated { get; set; }
}

/// <summary>
/// Details for evaluate action
/// </summary>
public class EvaluateDetails
{
    [JsonPropertyName("result")]
    public object? Result { get; set; }
}

/// <summary>
/// Details for waitForSelector action
/// </summary>
public class WaitForSelectorDetails
{
    [JsonPropertyName("found")]
    public bool Found { get; set; }

    [JsonPropertyName("elementTag")]
    public string? ElementTag { get; set; }
}
