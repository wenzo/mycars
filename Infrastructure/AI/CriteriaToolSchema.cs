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
              "description": "Tipo di carrozzeria SOLO se esplicitamente menzionato. Valori: berlina, suv, station wagon, city car, monovolume, coupe, cabrio, fuoristrada. Null se non menzionato (NON inferire da 'famiglia' o 'spazio')."
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
              "description": "SOLO la casa costruttrice (BMW, Volkswagen, Fiat, Toyota, Ford, Renault...). NON includere il modello. Null se non menzionata."
            },
            "Model": {
              "oneOf": [{ "type": "string" }, { "type": "null" }],
              "description": "SOLO il modello specifico (Golf, Panda, Yaris, Serie 3, Duster...). Separato da Brand. Null se non menzionato."
            },
            "MinHorsepowerCv": {
              "oneOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }],
              "description": "Potenza minima in CV. Usa SOLO se esplicitamente richiesta o implicata da traino pesante. Esempi: 'traino roulotte' → 130, 'potente/sportiva' → 180. Null altrimenti."
            },
            "MaxHorsepowerCv": {
              "oneOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }],
              "description": "Potenza massima in CV. Utile per 'consumi contenuti' o 'piccola utilitaria'. Null se non menzionato."
            },
            "MinEngineCc": {
              "oneOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }],
              "description": "Cilindrata minima in cc SOLO se menzionata. 'media cilindrata' → 1400, 'grande cilindrata' → 2000. Null altrimenti."
            },
            "MaxEngineCc": {
              "oneOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }],
              "description": "Cilindrata massima in cc SOLO se menzionata. 'piccola cilindrata' → 1200. Null altrimenti."
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
              "description": "Chilometraggio massimo in km SOLO se menzionato. 'pochi km' → 30000, 'basso chilometraggio' → 50000. Null altrimenti."
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
              "description": "Numero minimo di posti SOLO se menzionato esplicitamente ('7 posti', 'tre file di sedili'). Null altrimenti."
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

    // System prompt condiviso (tutti e tre gli extractor lo usano identico).
    internal static string BuildSystemPrompt()
    {
        int y = DateTime.UtcNow.Year;
        return
            $"Sei un estrattore di criteri per la ricerca di veicoli in concessionarie italiane. " +
            $"Analizza la frase dell'utente e chiama lo strumento '{ToolName}' compilando TUTTI i campi che corrispondono a concetti menzionati.\n\n" +
            $"REGOLA: compila un campo se e solo se il concetto è menzionato; non inventare criteri non detti. " +
            $"Per tutto ciò che non viene menzionato usa null.\n\n" +
            $"Brand = SOLO la casa costruttrice (BMW, Fiat, Volkswagen...). " +
            $"Model = SOLO il modello specifico (Golf, Panda, Serie 3...). " +
            $"'Volkswagen Golf' → Brand:\"Volkswagen\", Model:\"Golf\".\n\n" +
            $"Anno corrente: {y}. " +
            $"'Non più vecchia di 2 anni' → MinYear={y - 2}. " +
            $"'Recente' → MinYear={y - 3}.\n\n" +
            $"ESEMPI:\n" +
            $"• 'SUV diesel automatico' → FuelType:[\"diesel\"], BodyType:[\"suv\"], Transmission:\"automatico\"\n" +
            $"• 'Volkswagen Golf usata sotto 15000 euro' → Brand:\"Volkswagen\", Model:\"Golf\", Condition:\"usato\", PriceMax:15000\n" +
            $"• 'voglio noleggiare una berlina ibrida' → Intent:\"noleggio\", BodyType:[\"berlina\"], FuelType:[\"ibrida\"]\n" +
            $"• 'auto non più vecchia di 3 anni con pochi km' → MinYear:{y - 3}, MaxMileageKm:50000\n" +
            $"• 'per azienda IVA detraibile' → VatDeductible:true\n" +
            $"• 'auto rossa' → Color:\"rosso\"\n\n" +
            $"Campi non menzionati → null. NON usare array vuoti né zero.";
    }

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
