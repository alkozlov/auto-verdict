using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class FlexibleStringListJsonConverter : JsonConverter<IReadOnlyList<string>>
{
    public override IReadOnlyList<string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return [];

        if (reader.TokenType != JsonTokenType.StartArray)
            return [ReadValueAsString(ref reader)];

        var values = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return values;

            var value = ReadValueAsString(ref reader);
            if (!string.IsNullOrWhiteSpace(value))
                values.Add(value);
        }

        throw new JsonException("Unterminated string list array.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<string> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
            writer.WriteStringValue(item);
        writer.WriteEndArray();
    }

    private static string ReadValueAsString(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Number => reader.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.StartObject or JsonTokenType.StartArray => ReadContainerAsJson(ref reader),
            _ => string.Empty,
        };
    }

    private static string ReadContainerAsJson(ref Utf8JsonReader reader)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }
}
