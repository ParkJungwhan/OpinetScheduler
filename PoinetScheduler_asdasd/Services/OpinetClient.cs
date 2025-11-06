using System.Net;
using System.Net.Http;
using System.Text.Json;
using OpinetScheduler.Models;

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
    public async Task<OpinetEnvelope<T>> GetAsync<T>(
        string path,
        IDictionary<string, string?> query,
        CancellationToken ct = default)
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

    // 편의 메서드: 시군구별 평균가
    public Task<OpinetEnvelope<SigunPriceItem>> GetAvgSigunPriceAsync(string sido, string? sigun = null, string? prodcd = null, CancellationToken ct = default)
        => GetAsync<SigunPriceItem>("/api/avgSigunPrice.do", new Dictionary<string, string?>
        {
            ["sido"] = sido,
            ["sigun"] = sigun,
            ["prodcd"] = prodcd
        }, ct);

    // 편의 메서드: 최근 7일 전국 일일 평균가
    public Task<OpinetEnvelope<AvgRecentPriceItem>> GetAvgRecentPriceAsync(string? date = null, string? prodcd = null, CancellationToken ct = default)
        => GetAsync<AvgRecentPriceItem>("/api/avgRecentPrice.do", new Dictionary<string, string?>
        {
            ["date"] = date,
            ["prodcd"] = prodcd
        }, ct);

    // 편의 메서드: 최근 7일 상표별 평균가
    public Task<OpinetEnvelope<PollAvgRecentPriceItem>> GetPollAvgRecentPriceAsync(string? prodcd = null, string? pollcd = null, CancellationToken ct = default)
        => GetAsync<PollAvgRecentPriceItem>("/api/pollAvgRecentPrice.do", new Dictionary<string, string?>
        {
            ["prodcd"] = prodcd,
            ["pollcd"] = pollcd
        }, ct);

    // 편의 메서드: 최근 7일 지역별 평균가
    public Task<OpinetEnvelope<AreaAvgRecentPriceItem>> GetAreaAvgRecentPriceAsync(string area, string? date = null, string? prodcd = null, CancellationToken ct = default)
        => GetAsync<AreaAvgRecentPriceItem>("/api/areaAvgRecentPrice.do", new Dictionary<string, string?>
        {
            ["area"] = area,
            ["date"] = date,
            ["prodcd"] = prodcd
        }, ct);

    // 편의 메서드: 최근 1주 주간 평균가
    public Task<OpinetEnvelope<AvgLastWeekItem>> GetAvgLastWeekAsync(string? prodcd = null, string? sido = null, CancellationToken ct = default)
        => GetAsync<AvgLastWeekItem>("/api/avgLastWeek.do", new Dictionary<string, string?>
        {
            ["prodcd"] = prodcd,
            ["sido"] = sido
        }, ct);

    // 편의 메서드: 전국/지역별 최저가 Top N
    public Task<OpinetEnvelope<LowTopItem>> GetLowTopAsync(string prodcd, string? area = null, int? cnt = null, CancellationToken ct = default)
        => GetAsync<LowTopItem>("/api/lowTop10.do", new Dictionary<string, string?>
        {
            ["prodcd"] = prodcd,
            ["area"] = area,
            ["cnt"] = cnt?.ToString()
        }, ct);

    // 편의 메서드: 반경 내 검색
    public Task<OpinetEnvelope<AroundAllItem>> GetAroundAllAsync(double x, double y, int radiusMeters, string prodcd, int sort = 1, CancellationToken ct = default)
        => GetAsync<AroundAllItem>("/api/aroundAll.do", new Dictionary<string, string?>
        {
            ["x"] = x.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["y"] = y.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["radius"] = radiusMeters.ToString(),
            ["prodcd"] = prodcd,
            ["sort"] = sort.ToString()
        }, ct);

    // 편의 메서드: 상세정보(ID)
    public Task<OpinetEnvelope<DetailByIdItem>> GetDetailByIdAsync(string id, CancellationToken ct = default)
        => GetAsync<DetailByIdItem>("/api/detailById.do", new Dictionary<string, string?>
        {
            ["id"] = id
        }, ct);

    // 편의 메서드: 상호 검색
    public Task<OpinetEnvelope<SearchByNameItem>> SearchByNameAsync(string osnm, string? area = null, CancellationToken ct = default)
        => GetAsync<SearchByNameItem>("/api/searchByName.do", new Dictionary<string, string?>
        {
            ["osnm"] = osnm,
            ["area"] = area
        }, ct);

    // 편의 메서드: 요소수 가격(지역별)
    public Task<OpinetEnvelope<UreaPriceItem>> GetUreaPriceAsync(string area, CancellationToken ct = default)
        => GetAsync<UreaPriceItem>("/api/ureaPrice.do", new Dictionary<string, string?>
        {
            ["area"] = area
        }, ct);

    // 편의 메서드: 지역코드 조회
    public Task<OpinetEnvelope<AreaCodeItem>> GetAreaCodeAsync(string? area = null, CancellationToken ct = default)
        => GetAsync<AreaCodeItem>("/api/areaCode.do", new Dictionary<string, string?>
        {
            ["area"] = area
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