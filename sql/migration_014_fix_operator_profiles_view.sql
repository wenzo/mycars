-- migration_014_fix_operator_profiles_view.sql
-- La vista public_operator_profiles mancava dei flag rental_module_enabled,
-- rental_photos_enabled, rental_contract_pdf_enabled, rental_show_prices
-- aggiunti in migration_008 ma dimenticati in migration_012.

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
    o.address,
    o.city,
    o.province,
    o.zip_code,
    o.primary_color,
    o.secondary_color,
    o.accent_color,
    o.tagline,
    o.logo_url,
    o.cover_image_url,
    o.privacy_policy_html,
    o.rental_module_enabled,
    o.rental_photos_enabled,
    o.rental_contract_pdf_enabled,
    o.rental_show_prices,
    o.rental_conditions,
    o.rental_services_catalog
FROM public.operators o
WHERE o.is_active = true;
