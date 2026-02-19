using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .ClearProviders()
        .AddSimpleConsole(options =>
        {
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            options.SingleLine = true;
        })
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("Main");

var settings = configuration.GetRequiredSection("SchedulerSettings").Get<SchedulerSettings>()
    ?? throw new InvalidOperationException("SchedulerSettings를 읽을 수 없습니다.");

await using var db = new NpgsqlConnection(settings.Database.ConnectionString);
await db.OpenAsync();

await DbBootstrap.InitializeAsync(db);

var keyProvider = new OpinetApiKeyProvider(settings, loggerFactory.CreateLogger<OpinetApiKeyProvider>());
var apiKey = await keyProvider.GetApiKeyAsync(db)
    ?? throw new InvalidOperationException("활성 Opinet API Key를 찾지 못했습니다. opinet_api_credentials를 확인하세요.");

logger.LogInformation("DB 연결 성공 / API Key 로딩 성공(길이:{Length})", apiKey.Length);

var scheduler = new OpinetScheduler(
    settings,
    loggerFactory.CreateLogger<OpinetScheduler>(),
    loggerFactory.CreateLogger<OpinetClient>(),
    loggerFactory.CreateLogger<SnapshotWriter>());

if (args.Contains("--once", StringComparer.OrdinalIgnoreCase))
{
    var smoke = args.Contains("--smoke", StringComparer.OrdinalIgnoreCase);
    await scheduler.RunOnceAsync(db, apiKey, smoke);
    return;
}

await scheduler.RunLoopAsync(db, apiKey, CancellationToken.None);


public sealed class OpinetScheduler
{
    private readonly SchedulerSettings _settings;
    private readonly ILogger<OpinetScheduler> _logger;
    private readonly OpinetClient _client;
    private readonly SnapshotWriter _writer;

    public OpinetScheduler(
        SchedulerSettings settings,
        ILogger<OpinetScheduler> logger,
        ILogger<OpinetClient> clientLogger,
        ILogger<SnapshotWriter> writerLogger)
    {
        _settings = settings;
        _logger = logger;
        _client = new OpinetClient(settings, clientLogger);
        _writer = new SnapshotWriter(writerLogger);
    }

    public async Task RunLoopAsync(NpgsqlConnection db, string apiKey, CancellationToken cancellationToken)
    {
        _logger.LogInformation("스케줄러 시작. Tick={TickSeconds}s", _settings.Runtime.TickSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.Runtime.TickSeconds));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await RunDueJobsAsync(db, apiKey, cancellationToken);
        }
    }

    public async Task RunOnceAsync(NpgsqlConnection db, string apiKey, bool smoke)
    {
        if (smoke)
        {
            _logger.LogInformation("--once --smoke 모드 실행 (엔드포인트당 1회)");
            await RunSmokeJobsAsync(db, apiKey, CancellationToken.None);
            return;
        }

        _logger.LogInformation("--once 모드 실행");
        await RunDueJobsAsync(db, apiKey, CancellationToken.None, forceAll: true);
    }

    private async Task RunDueJobsAsync(NpgsqlConnection db, string apiKey, CancellationToken ct, bool forceAll = false)
    {
        var now = DateTimeOffset.Now;
        var nowUtc = now.UtcDateTime;
        var dailyUsage = await DbBootstrap.GetDailyUsageAsync(db, now.Date);
        var available = Math.Max(0, _settings.CallPolicy.DailyLimit - _settings.CallPolicy.SafetyReserve - dailyUsage);

        if (available <= 0)
        {
            _logger.LogWarning("일일 호출 가용량 소진. usage={Usage}, limit={Limit}, reserve={Reserve}",
                dailyUsage, _settings.CallPolicy.DailyLimit, _settings.CallPolicy.SafetyReserve);
            return;
        }

        var jobs = BuildJobs(now, forceAll);

        foreach (var job in jobs)
        {
            if (available <= 0)
            {
                _logger.LogWarning("호출 한도 도달로 잔여 작업 중단");
                break;
            }

            try
            {
                var response = await _client.GetJsonAsync(apiKey, job.Path, job.Query, ct);
                await _writer.StoreAsync(db, job.EndpointName, job.Query, response, nowUtc, ct);
                await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, true, 200, null);
                available--;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "호출 실패: {Endpoint}", job.EndpointName);
                await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, false, null, ex.Message);
            }
        }
    }

    private async Task RunSmokeJobsAsync(NpgsqlConnection db, string apiKey, CancellationToken ct)
    {
        var now = DateTimeOffset.Now;
        var nowUtc = now.UtcDateTime;
        var jobs = new List<ScheduledApiCall>
        {
            new("avgAllPrice", "/api/avgAllPrice.do", new()),
            new("avgSidoPrice", "/api/avgSidoPrice.do", new() { ["sido"] = "01" }),
            new("avgSigunPrice", "/api/avgSigunPrice.do", new() { ["sido"] = "01" }),
            new("avgRecentPrice", "/api/avgRecentPrice.do", new()),
            new("pollAvgRecentPrice", "/api/pollAvgRecentPrice.do", new() { ["prodcd"] = "B027" }),
            new("areaAvgRecentPrice", "/api/areaAvgRecentPrice.do", new() { ["area"] = "01" }),
            new("avgLastWeek", "/api/avgLastWeek.do", new() { ["prodcd"] = "B027" }),
            new("lowTop10", "/api/lowTop10.do", new() { ["prodcd"] = "B027", ["cnt"] = "1" }),
            new("aroundAll", "/api/aroundAll.do", new() { ["x"] = "314681.8", ["y"] = "544837", ["radius"] = "500", ["sort"] = "1", ["prodcd"] = "B027" }),
            new("detailById", "/api/detailById.do", new() { ["id"] = "A0002517" }),
            new("searchByName", "/api/searchByName.do", new() { ["osnm"] = "보라매", ["area"] = "01" }),
            new("areaCode", "/api/areaCode.do", new())
        };

        foreach (var job in jobs)
        {
            try
            {
                var response = await _client.GetJsonAsync(apiKey, job.Path, job.Query, ct);
                await _writer.StoreAsync(db, job.EndpointName, job.Query, response, nowUtc, ct);
                await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, true, 200, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMOKE 호출 실패: {Endpoint}", job.EndpointName);
                await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, false, null, ex.Message);
            }
        }
    }

    private List<ScheduledApiCall> BuildJobs(DateTimeOffset now, bool forceAll)
    {
        var jobs = new List<ScheduledApiCall>();

        // 1,2,3번 현재 평균가격 계열 (갱신: 1,2,9,12,16,19시)
        if (forceAll || IsAtHour(now, _settings.Refresh.CurrentPriceHours))
        {
            jobs.Add(new("avgAllPrice", "/api/avgAllPrice.do", new()));

            foreach (var sido in _settings.Region.SidoCodes)
            {
                jobs.Add(new("avgSidoPrice", "/api/avgSidoPrice.do", new() { ["sido"] = sido }));
                jobs.Add(new("avgSigunPrice", "/api/avgSigunPrice.do", new() { ["sido"] = sido }));
            }
        }

        // 4~6번 최근7일 평균가 계열 (일평균 확정: 24시)
        if (forceAll || IsAtHour(now, new[] { _settings.Refresh.DailyFinalizeHour }))
        {
            jobs.Add(new("avgRecentPrice", "/api/avgRecentPrice.do", new()));
            jobs.Add(new("pollAvgRecentPrice", "/api/pollAvgRecentPrice.do", new()));

            foreach (var area in _settings.Region.AreaCodesForAreaAvgRecent)
            {
                jobs.Add(new("areaAvgRecentPrice", "/api/areaAvgRecentPrice.do", new() { ["area"] = area }));
            }
        }

        // 7번 주간 평균가 (금요일 10시)
        if (forceAll || (now.DayOfWeek == DayOfWeek.Friday && now.Hour == _settings.Refresh.WeeklyPriceHour))
        {
            jobs.Add(new("avgLastWeek", "/api/avgLastWeek.do", new()));
        }

        // 8번 최저가 Top20
        if (forceAll || IsAtHour(now, _settings.Refresh.CurrentPriceHours))
        {
            foreach (var prod in _settings.Products.TopProducts)
            {
                jobs.Add(new("lowTop10", "/api/lowTop10.do", new() { ["prodcd"] = prod, ["cnt"] = "20" }));
            }
        }

        // 9번 반경검색 (고정 포인트들)
        if (forceAll || IsAtHour(now, _settings.Refresh.CurrentPriceHours))
        {
            foreach (var p in _settings.Region.AroundPoints)
            {
                jobs.Add(new("aroundAll", "/api/aroundAll.do", new()
                {
                    ["x"] = p.X.ToString(CultureInfo.InvariantCulture),
                    ["y"] = p.Y.ToString(CultureInfo.InvariantCulture),
                    ["radius"] = p.Radius.ToString(CultureInfo.InvariantCulture),
                    ["sort"] = p.Sort.ToString(CultureInfo.InvariantCulture),
                    ["prodcd"] = p.Product
                }));
            }
        }

        // 10,11번은 seed id/name 기반으로 실행
        if (forceAll || IsAtHour(now, _settings.Refresh.CurrentPriceHours))
        {
            foreach (var id in _settings.Region.StationIdsForDetail)
                jobs.Add(new("detailById", "/api/detailById.do", new() { ["id"] = id }));

            foreach (var name in _settings.Region.StationNamesForSearch)
                jobs.Add(new("searchByName", "/api/searchByName.do", new() { ["osnm"] = name }));
        }

        // 19번 지역코드: 하루 1회 전체동기화
        if (forceAll || IsAtHour(now, new[] { _settings.Refresh.AreaCodeSyncHour }))
        {
            jobs.Add(new("areaCode", "/api/areaCode.do", new()));
            foreach (var sido in _settings.Region.SidoCodes)
                jobs.Add(new("areaCode", "/api/areaCode.do", new() { ["area"] = sido }));
        }

        return jobs;
    }

    private static bool IsAtHour(DateTimeOffset now, IReadOnlyCollection<int> hours)
        => hours.Contains(now.Hour) && now.Minute < 5;
}

public sealed record ScheduledApiCall(string EndpointName, string Path, Dictionary<string, string> Query);

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

public sealed class SnapshotWriter
{
    private readonly ILogger<SnapshotWriter> _logger;

    public SnapshotWriter(ILogger<SnapshotWriter> logger) => _logger = logger;

    public async Task StoreAsync(
        NpgsqlConnection db,
        string endpoint,
        Dictionary<string, string> query,
        JsonDocument payload,
        DateTime collectedAtUtc,
        CancellationToken ct)
    {
        var queryJson = JsonSerializer.Serialize(query);
        var payloadJson = payload.RootElement.GetRawText();
        var hash = ToSha256Hex(endpoint + "|" + queryJson + "|" + payloadJson);

        await db.ExecuteAsync(new CommandDefinition(
            @"insert into opinet_api_snapshots(endpoint, query_json, payload_json, payload_hash, collected_at)
              values (@endpoint, @query_json::jsonb, @payload_json::jsonb, @payload_hash, @collected_at)
              on conflict (payload_hash) do nothing;",
            new { endpoint, query_json = queryJson, payload_json = payloadJson, payload_hash = hash, collected_at = collectedAtUtc },
            cancellationToken: ct));

        if (endpoint == "areaCode")
            await UpsertAreaCodeRowsAsync(db, payload, collectedAtUtc, ct);

        _logger.LogInformation("저장 완료: endpoint={Endpoint}", endpoint);
    }

    private static async Task UpsertAreaCodeRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        if (!payload.RootElement.TryGetProperty("RESULT", out var result)) return;
        if (!result.TryGetProperty("OIL", out var oilRows)) return;
        if (oilRows.ValueKind != JsonValueKind.Array) return;

        foreach (var row in oilRows.EnumerateArray())
        {
            var code = row.TryGetProperty("AREA_CD", out var c) ? c.GetString() : null;
            var name = row.TryGetProperty("AREA_NM", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

            var level = code.Length <= 2 ? "SIDO" : "SIGUN";
            var parent = code.Length > 2 ? code[..2] : null;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_area_codes(area_cd, area_nm, area_level, parent_area_cd, updated_at)
                  values (@code, @name, @level, @parent, @updated)
                  on conflict (area_cd) do update
                  set area_nm = excluded.area_nm,
                      area_level = excluded.area_level,
                      parent_area_cd = excluded.parent_area_cd,
                      updated_at = excluded.updated_at;",
                new { code, name, level, parent, updated = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static string ToSha256Hex(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public static class DbBootstrap
{
    public static async Task InitializeAsync(NpgsqlConnection db)
    {
        await db.ExecuteAsync(@"
create table if not exists opinet_api_credentials (
    id bigserial primary key,
    api_key text not null,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists opinet_api_call_log (
    id bigserial primary key,
    call_date date not null,
    called_at timestamptz not null,
    endpoint text not null,
    success boolean not null,
    http_status int null,
    error_message text null
);

create index if not exists ix_opinet_api_call_log_call_date on opinet_api_call_log(call_date);

create table if not exists opinet_api_snapshots (
    id bigserial primary key,
    endpoint text not null,
    query_json jsonb not null,
    payload_json jsonb not null,
    payload_hash text not null unique,
    collected_at timestamptz not null
);

create index if not exists ix_opinet_api_snapshots_endpoint_collected on opinet_api_snapshots(endpoint, collected_at desc);

create table if not exists opinet_area_codes (
    area_cd text primary key,
    area_nm text not null,
    area_level text not null,
    parent_area_cd text null,
    updated_at timestamptz not null
);
");
    }

    public static async Task<int> GetDailyUsageAsync(NpgsqlConnection db, DateTime date)
        => await db.QuerySingleAsync<int>("select count(*) from opinet_api_call_log where call_date = @d", new { d = date });

    public static Task LogCallAsync(NpgsqlConnection db, DateTime callDate, DateTime calledAtUtc, string endpoint, bool success, int? status, string? error)
        => db.ExecuteAsync(@"
insert into opinet_api_call_log(call_date, called_at, endpoint, success, http_status, error_message)
values (@call_date, @called_at, @endpoint, @success, @status, @error);",
            new { call_date = callDate, called_at = calledAtUtc, endpoint, success, status, error });
}

public sealed class OpinetApiKeyProvider
{
    private readonly SchedulerSettings _settings;
    private readonly ILogger<OpinetApiKeyProvider> _logger;

    public OpinetApiKeyProvider(SchedulerSettings settings, ILogger<OpinetApiKeyProvider> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<string?> GetApiKeyAsync(NpgsqlConnection connection)
    {
        _logger.LogInformation("Opinet API Key 조회 SQL 실행");
        return await connection.QueryFirstOrDefaultAsync<string>(_settings.ApiKey.SelectSql);
    }
}

public sealed class SchedulerSettings
{
    public required DatabaseSettings Database { get; init; }
    public required ApiKeySettings ApiKey { get; init; }
    public required CallPolicySettings CallPolicy { get; init; }
    public required ApiSettings Api { get; init; }
    public required RefreshSettings Refresh { get; init; }
    public required ProductSettings Products { get; init; }
    public required RegionSettings Region { get; init; }
    public required RuntimeSettings Runtime { get; init; }
}

public sealed class DatabaseSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Database { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};Pooling=true;Timeout=10;Command Timeout=30";
}

public sealed class ApiKeySettings
{
    public required string SelectSql { get; init; }
}

public sealed class CallPolicySettings
{
    public required int DailyLimit { get; init; }
    public required int SafetyReserve { get; init; }
}

public sealed class ApiSettings
{
    public required string BaseUrl { get; init; }
}

public sealed class RefreshSettings
{
    public required List<int> CurrentPriceHours { get; init; }
    public required int DailyFinalizeHour { get; init; }
    public required int WeeklyPriceHour { get; init; }
    public required int AreaCodeSyncHour { get; init; }
}

public sealed class ProductSettings
{
    public required List<string> TopProducts { get; init; }
}

public sealed class RegionSettings
{
    public required List<string> SidoCodes { get; init; }
    public required List<string> AreaCodesForAreaAvgRecent { get; init; }
    public required List<AroundPoint> AroundPoints { get; init; }
    public required List<string> StationIdsForDetail { get; init; }
    public required List<string> StationNamesForSearch { get; init; }
}

public sealed class AroundPoint
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required int Radius { get; init; }
    public required int Sort { get; init; }
    public required string Product { get; init; }
}

public sealed class RuntimeSettings
{
    public required int TickSeconds { get; init; }
}
