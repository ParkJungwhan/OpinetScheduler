using System.Globalization;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

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

    public async Task SyncAreaCodesAsync(NpgsqlConnection db, string apiKey, CancellationToken ct)
    {
        _logger.LogInformation("--sync-area-codes 모드 실행 (전국 + 시도별 시군구 전체)");

        var now = DateTimeOffset.Now;
        var nowUtc = now.UtcDateTime;

        var jobs = new List<ScheduledApiCall> { new("areaCode", "/api/areaCode.do", new()) };
        foreach (var sido in _settings.Region.SidoCodes)
            jobs.Add(new("areaCode", "/api/areaCode.do", new() { ["area"] = sido }));

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
                _logger.LogError(ex, "지역코드 동기화 실패: query={Query}", JsonSerializer.Serialize(job.Query));
                await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, false, null, ex.Message);
            }
        }

        var count = await db.QuerySingleAsync<int>("select count(*) from opinet_area_codes;");
        _logger.LogInformation("지역코드 동기화 완료. opinet_area_codes={Count}", count);
    }

    public async Task SyncApi1AvgAllPriceAsync(NpgsqlConnection db, string apiKey, CancellationToken ct)
    {
        _logger.LogInformation("--sync-api-1 모드 실행 (avgAllPrice 1회)");

        var now = DateTimeOffset.Now;
        var nowUtc = now.UtcDateTime;
        var job = new ScheduledApiCall("avgAllPrice", "/api/avgAllPrice.do", new());

        try
        {
            var response = await _client.GetJsonAsync(apiKey, job.Path, job.Query, ct);
            await _writer.StoreAsync(db, job.EndpointName, job.Query, response, nowUtc, ct);
            await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, true, 200, null);

            var count = await db.QuerySingleAsync<int>("select count(*) from opinet_avg_all_price;");
            _logger.LogInformation("API #1 적재 완료. opinet_avg_all_price={Count}", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API #1 동기화 실패");
            await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, false, null, ex.Message);
            throw;
        }
    }

    public async Task SyncApi2AvgSidoPriceAsync(NpgsqlConnection db, string apiKey, CancellationToken ct)
    {
        _logger.LogInformation("--sync-api-2 모드 실행 (avgSidoPrice 시도 전체)");

        var now = DateTimeOffset.Now;
        var nowUtc = now.UtcDateTime;

        foreach (var sido in _settings.Region.SidoCodes)
        {
            var job = new ScheduledApiCall("avgSidoPrice", "/api/avgSidoPrice.do", new() { ["sido"] = sido });
            try
            {
                var response = await _client.GetJsonAsync(apiKey, job.Path, job.Query, ct);
                await _writer.StoreAsync(db, job.EndpointName, job.Query, response, nowUtc, ct);
                await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, true, 200, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API #2 동기화 실패: sido={Sido}", sido);
                await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, false, null, ex.Message);
                throw;
            }
        }

        var count = await db.QuerySingleAsync<int>("select count(*) from opinet_avg_sido_price;");
        _logger.LogInformation("API #2 적재 완료. opinet_avg_sido_price={Count}", count);
    }

    public async Task SyncRequiredApisAsync(NpgsqlConnection db, string apiKey, CancellationToken ct)
    {
        _logger.LogInformation("필수 API 전체 동기화 시작 (1~11, 19 / 12~18 제외)");
        var jobs = BuildRequiredApiJobsForInitialLoad();
        await ExecuteJobsAsync(db, apiKey, ct, jobs, stopOnError: false);
        _logger.LogInformation("필수 API 전체 동기화 완료. 실행건수={Count}", jobs.Count);
    }

    private List<ScheduledApiCall> BuildRequiredApiJobsForInitialLoad()
    {
        var jobs = new List<ScheduledApiCall>
        {
            new("avgAllPrice", "/api/avgAllPrice.do", new())
        };

        foreach (var sido in _settings.Region.SidoCodes)
        {
            jobs.Add(new("avgSidoPrice", "/api/avgSidoPrice.do", new() { ["sido"] = sido }));
            jobs.Add(new("avgSigunPrice", "/api/avgSigunPrice.do", new() { ["sido"] = sido }));
        }

        jobs.Add(new("avgRecentPrice", "/api/avgRecentPrice.do", new()));
        jobs.Add(new("pollAvgRecentPrice", "/api/pollAvgRecentPrice.do", new()));

        foreach (var area in _settings.Region.AreaCodesForAreaAvgRecent)
            jobs.Add(new("areaAvgRecentPrice", "/api/areaAvgRecentPrice.do", new() { ["area"] = area }));

        jobs.Add(new("avgLastWeek", "/api/avgLastWeek.do", new()));

        foreach (var prod in _settings.Products.TopProducts)
            jobs.Add(new("lowTop10", "/api/lowTop10.do", new() { ["prodcd"] = prod, ["cnt"] = "20" }));

        foreach (var p in _settings.Region.AroundPoints)
            jobs.Add(new("aroundAll", "/api/aroundAll.do", new()
            {
                ["x"] = p.X.ToString(CultureInfo.InvariantCulture),
                ["y"] = p.Y.ToString(CultureInfo.InvariantCulture),
                ["radius"] = p.Radius.ToString(CultureInfo.InvariantCulture),
                ["sort"] = p.Sort.ToString(CultureInfo.InvariantCulture),
                ["prodcd"] = p.Product
            }));

        foreach (var id in _settings.Region.StationIdsForDetail)
            jobs.Add(new("detailById", "/api/detailById.do", new() { ["id"] = id }));

        foreach (var name in _settings.Region.StationNamesForSearch)
            jobs.Add(new("searchByName", "/api/searchByName.do", new() { ["osnm"] = name, ["area"] = "01" }));

        jobs.Add(new("areaCode", "/api/areaCode.do", new()));
        foreach (var sido in _settings.Region.SidoCodes)
            jobs.Add(new("areaCode", "/api/areaCode.do", new() { ["area"] = sido }));

        return jobs;
    }

    private async Task ExecuteJobsAsync(NpgsqlConnection db, string apiKey, CancellationToken ct, List<ScheduledApiCall> jobs, bool stopOnError)
    {
        var now = DateTimeOffset.Now;
        var nowUtc = now.UtcDateTime;

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
                _logger.LogError(ex, "동기화 실패: endpoint={Endpoint}, query={Query}", job.EndpointName, JsonSerializer.Serialize(job.Query));
                await DbBootstrap.LogCallAsync(db, now.Date, nowUtc, job.EndpointName, false, null, ex.Message);
                if (stopOnError) throw;
            }
        }
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
