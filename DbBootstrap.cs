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

create table if not exists opinet_avg_sigun_price (
    sigun_cd text not null,
    sigun_nm text null,
    prodcd text not null,
    price numeric(10,3) null,
    diff numeric(10,3) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now(),
    primary key (sigun_cd, prodcd)
);

create table if not exists opinet_avg_recent_price (
    base_date text not null,
    prodcd text not null,
    price numeric(10,3) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now(),
    primary key (base_date, prodcd)
);

create table if not exists opinet_poll_avg_recent_price (
    base_date text not null,
    prodcd text not null,
    poll_div_cd text not null,
    price numeric(10,3) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now(),
    primary key (base_date, prodcd, poll_div_cd)
);

create table if not exists opinet_area_avg_recent_price (
    base_date text not null,
    area_cd text not null,
    area_nm text null,
    prodcd text not null,
    price numeric(10,3) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now(),
    primary key (base_date, area_cd, prodcd)
);

create table if not exists opinet_avg_last_week (
    week text not null,
    sta_dt text null,
    end_dt text null,
    area_cd text not null,
    prodcd text not null,
    price numeric(10,3) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now(),
    primary key (week, area_cd, prodcd)
);

create table if not exists opinet_low_top (
    uni_id text primary key,
    price numeric(10,3) null,
    poll_div_cd text null,
    os_nm text null,
    van_adr text null,
    new_adr text null,
    gis_x_coor numeric(14,4) null,
    gis_y_coor numeric(14,4) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now()
);

create table if not exists opinet_around_all (
    uni_id text primary key,
    poll_div_cd text null,
    os_nm text null,
    price numeric(10,3) null,
    distance numeric(14,3) null,
    gis_x_coor numeric(14,4) null,
    gis_y_coor numeric(14,4) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now()
);

create table if not exists opinet_detail_by_id (
    uni_id text primary key,
    poll_div_cd text null,
    os_nm text null,
    van_adr text null,
    new_adr text null,
    tel text null,
    siguncd text null,
    lpg_yn text null,
    maint_yn text null,
    car_wash_yn text null,
    kpetro_yn text null,
    cvs_yn text null,
    gis_x_coor numeric(14,4) null,
    gis_y_coor numeric(14,4) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now()
);

create table if not exists opinet_detail_by_id_prices (
    uni_id text not null,
    prodcd text not null,
    price numeric(10,3) null,
    trade_dt text null,
    trade_tm text null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now(),
    primary key (uni_id, prodcd)
);

create table if not exists opinet_search_by_name (
    uni_id text primary key,
    poll_div_cd text null,
    gpoll_div_cd text null,
    os_nm text null,
    van_adr text null,
    new_adr text null,
    siguncd text null,
    lpg_yn text null,
    gis_x_coor numeric(14,4) null,
    gis_y_coor numeric(14,4) null,
    source_collected_at timestamptz not null,
    updated_at timestamptz not null default now()
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
