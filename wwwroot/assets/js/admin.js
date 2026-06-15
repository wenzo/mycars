'use strict';

// ── Sidebar toggle ─────────────────────────────────────────────────────────────

const sidebar   = document.getElementById('sidebar');
const hamburger = document.getElementById('hamburgerBtn');
const overlay   = document.getElementById('sidebarOverlay');

function openSidebar() {
    sidebar?.classList.add('open');
    overlay?.classList.add('visible');
}

function closeSidebar() {
    sidebar?.classList.remove('open');
    overlay?.classList.remove('visible');
}

hamburger?.addEventListener('click', openSidebar);
overlay?.addEventListener('click', closeSidebar);

// ── Active nav item ───────────────────────────────────────────────────────────

(function highlightNav() {
    const current = location.pathname.split('/').pop() || 'index.html';
    document.querySelectorAll('.nav-item[data-page]').forEach(el => {
        if (el.dataset.page === current) {
            el.classList.add('active');
            const group = el.closest('.nav-group');
            group?.classList.add('open');
        }
    });
})();

// ── Nav group toggle (sub-menus) ──────────────────────────────────────────────

document.querySelectorAll('.nav-group-toggle').forEach(toggle => {
    toggle.addEventListener('click', () => {
        toggle.closest('.nav-group')?.classList.toggle('open');
    });
});

// ── Confirm dialogs ───────────────────────────────────────────────────────────

function confirmAction(message, callback) {
    if (confirm(message)) callback();
}

// ── Toast notification ────────────────────────────────────────────────────────

function showToast(message, type = 'success') {
    const existing = document.querySelector('.toast');
    existing?.remove();

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    document.body.appendChild(toast);

    requestAnimationFrame(() => toast.classList.add('visible'));
    setTimeout(() => {
        toast.classList.remove('visible');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// ── API fetch helper ──────────────────────────────────────────────────────────

async function apiFetch(path, options = {}) {
    const res = await fetch(path, { credentials: 'include', ...options });
    if (!res.ok) {
        let msg = `HTTP ${res.status}`;
        try { const b = await res.json(); msg = b.message || msg; } catch {}
        throw new Error(msg);
    }
    if (res.status === 204) return null;
    return res.json();
}

// ── Dashboard: stats cards + nav badges ──────────────────────────────────────

function setBadge(id, count) {
    const el = document.getElementById(id);
    if (!el) return;
    if (count > 0) {
        el.textContent    = String(count);
        el.style.display  = '';
    } else {
        el.style.display  = 'none';
    }
}

async function loadStats() {
    try {
        const stats = await apiFetch('/api/admin/stats');
        Object.entries(stats).forEach(([key, value]) => {
            const el = document.querySelector(`[data-stat="${key}"]`);
            if (el) el.textContent = value;
        });
        setBadge('leadBadge',      stats.lead_aperti);
        setBadge('testDriveBadge', stats.test_drive);
        setBadge('vehiclesBadge',  stats.veicoli_attivi);
        setBadge('newsBadge',      stats.news_pubblicate);
    } catch (err) {
        console.error('Errore caricamento stats:', err);
    }
}

async function loadRentalBadge() {
    try {
        const data = await apiFetch('/api/admin/rentals/dashboard');
        setBadge('noleggiBadge', data.returning_today_count ?? 0);
    } catch { /* silenzioso */ }
}

// ── Dashboard: recent leads table ────────────────────────────────────────────

const LEAD_TIPO_LABEL = {
    info:       'Info',
    test_drive: 'Test Drive',
    offer:      'Offerta',
    financing:  'Finanziamento',
};

const LEAD_STATO_BADGE = {
    new:       '<span class="badge badge-new">Nuovo</span>',
    contacted: '<span class="badge badge-contacted">Contattato</span>',
    closed:    '<span class="badge badge-closed">Chiuso</span>',
    lost:      '<span class="badge badge-lost">Perso</span>',
    spam:      '<span class="badge badge-spam">Spam</span>',
};

async function loadLeadsTable() {
    const tbody = document.getElementById('leadsTableBody');
    if (!tbody) return;

    try {
        const leads = await apiFetch('/api/admin/leads/recent?count=5');
        if (!leads.length) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;color:var(--text-muted);padding:2rem">Nessun lead</td></tr>';
            return;
        }
        tbody.innerHTML = leads.map(l => {
            const date = new Date(l.createdAt).toLocaleDateString('it-IT');
            return `
                <tr>
                    <td class="td-main">${l.fullName}</td>
                    <td style="color:var(--text-muted)">—</td>
                    <td>${LEAD_TIPO_LABEL[l.leadType] ?? l.leadType}</td>
                    <td>${LEAD_STATO_BADGE[l.status] ?? l.status}</td>
                    <td>${date}</td>
                    <td class="td-actions">
                        <a class="btn btn-outline btn-sm" href="/admin/lead.html?open=${l.id}">Apri</a>
                    </td>
                </tr>`;
        }).join('');
    } catch {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;color:var(--text-muted);padding:2rem">Errore caricamento lead</td></tr>';
    }
}

// ── Dashboard: recent vehicles list ──────────────────────────────────────────

async function loadVehiclesList() {
    const list = document.getElementById('vehiclesList');
    if (!list) return;

    try {
        const vehicles = await apiFetch('/api/admin/vehicles/recent?count=5');
        if (!vehicles.length) {
            list.innerHTML = '<tr><td colspan="4" style="text-align:center;color:var(--text-muted);padding:2rem">Nessun veicolo</td></tr>';
            return;
        }
        list.innerHTML = vehicles.map(v => {
            const label = [v.model, v.version].filter(Boolean).join(' ');
            const price = v.price != null
                ? `€ ${Number(v.price).toLocaleString('it-IT')}`
                : '—';
            return `
                <tr>
                    <td class="td-main">${label}</td>
                    <td>
                        ${v.isNuovoArrivo   ? '<span class="badge badge-new">Nuovo Arrivo</span> '    : ''}
                        ${v.prontaConsegna  ? '<span class="badge badge-testdrive">P. Consegna</span>' : ''}
                    </td>
                    <td>${price}</td>
                    <td class="td-actions">
                        <a class="btn btn-outline btn-sm" href="/admin/veicoli.html?open=${v.id}">Modifica</a>
                    </td>
                </tr>`;
        }).join('');
    } catch {
        list.innerHTML = '<tr><td colspan="4" style="text-align:center;color:var(--text-muted);padding:2rem">Errore caricamento veicoli</td></tr>';
    }
}

// ── Init ──────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadStats();
    loadRentalBadge();
    loadLeadsTable();
    loadVehiclesList();
});
