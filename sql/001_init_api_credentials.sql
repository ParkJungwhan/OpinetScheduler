create table if not exists opinet_api_credentials (
    id bigserial primary key,
    api_key text not null,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_opinet_api_credentials_active_updated
    on opinet_api_credentials (is_active, updated_at desc);

-- 최초 1회, 실제 키로 교체해서 입력
-- insert into opinet_api_credentials (api_key, is_active) values ('YOUR_REAL_OPINET_KEY', true);
