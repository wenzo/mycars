-- ============================================================
-- Migration 001 — Nuovo Arrivo, Pronta Consegna, Test Drive
-- Applicare su uno schema MyCars già esistente in Supabase.
-- Tutte le operazioni sono idempotenti (IF NOT EXISTS / DO $$).
-- ============================================================

-- ── 1. NUOVO ENUM: lead_type ──────────────────────────────

do $$ begin
    create type public.lead_type as enum (
        'info',
        'test_drive',
        'offer',
        'financing'
    );
exception when duplicate_object then null;
end $$;

-- ── 2. NUOVI CAMPI: vehicles ──────────────────────────────

alter table public.vehicles
    add column if not exists pronta_consegna   boolean     not null default false,
    add column if not exists is_nuovo_arrivo   boolean     not null default false,
    add column if not exists nuovo_arrivo_until timestamptz;

-- ── 3. NUOVI CAMPI: vehicle_leads ────────────────────────

alter table public.vehicle_leads
    add column if not exists lead_type       public.lead_type not null default 'info',
    add column if not exists preferred_date  date,
    add column if not exists preferred_time  text;

-- ── 4. NUOVI INDICI PARZIALI ──────────────────────────────

create index if not exists ix_vehicles_nuovo_arrivo
    on public.vehicles (operator_id, is_nuovo_arrivo, nuovo_arrivo_until)
    where is_nuovo_arrivo = true and deleted_at is null;

create index if not exists ix_vehicles_pronta_consegna
    on public.vehicles (operator_id, pronta_consegna)
    where pronta_consegna = true and deleted_at is null;

create index if not exists ix_vehicle_leads_type
    on public.vehicle_leads (operator_id, lead_type, created_at desc);

-- ── 5. AGGIORNAMENTO VIEW: public_vehicle_cards ───────────
-- La vista viene ricreata aggiungendo i tre nuovi campi.
-- "create or replace view" è sicuro: non distrugge dipendenze.

create or replace view public.public_vehicle_cards as
select
    v.id,
    v.operator_id,
    o.slug                  as operator_slug,
    o.public_code           as operator_code,
    v.branch_id,
    v.internal_code,
    v.vehicle_type,
    b.name                  as brand_name,
    b.slug                  as brand_slug,
    v.model,
    v.version,
    bt.name                 as body_type_name,
    v.condition,
    v.usage_type,
    v.fuel,
    v.transmission,
    v.registration_month,
    v.registration_year,
    v.mileage_km,
    v.price,
    v.previous_price,
    v.currency,
    v.is_sold,
    v.show_as_sold,
    v.description,
    ma.public_url           as cover_image_url,
    ma.bucket               as cover_bucket,
    ma.storage_path         as cover_storage_path,
    br.name                 as branch_name,
    br.city,
    br.province,
    v.created_at,
    v.updated_at,
    -- nuovi campi: devono stare in fondo per CREATE OR REPLACE VIEW
    v.pronta_consegna,
    v.is_nuovo_arrivo,
    v.nuovo_arrivo_until
from public.vehicles v
join public.operators   o  on  o.id = v.operator_id and o.is_active = true
join public.brands      b  on  b.id = v.brand_id
left join public.body_types bt on bt.id = v.body_type_id
join public.branches    br on  br.id = v.branch_id and br.operator_id = v.operator_id
left join public.vehicle_media vm
    on  vm.vehicle_id   = v.id
    and vm.operator_id  = v.operator_id
    and vm.role         = 'cover'
left join public.media_assets ma
    on  ma.id           = vm.media_id
    and ma.operator_id  = v.operator_id
where
    v.is_published = true
    and v.deleted_at is null;

-- ── Fine migration 001 ────────────────────────────────────
