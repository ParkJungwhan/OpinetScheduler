using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

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

        if (endpoint == "avgAllPrice")
            await UpsertAvgAllPriceRowsAsync(db, payload, collectedAtUtc, ct);

        if (endpoint == "avgSidoPrice")
            await UpsertAvgSidoPriceRowsAsync(db, payload, collectedAtUtc, ct);

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

    private static async Task UpsertAvgAllPriceRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        if (!payload.RootElement.TryGetProperty("RESULT", out var result)) return;
        if (!result.TryGetProperty("OIL", out var oilRows)) return;
        if (oilRows.ValueKind != JsonValueKind.Array) return;

        foreach (var row in oilRows.EnumerateArray())
        {
            var tradeDt = row.TryGetProperty("TRADE_DT", out var td) ? GetTextValue(td) : null;
            var prodcd = row.TryGetProperty("PRODCD", out var pc) ? GetTextValue(pc) : null;
            var prodnm = row.TryGetProperty("PRODNM", out var pn) ? GetTextValue(pn) : null;
            var priceText = row.TryGetProperty("PRICE", out var pr) ? GetTextValue(pr) : null;
            var diffText = row.TryGetProperty("DIFF", out var df) ? GetTextValue(df) : null;

            if (string.IsNullOrWhiteSpace(tradeDt) || string.IsNullOrWhiteSpace(prodcd))
                continue;

            decimal? price = null;
            if (decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                price = p;

            decimal? diff = null;
            if (decimal.TryParse(diffText, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                diff = d;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_avg_all_price(trade_dt, prodcd, prodnm, price, diff, source_collected_at, updated_at)
                  values (@trade_dt, @prodcd, @prodnm, @price, @diff, @collected_at, now())
                  on conflict (trade_dt, prodcd) do update
                  set prodnm = excluded.prodnm,
                      price = excluded.price,
                      diff = excluded.diff,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new
                {
                    trade_dt = tradeDt,
                    prodcd,
                    prodnm,
                    price,
                    diff,
                    collected_at = collectedAtUtc
                },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertAvgSidoPriceRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        if (!payload.RootElement.TryGetProperty("RESULT", out var result)) return;
        if (!result.TryGetProperty("OIL", out var oilRows)) return;
        if (oilRows.ValueKind != JsonValueKind.Array) return;

        foreach (var row in oilRows.EnumerateArray())
        {
            var sidoCd = row.TryGetProperty("SIDOCD", out var sc) ? GetTextValue(sc) : null;
            var sidoNm = row.TryGetProperty("SIDONM", out var sn) ? GetTextValue(sn) : null;
            var prodcd = row.TryGetProperty("PRODCD", out var pc) ? GetTextValue(pc) : null;
            var priceText = row.TryGetProperty("PRICE", out var pr) ? GetTextValue(pr) : null;
            var diffText = row.TryGetProperty("DIFF", out var df) ? GetTextValue(df) : null;

            if (string.IsNullOrWhiteSpace(sidoCd) || string.IsNullOrWhiteSpace(prodcd))
                continue;

            decimal? price = null;
            if (decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                price = p;

            decimal? diff = null;
            if (decimal.TryParse(diffText, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                diff = d;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_avg_sido_price(sido_cd, sido_nm, prodcd, price, diff, source_collected_at, updated_at)
                  values (@sido_cd, @sido_nm, @prodcd, @price, @diff, @collected_at, now())
                  on conflict (sido_cd, prodcd) do update
                  set sido_nm = excluded.sido_nm,
                      price = excluded.price,
                      diff = excluded.diff,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new
                {
                    sido_cd = sidoCd,
                    sido_nm = sidoNm,
                    prodcd,
                    price,
                    diff,
                    collected_at = collectedAtUtc
                },
                cancellationToken: ct));
        }
    }

    private static string? GetTextValue(JsonElement e)
    {
        return e.ValueKind switch
        {
            JsonValueKind.String => e.GetString(),
            JsonValueKind.Number => e.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string ToSha256Hex(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
