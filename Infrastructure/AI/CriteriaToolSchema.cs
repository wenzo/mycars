using System.Text.Json;
using MyCars.Domain.Models;

namespace MyCars.Infrastructure.AI;

// Schema dello strumento "cerca_veicoli" definito una sola volta e riusato dai tre extractor.
internal static class CriteriaToolSchema
{
    internal const string ToolName = "cerca_veicoli";
    internal const string ToolDescription =
        "Estrai i criteri strutturati per la ricerca di veicoli da una descrizione " +
        "in linguaggio naturale in italiano.";

    // Il documento viene mantenuto vivo dal campo statico; RootElement resta valido.
    // NOTA: i campi opzionali usano oneOf con null per evitare che i modelli
    //        restituiscano array vuoti o zero al posto di valori assenti.
    private static readonly JsonDocument _schemaDoc = JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "BodyType": {
              "oneOf": [
                { "type": "string", "enum": ["berlina","suv","station_wagon","city_car","monovolume","coupe","cabrio","pickup"] },
                {
                  "type": "array",
                  "items": { "type": "string", "enum": ["berlina","suv","station_wagon","city_car","monovolume","coupe","cabrio","pickup"] }
                },
                { "type": "null" }
              ],
              "description": "Tipo di carrozzeria. Per famiglie con bambini usa suv, station_wagon o monovolume. Null se non menzionato. Può essere una stringa singola o un array."
            },
            "FuelType": {
              "oneOf": [
                { "type": "string", "enum": ["benzina","diesel","gpl","metano","ibrida","elettrica"] },
                {
                  "type": "array",
                  "items": { "type": "string", "enum": ["benzina","diesel","gpl","metano","ibrida","elettrica"] }
                },
                { "type": "null" }
              ],
              "description": "Tipo di alimentazione. Null se non menzionato. Può essere una stringa singola o un array."
            },
            "PriceMax": {
              "oneOf": [{ "type": "number", "exclusiveMinimum": 0 }, { "type": "null" }],
              "description": "Prezzo massimo in euro. Null se non menzionato."
            },
            "PriceMin": {
              "oneOf": [{ "type": "number", "exclusiveMinimum": 0 }, { "type": "null" }],
              "description": "Prezzo minimo in euro. Null se non menzionato."
            },
            "MinSeats": {
              "oneOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }],
              "description": "Numero minimo di posti. 'Famiglia con due bambini' implica almeno 5. Null se non menzionato."
            },
            "Transmission": {
              "oneOf": [
                { "type": "string", "enum": ["manuale","automatico"] },
                { "type": "null" }
              ],
              "description": "Tipo di cambio. Null se non menzionato. NON usare array."
            },
            "Intent": {
              "type": "string",
              "enum": ["acquisto","noleggio","qualsiasi"],
              "description": "Intenzione dell'utente. Usa 'qualsiasi' se non specificato."
            },
            "Sort": {
              "type": "string",
              "enum": ["prezzo_asc","prezzo_desc","anno_desc","km_asc","rilevanza"],
              "description": "Ordinamento risultati. Usa 'rilevanza' se non specificato."
            }
          },
          "required": ["Intent","Sort"]
        }
        """);

    private static JsonElement ParametersSchema => _schemaDoc.RootElement;

    // Tool in formato Anthropic (input_schema)
    internal static object AnthropicTool() => new
    {
        name         = ToolName,
        description  = ToolDescription,
        input_schema = ParametersSchema
    };

    // Tool in formato OpenAI / Groq (function.parameters)
    internal static object OpenAiTool() => new
    {
        type     = "function",
        function = new
        {
            name        = ToolName,
            description = ToolDescription,
            parameters  = ParametersSchema
        }
    };

    // Parsing condiviso per risposte OpenAI-compatibili (Groq e OpenAI).
    // arguments arriva come stringa JSON da deserializzare.
    internal static SearchCriteria? ParseOpenAiResponse(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("choices", out var choices))
            return null;

        foreach (var choice in choices.EnumerateArray())
        {
            if (!choice.TryGetProperty("message", out var message)) continue;
            if (!message.TryGetProperty("tool_calls", out var toolCalls)) continue;

            foreach (var call in toolCalls.EnumerateArray())
            {
                if (!call.TryGetProperty("function", out var fn)) continue;
                if (!fn.TryGetProperty("arguments", out var argsEl)) continue;

                var argsJson = argsEl.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(argsJson)) continue;

                return JsonSerializer.Deserialize<SearchCriteria>(
                    argsJson, _caseInsensitive);
            }
        }

        return null;
    }

    internal static readonly JsonSerializerOptions _caseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FlexibleStringListConverter() }
    };
}
