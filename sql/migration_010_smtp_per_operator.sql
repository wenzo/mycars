-- migration_010_smtp_per_operator.sql
-- Aggiunge le colonne SMTP per-operatore alla tabella operators.
-- Eseguire nel SQL Editor di Supabase.

ALTER TABLE public.operators
  ADD COLUMN IF NOT EXISTS smtp_host       TEXT,
  ADD COLUMN IF NOT EXISTS smtp_port       INTEGER,
  ADD COLUMN IF NOT EXISTS smtp_use_ssl    BOOLEAN NOT NULL DEFAULT TRUE,
  ADD COLUMN IF NOT EXISTS smtp_username   TEXT,
  ADD COLUMN IF NOT EXISTS smtp_password   TEXT,
  ADD COLUMN IF NOT EXISTS smtp_from_email TEXT,
  ADD COLUMN IF NOT EXISTS smtp_from_name  TEXT;
