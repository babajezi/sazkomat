using System.Text.Json.Serialization;

namespace Sazkomat.Data.Debug;

/// <summary>
/// Base class for debug actions with JSON polymorphism.
/// Uses custom DebugActionConverter to handle "type" discriminator at any position in JSON.
/// </summary>
[JsonConverter(typeof(DebugActionConverter))]
public abstract class DebugAction
{
    // Type is determined by JSON discriminator, no property needed
    [JsonIgnore]
    public abstract string ActionType { get; }
}

public class NavigateAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "navigate";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

public class WaitAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "wait";

    [JsonPropertyName("milliseconds")]
    public int Milliseconds { get; set; } = 1000;
}

public class WaitForSelectorAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "waitForSelector";

    [JsonPropertyName("selector")]
    public string Selector { get; set; } = "";

    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 30000;

    /// <summary>
    /// State to wait for: visible, hidden, attached, detached
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }
}

public class WaitForLoadStateAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "waitForLoadState";

    /// <summary>
    /// State: load, domcontentloaded, networkidle
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = "networkidle";

    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 30000;
}

public class ClickAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "click";

    [JsonPropertyName("selector")]
    public string Selector { get; set; } = "";
}

public class TypeTextAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "typeText";

    [JsonPropertyName("selector")]
    public string Selector { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public class SelectAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "select";

    [JsonPropertyName("selector")]
    public string Selector { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

public class ScreenshotAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "screenshot";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "screenshot";
}

public class LogElementsAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "logElements";

    [JsonPropertyName("selector")]
    public string Selector { get; set; } = "";

    [JsonPropertyName("attributes")]
    public List<string>? Attributes { get; set; }

    [JsonPropertyName("extractText")]
    public bool ExtractText { get; set; } = false;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;
}

public class ExtractHtmlAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "extractHtml";

    /// <summary>
    /// CSS selector. If null, extracts entire page HTML.
    /// </summary>
    [JsonPropertyName("selector")]
    public string? Selector { get; set; }

    /// <summary>
    /// Max characters to return (default 50000)
    /// </summary>
    [JsonPropertyName("maxLength")]
    public int MaxLength { get; set; } = 50000;
}

public class EvaluateAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "evaluate";

    [JsonPropertyName("script")]
    public string Script { get; set; } = "";

    /// <summary>
    /// Optional variable name to store the result for later use
    /// </summary>
    [JsonPropertyName("storeAs")]
    public string? StoreAs { get; set; }
}

public class NavigateToVariableAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "navigateToVariable";

    /// <summary>
    /// Name of the stored variable containing the URL
    /// </summary>
    [JsonPropertyName("variable")]
    public string Variable { get; set; } = "";
}

public class ScrollAction : DebugAction
{
    [JsonIgnore]
    public override string ActionType => "scroll";

    /// <summary>
    /// Direction: top, bottom, up, down
    /// </summary>
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "bottom";

    /// <summary>
    /// Pixels to scroll (only for up/down)
    /// </summary>
    [JsonPropertyName("pixels")]
    public int? Pixels { get; set; }
}
