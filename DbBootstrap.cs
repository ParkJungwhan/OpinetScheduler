using Dapper;
using Npgsql;

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

create table if not exists opinet_avg_all_price (
    trade_dt text not null,
    prodcd text not null,
    prodnm text null,
    price numeric(10,3) null,
    diff numeric(10,3) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now(),
    primary key (trade_dt, prodcd)
);

create table if not exists opinet_avg_sido_price (
    sido_cd text not null,
    sido_nm text null,
    prodcd text not null,
    price numeric(10,3) null,
    diff numeric(10,3) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now(),
    primary key (sido_cd, prodcd)
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
