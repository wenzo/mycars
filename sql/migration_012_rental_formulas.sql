-- migration_012_rental_formulas.sql
-- Formule di noleggio per durata, condizioni operative, catalogo servizi, opzione riscatto.
-- Estende operators, vehicles e rentals senza rompere la struttura esistente.

-- ============================================================
-- 1. OPERATORS — condizioni operative e catalogo servizi
-- ============================================================

ALTER TABLE public.operators
    ADD COLUMN IF NOT EXISTS rental_conditions       JSONB,
    ADD COLUMN IF NOT EXISTS rental_services_catalog JSONB;

-- rental_conditions:
--   { "min_driver_age": 21, "min_license_years": 2, "fuel_policy": "full_to_full",
--     "cleaning_penalty_note": "...", "credit_card_required": true,
--     "accepted_payment_methods": ["credit_card","debit_card","cash"],
--     "deposit_default": 500.00 }
--
-- rental_services_catalog:
--   { "included": ["rca","roadside_24h","maintenance"],
--     "optional": [
--       { "key": "damage_waiver", "label": "Riduzione Franchigia", "price_per_day": 15.00 },
--       { "key": "second_driver", "label": "Secondo Conducente",   "price_per_day": 5.00  },
--       { "key": "child_seat",    "label": "Seggiolino bimbo",     "price_flat": 10.00    },
--       { "key": "snow_chains",   "label": "Catene da neve",       "price_flat": 15.00    },
--       { "key": "gps",           "label": "Navigatore GPS",       "price_per_day": 5.00  },
--       { "key": "out_of_hours",  "label": "Consegna Fuori Orario","price_flat": 20.00    },
--       { "key": "straps_kit",    "label": "Kit Cinghie (Furgoni)","price_flat": 10.00    },
--       { "key": "hand_truck",    "label": "Carrello a mano",      "price_flat": 10.00    }
--     ] }

-- ============================================================
-- 2. VEHICLES — formule tariffarie, deposito, riscatto, note
-- ============================================================

ALTER TABLE public.vehicles
    ADD COLUMN IF NOT EXISTS rental_formulas         JSONB,
    ADD COLUMN IF NOT EXISTS rental_redemption       JSONB,
    ADD COLUMN IF NOT EXISTS rental_deposit_override NUMERIC(10,2),
    ADD COLUMN IF NOT EXISTS rental_vehicle_notes    TEXT;

-- rental_formulas:
--   { "daily":    { "price": 45.00, "km_included": 100, "price_extra_km": 0.20 },
--     "weekend":  { "price": 80.00, "km_included": 300, "price_extra_km": 0.18 },
--     "weekly":   { "price": 35.00, "km_included": 700, "price_extra_km": 0.15 },
--     "monthly":  { "price": 900.00,"km_included":3000, "price_extra_km": 0.12 },
--     "mid_term": { "price": 800.00,"km_included":2500, "price_extra_km": 0.12 } }
--   Chiave assente = formula non disponibile per questo veicolo.
--
-- rental_redemption:
--   { "enabled": true, "sale_price": 14500.00,
--     "canoni_discount_pct": 50, "notes": "..." }
--
-- rental_deposit_override: sovrascrive rental_conditions.deposit_default (NULL = usa default operatore)
-- rental_vehicle_notes: stato d'uso visibile al cliente nella pagina pubblica

-- ============================================================
-- 3. RENTALS — formula usata, km, opzioni scelte, prezzi calcolati
-- ============================================================

ALTER TABLE public.rentals
    ADD COLUMN IF NOT EXISTS rental_formula   TEXT,
    ADD COLUMN IF NOT EXISTS km_included      INTEGER,
    ADD COLUMN IF NOT EXISTS price_extra_km   NUMERIC(10,4),
    ADD COLUMN IF NOT EXISTS selected_options JSONB,
    ADD COLUMN IF NOT EXISTS base_price       NUMERIC(10,2),
    ADD COLUMN IF NOT EXISTS options_price    NUMERIC(10,2);

ALTER TABLE public.rentals
    DROP CONSTRAINT IF EXISTS chk_rentals_formula;

ALTER TABLE public.rentals
    ADD CONSTRAINT chk_rentals_formula
    CHECK (rental_formula IS NULL
        OR rental_formula IN ('daily','weekend','weekly','monthly','mid_term'));

-- selected_options:
--   [ { "key": "damage_waiver", "label": "Riduzione Franchigia",
--       "price_per_day": 15.00, "price_flat": null, "qty": 1 } ]
--
-- base_price    = prezzo formula × giorni (calcolato lato app, salvato per storico)
-- options_price = somma opzioni (calcolato lato app, salvato per storico)
-- agreed_price  rimane il campo definitivo (può sovrascrivere il calcolato)

-- ============================================================
-- 4. VISTA public_vehicle_cards — include nuovi campi noleggio
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
    -- Tariffe legacy (mantenute per retrocompatibilità)
    v.rental_price,
    v.rental_weekly_price,
    v.rental_weekend_price,
    -- Noleggio esteso
    v.rental_formulas,
    v.rental_redemption,
    v.rental_deposit_override,
    v.rental_vehicle_notes,
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

-- ============================================================
-- 5. VISTA public_operator_profiles — include catalogo noleggio
-- ============================================================

DROP VIEW IF EXISTS public.public_operator_profiles;
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
    o.tagline,
    o.logo_url,
    o.cover_image_url,
    o.privacy_policy_html,
    o.rental_conditions,
    o.rental_services_catalog
FROM public.operators o
WHERE o.is_active = true;
