using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MyCars.Configuration;
using MyCars.Domain.Interfaces;
using MyCars.Domain.Models;
using Microsoft.Extensions.Options;

namespace MyCars.Infrastructure.AI;

public sealed class GroqCriteriaExtractor : ICriteriaExtractor
{
    private readonly HttpClient _http;
    private readonly AiProviderOptions _opts;

    private static readonly JsonSerializerOptions _requestJson = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GroqCriteriaExtractor(IHttpClientFactory factory, IOptions<AiOptions> aiOpts)
    {
        _opts = aiOpts.Value.Providers.GetValueOrDefault("Groq") ?? new AiProviderOptions();
        _http = factory.CreateClient();
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<SearchCriteria?> ExtractAsync(string userQuery, CancellationToken ct)
    {
        var body = new
        {
            model      = _opts.Model,
            max_tokens = _opts.MaxTokens,
            tools      = new[] { CriteriaToolSchema.OpenAiTool() },
            tool_choice = new
            {
                type     = "function",
                function = new { name = CriteriaToolSchema.ToolName }
            },
            messages = new object[]
            {
                new { role = "system", content =
                    "Sei un assistente per la ricerca di veicoli in un'app per concessionarie italiane. " +
                    "Analizza la richiesta dell'utente ed estrai i criteri strutturati chiamando lo strumento fornito. " +
                    "IMPORTANTE: per i campi opzionali non menzionati nella query usa null, NON array vuoti né zero." },
                new { role = "user", content = userQuery }
            }
        };

        using var request = new StringContent(
            JsonSerializer.Serialize(body, _requestJson),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.PostAsync(_opts.BaseUrl, request, ct);
        if (!response.IsSuccessStatusCode) return null;

        // Risposta Groq (formato OpenAI): arguments è una stringa JSON da deserializzare
        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        return CriteriaToolSchema.ParseOpenAiResponse(doc);
    }
}
