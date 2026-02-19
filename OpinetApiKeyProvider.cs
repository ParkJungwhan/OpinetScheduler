using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

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
