'use strict';

const PAGE_SIZE = 20;
let currentStatus   = '';
let currentLeadType = '';

const LEAD_TIPO_LABEL = {
    info: 'Info', test_drive: 'Test Drive', offer: 'Offerta', financing: 'Finanziamento',
};

const LEAD_STATO_BADGE = {
    new:       '<span class="badge badge-new">Nuovo</span>',
    contacted: '<span class="badge badge-contacted">Contattato</span>',
    closed:    '<span class="badge badge-closed">Chiuso</span>',
    lost:      '<span class="badge badge-lost">Perso</span>',
    spam:      '<span class="badge badge-spam">Spam</span>',
};

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
                        <td class="td-main">${l.fullName}</td>
                        <td style="font-size:12px;color:var(--text-muted)">${contact}</td>
                        <td>${LEAD_TIPO_LABEL[l.leadType] ?? l.leadType}</td>
                        <td>${LEAD_STATO_BADGE[l.status] ?? l.status}</td>
                        <td>${date}</td>
                        <td class="td-actions">
                            <button class="btn btn-outline btn-sm">Apri</button>
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

    } catch {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:var(--text-muted);padding:2.5rem">Errore caricamento</td></tr>`;
    }
}

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
});
