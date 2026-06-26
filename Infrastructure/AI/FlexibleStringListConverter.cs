using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyCars.Infrastructure.AI;

// I modelli LLM a volte restituiscono una singola stringa invece di un array
// per campi definiti come array (es. "suv" invece di ["suv"]).
// Questo converter gestisce string | array | null uniformemente.
internal sealed class FlexibleStringListConverter : JsonConverter<List<string>?>
{
    public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                var s = reader.GetString();
                return string.IsNullOrWhiteSpace(s) ? null : [s];

            case JsonTokenType.StartArray:
                var list = new List<string>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var item = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(item)) list.Add(item!);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                return list.Count > 0 ? list : null;

            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, List<string>? value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStartArray();
        foreach (var item in value) writer.WriteStringValue(item);
        writer.WriteEndArray();
    }
}
