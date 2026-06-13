'use strict';

const PAGE_SIZE = 20;
let currentStatus   = '';
let currentLeadType = '';

// LEAD_TIPO_LABEL e LEAD_STATO_BADGE sono già definiti in admin.js

const LEAD_STATUS_LABEL = {
    new: 'Nuovo', contacted: 'Contattato', closed: 'Chiuso', lost: 'Perso', spam: 'Spam',
};

const LEAD_STATUS_ACTIONS = [
    { status: 'contacted', label: 'Segna Contattato' },
    { status: 'closed',    label: 'Segna Chiuso'     },
    { status: 'lost',      label: 'Segna Perso'      },
    { status: 'spam',      label: 'Spam'              },
];

function escHtml(s) {
    return (s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

// ── Lista lead ────────────────────────────────────────────────────────────────

async function loadLeads(page = 0) {
    const tbody = document.getElementById('leadsTableBody');
    if (!tbody) return;

    const search = document.getElementById('leadSearch')?.value.trim() || '';
    const params = new URLSearchParams({ page, pageSize: PAGE_SIZE });
    if (currentStatus)   params.set('status',   currentStatus);
    if (currentLeadType) params.set('leadType',  currentLeadType);

    try {
        const data = await apiFetch(`/api/admin/leads?${params}`);
        const { items, totalCount } = data;

        const filtered = search
            ? items.filter(l =>
                (l.fullName ?? '').toLowerCase().includes(search.toLowerCase()) ||
                (l.email    ?? '').toLowerCase().includes(search.toLowerCase()) ||
                (l.phone    ?? '').toLowerCase().includes(search.toLowerCase()))
            : items;

        if (!filtered.length) {
            tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:var(--text-muted);padding:2.5rem">Nessun lead trovato</td></tr>`;
        } else {
            tbody.innerHTML = filtered.map(l => {
                const date    = new Date(l.createdAt).toLocaleDateString('it-IT');
                const contact = l.email ?? l.phone ?? '—';
                return `
                    <tr>
                        <td class="td-main">${escHtml(l.fullName)}</td>
                        <td style="font-size:12px;color:var(--text-muted)">${escHtml(contact)}</td>
                        <td>${LEAD_TIPO_LABEL[l.leadType] ?? l.leadType}</td>
                        <td>${LEAD_STATO_BADGE[l.status] ?? l.status}</td>
                        <td>${date}</td>
                        <td class="td-actions">
                            <button class="btn btn-outline btn-sm" onclick="openLead('${l.id}')">Apri</button>
                        </td>
                    </tr>`;
            }).join('');
        }

        const info = document.getElementById('leadsPaginationInfo');
        const prev = document.getElementById('leadsPrev');
        const next = document.getElementById('leadsNext');
        if (info) info.textContent = `${totalCount} lead`;
        if (prev) { prev.disabled = page === 0; prev.onclick = () => loadLeads(page - 1); }
        if (next) { next.disabled = (page + 1) * PAGE_SIZE >= totalCount; next.onclick = () => loadLeads(page + 1); }

    } catch (err) {
        console.error('Errore caricamento lead:', err);
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:var(--text-muted);padding:2.5rem">Errore caricamento</td></tr>`;
    }
}

// ── Drawer dettaglio lead ─────────────────────────────────────────────────────

let _currentLeadId = null;

async function openLead(id) {
    _currentLeadId = id;
    const body = document.getElementById('leadDrawerBody');
    body.innerHTML = '<p style="color:var(--text-muted)">Caricamento…</p>';
    document.getElementById('leadDrawer').classList.add('open');
    document.getElementById('leadDrawerOverlay').classList.add('open');

    try {
        const l = await apiFetch(`/api/admin/leads/${id}`);
        const date = new Date(l.createdAt).toLocaleString('it-IT');
        const tipo = LEAD_TIPO_LABEL[l.leadType] ?? l.leadType;

        const prefDate = l.preferredDate
            ? `<div class="form-group">
                 <label class="form-label">Data preferita</label>
                 <p>${l.preferredDate}${l.preferredTime ? ' alle ' + escHtml(l.preferredTime) : ''}</p>
               </div>`
            : '';
        const msg = l.message
            ? `<div class="form-group">
                 <label class="form-label">Messaggio</label>
                 <p style="white-space:pre-wrap;font-size:14px">${escHtml(l.message)}</p>
               </div>`
            : '';

        const actions = LEAD_STATUS_ACTIONS
            .filter(a => a.status !== l.status)
            .map(a => `<button class="btn btn-sm btn-secondary" onclick="setLeadStatus('${l.id}','${a.status}')">${a.label}</button>`)
            .join('');

        body.innerHTML = `
            <div style="display:flex;flex-direction:column;gap:20px">
                <div>
                    <h3 style="margin:0 0 8px;font-size:18px">${escHtml(l.fullName)}</h3>
                    <div style="display:flex;gap:8px;flex-wrap:wrap">
                        ${LEAD_STATO_BADGE[l.status] ?? `<span class="badge">${escHtml(LEAD_STATUS_LABEL[l.status] ?? l.status)}</span>`}
                        <span class="badge badge-draft">${escHtml(tipo)}</span>
                    </div>
                </div>

                <div>
                    <div class="form-section-title">Contatto</div>
                    ${l.email ? `<div class="form-group"><label class="form-label">Email</label><p><a href="mailto:${escHtml(l.email)}">${escHtml(l.email)}</a></p></div>` : ''}
                    ${l.phone ? `<div class="form-group"><label class="form-label">Telefono</label><p><a href="tel:${escHtml(l.phone)}">${escHtml(l.phone)}</a></p></div>` : ''}
                    <div class="form-group"><label class="form-label">Ricevuto il</label><p style="font-size:13px;color:var(--text-muted)">${date}</p></div>
                </div>

                ${prefDate || msg ? `<div>${prefDate}${msg}</div>` : ''}

                <div>
                    <div class="form-section-title">Aggiorna stato</div>
                    <div style="display:flex;flex-wrap:wrap;gap:8px">${actions || '<span style="color:var(--text-muted);font-size:13px">Nessuna azione disponibile</span>'}</div>
                </div>
            </div>`;
    } catch (err) {
        body.innerHTML = `<p style="color:var(--red)">Errore: ${escHtml(err.message)}</p>`;
    }
}

function closeLead() {
    document.getElementById('leadDrawer').classList.remove('open');
    document.getElementById('leadDrawerOverlay').classList.remove('open');
    _currentLeadId = null;
}

async function setLeadStatus(id, status) {
    try {
        await apiFetch(`/api/admin/leads/${id}/status`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ status }),
        });
        showToast('Stato aggiornato');
        await loadLeads();
        openLead(id);
    } catch (err) {
        showToast('Errore: ' + err.message, 'error');
    }
}

// ── Init ──────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadLeads();

    document.querySelectorAll('#leadStatusTabs .filter-tab').forEach(tab => {
        tab.addEventListener('click', () => {
            document.querySelectorAll('#leadStatusTabs .filter-tab').forEach(t => t.classList.remove('active'));
            tab.classList.add('active');
            currentStatus = tab.dataset.status;
            loadLeads(0);
        });
    });

    document.getElementById('leadTypeFilter')?.addEventListener('change', e => {
        currentLeadType = e.target.value;
        loadLeads(0);
    });

    let debounce;
    document.getElementById('leadSearch')?.addEventListener('input', () => {
        clearTimeout(debounce);
        debounce = setTimeout(() => loadLeads(0), 300);
    });

    document.getElementById('leadDrawerClose')?.addEventListener('click', closeLead);
    document.getElementById('leadDrawerOverlay')?.addEventListener('click', closeLead);

    // Auto-apri lead passato via URL ?open=<id>
    const params = new URLSearchParams(location.search);
    const openId = params.get('open');
    if (openId) openLead(openId);
});
