'use strict';

async function loadOperators() {
    const tbody = document.getElementById('operatorsTableBody');
    if (!tbody) return;

    const search = document.getElementById('operatorSearch')?.value.trim() || '';

    try {
        const operators = await apiFetch('/api/admin/operators');

        const filtered = search
            ? operators.filter(o =>
                (o.businessName ?? '').toLowerCase().includes(search.toLowerCase()) ||
                (o.slug         ?? '').toLowerCase().includes(search.toLowerCase()) ||
                (o.publicCode   ?? '').toLowerCase().includes(search.toLowerCase()))
            : operators;

        if (!filtered.length) {
            tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Nessun operatore trovato</td></tr>`;
            return;
        }

        tbody.innerHTML = filtered.map(o => {
            const attivo = o.isActive
                ? '<span class="badge badge-published">Attivo</span>'
                : '<span class="badge badge-draft">Inattivo</span>';
            const sito = o.websiteUrl
                ? `<a href="${o.websiteUrl}" target="_blank" rel="noopener" style="color:var(--blue)">${o.websiteUrl}</a>`
                : '—';
            const date = new Date(o.createdAt).toLocaleDateString('it-IT');
            return `
                <tr>
                    <td class="td-main">${o.businessName}</td>
                    <td style="font-size:12px">${o.slug}</td>
                    <td style="font-size:12px;color:var(--text-muted)">${o.publicCode}</td>
                    <td style="font-size:12px">${sito}</td>
                    <td>${attivo}</td>
                    <td>${date}</td>
                    <td class="td-actions">
                        <button class="btn btn-outline btn-sm">Dettaglio</button>
                    </td>
                </tr>`;
        }).join('');

    } catch {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Errore caricamento</td></tr>`;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    loadOperators();

    let debounce;
    document.getElementById('operatorSearch')?.addEventListener('input', () => {
        clearTimeout(debounce);
        debounce = setTimeout(loadOperators, 300);
    });
});
