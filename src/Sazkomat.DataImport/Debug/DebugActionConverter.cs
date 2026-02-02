using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sazkomat.DataImport.Debug;

/// <summary>
/// Custom JSON converter for DebugAction that handles polymorphic deserialization
/// regardless of the position of the "type" property in the JSON object.
///
/// System.Text.Json's built-in polymorphic support requires the type discriminator
/// to be at the beginning of the JSON object. This converter reads the entire object
/// first, finds the "type" property, and then deserializes to the correct concrete type.
/// </summary>
public class DebugActionConverter : JsonConverter<DebugAction>
{
    public override DebugAction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProperty))
        {
            throw new JsonException("Missing 'type' property in DebugAction JSON object");
        }

        var actionType = typeProperty.GetString();
        var json = root.GetRawText();

        // Create options without this converter to avoid infinite recursion
        var innerOptions = new JsonSerializerOptions(options);
        innerOptions.Converters.Clear();
        // Copy other settings but not converters
        foreach (var converter in options.Converters)
        {
            if (converter.GetType() != typeof(DebugActionConverter))
            {
                innerOptions.Converters.Add(converter);
            }
        }

        return actionType switch
        {
            "navigate" => JsonSerializer.Deserialize<NavigateAction>(json, innerOptions),
            "navigateToVariable" => JsonSerializer.Deserialize<NavigateToVariableAction>(json, innerOptions),
            "wait" => JsonSerializer.Deserialize<WaitAction>(json, innerOptions),
            "waitForSelector" => JsonSerializer.Deserialize<WaitForSelectorAction>(json, innerOptions),
            "waitForLoadState" => JsonSerializer.Deserialize<WaitForLoadStateAction>(json, innerOptions),
            "click" => JsonSerializer.Deserialize<ClickAction>(json, innerOptions),
            "typeText" => JsonSerializer.Deserialize<TypeTextAction>(json, innerOptions),
            "select" => JsonSerializer.Deserialize<SelectAction>(json, innerOptions),
            "screenshot" => JsonSerializer.Deserialize<ScreenshotAction>(json, innerOptions),
            "logElements" => JsonSerializer.Deserialize<LogElementsAction>(json, innerOptions),
            "extractHtml" => JsonSerializer.Deserialize<ExtractHtmlAction>(json, innerOptions),
            "evaluate" => JsonSerializer.Deserialize<EvaluateAction>(json, innerOptions),
            "scroll" => JsonSerializer.Deserialize<ScrollAction>(json, innerOptions),
            _ => throw new JsonException($"Unknown DebugAction type: '{actionType}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, DebugAction value, JsonSerializerOptions options)
    {
        // Serialize as the concrete type to include all properties
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
