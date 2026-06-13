'use strict';

const PAGE_SIZE = 20;

const STATO_BADGE = {
    new:       '<span class="badge badge-new">Nuovo</span>',
    contacted: '<span class="badge badge-contacted">Contattato</span>',
    closed:    '<span class="badge badge-closed">Chiuso</span>',
    lost:      '<span class="badge badge-lost">Perso</span>',
};

const STATO_LABEL = {
    new:       'Nuovo',
    contacted: 'Contattato',
    closed:    'Chiuso',
    lost:      'Perso',
};

let _leadsCache = [];

function fmtDate(val) {
    if (!val) return '—';
    const d = new Date(val);
    if (isNaN(d.getTime()) || d.getFullYear() < 2000) return '—';
    return d.toLocaleDateString('it-IT');
}

function openLeadDetail(id) {
    const l = _leadsCache.find(x => x.id === id);
    if (!l) return;

    const prefDate = fmtDate(l.preferredDate);
    const prefTime = l.preferredTime ?? '—';
    const received = fmtDate(l.createdAt);

    const overlay = document.createElement('div');
    overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:1000;display:flex;align-items:center;justify-content:center;padding:16px';
    overlay.innerHTML = `
        <div style="background:#fff;border-radius:16px;width:100%;max-width:480px;overflow:hidden;box-shadow:0 20px 60px rgba(0,0,0,.2)">
            <div style="background:var(--navy);padding:18px 20px;display:flex;align-items:center;justify-content:space-between">
                <div style="font-size:16px;font-weight:700;color:#fff">Richiesta Test Drive</div>
                <button id="tdDetailClose" style="background:rgba(255,255,255,.15);border:none;border-radius:50%;width:30px;height:30px;color:#fff;cursor:pointer;font-size:16px;display:flex;align-items:center;justify-content:center">✕</button>
            </div>
            <div style="padding:20px;display:flex;flex-direction:column;gap:14px">
                <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
                    <div>
                        <div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:3px">Cliente</div>
                        <div style="font-size:14px;font-weight:600;color:var(--text)">${l.fullName}</div>
                    </div>
                    <div>
                        <div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:3px">Ricevuta</div>
                        <div style="font-size:14px;color:var(--text)">${received}</div>
                    </div>
                    ${l.phone ? `<div>
                        <div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:3px">Telefono</div>
                        <div style="font-size:14px;color:var(--text)">${l.phone}</div>
                    </div>` : ''}
                    ${l.email ? `<div>
                        <div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:3px">Email</div>
                        <div style="font-size:14px;color:var(--text)">${l.email}</div>
                    </div>` : ''}
                    <div>
                        <div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:3px">Data preferita</div>
                        <div style="font-size:14px;color:var(--text)">${prefDate}</div>
                    </div>
                    <div>
                        <div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:3px">Orario preferito</div>
                        <div style="font-size:14px;color:var(--text)">${prefTime}</div>
                    </div>
                </div>
                ${l.message ? `<div>
                    <div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:5px">Messaggio</div>
                    <div style="font-size:13px;color:var(--text);background:var(--surface);border-radius:8px;padding:10px 12px;line-height:1.5">${l.message}</div>
                </div>` : ''}
                <div>
                    <div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:5px">Stato</div>
                    <select id="tdDetailStatus" style="height:38px;border:2px solid var(--border);border-radius:8px;padding:0 10px;font-size:14px;width:100%">
                        ${Object.entries(STATO_LABEL).map(([v, t]) => `<option value="${v}" ${l.status === v ? 'selected' : ''}>${t}</option>`).join('')}
                    </select>
                </div>
                <button id="tdDetailSave" style="height:42px;background:var(--navy);color:#fff;border:none;border-radius:10px;font-size:14px;font-weight:700;cursor:pointer">
                    Salva stato
                </button>
            </div>
        </div>`;

    document.body.appendChild(overlay);
    overlay.addEventListener('click', e => { if (e.target === overlay) overlay.remove(); });
    overlay.querySelector('#tdDetailClose').addEventListener('click', () => overlay.remove());
    overlay.querySelector('#tdDetailSave').addEventListener('click', async () => {
        const newStatus = overlay.querySelector('#tdDetailStatus').value;
        try {
            await apiFetch(`/api/admin/leads/${l.id}/status`, { method: 'PATCH', body: JSON.stringify({ status: newStatus }) });
            l.status = newStatus;
            overlay.remove();
            loadTestDrives();
        } catch {
            alert('Errore durante il salvataggio dello stato.');
        }
    });
}

async function loadTestDrives(page = 0) {
    const tbody = document.getElementById('testDriveTableBody');
    if (!tbody) return;

    const search = document.getElementById('tdSearch')?.value.trim() || '';
    const status = document.getElementById('tdStatusFilter')?.value  || '';

    const params = new URLSearchParams({ page, pageSize: PAGE_SIZE, leadType: 'test_drive' });
    if (status) params.set('status', status);

    try {
        const data = await apiFetch(`/api/admin/leads?${params}`);
        const { items, totalCount } = data;
        _leadsCache = items;

        const filtered = search
            ? items.filter(l => (l.fullName ?? '').toLowerCase().includes(search.toLowerCase()))
            : items;

        if (!filtered.length) {
            tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Nessuna richiesta test drive</td></tr>`;
        } else {
            tbody.innerHTML = filtered.map(l => {
                const received = fmtDate(l.createdAt);
                const prefDate = fmtDate(l.preferredDate);
                const prefTime = l.preferredTime ?? '—';
                const contact  = l.email ?? l.phone ?? '—';
                return `
                    <tr>
                        <td class="td-main">${l.fullName}</td>
                        <td style="font-size:12px;color:var(--text-muted)">${contact}</td>
                        <td>${prefDate}</td>
                        <td>${prefTime}</td>
                        <td>${STATO_BADGE[l.status] ?? l.status}</td>
                        <td>${received}</td>
                        <td class="td-actions">
                            <button class="btn btn-outline btn-sm" onclick="openLeadDetail('${l.id}')">Apri</button>
                        </td>
                    </tr>`;
            }).join('');
        }

        const info = document.getElementById('tdPaginationInfo');
        const prev = document.getElementById('tdPrev');
        const next = document.getElementById('tdNext');
        if (info) info.textContent = `${totalCount} richieste`;
        if (prev) { prev.disabled = page === 0; prev.onclick = () => loadTestDrives(page - 1); }
        if (next) { next.disabled = (page + 1) * PAGE_SIZE >= totalCount; next.onclick = () => loadTestDrives(page + 1); }

    } catch {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Errore caricamento</td></tr>`;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    loadTestDrives();

    let debounce;
    document.getElementById('tdSearch')?.addEventListener('input', () => {
        clearTimeout(debounce);
        debounce = setTimeout(() => loadTestDrives(0), 300);
    });
    document.getElementById('tdStatusFilter')?.addEventListener('change', () => loadTestDrives(0));
});
