
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
    public int DetailByIdMaxTargets { get; init; } = 50;
}
