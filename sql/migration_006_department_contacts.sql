-- migration_006_department_contacts.sql
-- Aggiunge responsible_name, phone, email ai reparti

ALTER TABLE public.departments
    ADD COLUMN IF NOT EXISTS responsible_name text,
    ADD COLUMN IF NOT EXISTS phone            text,
    ADD COLUMN IF NOT EXISTS email            text;
