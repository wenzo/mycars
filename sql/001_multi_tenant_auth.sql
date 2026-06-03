-- =============================================================================
-- MyCars — Migrazione 001: struttura multi-tenant completa
-- =============================================================================
-- Eseguire nella SQL Editor di Supabase come utente con permessi DDL.
-- Gestisce sia un database nuovo sia uno già parzialmente inizializzato.
-- =============================================================================


-- -----------------------------------------------------------------------------
-- 1. OPERATOR_USERS
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS public.operator_users (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id     UUID        NOT NULL REFERENCES public.operators(id) ON DELETE CASCADE,
    email           TEXT        NOT NULL,
    password_hash   TEXT        NOT NULL,
    full_name       TEXT        NOT NULL DEFAULT '',
    is_active       BOOL        NOT NULL DEFAULT true,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_login_at   TIMESTAMPTZ,
    CONSTRAINT operator_users_email_key UNIQUE (email)
);

COMMENT ON TABLE  public.operator_users               IS 'Account di accesso al portale per ogni concessionaria';
COMMENT ON COLUMN public.operator_users.password_hash IS 'BCrypt workFactor 12';


-- -----------------------------------------------------------------------------
-- 2. DEPARTMENTS (reparti)
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS public.departments (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id UUID        NOT NULL REFERENCES public.operators(id) ON DELETE CASCADE,
    branch_id   UUID        REFERENCES public.branches(id) ON DELETE SET NULL,
    name        TEXT        NOT NULL,
    description TEXT,
    sort_order  INT         NOT NULL DEFAULT 0,
    is_active   BOOL        NOT NULL DEFAULT true,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_departments_operator ON public.departments(operator_id);
CREATE INDEX IF NOT EXISTS idx_departments_branch   ON public.departments(branch_id);


-- -----------------------------------------------------------------------------
-- 3. PUSH_SUBSCRIPTIONS (Web Push VAPID)
--    Se la tabella esiste già (es. con la vecchia colonna fcm_token)
--    la ricrea con lo schema VAPID corretto.
-- -----------------------------------------------------------------------------

DO $$
BEGIN
    -- Controlla se la colonna 'endpoint' esiste già
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name   = 'push_subscriptions'
          AND column_name  = 'endpoint'
    ) THEN
        -- La tabella esiste ma ha lo schema vecchio (fcm_token) → la ricrea
        DROP TABLE IF EXISTS public.push_subscriptions CASCADE;
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS public.push_subscriptions (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id     UUID        REFERENCES public.operators(id) ON DELETE CASCADE,
    vehicle_id      UUID        REFERENCES public.vehicles(id)  ON DELETE SET NULL,
    endpoint        TEXT        NOT NULL,
    p256dh          TEXT        NOT NULL,
    auth            TEXT        NOT NULL,
    device_type     TEXT        NOT NULL DEFAULT 'web',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_active_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT push_subscriptions_endpoint_key UNIQUE (endpoint)
);

CREATE INDEX IF NOT EXISTS idx_push_operator ON public.push_subscriptions(operator_id);
CREATE INDEX IF NOT EXISTS idx_push_vehicle  ON public.push_subscriptions(vehicle_id);

COMMENT ON TABLE  public.push_subscriptions        IS 'Sottoscrizioni Web Push VAPID per operatore';
COMMENT ON COLUMN public.push_subscriptions.endpoint IS 'URL del push service (chiave logica univoca)';
COMMENT ON COLUMN public.push_subscriptions.p256dh   IS 'Chiave pubblica ECDH P-256 del dispositivo (base64url)';
COMMENT ON COLUMN public.push_subscriptions.auth     IS 'Auth secret del dispositivo (base64url)';


-- -----------------------------------------------------------------------------
-- 4. VEHICLES — colonna TARGA
-- -----------------------------------------------------------------------------

ALTER TABLE public.vehicles
    ADD COLUMN IF NOT EXISTS targa TEXT;

CREATE INDEX IF NOT EXISTS idx_vehicles_targa
    ON public.vehicles(targa)
    WHERE targa IS NOT NULL;

COMMENT ON COLUMN public.vehicles.targa IS 'Targa automobilistica (es. AB123CD)';


-- -----------------------------------------------------------------------------
-- 5. VEHICLES — colonna DEPARTMENT_ID
-- -----------------------------------------------------------------------------

ALTER TABLE public.vehicles
    ADD COLUMN IF NOT EXISTS department_id UUID REFERENCES public.departments(id) ON DELETE SET NULL;


-- -----------------------------------------------------------------------------
-- 6. VISTA PUBBLICA operator_user_profiles (senza password_hash)
-- -----------------------------------------------------------------------------

CREATE OR REPLACE VIEW public.operator_user_profiles AS
SELECT id, operator_id, email, full_name, is_active, created_at, last_login_at
FROM public.operator_users;


-- =============================================================================
-- RIEPILOGO
-- =============================================================================
--  NUOVE:      operator_users, departments, push_subscriptions (ricreata)
--  MODIFICATE: vehicles (+targa, +department_id)
-- =============================================================================
