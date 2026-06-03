-- ============================================================
-- Migrazione 003: topic di iscrizione alle notifiche push
--
-- topic_general  → comunicazioni generali del concessionario
-- topic_vehicles → notifiche su veicoli (nuovo arrivo, pronta consegna…)
-- topic_news     → notifiche su news e promozioni
--
-- Default: tutti e tre abilitati (comportamento precedente invariato).
-- ============================================================

-- ── 1. Topic sulle subscription ───────────────────────────

ALTER TABLE public.push_subscriptions
    ADD COLUMN IF NOT EXISTS topic_general  boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS topic_vehicles boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS topic_news     boolean NOT NULL DEFAULT true;

CREATE INDEX IF NOT EXISTS ix_push_sub_topic_vehicles
    ON public.push_subscriptions (operator_id, topic_vehicles)
    WHERE topic_vehicles = true;

CREATE INDEX IF NOT EXISTS ix_push_sub_topic_news
    ON public.push_subscriptions (operator_id, topic_news)
    WHERE topic_news = true;

CREATE INDEX IF NOT EXISTS ix_push_sub_topic_general
    ON public.push_subscriptions (operator_id, topic_general)
    WHERE topic_general = true;

-- ── 2. Topic sulle notifiche pianificate ──────────────────

ALTER TABLE public.scheduled_push_notifications
    ADD COLUMN IF NOT EXISTS topic text NOT NULL DEFAULT 'general';
        -- valori: 'general' | 'news' | 'vehicles'

-- ── Fine migration 003 ────────────────────────────────────
