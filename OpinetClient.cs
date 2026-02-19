using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public sealed class OpinetClient
{
    private readonly SchedulerSettings _settings;
    private readonly ILogger<OpinetClient> _logger;
    private readonly HttpClient _httpClient;

    public OpinetClient(SchedulerSettings settings, ILogger<OpinetClient> logger)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = new HttpClient { BaseAddress = new Uri(_settings.Api.BaseUrl), Timeout = TimeSpan.FromSeconds(20) };
    }

    public async Task<JsonDocument> GetJsonAsync(string apiKey, string path, Dictionary<string, string> query, CancellationToken ct)
    {
        var qs = new Dictionary<string, string>(query)
        {
            ["code"] = apiKey,
            ["out"] = "json"
        };

        var uri = BuildUri(path, qs);
        _logger.LogInformation("GET {Uri}", uri);

        using var res = await _httpClient.GetAsync(uri, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"HTTP {(int)res.StatusCode}: {body}");

        return JsonDocument.Parse(body);
    }

    private static string BuildUri(string path, Dictionary<string, string> query)
    {
        var sb = new StringBuilder(path);
        sb.Append('?');
        sb.Append(string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}")));
        return sb.ToString();
    }
}
