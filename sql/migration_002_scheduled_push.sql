-- ============================================================
-- Migrazione 002: notifiche push pianificate
-- ============================================================

CREATE TABLE IF NOT EXISTS public.scheduled_push_notifications (
    id           uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id  uuid        NOT NULL,
    news_id      uuid        REFERENCES public.news_items(id) ON DELETE SET NULL,
    title        text        NOT NULL,
    body         text        NOT NULL,
    image_url    text,
    scheduled_at timestamptz NOT NULL,
    sent_at      timestamptz,
    error        text,
    created_at   timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_spn_operator_pending
    ON public.scheduled_push_notifications (operator_id, scheduled_at)
    WHERE sent_at IS NULL;
