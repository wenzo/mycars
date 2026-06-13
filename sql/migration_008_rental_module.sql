-- migration_008_rental_module.sql
-- Modulo Noleggio: tabelle rentals, rental_photos, estensione vehicles e operators.
-- Livello Base + Semi-strutturato predisposto (tariffario, cauzione, PDF, foto, push).
-- Esclusa: contabilità/fatturazione.

-- ============================================================
-- 1. ENUM
-- ============================================================

DO $$ BEGIN
    CREATE TYPE public.rental_status AS ENUM (
        'booked',    -- prenotato
        'active',    -- consegnato, in corso
        'closed',    -- restituito, concluso
        'cancelled'  -- annullato
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE public.fuel_level AS ENUM (
        'full',
        'three_quarters',
        'half',
        'quarter',
        'empty'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE public.rental_photo_phase AS ENUM (
        'departure',
        'return'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE public.payment_method AS ENUM (
        'cash',
        'pos',
        'transfer'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- ============================================================
-- 2. VEHICLES — campi aggiuntivi noleggio
-- ============================================================

ALTER TABLE public.vehicles
    ADD COLUMN IF NOT EXISTS rental_only          BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS rental_weekly_price  NUMERIC(10,2),
    ADD COLUMN IF NOT EXISTS rental_weekend_price NUMERIC(10,2);

-- rental_only = TRUE → veicolo non compare in vetrina vendita (solo flotta noleggio)
-- rental_price già presente = tariffa giornaliera
-- rental_weekly_price = tariffa settimanale
-- rental_weekend_price = tariffa weekend

CREATE INDEX IF NOT EXISTS ix_vehicles_for_rental
    ON public.vehicles (operator_id, for_rental)
    WHERE for_rental = TRUE AND deleted_at IS NULL;

-- ============================================================
-- 3. OPERATORS — impostazioni modulo noleggio
-- ============================================================

ALTER TABLE public.operators
    ADD COLUMN IF NOT EXISTS rental_module_enabled      BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS rental_photos_enabled      BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS rental_contract_pdf_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS rental_show_prices         BOOLEAN NOT NULL DEFAULT FALSE;

-- ============================================================
-- 4. TABELLA RENTALS
-- ============================================================

CREATE TABLE IF NOT EXISTS public.rentals (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id         UUID NOT NULL REFERENCES public.operators(id) ON DELETE CASCADE,
    vehicle_id          UUID NOT NULL REFERENCES public.vehicles(id),

    -- Cliente
    customer_name       TEXT NOT NULL,
    customer_phone      TEXT,
    customer_license    TEXT,
    customer_fiscal_code TEXT,

    -- Date
    start_date          DATE NOT NULL,
    planned_end_date    DATE NOT NULL,
    actual_end_date     DATE,

    -- Stato veicolo alla consegna / rientro
    km_departure        INTEGER,
    km_return           INTEGER,
    fuel_departure      public.fuel_level,
    fuel_return         public.fuel_level,

    -- Economico (opzionale, semi-strutturato)
    agreed_price        NUMERIC(10,2),
    deposit_amount      NUMERIC(10,2),
    deposit_returned    BOOLEAN NOT NULL DEFAULT FALSE,
    payment_method      public.payment_method,
    is_paid             BOOLEAN NOT NULL DEFAULT FALSE,

    -- Stato workflow
    status              public.rental_status NOT NULL DEFAULT 'booked',

    notes               TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_rentals_dates
        CHECK (planned_end_date >= start_date),
    CONSTRAINT chk_rentals_km
        CHECK (km_return IS NULL OR km_departure IS NULL OR km_return >= km_departure)
);

-- Trigger updated_at
DROP TRIGGER IF EXISTS trg_rentals_updated_at ON public.rentals;
CREATE TRIGGER trg_rentals_updated_at
    BEFORE UPDATE ON public.rentals
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- ============================================================
-- 5. CONTROLLO CONFLITTI (trigger anti-doppio-noleggio)
-- ============================================================

CREATE OR REPLACE FUNCTION public.check_rental_availability()
RETURNS TRIGGER AS $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM public.rentals
        WHERE vehicle_id         = NEW.vehicle_id
          AND status            IN ('booked', 'active')
          AND id                != NEW.id
          AND start_date        <= NEW.planned_end_date
          AND planned_end_date  >= NEW.start_date
    ) THEN
        RAISE EXCEPTION 'Veicolo non disponibile nel periodo richiesto (conflitto con noleggio esistente).';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_rentals_check_availability ON public.rentals;
CREATE TRIGGER trg_rentals_check_availability
    BEFORE INSERT OR UPDATE ON public.rentals
    FOR EACH ROW
    WHEN (NEW.status IN ('booked', 'active'))
    EXECUTE FUNCTION public.check_rental_availability();

-- ============================================================
-- 6. INDICI RENTALS
-- ============================================================

CREATE INDEX IF NOT EXISTS ix_rentals_operator  ON public.rentals (operator_id);
CREATE INDEX IF NOT EXISTS ix_rentals_vehicle   ON public.rentals (vehicle_id);
CREATE INDEX IF NOT EXISTS ix_rentals_status    ON public.rentals (operator_id, status);
CREATE INDEX IF NOT EXISTS ix_rentals_dates     ON public.rentals (operator_id, start_date, planned_end_date);

-- ============================================================
-- 7. TABELLA RENTAL_PHOTOS
-- ============================================================

CREATE TABLE IF NOT EXISTS public.rental_photos (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rental_id   UUID NOT NULL REFERENCES public.rentals(id) ON DELETE CASCADE,
    operator_id UUID NOT NULL REFERENCES public.operators(id) ON DELETE CASCADE,
    phase       public.rental_photo_phase NOT NULL,
    url         TEXT NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_rental_photos_rental
    ON public.rental_photos (rental_id, phase);

-- ============================================================
-- 8. RLS
-- ============================================================

ALTER TABLE public.rentals       ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.rental_photos ENABLE ROW LEVEL SECURITY;

-- Il backend usa service_role → accesso completo
CREATE POLICY rentals_service_role ON public.rentals
    FOR ALL USING (auth.role() = 'service_role');

CREATE POLICY rental_photos_service_role ON public.rental_photos
    FOR ALL USING (auth.role() = 'service_role');

-- ============================================================
-- 9. AGGIORNAMENTO VISTA public_vehicle_cards
--    (aggiunge rental_only, rental_weekly_price, rental_weekend_price)
-- ============================================================

DROP VIEW IF EXISTS public.public_vehicle_cards;
CREATE VIEW public.public_vehicle_cards AS
SELECT
    v.id,
    v.operator_id,
    o.slug              AS operator_slug,
    o.public_code       AS operator_code,
    v.branch_id,
    v.internal_code,
    v.vehicle_type,
    b.name              AS brand_name,
    b.slug              AS brand_slug,
    v.model,
    v.version,
    bt.name             AS body_type_name,
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
    v.vat_deductible,
    v.imported,
    v.handicap_accessible,
    v.for_sale,
    v.for_rental,
    v.rental_only,
    v.rental_price,
    v.rental_weekly_price,
    v.rental_weekend_price,
    v.is_sold,
    v.show_as_sold,
    v.pronta_consegna,
    v.is_nuovo_arrivo,
    v.nuovo_arrivo_until,
    v.description,
    COALESCE(ma.public_url, v.cover_image_url) AS cover_image_url,
    ma.bucket           AS cover_bucket,
    ma.storage_path     AS cover_storage_path,
    br.name             AS branch_name,
    br.city,
    br.province,
    v.created_at,
    v.updated_at
FROM public.vehicles v
JOIN public.operators o
    ON o.id = v.operator_id AND o.is_active = true
JOIN public.brands b
    ON b.id = v.brand_id
LEFT JOIN public.body_types bt
    ON bt.id = v.body_type_id
JOIN public.branches br
    ON br.id = v.branch_id AND br.operator_id = v.operator_id
LEFT JOIN public.vehicle_media vm
    ON vm.vehicle_id = v.id
   AND vm.operator_id = v.operator_id
   AND vm.role = 'cover'
LEFT JOIN public.media_assets ma
    ON ma.id = vm.media_id
   AND ma.operator_id = v.operator_id
WHERE
    v.is_published = true
    AND v.deleted_at IS NULL;
