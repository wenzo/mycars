using System.Net.Http.Headers;
using System.Text;

namespace MyCars.Infrastructure.Http;

public sealed class SupabaseRestClient : ISupabaseRestClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    public SupabaseRestClient(HttpClient http, IOptions<SupabaseOptions> opts)
    {
        var o = opts.Value;

        if (string.IsNullOrWhiteSpace(o.Url))
            throw new InvalidOperationException("Supabase:Url non configurato.");
        if (string.IsNullOrWhiteSpace(o.ServiceRoleKey))
            throw new InvalidOperationException("Supabase:ServiceRoleKey non configurato.");

        _http = http;
        _http.BaseAddress = new Uri(o.Url.TrimEnd('/') + o.RestBasePath.TrimEnd('/') + "/");
        _http.Timeout     = TimeSpan.FromSeconds(o.HttpClient.TimeoutSeconds);
        _http.DefaultRequestHeaders.Add("apikey", o.ServiceRoleKey);
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", o.ServiceRoleKey);
    }

    public async Task<IReadOnlyList<T>> SelectAsync<T>(
        string table, string? filter = null, string? select = null,
        string? order = null, int? limit = null, int? offset = null)
    {
        var url = BuildUrl(table, filter, select, order, limit, offset);
        var response = await _http.GetAsync(url);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<T>>(json, _json) ?? [];
    }

    public async Task<T?> SelectOneAsync<T>(string table, string filter, string? select = null)
    {
        var list = await SelectAsync<T>(table, filter, select, limit: 1);
        return list.Count > 0 ? list[0] : default;
    }

    public async Task<T?> InsertAsync<T>(string table, object payload)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, table)
        {
            Content = Serialize(payload),
        };
        req.Headers.Add("Prefer", "return=representation");

        var response = await _http.SendAsync(req);
        await EnsureSuccessAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<T>>(json, _json);
        return list is { Count: > 0 } ? list[0] : default;
    }

    public async Task<T?> UpdateAsync<T>(string table, string filter, object payload)
    {
        var url = string.IsNullOrEmpty(filter) ? table : $"{table}?{filter}";
        using var req = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = Serialize(payload),
        };
        req.Headers.Add("Prefer", "return=representation");

        var response = await _http.SendAsync(req);
        await EnsureSuccessAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<T>>(json, _json);
        return list is { Count: > 0 } ? list[0] : default;
    }

    public async Task DeleteAsync(string table, string filter)
    {
        var url = $"{table}?{filter}";
        var response = await _http.DeleteAsync(url);
        await EnsureSuccessAsync(response);
    }

    public async Task<long> CountAsync(string table, string? filter = null)
    {
        var url = string.IsNullOrEmpty(filter) ? table : $"{table}?{filter}";
        using var req = new HttpRequestMessage(HttpMethod.Head, url);
        req.Headers.Add("Prefer", "count=exact");

        var response = await _http.SendAsync(req);
        await EnsureSuccessAsync(response);

        // PostgREST risponde con Content-Range: 0-{n-1}/{total}  oppure  */{total}
        if (response.Headers.TryGetValues("Content-Range", out var values))
        {
            var range = values.FirstOrDefault();
            if (range is not null)
            {
                var slash = range.IndexOf('/');
                if (slash >= 0 && long.TryParse(range[(slash + 1)..], out var count))
                    return count;
            }
        }
        return 0;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string BuildUrl(
        string table, string? filter, string? select,
        string? order, int? limit, int? offset)
    {
        var parts = new List<string>(6);
        if (!string.IsNullOrEmpty(select)) parts.Add($"select={Uri.EscapeDataString(select)}");
        if (!string.IsNullOrEmpty(filter)) parts.Add(filter.TrimStart('?'));
        if (!string.IsNullOrEmpty(order))  parts.Add($"order={order}");
        if (limit.HasValue)  parts.Add($"limit={limit}");
        if (offset.HasValue) parts.Add($"offset={offset}");
        return parts.Count > 0 ? $"{table}?{string.Join("&", parts)}" : table;
    }

    private static StringContent Serialize(object payload)
    {
        var json = JsonSerializer.Serialize(payload, _json);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new HttpRequestException(
            $"Supabase REST {response.StatusCode}: {body}",
            null,
            response.StatusCode);
    }
}
