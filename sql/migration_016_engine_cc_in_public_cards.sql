-- migration_016_engine_cc_in_public_cards.sql
-- Aggiunge alla view public_vehicle_cards i campi necessari alla ricerca AI:
--   engine_capacity_cc, seats, color, emission_class, doors

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
    v.horsepower_cv,
    v.power_kw,
    v.engine_capacity_cc,
    v.registration_month,
    v.registration_year,
    v.mileage_km,
    v.doors,
    v.seats,
    v.color,
    v.emission_class,
    v.price,
    v.previous_price,
    v.currency,
    v.vat_deductible,
    v.imported,
    v.handicap_accessible,
    v.damaged,
    v.for_sale,
    v.for_rental,
    v.rental_price,
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
