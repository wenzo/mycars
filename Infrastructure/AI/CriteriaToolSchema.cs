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
                { "type": "string", "enum": ["berlina","suv","station wagon","city car","monovolume","coupe","cabrio","fuoristrada"] },
                {
                  "type": "array",
                  "items": { "type": "string", "enum": ["berlina","suv","station wagon","city car","monovolume","coupe","cabrio","fuoristrada"] }
                },
                { "type": "null" }
              ],
              "description": "Tipo di carrozzeria. Valori esatti: berlina (3 o 5 porte), suv, station wagon, city car, monovolume, coupe, cabrio, fuoristrada. Per famiglie con bambini usa suv, station wagon o monovolume. Null se non menzionato."
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
            "Brand": {
              "oneOf": [{ "type": "string" }, { "type": "null" }],
              "description": "Marca/costruttore del veicolo (es. BMW, Volkswagen, Fiat, Toyota, Ford). Null se non menzionata. NON includere il modello, solo la marca."
            },
            "MinHorsepowerCv": {
              "oneOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }],
              "description": "Potenza minima in CV. Guide: 'adeguata al traino di roulotte/caravan' → 130, 'potente' → 150, 'sportiva' o 'molto potente' → 200. Null se non menzionato."
            },
            "MaxHorsepowerCv": {
              "oneOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }],
              "description": "Potenza massima in CV. Utile per 'consumi contenuti' o 'piccola utilitaria'. Null se non menzionato."
            },
            "MinEngineCc": {
              "oneOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }],
              "description": "Cilindrata minima in cc. Guide: 'media cilindrata' → 1400, 'grande cilindrata' o 'motore importante' → 2000. Null se non menzionato."
            },
            "MaxEngineCc": {
              "oneOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }],
              "description": "Cilindrata massima in cc. Guide: 'piccola cilindrata' → 1200, 'media cilindrata' → 2000. Null se non menzionato."
            },
            "MinYear": {
              "oneOf": [{ "type": "integer" }, { "type": "null" }],
              "description": "Anno minimo di immatricolazione (incluso). Calcola sempre dall'anno corrente fornito nel contesto: 'non più vecchia di 2 anni' → annoCorrente-2, 'recente' → annoCorrente-3, 'nuova' → annoCorrente-1. Null se non menzionato."
            },
            "MaxYear": {
              "oneOf": [{ "type": "integer" }, { "type": "null" }],
              "description": "Anno massimo di immatricolazione (incluso). Es. 'di qualche anno fa' o 'non troppo recente'. Null se non menzionato."
            },
            "Color": {
              "oneOf": [{ "type": "string" }, { "type": "null" }],
              "description": "Colore del veicolo in italiano minuscolo (es. 'rosso', 'bianco', 'nero', 'grigio', 'blu', 'argento', 'verde', 'giallo', 'arancione'). Null se non menzionato."
            },
            "EmissionClass": {
              "oneOf": [{ "type": "string" }, { "type": "null" }],
              "description": "Classe di emissioni Euro (es. 'Euro 6', 'Euro 5', 'Euro 4'). Null se non menzionata."
            },
            "DescriptionKeyword": {
              "oneOf": [{ "type": "string" }, { "type": "null" }],
              "description": "Una singola parola o breve frase da cercare nel campo note/descrizione inserito dal rivenditore. Usa per caratteristiche particolari non strutturate: 'revisione effettuata', 'tagliando', 'tetto apribile', 'cerchi in lega', 'navigatore', 'telecamera posteriore', 'gancio traino'. Null se non menzionato."
            },
            "Condition": {
              "oneOf": [
                { "type": "string", "enum": ["nuovo","usato","km0"] },
                { "type": "null" }
              ],
              "description": "Stato del veicolo: 'nuovo' (mai immatricolato), 'usato' (già immatricolato), 'km0' (immatricolato ma mai venduto, spesso come da showroom). Null se non specificato."
            },
            "MaxMileageKm": {
              "oneOf": [{ "type": "integer", "minimum": 0 }, { "type": "null" }],
              "description": "Chilometraggio massimo in km. Guide: 'pochi km' → 30000, 'basso chilometraggio' → 50000, 'meno di X km' → X. Null se non menzionato."
            },
            "VatDeductible": {
              "oneOf": [{ "type": "boolean" }, { "type": "null" }],
              "description": "IVA esposta/detraibile. true se l'utente dice 'IVA detraibile', 'per azienda', 'per partita IVA', 'IVA esposta'. Null se non menzionato."
            },
            "HandicapAccessible": {
              "oneOf": [{ "type": "boolean" }, { "type": "null" }],
              "description": "Veicolo accessibile/attrezzato per portatori di handicap o disabilità. true se menzionato. Null se non menzionato."
            },
            "Imported": {
              "oneOf": [{ "type": "boolean" }, { "type": "null" }],
              "description": "Veicolo di importazione (provenienza estera). true se l'utente dice 'importata', 'proveniente dall'estero'. Null se non menzionato."
            },
            "Damaged": {
              "oneOf": [{ "type": "boolean" }, { "type": "null" }],
              "description": "Veicolo incidentato. false se l'utente dice 'non incidentata', 'senza danni', 'mai incidentata' (caso più comune). true se cerca specificamente auto incidentate. Null se non menzionato."
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
