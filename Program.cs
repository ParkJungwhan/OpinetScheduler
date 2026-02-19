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

var logger = loggerFactory.CreateLogger("Bootstrap");

var settings = configuration.GetRequiredSection("SchedulerSettings").Get<SchedulerSettings>()
    ?? throw new InvalidOperationException("SchedulerSettings를 읽을 수 없습니다.");

logger.LogInformation("OpinetScheduler 초기 부트스트랩 시작");
logger.LogInformation("DB 연결 대상: {Host}:{Port}/{Database}", settings.Database.Host, settings.Database.Port, settings.Database.Database);

try
{
    await using var connection = new NpgsqlConnection(settings.Database.ConnectionString);
    await connection.OpenAsync();

    var dbNow = await connection.QuerySingleAsync<DateTime>("select now();");
    logger.LogInformation("DB 연결 성공. DB 시간: {DbNow}", dbNow);

    var keyProvider = new OpinetApiKeyProvider(settings, loggerFactory.CreateLogger<OpinetApiKeyProvider>());
    var apiKey = await keyProvider.GetApiKeyAsync(connection);

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("Opinet API Key가 비어 있습니다. DB 키 테이블을 확인하세요.");
    }

    logger.LogInformation("Opinet API Key 로딩 성공 (길이: {Length})", apiKey.Length);

    var policy = new DailyCallPolicy(settings.CallPolicy);
    logger.LogInformation("일일 호출 한도: {Limit}", policy.DailyLimit);
    logger.LogInformation("현재 설정된 엔드포인트 수: {Count}", settings.Endpoints.Count);

    foreach (var endpoint in settings.Endpoints)
    {
        var max = policy.GetSafeDailyBudgetPerEndpoint(settings.Endpoints.Count);
        logger.LogInformation("- {Name}: refresh={RefreshHint}, out=json, endpointBudget={Budget}/day", endpoint.Name, endpoint.RefreshHint, max);
    }

    logger.LogInformation("초기 세팅 및 DB 접속 확인 완료. 실제 API 호출 루프 구현 준비 완료.");
}
catch (Exception ex)
{
    logger.LogError(ex, "초기화 실패");
    Environment.ExitCode = 1;
}

public sealed class SchedulerSettings
{
    public required DatabaseSettings Database { get; init; }
    public required ApiKeySettings ApiKey { get; init; }
    public required CallPolicySettings CallPolicy { get; init; }
    public required List<ApiEndpointSetting> Endpoints { get; init; } = new();
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

public sealed class ApiEndpointSetting
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required string RefreshHint { get; init; }
}

public sealed class DailyCallPolicy
{
    public int DailyLimit { get; }
    private readonly int _safetyReserve;

    public DailyCallPolicy(CallPolicySettings settings)
    {
        DailyLimit = settings.DailyLimit;
        _safetyReserve = settings.SafetyReserve;

        if (DailyLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.DailyLimit), "DailyLimit은 1 이상이어야 합니다.");
        }

        if (_safetyReserve < 0 || _safetyReserve >= DailyLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(settings.SafetyReserve), "SafetyReserve는 0 이상, DailyLimit 미만이어야 합니다.");
        }
    }

    public int GetSafeDailyBudgetPerEndpoint(int endpointCount)
    {
        if (endpointCount <= 0) return 0;
        var safeTotal = DailyLimit - _safetyReserve;
        return Math.Max(1, safeTotal / endpointCount);
    }
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
