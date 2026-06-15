-- ── Migration 011: Privacy Policy per operatore ─────────────────────────────

-- 1. Colonna nella tabella operators
alter table public.operators
    add column if not exists privacy_policy_html text;

-- 2. Aggiorna la vista pubblica per esporre privacy_policy_html all'app mobile
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
    o.cover_image_url,
    o.privacy_policy_html
from public.operators o
where o.is_active = true;
