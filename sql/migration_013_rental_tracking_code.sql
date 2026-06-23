-- migration_013_rental_tracking_code.sql
-- Aggiunge il codice di tracciamento alle richieste noleggio (vehicle_leads)
-- e aggiunge 'rental' al tipo lead_type.

-- ============================================================
-- 1. lead_type enum — aggiungi 'rental'
-- ============================================================

ALTER TYPE public.lead_type ADD VALUE IF NOT EXISTS 'rental';

-- ============================================================
-- 2. vehicle_leads — colonna tracking_code
-- ============================================================

ALTER TABLE public.vehicle_leads
    ADD COLUMN IF NOT EXISTS tracking_code TEXT;

CREATE UNIQUE INDEX IF NOT EXISTS uix_vehicle_leads_tracking_code
    ON public.vehicle_leads (tracking_code)
    WHERE tracking_code IS NOT NULL;
