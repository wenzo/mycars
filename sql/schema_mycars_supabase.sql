-- ============================================================
-- App MyCars - Schema PostgreSQL multi-tenant per ASP.NET Core
-- Versione: tenant/operator condivisi nello stesso database.
-- Ogni tabella di dominio contiene operator_id quando il dato appartiene
-- a uno specifico venditore/concessionario.
-- ============================================================

create extension if not exists pgcrypto;
create extension if not exists citext;

-- ============================================================
-- 1. ENUM DI DOMINIO
-- ============================================================

do $$ begin
    create type public.vehicle_type as enum (
        'autovettura',
        'motoveicolo',
        'autocarro',
        'autocaravan'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.fuel_type as enum (
        'benzina',
        'diesel',
        'ibrida',
        'elettrica',
        'gpl',
        'metano',
        'idrogeno',
        'altro'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.vehicle_condition as enum (
        'nuovo',
        'usato',
        'km_0',
        'conto_vendita',
        'epoca'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.vehicle_usage as enum (
        'privato',
        'aziendale',
        'noleggio',
        'dimostrativo'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.transmission_type as enum (
        'manuale',
        'automatico',
        'semiautomatico'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.media_role as enum (
        'cover',
        'gallery',
        'logo',
        'department_image',
        'news_cover',
        'document',
        'video'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.media_owner_type as enum (
        'operator',
        'branch',
        'department',
        'vehicle',
        'news'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.operator_member_role as enum (
        'owner',
        'admin',
        'editor',
        'viewer'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.news_type as enum (
        'generic',
        'new_arrival',
        'promotion',
        'event',
        'financing',
        'service'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.lead_status as enum (
        'new',
        'contacted',
        'closed',
        'lost',
        'spam'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.lead_type as enum (
        'info',
        'test_drive',
        'offer',
        'financing'
    );
exception when duplicate_object then null;
end $$;

do $$ begin
    create type public.client_platform as enum (
        'android',
        'ios',
        'web',
        'unknown'
    );
exception when duplicate_object then null;
end $$;

-- ============================================================
-- 2. UTILITY
-- ============================================================

create or replace function public.set_updated_at()
returns trigger
language plpgsql
as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

-- ============================================================
-- 3. TENANT / OPERATORI / CODICI APP
-- ============================================================

-- Root tenant. Ogni venditore/concessionario e' un operator.
create table if not exists public.operators (
    id uuid primary key default gen_random_uuid(),

    business_name text not null,
    slug text not null unique,

    -- Codice pubblico principale da comunicare ai clienti.
    -- Non e' una password: serve a risolvere la vetrina del venditore.
    public_code citext not null unique,

    vat_number varchar(32),
    fiscal_code varchar(32),
    website_url text,
    phone text,
    email citext,
    whatsapp_number text,

    logo_media_id uuid,
    primary_color varchar(20),
    secondary_color varchar(20),
    accent_color varchar(20),

    is_active boolean not null default true,
    is_visible_in_marketplace boolean not null default false,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    constraint chk_operators_public_code
        check (public_code ~* '^[A-Z0-9][A-Z0-9_-]{2,31}$')
);

drop trigger if exists trg_operators_updated_at on public.operators;
create trigger trg_operators_updated_at
before update on public.operators
for each row execute function public.set_updated_at();

-- Codici aggiuntivi/campagna. Esempi: PIRRO, PIRRO-FIERA, QR2026.
-- Permette di tracciare da quale canale l'utente ha attivato la vetrina.
create table if not exists public.operator_app_codes (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null references public.operators(id) on delete cascade,

    code citext not null unique,
    label text,
    is_primary boolean not null default false,
    is_active boolean not null default true,
    expires_at timestamptz,
    max_uses int,
    use_count int not null default 0,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    constraint chk_operator_app_codes_code
        check (code ~* '^[A-Z0-9][A-Z0-9_-]{2,31}$'),
    constraint chk_operator_app_codes_max_uses
        check (max_uses is null or max_uses > 0),
    constraint chk_operator_app_codes_use_count
        check (use_count >= 0)
);

drop trigger if exists trg_operator_app_codes_updated_at on public.operator_app_codes;
create trigger trg_operator_app_codes_updated_at
before update on public.operator_app_codes
for each row execute function public.set_updated_at();

create index if not exists ix_operator_app_codes_operator
    on public.operator_app_codes (operator_id, is_active);

-- Membri/staff del venditore. In ASP.NET Core Identity con chiave Guid,
-- user_id puo' riferirsi ad AspNetUsers.Id. Se usi Identity standard string,
-- cambia user_id da uuid a text.
create table if not exists public.operator_members (
    operator_id uuid not null references public.operators(id) on delete cascade,
    user_id uuid not null,
    role public.operator_member_role not null default 'editor',
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    primary key (operator_id, user_id)
);

create index if not exists ix_operator_members_user
    on public.operator_members (user_id, is_active);

-- ============================================================
-- 4. SEDI E REPARTI
-- ============================================================

create table if not exists public.branches (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null references public.operators(id) on delete cascade,

    name text not null,
    slug text not null,
    legal_name text,
    address text,
    zip_code varchar(10),
    city text,
    province varchar(10),
    country_code char(2) not null default 'IT',
    latitude numeric(10,7),
    longitude numeric(10,7),
    phone text,
    email citext,
    whatsapp_number text,
    website_url text,

    is_active boolean not null default true,
    sort_order int not null default 0,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    unique (operator_id, slug),
    unique (id, operator_id)
);

drop trigger if exists trg_branches_updated_at on public.branches;
create trigger trg_branches_updated_at
before update on public.branches
for each row execute function public.set_updated_at();

create table if not exists public.department_types (
    id uuid primary key default gen_random_uuid(),
    name text not null,
    slug text not null unique,
    sort_order int not null default 0,
    is_active boolean not null default true
);

create table if not exists public.branch_departments (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null,
    branch_id uuid not null,
    department_type_id uuid references public.department_types(id) on delete set null,

    name text not null,
    slug text not null,
    phone text,
    email citext,
    whatsapp_number text,
    opening_hours text,
    manager_name text,
    image_media_id uuid,
    is_active boolean not null default true,
    sort_order int not null default 0,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    constraint fk_branch_departments_branch_operator
        foreign key (branch_id, operator_id)
        references public.branches(id, operator_id)
        on delete cascade,

    unique (operator_id, branch_id, slug),
    unique (id, operator_id)
);

drop trigger if exists trg_branch_departments_updated_at on public.branch_departments;
create trigger trg_branch_departments_updated_at
before update on public.branch_departments
for each row execute function public.set_updated_at();

-- ============================================================
-- 5. MEDIA / STORAGE
-- ============================================================

create table if not exists public.media_assets (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null references public.operators(id) on delete cascade,

    bucket text not null default 'vehicle-images',
    storage_path text not null,
    public_url text,
    original_file_name text,
    mime_type text,
    extension text,
    size_bytes bigint,
    width int,
    height int,
    alt_text text,

    owner_type public.media_owner_type,
    role public.media_role,
    is_public boolean not null default true,
    sort_order int not null default 0,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    unique (operator_id, bucket, storage_path),
    unique (id, operator_id)
);

drop trigger if exists trg_media_assets_updated_at on public.media_assets;
create trigger trg_media_assets_updated_at
before update on public.media_assets
for each row execute function public.set_updated_at();

alter table public.operators
    drop constraint if exists fk_operators_logo_media;

alter table public.operators
    add constraint fk_operators_logo_media
    foreign key (logo_media_id, id)
    references public.media_assets(id, operator_id)
    on delete set null (logo_media_id);

alter table public.branch_departments
    drop constraint if exists fk_branch_departments_image_media;

alter table public.branch_departments
    add constraint fk_branch_departments_image_media
    foreign key (image_media_id, operator_id)
    references public.media_assets(id, operator_id)
    on delete set null (image_media_id);

-- ============================================================
-- 6. CATALOGHI GLOBALI: MARCHE E CARROZZERIE
-- ============================================================

-- Globali, non legati al tenant. Evita duplicazioni fra operatori.
create table if not exists public.brands (
    id uuid primary key default gen_random_uuid(),
    name text not null,
    slug text not null unique,
    logo_media_id uuid,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

drop trigger if exists trg_brands_updated_at on public.brands;
create trigger trg_brands_updated_at
before update on public.brands
for each row execute function public.set_updated_at();

create table if not exists public.brand_vehicle_types (
    brand_id uuid not null references public.brands(id) on delete cascade,
    vehicle_type public.vehicle_type not null,
    primary key (brand_id, vehicle_type)
);

create table if not exists public.body_types (
    id uuid primary key default gen_random_uuid(),
    vehicle_type public.vehicle_type not null,
    name text not null,
    slug text not null,
    is_active boolean not null default true,
    sort_order int not null default 0,
    created_at timestamptz not null default now(),

    unique (vehicle_type, slug),
    unique (id, vehicle_type)
);

-- ============================================================
-- 7. VEICOLI
-- ============================================================

create table if not exists public.vehicles (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null references public.operators(id) on delete cascade,

    branch_id uuid not null,
    department_id uuid,

    internal_code text not null,
    external_code text,
    vin varchar(32),

    vehicle_type public.vehicle_type not null,
    brand_id uuid not null,
    model text not null,
    version text,
    body_type_id uuid,

    usage_type public.vehicle_usage,
    condition public.vehicle_condition not null default 'usato',
    fuel public.fuel_type,
    transmission public.transmission_type,

    engine_capacity_cc int,
    horsepower_cv int,
    power_kw int,
    registration_month smallint,
    registration_year smallint,
    mileage_km int not null default 0,

    doors smallint,
    seats smallint,
    color text,
    emission_class text,

    handicap_accessible boolean not null default false,
    vat_deductible boolean not null default false,
    damaged boolean not null default false,
    imported boolean not null default false,

    description text,
    equipment jsonb not null default '[]'::jsonb,

    price numeric(12,2),
    previous_price numeric(12,2),
    currency char(3) not null default 'EUR',
    negotiable boolean not null default false,

    listing_date date,
    is_sold boolean not null default false,
    show_as_sold boolean not null default false,
    sold_at timestamptz,

    pronta_consegna boolean not null default false,
    is_nuovo_arrivo boolean not null default false,
    nuovo_arrivo_until timestamptz,

    is_published boolean not null default true,
    published_at timestamptz,
    sort_order int not null default 0,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz,

    constraint fk_vehicles_branch_operator
        foreign key (branch_id, operator_id)
        references public.branches(id, operator_id)
        on delete restrict,

    constraint fk_vehicles_department_operator
        foreign key (department_id, operator_id)
        references public.branch_departments(id, operator_id)
        on delete set null (department_id),

    constraint fk_vehicles_brand_type
        foreign key (brand_id, vehicle_type)
        references public.brand_vehicle_types(brand_id, vehicle_type)
        on delete restrict,

    constraint fk_vehicles_body_type
        foreign key (body_type_id, vehicle_type)
        references public.body_types(id, vehicle_type)
        on delete restrict,

    constraint chk_registration_month
        check (registration_month is null or registration_month between 1 and 12),

    constraint chk_registration_year
        check (registration_year is null or registration_year between 1900 and extract(year from now())::int + 1),

    constraint chk_vehicle_numbers
        check (
            mileage_km >= 0
            and (engine_capacity_cc is null or engine_capacity_cc >= 0)
            and (horsepower_cv is null or horsepower_cv >= 0)
            and (power_kw is null or power_kw >= 0)
            and (price is null or price >= 0)
            and (previous_price is null or previous_price >= 0)
            and (doors is null or doors >= 0)
            and (seats is null or seats >= 0)
        ),

    unique (operator_id, internal_code),
    unique (id, operator_id)
);

drop trigger if exists trg_vehicles_updated_at on public.vehicles;
create trigger trg_vehicles_updated_at
before update on public.vehicles
for each row execute function public.set_updated_at();

create index if not exists ix_vehicles_operator_type
    on public.vehicles (operator_id, vehicle_type);

create index if not exists ix_vehicles_operator_branch
    on public.vehicles (operator_id, branch_id);

create index if not exists ix_vehicles_operator_brand
    on public.vehicles (operator_id, brand_id);

create index if not exists ix_vehicles_public_search
    on public.vehicles (operator_id, is_published, deleted_at, is_sold, price);

create index if not exists ix_vehicles_mileage
    on public.vehicles (operator_id, mileage_km);

create index if not exists ix_vehicles_nuovo_arrivo
    on public.vehicles (operator_id, is_nuovo_arrivo, nuovo_arrivo_until)
    where is_nuovo_arrivo = true and deleted_at is null;

create index if not exists ix_vehicles_pronta_consegna
    on public.vehicles (operator_id, pronta_consegna)
    where pronta_consegna = true and deleted_at is null;

create index if not exists ix_vehicles_registration
    on public.vehicles (operator_id, registration_year, registration_month);

create index if not exists ix_vehicles_search_text
    on public.vehicles
    using gin (
        to_tsvector(
            'italian',
            coalesce(internal_code, '') || ' ' ||
            coalesce(external_code, '') || ' ' ||
            coalesce(model, '') || ' ' ||
            coalesce(version, '') || ' ' ||
            coalesce(description, '')
        )
    );

create table if not exists public.vehicle_media (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null,
    vehicle_id uuid not null,
    media_id uuid not null,
    role public.media_role not null default 'gallery',
    caption text,
    sort_order int not null default 0,
    created_at timestamptz not null default now(),

    constraint fk_vehicle_media_vehicle_operator
        foreign key (vehicle_id, operator_id)
        references public.vehicles(id, operator_id)
        on delete cascade,

    constraint fk_vehicle_media_media_operator
        foreign key (media_id, operator_id)
        references public.media_assets(id, operator_id)
        on delete cascade,

    unique (operator_id, vehicle_id, media_id)
);

create unique index if not exists ux_vehicle_media_one_cover
    on public.vehicle_media (operator_id, vehicle_id)
    where role = 'cover';

create index if not exists ix_vehicle_media_vehicle_sort
    on public.vehicle_media (operator_id, vehicle_id, sort_order);

create table if not exists public.vehicle_features (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null,
    vehicle_id uuid not null,
    feature_text text not null,
    sort_order int not null default 0,
    created_at timestamptz not null default now(),

    constraint fk_vehicle_features_vehicle_operator
        foreign key (vehicle_id, operator_id)
        references public.vehicles(id, operator_id)
        on delete cascade
);

create index if not exists ix_vehicle_features_vehicle
    on public.vehicle_features (operator_id, vehicle_id, sort_order);

create table if not exists public.vehicle_price_history (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null,
    vehicle_id uuid not null,
    price numeric(12,2) not null check (price >= 0),
    currency char(3) not null default 'EUR',
    valid_from timestamptz not null default now(),
    note text,

    constraint fk_vehicle_price_history_vehicle_operator
        foreign key (vehicle_id, operator_id)
        references public.vehicles(id, operator_id)
        on delete cascade
);

create index if not exists ix_vehicle_price_history_vehicle
    on public.vehicle_price_history (operator_id, vehicle_id, valid_from desc);

-- ============================================================
-- 8. NEWS / PROMO / NUOVI ARRIVI
-- ============================================================

create table if not exists public.news_items (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null references public.operators(id) on delete cascade,
    branch_id uuid,

    news_type public.news_type not null default 'generic',
    code text,
    title text not null,
    slug text not null,
    excerpt text,
    body text,
    cover_media_id uuid,
    link_url text,

    starts_at timestamptz,
    expires_at timestamptz,
    is_published boolean not null default false,
    published_at timestamptz,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    constraint fk_news_branch_operator
        foreign key (branch_id, operator_id)
        references public.branches(id, operator_id)
        on delete set null (branch_id),

    constraint fk_news_cover_media_operator
        foreign key (cover_media_id, operator_id)
        references public.media_assets(id, operator_id)
        on delete set null (cover_media_id),

    unique (operator_id, slug)
);

drop trigger if exists trg_news_items_updated_at on public.news_items;
create trigger trg_news_items_updated_at
before update on public.news_items
for each row execute function public.set_updated_at();

create index if not exists ix_news_items_public
    on public.news_items (operator_id, is_published, starts_at, expires_at);

-- ============================================================
-- 9. LEAD / RICHIESTE CLIENTI
-- ============================================================

create table if not exists public.vehicle_leads (
    id uuid primary key default gen_random_uuid(),
    operator_id uuid not null references public.operators(id) on delete cascade,
    vehicle_id uuid,
    branch_id uuid,

    full_name text not null,
    email citext,
    phone text,
    message text,
    privacy_accepted boolean not null default false,
    marketing_accepted boolean not null default false,

    source text not null default 'app',
    status public.lead_status not null default 'new',
    lead_type public.lead_type not null default 'info',
    preferred_date date,
    preferred_time text,
    ip_address inet,
    user_agent text,

    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    constraint fk_vehicle_leads_vehicle_operator
        foreign key (vehicle_id, operator_id)
        references public.vehicles(id, operator_id)
        on delete set null (vehicle_id),

    constraint fk_vehicle_leads_branch_operator
        foreign key (branch_id, operator_id)
        references public.branches(id, operator_id)
        on delete set null (branch_id)
);

drop trigger if exists trg_vehicle_leads_updated_at on public.vehicle_leads;
create trigger trg_vehicle_leads_updated_at
before update on public.vehicle_leads
for each row execute function public.set_updated_at();

create index if not exists ix_vehicle_leads_operator_status
    on public.vehicle_leads (operator_id, status, created_at desc);

-- ============================================================
-- 10. APP CLIENT GENERICA: INSTALLAZIONI, ASSOCIAZIONE AL VENDITORE, PREFERITI
-- ============================================================

-- Rappresenta una installazione/app instance. Non richiede login cliente.
create table if not exists public.client_app_installations (
    id uuid primary key default gen_random_uuid(),
    app_instance_id uuid not null unique,
    platform public.client_platform not null default 'unknown',
    app_version text,
    device_model text,
    os_version text,
    created_at timestamptz not null default now(),
    last_seen_at timestamptz
);

-- Un'app puo' essere associata a uno o piu' operatori.
-- Se vuoi imporre un solo venditore alla volta, gestiscilo lato backend/app
-- con un campo current_operator_id nello stato locale dell'app.
create table if not exists public.client_operator_links (
    installation_id uuid not null references public.client_app_installations(id) on delete cascade,
    operator_id uuid not null references public.operators(id) on delete cascade,

    first_code citext,
    notifications_enabled boolean not null default false,
    push_token text,
    linked_at timestamptz not null default now(),
    last_selected_at timestamptz,

    primary key (installation_id, operator_id)
);

create index if not exists ix_client_operator_links_operator
    on public.client_operator_links (operator_id, notifications_enabled);

create table if not exists public.client_favorites (
    installation_id uuid not null references public.client_app_installations(id) on delete cascade,
    operator_id uuid not null,
    vehicle_id uuid not null,
    created_at timestamptz not null default now(),

    primary key (installation_id, vehicle_id),

    constraint fk_client_favorites_vehicle_operator
        foreign key (vehicle_id, operator_id)
        references public.vehicles(id, operator_id)
        on delete cascade
);

create index if not exists ix_client_favorites_operator_vehicle
    on public.client_favorites (operator_id, vehicle_id);

-- ============================================================
-- 11. VISTE PUBBLICHE UTILI PER API CLIENT
-- ============================================================

create or replace view public.public_operator_profiles as
select
    o.id,
    o.business_name,
    o.slug,
    o.public_code,
    o.website_url,
    o.phone,
    o.email,
    o.whatsapp_number,
    o.primary_color,
    o.secondary_color,
    o.accent_color,
    ma.public_url as logo_url,
    ma.bucket as logo_bucket,
    ma.storage_path as logo_storage_path
from public.operators o
left join public.media_assets ma
    on ma.id = o.logo_media_id
   and ma.operator_id = o.id
where o.is_active = true;

create or replace view public.public_vehicle_cards as
select
    v.id,
    v.operator_id,
    o.slug as operator_slug,
    o.public_code as operator_code,
    v.branch_id,
    v.internal_code,
    v.vehicle_type,
    b.name as brand_name,
    b.slug as brand_slug,
    v.model,
    v.version,
    bt.name as body_type_name,
    v.condition,
    v.usage_type,
    v.fuel,
    v.transmission,
    v.horsepower_cv,
    v.power_kw,
    v.registration_month,
    v.registration_year,
    v.mileage_km,
    v.price,
    v.previous_price,
    v.currency,
    v.is_sold,
    v.show_as_sold,
    v.pronta_consegna,
    v.is_nuovo_arrivo,
    v.nuovo_arrivo_until,
    v.description,
    ma.public_url as cover_image_url,
    ma.bucket as cover_bucket,
    ma.storage_path as cover_storage_path,
    br.name as branch_name,
    br.city,
    br.province,
    v.created_at,
    v.updated_at
from public.vehicles v
join public.operators o on o.id = v.operator_id and o.is_active = true
join public.brands b on b.id = v.brand_id
left join public.body_types bt on bt.id = v.body_type_id
join public.branches br on br.id = v.branch_id and br.operator_id = v.operator_id
left join public.vehicle_media vm
    on vm.vehicle_id = v.id
   and vm.operator_id = v.operator_id
   and vm.role = 'cover'
left join public.media_assets ma
    on ma.id = vm.media_id
   and ma.operator_id = v.operator_id
where
    v.is_published = true
    and v.deleted_at is null;

-- Risoluzione codice app -> operatore.
create or replace function public.resolve_operator_by_code(p_code citext)
returns table (
    operator_id uuid,
    business_name text,
    slug text,
    public_code citext,
    website_url text,
    phone text,
    email citext,
    whatsapp_number text,
    primary_color varchar,
    secondary_color varchar,
    accent_color varchar,
    logo_url text
)
language sql
stable
as $$
    select
        o.id,
        o.business_name,
        o.slug,
        o.public_code,
        o.website_url,
        o.phone,
        o.email,
        o.whatsapp_number,
        o.primary_color,
        o.secondary_color,
        o.accent_color,
        ma.public_url as logo_url
    from public.operator_app_codes c
    join public.operators o on o.id = c.operator_id
    left join public.media_assets ma on ma.id = o.logo_media_id and ma.operator_id = o.id
    where c.code = p_code
      and c.is_active = true
      and o.is_active = true
      and (c.expires_at is null or c.expires_at > now())
      and (c.max_uses is null or c.use_count < c.max_uses)
    limit 1;
$$;

-- ============================================================
-- 12. SEED MINIMO DI CATALOGHI
-- I seed completi di marche/carrozzerie possono essere riusati dal file precedente.
-- ============================================================

insert into public.department_types (name, slug, sort_order)
values
    ('Vendite', 'vendite', 1),
    ('PostVendita/Service', 'postvendita-service', 2),
    ('Noleggio', 'noleggio', 3),
    ('BackOffice/Amministrazione', 'backoffice-amministrazione', 4),
    ('General Manager', 'general-manager', 5)
on conflict (slug) do update
set name = excluded.name,
    sort_order = excluded.sort_order;

insert into public.body_types (vehicle_type, name, slug, sort_order)
values
    ('autovettura'::public.vehicle_type, 'Berlina 3 porte', 'berlina-3-porte', 1),
    ('autovettura'::public.vehicle_type, 'Berlina 5 porte', 'berlina-5-porte', 2),
    ('autovettura'::public.vehicle_type, 'Station wagon', 'station-wagon', 3),
    ('autovettura'::public.vehicle_type, 'Coupé', 'coupe', 4),
    ('autovettura'::public.vehicle_type, 'Cabriolet', 'cabriolet', 5),
    ('autovettura'::public.vehicle_type, 'City car', 'city-car', 6),
    ('autovettura'::public.vehicle_type, 'Monovolume', 'monovolume', 7),
    ('autovettura'::public.vehicle_type, 'SUV', 'suv', 8),
    ('autovettura'::public.vehicle_type, 'Fuoristrada', 'fuoristrada', 9),
    ('motoveicolo'::public.vehicle_type, 'Scooter', 'scooter', 1),
    ('motoveicolo'::public.vehicle_type, 'Naked', 'naked', 2),
    ('motoveicolo'::public.vehicle_type, 'Enduro', 'enduro', 3),
    ('motoveicolo'::public.vehicle_type, 'Tourer', 'tourer', 4),
    ('motoveicolo'::public.vehicle_type, 'Cruiser', 'cruiser', 5),
    ('autocarro'::public.vehicle_type, 'Furgone', 'furgone', 1),
    ('autocarro'::public.vehicle_type, 'Autocarro', 'autocarro', 2),
    ('autocarro'::public.vehicle_type, 'Autoarticolato', 'autoarticolato', 3),
    ('autocaravan'::public.vehicle_type, 'Mansardato', 'mansardato', 1),
    ('autocaravan'::public.vehicle_type, 'Semi-integrale', 'semi-integrale', 2),
    ('autocaravan'::public.vehicle_type, 'Integrale', 'integrale', 3),
    ('autocaravan'::public.vehicle_type, 'Motorhome', 'motorhome', 4),
    ('autocaravan'::public.vehicle_type, 'Furgonato', 'furgonato', 5)
on conflict (vehicle_type, slug) do update
set name = excluded.name,
    sort_order = excluded.sort_order;

-- Esempio operatore demo, da rimuovere/adattare in produzione.
insert into public.operators (business_name, slug, public_code, vat_number, website_url, primary_color, accent_color)
values ('Pirro Auto', 'pirro-auto', 'PIRRO', '03296880713', 'https://www.fiatpirro.it', '#1E3A5F', '#D62828')
on conflict (slug) do update
set business_name = excluded.business_name,
    public_code = excluded.public_code,
    vat_number = excluded.vat_number,
    website_url = excluded.website_url,
    primary_color = excluded.primary_color,
    accent_color = excluded.accent_color;

insert into public.operator_app_codes (operator_id, code, label, is_primary)
select id, public_code, 'Codice principale', true
from public.operators
where slug = 'pirro-auto'
on conflict (code) do update
set operator_id = excluded.operator_id,
    label = excluded.label,
    is_primary = excluded.is_primary;

-- Fine script.
