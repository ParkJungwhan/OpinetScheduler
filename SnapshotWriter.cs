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

        switch (endpoint)
        {
            case "areaCode": await UpsertAreaCodeRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "avgAllPrice": await UpsertAvgAllPriceRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "avgSidoPrice": await UpsertAvgSidoPriceRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "avgSigunPrice": await UpsertAvgSigunPriceRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "avgRecentPrice": await UpsertAvgRecentPriceRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "pollAvgRecentPrice": await UpsertPollAvgRecentPriceRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "areaAvgRecentPrice": await UpsertAreaAvgRecentPriceRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "avgLastWeek": await UpsertAvgLastWeekRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "lowTop10": await UpsertLowTopRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "aroundAll": await UpsertAroundAllRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "detailById": await UpsertDetailByIdRowsAsync(db, payload, collectedAtUtc, ct); break;
            case "searchByName": await UpsertSearchByNameRowsAsync(db, payload, collectedAtUtc, ct); break;
        }

        _logger.LogInformation("저장 완료: endpoint={Endpoint}", endpoint);
    }

    private static IEnumerable<JsonElement> EnumerateOil(JsonDocument payload)
    {
        if (!payload.RootElement.TryGetProperty("RESULT", out var result)) yield break;
        if (!result.TryGetProperty("OIL", out var oilRows)) yield break;

        if (oilRows.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in oilRows.EnumerateArray()) yield return row;
            yield break;
        }

        if (oilRows.ValueKind == JsonValueKind.Object) yield return oilRows;
    }

    private static async Task UpsertAreaCodeRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var code = row.TryGetProperty("AREA_CD", out var c) ? GetTextValue(c) : null;
            var name = row.TryGetProperty("AREA_NM", out var n) ? GetTextValue(n) : null;
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
        foreach (var row in EnumerateOil(payload))
        {
            var tradeDt = Get(row, "TRADE_DT");
            var prodcd = Get(row, "PRODCD");
            if (string.IsNullOrWhiteSpace(tradeDt) || string.IsNullOrWhiteSpace(prodcd)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_avg_all_price(trade_dt, prodcd, prodnm, price, diff, source_collected_at, updated_at)
                  values (@trade_dt, @prodcd, @prodnm, @price, @diff, @collected_at, now())
                  on conflict (trade_dt, prodcd) do update
                  set prodnm = excluded.prodnm,
                      price = excluded.price,
                      diff = excluded.diff,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { trade_dt = tradeDt, prodcd, prodnm = Get(row, "PRODNM"), price = ToDecimal(Get(row, "PRICE")), diff = ToDecimal(Get(row, "DIFF")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertAvgSidoPriceRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var sidoCd = Get(row, "SIDOCD");
            var prodcd = Get(row, "PRODCD");
            if (string.IsNullOrWhiteSpace(sidoCd) || string.IsNullOrWhiteSpace(prodcd)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_avg_sido_price(sido_cd, sido_nm, prodcd, price, diff, source_collected_at, updated_at)
                  values (@sido_cd, @sido_nm, @prodcd, @price, @diff, @collected_at, now())
                  on conflict (sido_cd, prodcd) do update
                  set sido_nm = excluded.sido_nm,
                      price = excluded.price,
                      diff = excluded.diff,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { sido_cd = sidoCd, sido_nm = Get(row, "SIDONM"), prodcd, price = ToDecimal(Get(row, "PRICE")), diff = ToDecimal(Get(row, "DIFF")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertAvgSigunPriceRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var sigunCd = Get(row, "SIGUNCD");
            var prodcd = Get(row, "PRODCD");
            if (string.IsNullOrWhiteSpace(sigunCd) || string.IsNullOrWhiteSpace(prodcd)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_avg_sigun_price(sigun_cd, sigun_nm, prodcd, price, diff, source_collected_at, updated_at)
                  values (@sigun_cd, @sigun_nm, @prodcd, @price, @diff, @collected_at, now())
                  on conflict (sigun_cd, prodcd) do update
                  set sigun_nm = excluded.sigun_nm,
                      price = excluded.price,
                      diff = excluded.diff,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { sigun_cd = sigunCd, sigun_nm = Get(row, "SIGUNNM"), prodcd, price = ToDecimal(Get(row, "PRICE")), diff = ToDecimal(Get(row, "DIFF")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertAvgRecentPriceRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var date = Get(row, "DATE");
            var prodcd = Get(row, "PRODCD");
            if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(prodcd)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_avg_recent_price(base_date, prodcd, price, source_collected_at, updated_at)
                  values (@base_date, @prodcd, @price, @collected_at, now())
                  on conflict (base_date, prodcd) do update
                  set price = excluded.price,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { base_date = date, prodcd, price = ToDecimal(Get(row, "PRICE")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertPollAvgRecentPriceRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var date = Get(row, "DATE");
            var prodcd = Get(row, "PRODCD");
            var poll = Get(row, "POLL_DIV_CD");
            if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(prodcd) || string.IsNullOrWhiteSpace(poll)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_poll_avg_recent_price(base_date, prodcd, poll_div_cd, price, source_collected_at, updated_at)
                  values (@base_date, @prodcd, @poll_div_cd, @price, @collected_at, now())
                  on conflict (base_date, prodcd, poll_div_cd) do update
                  set price = excluded.price,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { base_date = date, prodcd, poll_div_cd = poll, price = ToDecimal(Get(row, "PRICE")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertAreaAvgRecentPriceRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var date = Get(row, "DATE");
            var areaCd = Get(row, "AREA_CD");
            var prodcd = Get(row, "PRODCD");
            if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(areaCd) || string.IsNullOrWhiteSpace(prodcd)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_area_avg_recent_price(base_date, area_cd, area_nm, prodcd, price, source_collected_at, updated_at)
                  values (@base_date, @area_cd, @area_nm, @prodcd, @price, @collected_at, now())
                  on conflict (base_date, area_cd, prodcd) do update
                  set area_nm = excluded.area_nm,
                      price = excluded.price,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { base_date = date, area_cd = areaCd, area_nm = Get(row, "AREA_NM"), prodcd, price = ToDecimal(Get(row, "PRICE")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertAvgLastWeekRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var week = Get(row, "WEEK");
            var areaCd = Get(row, "AREA_CD");
            var prodcd = Get(row, "PRODCD");
            if (string.IsNullOrWhiteSpace(week) || string.IsNullOrWhiteSpace(areaCd) || string.IsNullOrWhiteSpace(prodcd)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_avg_last_week(week, sta_dt, end_dt, area_cd, prodcd, price, source_collected_at, updated_at)
                  values (@week, @sta_dt, @end_dt, @area_cd, @prodcd, @price, @collected_at, now())
                  on conflict (week, area_cd, prodcd) do update
                  set sta_dt = excluded.sta_dt,
                      end_dt = excluded.end_dt,
                      price = excluded.price,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { week, sta_dt = Get(row, "STA_DT"), end_dt = Get(row, "END_DT"), area_cd = areaCd, prodcd, price = ToDecimal(Get(row, "PRICE")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertLowTopRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var uniId = Get(row, "UNI_ID");
            if (string.IsNullOrWhiteSpace(uniId)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_low_top(uni_id, price, poll_div_cd, os_nm, van_adr, new_adr, gis_x_coor, gis_y_coor, source_collected_at, updated_at)
                  values (@uni_id, @price, @poll_div_cd, @os_nm, @van_adr, @new_adr, @gis_x_coor, @gis_y_coor, @collected_at, now())
                  on conflict (uni_id) do update
                  set price = excluded.price,
                      poll_div_cd = excluded.poll_div_cd,
                      os_nm = excluded.os_nm,
                      van_adr = excluded.van_adr,
                      new_adr = excluded.new_adr,
                      gis_x_coor = excluded.gis_x_coor,
                      gis_y_coor = excluded.gis_y_coor,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { uni_id = uniId, price = ToDecimal(Get(row, "PRICE")), poll_div_cd = Get(row, "POLL_DIV_CD"), os_nm = Get(row, "OS_NM"), van_adr = Get(row, "VAN_ADR"), new_adr = Get(row, "NEW_ADR"), gis_x_coor = ToDecimal(Get(row, "GIS_X_COOR")), gis_y_coor = ToDecimal(Get(row, "GIS_Y_COOR")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertAroundAllRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var uniId = Get(row, "UNI_ID");
            if (string.IsNullOrWhiteSpace(uniId)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_around_all(uni_id, poll_div_cd, os_nm, price, distance, gis_x_coor, gis_y_coor, source_collected_at, updated_at)
                  values (@uni_id, @poll_div_cd, @os_nm, @price, @distance, @gis_x_coor, @gis_y_coor, @collected_at, now())
                  on conflict (uni_id) do update
                  set poll_div_cd = excluded.poll_div_cd,
                      os_nm = excluded.os_nm,
                      price = excluded.price,
                      distance = excluded.distance,
                      gis_x_coor = excluded.gis_x_coor,
                      gis_y_coor = excluded.gis_y_coor,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { uni_id = uniId, poll_div_cd = Get(row, "POLL_DIV_CD"), os_nm = Get(row, "OS_NM"), price = ToDecimal(Get(row, "PRICE")), distance = ToDecimal(Get(row, "DISTANCE")), gis_x_coor = ToDecimal(Get(row, "GIS_X_COOR")), gis_y_coor = ToDecimal(Get(row, "GIS_Y_COOR")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static async Task UpsertDetailByIdRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var uniId = Get(row, "UNI_ID");
            if (string.IsNullOrWhiteSpace(uniId)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_detail_by_id(uni_id, poll_div_cd, os_nm, van_adr, new_adr, tel, siguncd, lpg_yn, maint_yn, car_wash_yn, kpetro_yn, cvs_yn, gis_x_coor, gis_y_coor, source_collected_at, updated_at)
                  values (@uni_id, @poll_div_cd, @os_nm, @van_adr, @new_adr, @tel, @siguncd, @lpg_yn, @maint_yn, @car_wash_yn, @kpetro_yn, @cvs_yn, @gis_x_coor, @gis_y_coor, @collected_at, now())
                  on conflict (uni_id) do update
                  set poll_div_cd = excluded.poll_div_cd,
                      os_nm = excluded.os_nm,
                      van_adr = excluded.van_adr,
                      new_adr = excluded.new_adr,
                      tel = excluded.tel,
                      siguncd = excluded.siguncd,
                      lpg_yn = excluded.lpg_yn,
                      maint_yn = excluded.maint_yn,
                      car_wash_yn = excluded.car_wash_yn,
                      kpetro_yn = excluded.kpetro_yn,
                      cvs_yn = excluded.cvs_yn,
                      gis_x_coor = excluded.gis_x_coor,
                      gis_y_coor = excluded.gis_y_coor,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new
                {
                    uni_id = uniId,
                    poll_div_cd = Get(row, "POLL_DIV_CD"),
                    os_nm = Get(row, "OS_NM"),
                    van_adr = Get(row, "VAN_ADR"),
                    new_adr = Get(row, "NEW_ADR"),
                    tel = Get(row, "TEL"),
                    siguncd = Get(row, "SIGUNCD"),
                    lpg_yn = Get(row, "LPG_YN"),
                    maint_yn = Get(row, "MAINT_YN"),
                    car_wash_yn = Get(row, "CAR_WASH_YN"),
                    kpetro_yn = Get(row, "KPETRO_YN"),
                    cvs_yn = Get(row, "CVS_YN"),
                    gis_x_coor = ToDecimal(Get(row, "GIS_X_COOR")),
                    gis_y_coor = ToDecimal(Get(row, "GIS_Y_COOR")),
                    collected_at = collectedAtUtc
                },
                cancellationToken: ct));

            if (row.TryGetProperty("OIL_PRICE", out var oilPrices) && oilPrices.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in oilPrices.EnumerateArray())
                {
                    var prodcd = Get(p, "PRODCD");
                    if (string.IsNullOrWhiteSpace(prodcd)) continue;
                    await db.ExecuteAsync(new CommandDefinition(
                        @"insert into opinet_detail_by_id_prices(uni_id, prodcd, price, trade_dt, trade_tm, source_collected_at, updated_at)
                          values (@uni_id, @prodcd, @price, @trade_dt, @trade_tm, @collected_at, now())
                          on conflict (uni_id, prodcd) do update
                          set price = excluded.price,
                              trade_dt = excluded.trade_dt,
                              trade_tm = excluded.trade_tm,
                              source_collected_at = excluded.source_collected_at,
                              updated_at = now();",
                        new { uni_id = uniId, prodcd, price = ToDecimal(Get(p, "PRICE")), trade_dt = Get(p, "TRADE_DT"), trade_tm = Get(p, "TRADE_TM"), collected_at = collectedAtUtc },
                        cancellationToken: ct));
                }
            }
        }
    }

    private static async Task UpsertSearchByNameRowsAsync(NpgsqlConnection db, JsonDocument payload, DateTime collectedAtUtc, CancellationToken ct)
    {
        foreach (var row in EnumerateOil(payload))
        {
            var uniId = Get(row, "UNI_ID");
            if (string.IsNullOrWhiteSpace(uniId)) continue;

            await db.ExecuteAsync(new CommandDefinition(
                @"insert into opinet_search_by_name(uni_id, poll_div_cd, gpoll_div_cd, os_nm, van_adr, new_adr, siguncd, lpg_yn, gis_x_coor, gis_y_coor, source_collected_at, updated_at)
                  values (@uni_id, @poll_div_cd, @gpoll_div_cd, @os_nm, @van_adr, @new_adr, @siguncd, @lpg_yn, @gis_x_coor, @gis_y_coor, @collected_at, now())
                  on conflict (uni_id) do update
                  set poll_div_cd = excluded.poll_div_cd,
                      gpoll_div_cd = excluded.gpoll_div_cd,
                      os_nm = excluded.os_nm,
                      van_adr = excluded.van_adr,
                      new_adr = excluded.new_adr,
                      siguncd = excluded.siguncd,
                      lpg_yn = excluded.lpg_yn,
                      gis_x_coor = excluded.gis_x_coor,
                      gis_y_coor = excluded.gis_y_coor,
                      source_collected_at = excluded.source_collected_at,
                      updated_at = now();",
                new { uni_id = uniId, poll_div_cd = Get(row, "POLL_DIV_CD"), gpoll_div_cd = Get(row, "GPOLL_DIV_CD"), os_nm = Get(row, "OS_NM"), van_adr = Get(row, "VAN_ADR"), new_adr = Get(row, "NEW_ADR"), siguncd = Get(row, "SIGUNCD"), lpg_yn = Get(row, "LPG_YN"), gis_x_coor = ToDecimal(Get(row, "GIS_X_COOR")), gis_y_coor = ToDecimal(Get(row, "GIS_Y_COOR")), collected_at = collectedAtUtc },
                cancellationToken: ct));
        }
    }

    private static string? Get(JsonElement row, string key)
        => row.TryGetProperty(key, out var e) ? GetTextValue(e) : null;

    private static decimal? ToDecimal(string? text)
        => decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

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
