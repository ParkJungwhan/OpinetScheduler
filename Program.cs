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

if (args.Contains("--sync-area-codes", StringComparer.OrdinalIgnoreCase))
{
    await scheduler.SyncAreaCodesAsync(db, apiKey, CancellationToken.None);
    return;
}

if (args.Contains("--sync-api-1", StringComparer.OrdinalIgnoreCase))
{
    await scheduler.SyncApi1AvgAllPriceAsync(db, apiKey, CancellationToken.None);
    return;
}

if (args.Contains("--sync-api-2", StringComparer.OrdinalIgnoreCase))
{
    await scheduler.SyncApi2AvgSidoPriceAsync(db, apiKey, CancellationToken.None);
    return;
}

if (args.Contains("--sync-api-3", StringComparer.OrdinalIgnoreCase)) { await scheduler.SyncApi3AvgSigunPriceAsync(db, apiKey, CancellationToken.None); return; }
if (args.Contains("--sync-api-4", StringComparer.OrdinalIgnoreCase)) { await scheduler.SyncApi4AvgRecentPriceAsync(db, apiKey, CancellationToken.None); return; }
if (args.Contains("--sync-api-5", StringComparer.OrdinalIgnoreCase)) { await scheduler.SyncApi5PollAvgRecentPriceAsync(db, apiKey, CancellationToken.None); return; }
if (args.Contains("--sync-api-6", StringComparer.OrdinalIgnoreCase)) { await scheduler.SyncApi6AreaAvgRecentPriceAsync(db, apiKey, CancellationToken.None); return; }
if (args.Contains("--sync-api-7", StringComparer.OrdinalIgnoreCase)) { await scheduler.SyncApi7AvgLastWeekAsync(db, apiKey, CancellationToken.None); return; }
if (args.Contains("--sync-api-8", StringComparer.OrdinalIgnoreCase)) { await scheduler.SyncApi8LowTopAsync(db, apiKey, CancellationToken.None); return; }
if (args.Contains("--sync-api-9", StringComparer.OrdinalIgnoreCase)) { await scheduler.SyncApi9AroundAllAsync(db, apiKey, CancellationToken.None); return; }
if (args.Contains("--sync-api-10", StringComparer.OrdinalIgnoreCase)) { await scheduler.SyncApi10DetailByIdAsync(db, apiKey, CancellationToken.None); return; }
if (args.Contains("--sync-api-11", StringComparer.OrdinalIgnoreCase)) { await scheduler.SyncApi11SearchByNameAsync(db, apiKey, CancellationToken.None); return; }

if (args.Contains("--sync-required-apis", StringComparer.OrdinalIgnoreCase))
{
    await scheduler.SyncRequiredApisAsync(db, apiKey, CancellationToken.None);
    return;
}

if (args.Contains("--sync-detail-by-id", StringComparer.OrdinalIgnoreCase))
{
    await scheduler.SyncDetailByIdFromStoredTargetsAsync(db, apiKey, CancellationToken.None);
    return;
}

if (args.Contains("--once", StringComparer.OrdinalIgnoreCase))
{
    var smoke = args.Contains("--smoke", StringComparer.OrdinalIgnoreCase);
    await scheduler.RunOnceAsync(db, apiKey, smoke);
    return;
}

await scheduler.RunLoopAsync(db, apiKey, CancellationToken.None);
