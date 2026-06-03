-- ============================================================
--  MyCars — Migrazione 001
--  Sessione: autenticazione multi-tenant, superadmin,
--            iscrizione concessionari, profilo con upload immagini,
--            notifiche push per email
-- ============================================================

-- ── 1. operators ─────────────────────────────────────────────
-- Colonna logo_url diretta (prioritaria rispetto al JOIN su media_assets).
-- COALESCE(o.logo_url, ma.public_url) viene usato nei SELECT per
-- mantenere la compatibilità con i record già esistenti.
ALTER TABLE public.operators
    ADD COLUMN IF NOT EXISTS logo_url        TEXT,
    ADD COLUMN IF NOT EXISTS cover_image_url TEXT;

-- ── 2. push_subscriptions ────────────────────────────────────
-- Consente di targetizzare le notifiche push per email utente.
ALTER TABLE public.push_subscriptions
    ADD COLUMN IF NOT EXISTS user_email TEXT;

CREATE INDEX IF NOT EXISTS idx_push_subscriptions_user_email
    ON public.push_subscriptions (operator_id, lower(user_email))
    WHERE user_email IS NOT NULL;

-- ── 3. news_items ─────────────────────────────────────────────
-- cover_image_url era già nel modello ma potrebbe mancare in tabella.
ALTER TABLE public.news_items
    ADD COLUMN IF NOT EXISTS cover_image_url TEXT;

-- ── 4. operator_users ────────────────────────────────────────
-- Utenti admin per-concessionario (multi-tenant).
-- La tabella può già esistere; IF NOT EXISTS è idempotente.
CREATE TABLE IF NOT EXISTS public.operator_users (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id   UUID        NOT NULL REFERENCES public.operators(id) ON DELETE CASCADE,
    email         TEXT        NOT NULL,
    password_hash TEXT        NOT NULL,
    full_name     TEXT        NOT NULL DEFAULT '',
    is_active     BOOLEAN     NOT NULL DEFAULT true,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_login_at TIMESTAMPTZ,

    CONSTRAINT operator_users_email_key UNIQUE (email)
);

CREATE INDEX IF NOT EXISTS idx_operator_users_email_active
    ON public.operator_users (lower(email))
    WHERE is_active = true;

-- ── 5. operator_registrations ────────────────────────────────
-- Richieste di iscrizione inviate dai concessionari via form pubblico.
-- Status lifecycle: pending → approved | rejected
CREATE TABLE IF NOT EXISTS public.operator_registrations (
    id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    business_name  TEXT        NOT NULL,
    vat_number     TEXT,
    email          TEXT        NOT NULL,
    phone          TEXT,
    contact_person TEXT        NOT NULL,
    address        TEXT,
    city           TEXT,
    province       CHAR(2),
    website        TEXT,
    notes          TEXT,
    status         TEXT        NOT NULL DEFAULT 'pending'
                               CHECK (status IN ('pending', 'approved', 'rejected')),
    reviewed_at    TIMESTAMPTZ,
    review_notes   TEXT,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_operator_registrations_status
    ON public.operator_registrations (status, created_at DESC);

-- ── 6. Vista public_operator_profiles ────────────────────────
-- CREATE OR REPLACE non può cambiare ordine/nomi delle colonne esistenti,
-- quindi si fa DROP + CREATE. CASCADE rimuove eventuali dipendenze
-- (nessuna attesa in questa base di codice).
DROP VIEW IF EXISTS public.public_operator_profiles CASCADE;

CREATE VIEW public.public_operator_profiles AS
SELECT
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
    o.is_active,
    o.created_at,
    o.updated_at,
    o.cover_image_url,
    COALESCE(o.logo_url, ma.public_url) AS logo_url
FROM public.operators o
LEFT JOIN public.media_assets ma
       ON ma.id = o.logo_media_id AND ma.operator_id = o.id;

-- ── 7. Cartella uploads (promemoria) ─────────────────────────
-- I file caricati vengono salvati in wwwroot/uploads/operators/{operatorId}/
-- Non sono richieste modifiche al DB per questo.

-- ── 8. Utilità: genera hash password per SuperAdmin ──────────
-- Eseguire da CLI prima del deploy:
--
--   dotnet run -- hash-password <password_scelta>
--
-- Poi impostare in user-secrets:
--
--   dotnet user-secrets set "SuperAdmin:Username"     "superadmin"
--   dotnet user-secrets set "SuperAdmin:PasswordHash" "<hash>"
--
-- Configurazione SMTP (in user-secrets o variabili d'ambiente):
--
--   dotnet user-secrets set "Smtp:Host"      "smtp.esempio.it"
--   dotnet user-secrets set "Smtp:Port"      "587"
--   dotnet user-secrets set "Smtp:UseSsl"    "true"
--   dotnet user-secrets set "Smtp:Username"  "noreply@esempio.it"
--   dotnet user-secrets set "Smtp:Password"  "<password_smtp>"
--   dotnet user-secrets set "Smtp:FromEmail" "noreply@esempio.it"
--   dotnet user-secrets set "Smtp:FromName"  "MyCars"
