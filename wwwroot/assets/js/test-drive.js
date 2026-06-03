'use strict';

const PAGE_SIZE = 20;

const STATO_BADGE = {
    new:       '<span class="badge badge-new">Nuovo</span>',
    contacted: '<span class="badge badge-contacted">Contattato</span>',
    closed:    '<span class="badge badge-closed">Chiuso</span>',
    lost:      '<span class="badge badge-lost">Perso</span>',
};

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

        const filtered = search
            ? items.filter(l => (l.fullName ?? '').toLowerCase().includes(search.toLowerCase()))
            : items;

        if (!filtered.length) {
            tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Nessuna richiesta test drive</td></tr>`;
        } else {
            tbody.innerHTML = filtered.map(l => {
                const received = new Date(l.createdAt).toLocaleDateString('it-IT');
                const prefDate = l.preferredDate
                    ? new Date(l.preferredDate).toLocaleDateString('it-IT')
                    : '—';
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
                            <button class="btn btn-outline btn-sm">Apri</button>
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
