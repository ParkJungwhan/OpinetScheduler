using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace OpinetScheduler.Services;

public class OpinetClient
{
    private static readonly Uri DefaultBaseUri = new("https://www.opinet.co.kr");
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _apiKey;

    public OpinetClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress is null)
            _http.BaseAddress = DefaultBaseUri;
        _http.Timeout = TimeSpan.FromSeconds(15);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _apiKey = Environment.GetEnvironmentVariable("OPINET_API_KEY")
                  ?? throw new InvalidOperationException("환경변수 OPINET_API_KEY 가 설정되어 있지 않습니다.");
    }

    // 공통 GET 호출 (RESULT.OIL 형태의 응답 래핑)
    public async Task<OpinetEnvelope<T>> GetAsync<T>(string path, IDictionary<string, string?> query, CancellationToken ct = default)
    {
        var uri = BuildUri(path, query);
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);

        // 간단 리트라이 (최대 3회, 300ms, 900ms 백오프)
        Exception? lastEx = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                if (res.StatusCode == HttpStatusCode.TooManyRequests || (int)res.StatusCode == 429)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(300 * Math.Pow(3, attempt)), ct);
                    continue;
                }
                res.EnsureSuccessStatusCode();
                await using var stream = await res.Content.ReadAsStreamAsync(ct);
                var payload = await JsonSerializer.DeserializeAsync<OpinetEnvelope<T>>(stream, _jsonOptions, ct);
                if (payload is null)
                    throw new InvalidOperationException("응답 역직렬화에 실패했습니다.");
                return payload;
            }
            catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException or InvalidOperationException)
            {
                lastEx = ex;
                if (attempt == 2) break;
                await Task.Delay(TimeSpan.FromMilliseconds(300 * Math.Pow(3, attempt)), ct);
            }
        }
        throw new InvalidOperationException($"Opinet 호출 실패: {path}", lastEx);
    }

    // 편의 메서드: 전국 평균가 (avgAllPrice)
    public Task<OpinetEnvelope<AvgAllPriceItem>> GetAvgAllPriceAsync(CancellationToken ct = default)
        => GetAsync<AvgAllPriceItem>("/api/avgAllPrice.do", new Dictionary<string, string?>(), ct);

    // 편의 메서드: 시도별 평균가 (선택 파라미터 포함)
    public Task<OpinetEnvelope<SidoPriceItem>> GetAvgSidoPriceAsync(string? sido = null, string? prodcd = null, CancellationToken ct = default)
        => GetAsync<SidoPriceItem>("/api/avgSidoPrice.do", new Dictionary<string, string?>
        {
            ["sido"] = sido,
            ["prodcd"] = prodcd
        }, ct);

    private Uri BuildUri(string path, IDictionary<string, string?> query)
    {
        var qp = new List<string>
        {
            $"code={Uri.EscapeDataString(_apiKey)}",
            "out=json"
        };
        foreach (var kv in query)
        {
            if (string.IsNullOrWhiteSpace(kv.Value)) continue;
            qp.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        }
        var baseUri = _http.BaseAddress ?? DefaultBaseUri;
        var delimiter = path.StartsWith('/') ? string.Empty : "/";
        var uri = new Uri(baseUri, $"{path}{(path.Contains('?') ? "&" : "?")}{string.Join("&", qp)}");
        return uri;
    }
}

// 공통 응답 래퍼 (RESULT.OIL)
public class OpinetEnvelope<T>
{
    public OpinetResult<T>? RESULT { get; set; }
}

public class OpinetResult<T>
{
    public List<T>? OIL { get; set; }
}

// 샘플 DTO들 (필요 시 추가)
public class AvgAllPriceItem
{
    public string? TRADE_DT { get; set; }
    public string? PRODCD { get; set; }
    public string? PRODNM { get; set; }
    public string? PRICE { get; set; }
    public string? DIFF { get; set; }
}

public class SidoPriceItem
{
    public string? SIDOCD { get; set; }
    public string? SIDONM { get; set; }
    public string? PRODCD { get; set; }
    public string? PRICE { get; set; }
    public string? DIFF { get; set; }
}

