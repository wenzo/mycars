'use strict';

// ── State ─────────────────────────────────────────────────────────────────────

let allRentals   = [];
let allVehicles  = [];
let currentPage  = 0;
let totalRentals = 0;
const PAGE_SIZE  = 20;
let editingId    = null;
let checkAvailTimer = null;

const STATUS_LABEL = {
    booked:    'Prenotato',
    active:    'In corso',
    closed:    'Concluso',
    cancelled: 'Annullato',
};
const STATUS_CLASS = {
    booked:    'badge-warning',
    active:    'badge-published',
    closed:    'badge-draft',
    cancelled: 'badge-draft',
};
const FUEL_LABEL = {
    full:           'Pieno',
    three_quarters: '3/4',
    half:           'Metà',
    quarter:        '1/4',
    empty:          'Vuoto',
};

// ── Helpers ───────────────────────────────────────────────────────────────────

function escHtml(s) {
    return (s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
}

function fmtDate(iso) {
    if (!iso) return '—';
    const [y, m, d] = iso.split('T')[0].split('-');
    return `${d}/${m}/${y}`;
}

function fmtPrice(v) {
    return v != null ? `€ ${parseFloat(v).toFixed(2)}` : '—';
}

// ── Dashboard KPI ─────────────────────────────────────────────────────────────

async function loadDashboard() {
    try {
        const data = await apiFetch('/api/admin/rentals/dashboard');
        document.getElementById('kpiActive').textContent    = data.active_count ?? 0;
        document.getElementById('kpiBooked').textContent    = data.booked_count ?? 0;
        document.getElementById('kpiReturning').textContent = data.returning_today_count ?? 0;
        const badge = document.getElementById('noleggiBadge');
        if (badge && data.returning_today_count > 0) {
            badge.textContent = data.returning_today_count;
            badge.style.display = '';
        }
    } catch { /* silenzioso */ }
}

// ── Carica lista ──────────────────────────────────────────────────────────────

async function loadRentals(page = 0) {
    currentPage = page;
    const tbody  = document.getElementById('rentalsTableBody');
    const status = document.getElementById('statusFilter')?.value ?? '';

    try {
        let url = `/api/admin/rentals?page=${page}&pageSize=${PAGE_SIZE}`;
        if (status) url += `&status=${status}`;
        const data = await apiFetch(url);
        allRentals   = data.items ?? [];
        totalRentals = data.totalCount ?? 0;
        renderRentals();
        updatePagination();
    } catch {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Errore caricamento</td></tr>`;
    }
}

function renderRentals() {
    const tbody  = document.getElementById('rentalsTableBody');
    const search = (document.getElementById('rentalSearch')?.value ?? '').toLowerCase();

    const items = search
        ? allRentals.filter(r =>
            (r.customerName ?? '').toLowerCase().includes(search) ||
            ((r.vehicleBrand ?? '') + ' ' + (r.vehicleModel ?? '')).toLowerCase().includes(search) ||
            (r.vehicleTarga ?? '').toLowerCase().includes(search))
        : allRentals;

    if (!items.length) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Nessun noleggio trovato</td></tr>`;
        return;
    }

    tbody.innerHTML = items.map(r => {
        const vDesc = escHtml([r.vehicleBrand, r.vehicleModel, r.vehicleTarga].filter(Boolean).join(' · '));
        const badge = `<span class="badge ${STATUS_CLASS[r.status] ?? 'badge-draft'}">${STATUS_LABEL[r.status] ?? r.status}</span>`;
        return `
        <tr>
            <td><strong>${escHtml(r.customerName)}</strong><br><small>${escHtml(r.customerPhone ?? '')}</small></td>
            <td>${vDesc}</td>
            <td>${fmtDate(r.startDate)}</td>
            <td>${fmtDate(r.plannedEndDate)}</td>
            <td>${badge}</td>
            <td>${fmtPrice(r.agreedPrice)}</td>
            <td style="white-space:nowrap">
                ${r.status === 'booked' ? `<button class="btn btn-sm btn-secondary" onclick="openActivate('${r.id}')">Consegna</button> ` : ''}
                ${r.status === 'active' ? `<button class="btn btn-sm btn-secondary" onclick="openClose('${r.id}')">Rientro</button> ` : ''}
                ${(r.status === 'booked' || r.status === 'active') ? `<button class="btn btn-sm btn-secondary" onclick="openEdit('${r.id}')">Modifica</button> ` : ''}
                <a class="btn btn-sm btn-secondary" href="/api/admin/rentals/${r.id}/contract" target="_blank">PDF</a>
                ${(r.status === 'booked' || r.status === 'active') ? `<button class="btn btn-sm btn-danger" onclick="doCancel('${r.id}')">Annulla</button>` : ''}
            </td>
        </tr>`;
    }).join('');
}

function updatePagination() {
    const info  = document.getElementById('rentalsPaginationInfo');
    const prev  = document.getElementById('rentalsPrev');
    const next  = document.getElementById('rentalsNext');
    const start = currentPage * PAGE_SIZE + 1;
    const end   = Math.min(start + PAGE_SIZE - 1, totalRentals);
    if (info) info.textContent = totalRentals > 0 ? `${start}–${end} di ${totalRentals}` : '0 risultati';
    if (prev) prev.disabled = currentPage === 0;
    if (next) next.disabled = end >= totalRentals;
}

// ── Carica veicoli noleggiabili ───────────────────────────────────────────────

async function loadRentalVehicles() {
    try {
        const data = await apiFetch('/api/admin/vehicles?forRental=true&pageSize=100');
        allVehicles = (data.items ?? []).filter(v => !v.isSold && !v.deletedAt);
        const sel = document.getElementById('rVehicle');
        if (!sel) return;
        allVehicles.forEach(v => {
            const opt = document.createElement('option');
            opt.value = v.id;
            const label = [v.brandName, v.model, v.targa ? '– ' + v.targa : v.internalCode]
                .filter(Boolean).join(' ');
            opt.textContent = label.trim() || v.id;
            sel.appendChild(opt);
        });
    } catch (e) { console.error('Errore caricamento veicoli:', e); }
}

// ── Drawer: Apri nuovo ────────────────────────────────────────────────────────

function openNew() {
    editingId = null;
    resetForm();
    document.getElementById('rentalDrawerTitle').textContent = 'Nuovo noleggio';
    const today = new Date().toISOString().slice(0, 10);
    document.getElementById('rStartDate').value = today;
    document.getElementById('rEndDate').value   = today;
    openDrawer();
}

function openEdit(id) {
    const rental = allRentals.find(r => r.id === id);
    if (!rental) return;
    editingId = id;
    resetForm();
    document.getElementById('rentalDrawerTitle').textContent = 'Modifica noleggio';
    document.getElementById('rId').value           = id;
    document.getElementById('rVehicle').value      = rental.vehicleId;
    document.getElementById('rVehicle').disabled   = true;
    document.getElementById('rStartDate').value    = rental.startDate?.slice(0, 10) ?? '';
    document.getElementById('rEndDate').value      = rental.plannedEndDate?.slice(0, 10) ?? '';
    document.getElementById('rCustomerName').value = rental.customerName ?? '';
    document.getElementById('rCustomerPhone').value = rental.customerPhone ?? '';
    document.getElementById('rCustomerLicense').value = rental.customerLicense ?? '';
    document.getElementById('rCustomerCf').value   = rental.customerFiscalCode ?? '';
    document.getElementById('rAgreedPrice').value  = rental.agreedPrice ?? '';
    document.getElementById('rDeposit').value      = rental.depositAmount ?? '';
    document.getElementById('rPaymentMethod').value = rental.paymentMethod ?? '';
    document.getElementById('rDepositReturned').checked = rental.depositReturned ?? false;
    document.getElementById('rIsPaid').checked     = rental.isPaid ?? false;
    document.getElementById('rNotes').value        = rental.notes ?? '';
    openDrawer();
}

function openDrawer() {
    document.getElementById('rentalDrawer').classList.add('open');
    document.getElementById('rentalDrawerOverlay').classList.add('open');
}

function closeDrawer() {
    document.getElementById('rentalDrawer').classList.remove('open');
    document.getElementById('rentalDrawerOverlay').classList.remove('open');
    document.getElementById('rVehicle').disabled = false;
}

function resetForm() {
    document.getElementById('rentalForm').reset();
    document.getElementById('rId').value = '';
    document.getElementById('availabilityMsg').textContent = '';
    document.getElementById('rVehicle').disabled = false;
}

// ── Verifica disponibilità ────────────────────────────────────────────────────

function scheduleAvailCheck() {
    clearTimeout(checkAvailTimer);
    checkAvailTimer = setTimeout(checkAvailability, 400);
}

async function checkAvailability() {
    const vehicleId = document.getElementById('rVehicle').value;
    const startDate = document.getElementById('rStartDate').value;
    const endDate   = document.getElementById('rEndDate').value;
    const msg       = document.getElementById('availabilityMsg');

    if (!vehicleId || !startDate || !endDate || endDate < startDate) {
        msg.textContent = '';
        return;
    }
    try {
        let url = `/api/admin/rentals/availability?vehicleId=${vehicleId}&startDate=${startDate}&endDate=${endDate}`;
        if (editingId) url += `&excludeRentalId=${editingId}`;
        const data = await apiFetch(url);
        if (data.available) {
            msg.innerHTML = '<span style="color:var(--green)">✓ Disponibile nel periodo selezionato</span>';
        } else {
            msg.innerHTML = '<span style="color:var(--red)">✗ Veicolo non disponibile nel periodo</span>';
        }
    } catch {
        msg.textContent = '';
    }
}

// ── Submit form ───────────────────────────────────────────────────────────────

async function submitRental(e) {
    e.preventDefault();
    const btn = document.getElementById('rentalSaveBtn');
    btn.disabled = true;
    btn.textContent = 'Salvataggio…';

    const payload = {
        vehicleId:         document.getElementById('rVehicle').value,
        customerName:      document.getElementById('rCustomerName').value.trim(),
        customerPhone:     document.getElementById('rCustomerPhone').value.trim() || null,
        customerLicense:   document.getElementById('rCustomerLicense').value.trim() || null,
        customerFiscalCode:document.getElementById('rCustomerCf').value.trim() || null,
        startDate:         document.getElementById('rStartDate').value,
        plannedEndDate:    document.getElementById('rEndDate').value,
        agreedPrice:       parseFloat(document.getElementById('rAgreedPrice').value) || null,
        depositAmount:     parseFloat(document.getElementById('rDeposit').value) || null,
        paymentMethod:     document.getElementById('rPaymentMethod').value || null,
        depositReturned:   document.getElementById('rDepositReturned').checked,
        isPaid:            document.getElementById('rIsPaid').checked,
        notes:             document.getElementById('rNotes').value.trim() || null,
    };

    try {
        if (editingId) {
            await apiFetch(`/api/admin/rentals/${editingId}`, { method: 'PUT', ...jsonOpts(payload) });
        } else {
            await apiFetch('/api/admin/rentals', { method: 'POST', ...jsonOpts(payload) });
        }
        closeDrawer();
        await loadRentals(currentPage);
        await loadDashboard();
    } catch (err) {
        alert('Errore: ' + (err.message ?? 'Errore salvataggio'));
    } finally {
        btn.disabled = false;
        btn.textContent = 'Salva noleggio';
    }
}

// ── Azioni stato ──────────────────────────────────────────────────────────────

function openActivate(id) {
    const modal = document.getElementById('actionModal');
    document.getElementById('actionModalTitle').textContent = 'Registra Consegna';
    document.getElementById('actionModalBody').innerHTML = `
        <div class="form-group"><label class="form-label">Km alla partenza</label>
            <input class="form-control" type="number" id="actKm" min="0" placeholder="Es. 45230"></div>
        <div class="form-group"><label class="form-label">Carburante partenza</label>
            <select class="form-select" id="actFuel">
                <option value="">— Seleziona —</option>
                <option value="full">Pieno</option>
                <option value="three_quarters">3/4</option>
                <option value="half">Metà</option>
                <option value="quarter">1/4</option>
                <option value="empty">Vuoto</option>
            </select></div>`;
    document.getElementById('actionModalFooter').innerHTML =
        `<button class="btn btn-primary" onclick="doActivate('${id}')">Conferma consegna</button>
         <button class="btn btn-secondary" onclick="closeActionModal()">Annulla</button>`;
    openActionModal();
}

function openClose(id) {
    const today = new Date().toISOString().slice(0, 10);
    document.getElementById('actionModalTitle').textContent = 'Registra Rientro';
    document.getElementById('actionModalBody').innerHTML = `
        <div class="form-group"><label class="form-label">Data rientro effettivo</label>
            <input class="form-control" type="date" id="actEndDate" value="${today}"></div>
        <div class="form-group"><label class="form-label">Km al rientro</label>
            <input class="form-control" type="number" id="actKmRet" min="0" placeholder="Es. 45800"></div>
        <div class="form-group"><label class="form-label">Carburante rientro</label>
            <select class="form-select" id="actFuelRet">
                <option value="">— Seleziona —</option>
                <option value="full">Pieno</option>
                <option value="three_quarters">3/4</option>
                <option value="half">Metà</option>
                <option value="quarter">1/4</option>
                <option value="empty">Vuoto</option>
            </select></div>`;
    document.getElementById('actionModalFooter').innerHTML =
        `<button class="btn btn-primary" onclick="doClose('${id}')">Conferma rientro</button>
         <button class="btn btn-secondary" onclick="closeActionModal()">Annulla</button>`;
    openActionModal();
}

function openActionModal() {
    document.getElementById('actionModalOverlay').style.display = 'flex';
}
function closeActionModal() {
    document.getElementById('actionModalOverlay').style.display = 'none';
}

async function doActivate(id) {
    const km   = parseInt(document.getElementById('actKm')?.value) || null;
    const fuel = document.getElementById('actFuel')?.value || null;
    try {
        await apiFetch(`/api/admin/rentals/${id}/activate`, { method: 'POST', ...jsonOpts({ kmDeparture: km, fuelDeparture: fuel }) });
        closeActionModal();
        await loadRentals(currentPage);
        await loadDashboard();
    } catch (err) { alert('Errore: ' + (err.message ?? '')); }
}

async function doClose(id) {
    const endDate = document.getElementById('actEndDate')?.value || null;
    const km      = parseInt(document.getElementById('actKmRet')?.value) || null;
    const fuel    = document.getElementById('actFuelRet')?.value || null;
    try {
        await apiFetch(`/api/admin/rentals/${id}/close`, { method: 'POST', ...jsonOpts({ actualEndDate: endDate, kmReturn: km, fuelReturn: fuel }) });
        closeActionModal();
        await loadRentals(currentPage);
        await loadDashboard();
    } catch (err) { alert('Errore: ' + (err.message ?? '')); }
}

async function doCancel(id) {
    if (!confirm('Annullare questo noleggio?')) return;
    try {
        await apiFetch(`/api/admin/rentals/${id}/cancel`, { method: 'POST' });
        await loadRentals(currentPage);
        await loadDashboard();
    } catch (err) { alert('Errore: ' + (err.message ?? '')); }
}

// ── Init ─────────────────────────────────────────────────────────────────────

const jsonOpts = body => ({
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
});

document.addEventListener('DOMContentLoaded', () => {
    loadDashboard();
    loadRentals();
    loadRentalVehicles();

    document.getElementById('addRentalBtn')?.addEventListener('click', openNew);
    document.getElementById('rentalDrawerClose')?.addEventListener('click', closeDrawer);
    document.getElementById('rentalDrawerOverlay')?.addEventListener('click', closeDrawer);
    document.getElementById('rentalCancelBtn')?.addEventListener('click', closeDrawer);
    document.getElementById('rentalForm')?.addEventListener('submit', submitRental);

    document.getElementById('actionModalClose')?.addEventListener('click', closeActionModal);
    document.getElementById('actionModalOverlay')?.addEventListener('click', (e) => {
        if (e.target.id === 'actionModalOverlay') closeActionModal();
    });

    document.getElementById('rentalSearch')?.addEventListener('input', () => renderRentals());
    document.getElementById('statusFilter')?.addEventListener('change', () => loadRentals(0));

    document.getElementById('rVehicle')?.addEventListener('change', scheduleAvailCheck);
    document.getElementById('rStartDate')?.addEventListener('change', scheduleAvailCheck);
    document.getElementById('rEndDate')?.addEventListener('change', scheduleAvailCheck);

    document.getElementById('rentalsPrev')?.addEventListener('click', () => loadRentals(currentPage - 1));
    document.getElementById('rentalsNext')?.addEventListener('click', () => loadRentals(currentPage + 1));

    // Hamburger
    document.getElementById('hamburgerBtn')?.addEventListener('click', () => {
        document.getElementById('sidebar')?.classList.toggle('open');
        document.getElementById('sidebarOverlay')?.classList.toggle('open');
    });
    document.getElementById('sidebarOverlay')?.addEventListener('click', () => {
        document.getElementById('sidebar')?.classList.remove('open');
        document.getElementById('sidebarOverlay')?.classList.remove('open');
    });

    // Highlight nav active
    document.querySelectorAll('.nav-item').forEach(a => {
        if (a.dataset.page === 'noleggi.html') a.classList.add('active');
    });
});
