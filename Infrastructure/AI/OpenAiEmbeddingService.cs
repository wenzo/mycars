using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MyCars.Configuration;
using MyCars.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyCars.Infrastructure.AI;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient      _http;
    private readonly string?         _apiKey;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    private const string EmbeddingModel = "text-embedding-3-small";
    private const string EmbeddingUrl   = "https://api.openai.com/v1/embeddings";

    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

    public OpenAiEmbeddingService(
        HttpClient http,
        IOptions<AiOptions> aiOpts,
        ILogger<OpenAiEmbeddingService> logger)
    {
        _logger = logger;
        _apiKey = aiOpts.Value.Providers.GetValueOrDefault("OpenAI")?.ApiKey;
        _http   = http;
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<float[]?> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("OpenAI embedding: ApiKey non configurata.");
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, EmbeddingUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { input = text, model = EmbeddingModel }),
            Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("OpenAI embedding HTTP {Status}: {Err}",
                (int)response.StatusCode, err[..Math.Min(300, err.Length)]);
            return null;
        }

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
            return null;

        var embEl = data[0].GetProperty("embedding");
        var vec   = new float[embEl.GetArrayLength()];
        int i = 0;
        foreach (var e in embEl.EnumerateArray())
            vec[i++] = e.GetSingle();

        _logger.LogDebug("Embedding generato: {Dims} dimensioni per '{Text}'",
            vec.Length, text[..Math.Min(60, text.Length)]);
        return vec;
    }
}
