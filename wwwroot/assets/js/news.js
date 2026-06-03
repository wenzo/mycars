'use strict';

const PAGE_SIZE = 20;

const NEWS_TYPE_LABEL = {
    generic:     'Generica',
    promotion:   'Promozione',
    event:       'Evento',
    new_arrival: 'Nuovo Arrivo',
    financing:   'Finanziamento',
    service:     'Servizio',
};

// ── Lista ─────────────────────────────────────────────────────────────────────

async function loadNews(page = 0) {
    const tbody = document.getElementById('newsTableBody');
    if (!tbody) return;

    const search     = document.getElementById('newsSearch')?.value.trim()         || '';
    const newsType   = document.getElementById('newsTypeFilter')?.value            || '';
    const publishedV = document.getElementById('newsPublishedFilter')?.value       || '';

    const params = new URLSearchParams({ page, pageSize: PAGE_SIZE });
    if (newsType)          params.set('newsType',    newsType);
    if (publishedV !== '') params.set('isPublished', publishedV);

    try {
        const data = await apiFetch(`/api/admin/news?${params}`);
        const { items, totalCount } = data;

        const filtered = search
            ? items.filter(n => (n.title ?? '').toLowerCase().includes(search.toLowerCase()))
            : items;

        if (!filtered.length) {
            tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Nessuna news trovata</td></tr>`;
        } else {
            tbody.innerHTML = filtered.map(n => {
                const stato   = n.isPublished
                    ? '<span class="badge badge-published">Pubblicata</span>'
                    : '<span class="badge badge-draft">Bozza</span>';
                const from    = n.startsAt  ? new Date(n.startsAt).toLocaleDateString('it-IT')  : '—';
                const to      = n.expiresAt ? new Date(n.expiresAt).toLocaleDateString('it-IT') : '—';
                const created = new Date(n.createdAt).toLocaleDateString('it-IT');
                return `
                    <tr>
                        <td class="td-main">${escHtml(n.title)}</td>
                        <td>${NEWS_TYPE_LABEL[n.newsType] ?? n.newsType}</td>
                        <td>${stato}</td>
                        <td>${from}</td>
                        <td>${to}</td>
                        <td>${created}</td>
                        <td class="td-actions">
                            <button class="btn btn-outline btn-sm" data-id="${n.id}">Modifica</button>
                            <button class="btn btn-outline btn-sm" data-notify="${n.id}" data-title="${escHtml(n.title)}" data-excerpt="${escHtml(n.excerpt ?? '')}" title="Invia notifica push">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width:13px;height:13px" aria-hidden="true"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>
                            </button>
                        </td>
                    </tr>`;
            }).join('');

            tbody.querySelectorAll('[data-id]').forEach(btn =>
                btn.addEventListener('click', () => openDrawer(btn.dataset.id)));

            tbody.querySelectorAll('[data-notify]').forEach(btn =>
                btn.addEventListener('click', () =>
                    openPushModal(btn.dataset.notify, btn.dataset.title, btn.dataset.excerpt)));
        }

        const info = document.getElementById('newsPaginationInfo');
        const prev = document.getElementById('newsPrev');
        const next = document.getElementById('newsNext');
        if (info) info.textContent = `${totalCount} news`;
        if (prev) { prev.disabled = page === 0; prev.onclick = () => loadNews(page - 1); }
        if (next) { next.disabled = (page + 1) * PAGE_SIZE >= totalCount; next.onclick = () => loadNews(page + 1); }

    } catch {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Errore caricamento</td></tr>`;
    }
}

function escHtml(str) {
    return (str ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

// ── News Drawer ───────────────────────────────────────────────────────────────

let currentNewsId = null;

async function openDrawer(newsId = null) {
    currentNewsId = newsId;

    const title  = document.getElementById('newsDrawerTitle');
    const delBtn = document.getElementById('newsDrawerDelete');
    const form   = document.getElementById('newsForm');

    form?.reset();
    if (title)  title.textContent    = newsId ? 'Modifica news' : 'Aggiungi news';
    if (delBtn) delBtn.style.display = newsId ? '' : 'none';

    initTinyMCE();

    if (newsId) {
        try {
            const n = await apiFetch(`/api/admin/news/${newsId}`);
            const set = (id, val) => { const el = document.getElementById(id); if (el) el.value = val ?? ''; };
            const chk = (id, val) => { const el = document.getElementById(id); if (el) el.checked = !!val; };
            set('nType',     n.newsType);
            set('nCode',     n.code);
            set('nTitle',    n.title);
            set('nExcerpt',  n.excerpt);
            set('nStartsAt', n.startsAt  ? n.startsAt.split('T')[0]  : '');
            set('nExpiresAt',n.expiresAt ? n.expiresAt.split('T')[0] : '');
            set('nLinkUrl',  n.linkUrl);
            chk('nPublished', n.isPublished);

            const ed = tinymce.get('newsBody');
            if (ed) ed.setContent(n.body ?? '');
            else document.getElementById('newsBody').value = n.body ?? '';
        } catch {
            showToast('Errore caricamento news.', 'error');
        }
    } else {
        const ed = tinymce.get('newsBody');
        if (ed) ed.setContent('');
    }

    document.getElementById('newsDrawerOverlay')?.classList.add('open');
    document.getElementById('newsDrawer')?.classList.add('open');
}

function closeDrawer() {
    document.getElementById('newsDrawerOverlay')?.classList.remove('open');
    document.getElementById('newsDrawer')?.classList.remove('open');
    destroyTinyMCE();
    currentNewsId = null;
}

function initTinyMCE() {
    if (tinymce.get('newsBody')) return;
    tinymce.init({
        selector: '#newsBody',
        base_url: 'https://cdn.jsdelivr.net/npm/tinymce@6',
        suffix: '.min',
        height: 280,
        menubar: false,
        promotion: false,
        branding: false,
        plugins: 'lists link code',
        toolbar: 'bold italic underline | bullist numlist | link | code | removeformat',
        content_style: 'body { font-family: -apple-system, BlinkMacSystemFont, sans-serif; font-size: 14px; }',
    });
}

function destroyTinyMCE() {
    const ed = tinymce.get('newsBody');
    if (ed) ed.remove();
}

// ── Submit news ───────────────────────────────────────────────────────────────

async function saveNews(e) {
    e.preventDefault();

    const saveBtn = document.getElementById('newsDrawerSave');
    if (saveBtn) saveBtn.disabled = true;

    const ed = tinymce.get('newsBody');
    if (ed) ed.save();

    const get = id => document.getElementById(id)?.value.trim() || null;
    const chk = id => document.getElementById(id)?.checked ?? false;

    const startsAtRaw  = get('nStartsAt');
    const expiresAtRaw = get('nExpiresAt');

    const payload = {
        newsType:    document.getElementById('nType')?.value || 'generic',
        code:        get('nCode'),
        title:       get('nTitle') ?? '',
        excerpt:     get('nExcerpt'),
        body:        document.getElementById('newsBody')?.value || null,
        linkUrl:     get('nLinkUrl'),
        startsAt:    startsAtRaw  ? startsAtRaw  + 'T00:00:00Z' : null,
        expiresAt:   expiresAtRaw ? expiresAtRaw + 'T00:00:00Z' : null,
        isPublished: chk('nPublished'),
    };

    const jsonOpts = body => ({ headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });

    try {
        let saved;
        if (currentNewsId) {
            saved = await apiFetch(`/api/admin/news/${currentNewsId}`, { method: 'PUT', ...jsonOpts(payload) });
        } else {
            saved = await apiFetch('/api/admin/news', { method: 'POST', ...jsonOpts(payload) });
        }
        showToast(currentNewsId ? 'News aggiornata.' : 'News creata.');

        const savedId = saved?.id ?? currentNewsId;
        closeDrawer();
        loadNews();

        // Proponi notifica push dopo il salvataggio
        if (savedId) {
            setTimeout(() => openPushModal(savedId, payload.title, payload.excerpt ?? ''), 300);
        }
    } catch (err) {
        showToast(err?.message || 'Errore salvataggio.', 'error');
    } finally {
        if (saveBtn) saveBtn.disabled = false;
    }
}

// ── Delete news ───────────────────────────────────────────────────────────────

async function deleteNews() {
    if (!currentNewsId) return;
    if (!confirm('Eliminare questa news? L\'operazione non è reversibile.')) return;

    try {
        await apiFetch(`/api/admin/news/${currentNewsId}`, { method: 'DELETE' });
        showToast('News eliminata.');
        closeDrawer();
        loadNews();
    } catch {
        showToast('Errore eliminazione.', 'error');
    }
}

// ── Push Notification Modal ───────────────────────────────────────────────────

let currentPushNewsId = null;

function openPushModal(newsId, title = '', body = '') {
    currentPushNewsId = newsId;

    const titleEl = document.getElementById('pnTitle');
    const bodyEl  = document.getElementById('pnBody');
    const result  = document.getElementById('pnResult');

    if (titleEl) titleEl.value = title;
    // Usa excerpt come testo; se vuoto usa titolo troncato
    if (bodyEl)  bodyEl.value  = body || title.substring(0, 100);
    if (result)  { result.style.display = 'none'; result.textContent = ''; }

    // Reset invio subito
    const nowRadio = document.getElementById('pnNow');
    if (nowRadio) { nowRadio.checked = true; toggleScheduleFields(); }

    document.getElementById('pushModalOverlay')?.classList.add('open');
    document.getElementById('pushModal')?.classList.add('open');
}

function closePushModal() {
    document.getElementById('pushModalOverlay')?.classList.remove('open');
    document.getElementById('pushModal')?.classList.remove('open');
    currentPushNewsId = null;
}

function toggleScheduleFields() {
    const isScheduled = document.getElementById('pnScheduled')?.checked;
    const fields = document.getElementById('pnScheduleFields');
    if (fields) fields.style.display = isScheduled ? '' : 'none';
}

async function sendPushNotification() {
    if (!currentPushNewsId) return;

    const title    = document.getElementById('pnTitle')?.value.trim();
    const body     = document.getElementById('pnBody')?.value.trim();
    const isSchd   = document.getElementById('pnScheduled')?.checked;
    const dateVal  = document.getElementById('pnDate')?.value;
    const timeVal  = document.getElementById('pnTime')?.value || '09:00';
    const result   = document.getElementById('pnResult');
    const sendBtn  = document.getElementById('pushModalSend');

    if (!title || !body) { showToast('Titolo e testo sono obbligatori.', 'error'); return; }

    let sendAt = null;
    if (isSchd) {
        if (!dateVal) { showToast('Scegli la data di invio.', 'error'); return; }
        sendAt = new Date(`${dateVal}T${timeVal}:00`).toISOString();
        if (new Date(sendAt) <= new Date()) { showToast('La data pianificata deve essere futura.', 'error'); return; }
    }

    if (sendBtn) sendBtn.disabled = true;

    const jsonOpts = b => ({ headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(b) });

    try {
        const res = await apiFetch(`/api/admin/news/${currentPushNewsId}/notify`, {
            method: 'POST',
            ...jsonOpts({ title, body, sendAt }),
        });

        if (result) {
            result.style.display = 'block';
            if (res.scheduled) {
                const dt = new Date(res.sendAt).toLocaleString('it-IT');
                result.style.background = 'var(--surface2)';
                result.style.color = 'var(--text)';
                result.textContent = `✓ Notifica pianificata per ${dt}.`;
            } else if (res.sent === 0) {
                result.style.background = 'var(--surface2)';
                result.style.color = 'var(--text-mid)';
                result.textContent = 'Nessun dispositivo iscritto alle notifiche.';
            } else {
                result.style.background = '#ecfdf5';
                result.style.color = '#065f46';
                result.textContent = `✓ Notifica inviata a ${res.sent} di ${res.total} dispositivi.`;
            }
        }
        // Chiudi automaticamente dopo 2 secondi
        setTimeout(closePushModal, 2000);
    } catch (err) {
        showToast(err?.message || 'Errore invio notifica.', 'error');
    } finally {
        if (sendBtn) sendBtn.disabled = false;
    }
}

// ── Init ──────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadNews();

    // News drawer
    document.getElementById('addNewsBtn')?.addEventListener('click', () => openDrawer());
    document.getElementById('newsDrawerClose')?.addEventListener('click', closeDrawer);
    document.getElementById('newsDrawerCancel')?.addEventListener('click', closeDrawer);
    document.getElementById('newsDrawerOverlay')?.addEventListener('click', closeDrawer);
    document.getElementById('newsDrawerDelete')?.addEventListener('click', deleteNews);
    document.getElementById('newsForm')?.addEventListener('submit', saveNews);

    // Push modal
    document.getElementById('pushModalClose')?.addEventListener('click', closePushModal);
    document.getElementById('pushModalSkip')?.addEventListener('click', closePushModal);
    document.getElementById('pushModalOverlay')?.addEventListener('click', closePushModal);
    document.getElementById('pushModalSend')?.addEventListener('click', sendPushNotification);
    document.querySelectorAll('input[name="pnWhen"]').forEach(r =>
        r.addEventListener('change', toggleScheduleFields));

    // Filtri lista
    let debounce;
    document.getElementById('newsSearch')?.addEventListener('input', () => {
        clearTimeout(debounce);
        debounce = setTimeout(() => loadNews(0), 300);
    });
    document.getElementById('newsTypeFilter')?.addEventListener('change',      () => loadNews(0));
    document.getElementById('newsPublishedFilter')?.addEventListener('change', () => loadNews(0));
});
