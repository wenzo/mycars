-- migration_017: tabella embedding vettoriali per ricerca semantica (pgvector)
-- Eseguire su Supabase SQL Editor

-- pgvector è già abilitato su Supabase; il comando è idempotente
CREATE EXTENSION IF NOT EXISTS vector;

-- Tabella embedding per ogni veicolo
CREATE TABLE IF NOT EXISTS public.vehicle_embeddings (
  vehicle_id  UUID PRIMARY KEY REFERENCES public.vehicles(id) ON DELETE CASCADE,
  operator_id UUID NOT NULL,
  embedding   vector(1536),       -- text-embedding-3-small (OpenAI)
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Indice per operator (filtro per concessionaria)
CREATE INDEX IF NOT EXISTS vehicle_embeddings_operator_idx
  ON public.vehicle_embeddings (operator_id);

-- Indice IVFFlat per cosine similarity (usato da <=>)
-- NB: funziona bene con almeno 100 righe; su cataloghi piccoli usa scansione lineare ugualmente efficace
CREATE INDEX IF NOT EXISTS vehicle_embeddings_ivfflat_idx
  ON public.vehicle_embeddings
  USING ivfflat (embedding vector_cosine_ops)
  WITH (lists = 50);

-- RLS: il service role può leggere/scrivere, gli utenti anonimi non accedono
ALTER TABLE public.vehicle_embeddings ENABLE ROW LEVEL SECURITY;

CREATE POLICY "service_role_all" ON public.vehicle_embeddings
  FOR ALL TO service_role USING (true) WITH CHECK (true);
