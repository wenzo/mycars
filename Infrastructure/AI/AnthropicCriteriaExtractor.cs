using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MyCars.Configuration;
using MyCars.Domain.Interfaces;
using MyCars.Domain.Models;
using Microsoft.Extensions.Options;

namespace MyCars.Infrastructure.AI;

public sealed class AnthropicCriteriaExtractor : ICriteriaExtractor
{
    private readonly HttpClient _http;
    private readonly AiProviderOptions _opts;

    private static readonly JsonSerializerOptions _requestJson = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AnthropicCriteriaExtractor(IHttpClientFactory factory, IOptions<AiOptions> aiOpts)
    {
        _opts = aiOpts.Value.Providers.GetValueOrDefault("Anthropic") ?? new AiProviderOptions();
        _http = factory.CreateClient();
        _http.DefaultRequestHeaders.Add("x-api-key", _opts.ApiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<SearchCriteria?> ExtractAsync(string userQuery, CancellationToken ct)
    {
        var body = new
        {
            model      = _opts.Model,
            max_tokens = _opts.MaxTokens,
            system     = "Sei un assistente per la ricerca di veicoli in un'app per concessionarie italiane. " +
                         "Analizza la richiesta dell'utente ed estrai i criteri strutturati chiamando lo strumento fornito.",
            tools      = new[] { CriteriaToolSchema.AnthropicTool() },
            tool_choice = new { type = "tool", name = CriteriaToolSchema.ToolName },
            messages   = new[]
            {
                new { role = "user", content = userQuery }
            }
        };

        using var request = new StringContent(
            JsonSerializer.Serialize(body, _requestJson),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.PostAsync(_opts.BaseUrl, request, ct);
        if (!response.IsSuccessStatusCode) return null;

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        // Risposta Anthropic: content[].type == "tool_use" -> .input è già un oggetto
        if (!doc.RootElement.TryGetProperty("content", out var contentArray))
            return null;

        foreach (var block in contentArray.EnumerateArray())
        {
            if (!block.TryGetProperty("type", out var typeEl)) continue;
            if (typeEl.GetString() != "tool_use") continue;
            if (!block.TryGetProperty("input", out var inputEl)) continue;

            return JsonSerializer.Deserialize<SearchCriteria>(
                inputEl.GetRawText(), CriteriaToolSchema._caseInsensitive);
        }

        return null;
    }
}
