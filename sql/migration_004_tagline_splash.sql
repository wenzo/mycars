-- ============================================================
-- Migration 004 — Tagline / Slogan splash screen
-- Aggiunge operators.tagline e aggiorna public_operator_profiles
-- per esporre il campo all'app mobile.
-- ============================================================

-- ── 1. Nuovo campo ────────────────────────────────────────────

alter table public.operators
    add column if not exists tagline text;

-- ── 2. View pubblica aggiornata ───────────────────────────────
-- CREATE OR REPLACE VIEW non può rimuovere colonne esistenti.
-- Si fa DROP + CREATE (nessuna dipendenza critica su questa view).

drop view if exists public.public_operator_profiles;

create view public.public_operator_profiles as
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
    o.tagline,
    o.logo_url,
    o.cover_image_url
from public.operators o
where o.is_active = true;

-- ── Fine migration 004 ────────────────────────────────────────
