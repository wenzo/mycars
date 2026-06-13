'use strict';

// ── State ─────────────────────────────────────────────────────────────────────

let allBranches    = [];
let allDepartments = [];
let currentBranchId = null;
let currentDeptId   = null;
let cachedProfile   = null;  // profilo operatore caricato al bisogno

// ── Helpers ───────────────────────────────────────────────────────────────────

function escHtml(str) {
    return (str ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function activeBadge(val) {
    return val
        ? '<span class="badge badge-published">Attiva</span>'
        : '<span class="badge badge-draft">Inattiva</span>';
}

const jsonOpts = body => ({
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
});

// ── Branches ──────────────────────────────────────────────────────────────────

async function loadBranches() {
    const tbody = document.getElementById('branchesTableBody');
    if (!tbody) return;

    try {
        allBranches = await apiFetch('/api/admin/branches');
        renderBranches();
    } catch {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Errore caricamento</td></tr>`;
    }
}

function renderBranches() {
    const tbody  = document.getElementById('branchesTableBody');
    const search = (document.getElementById('branchSearch')?.value ?? '').toLowerCase();

    const filtered = search
        ? allBranches.filter(b =>
            (b.name ?? '').toLowerCase().includes(search) ||
            (b.city ?? '').toLowerCase().includes(search))
        : allBranches;

    if (!filtered.length) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:2.5rem">Nessuna sede trovata</td></tr>`;
        return;
    }

    tbody.innerHTML = filtered.map(b => {
        const coords = (b.latitude != null && b.longitude != null)
            ? `<span title="${Number(b.latitude).toFixed(5)}, ${Number(b.longitude).toFixed(5)}" style="color:var(--green)">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width:14px;height:14px;vertical-align:middle"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>
               </span>`
            : '<span style="color:var(--text-light);font-size:12px">—</span>';
        const legalBadge = b.isLegalAddress
            ? '<span class="badge badge-new" style="font-size:11px">Sede legale</span>'
            : '<span style="color:var(--text-light);font-size:12px">—</span>';
        return `
        <tr>
            <td class="td-main">${escHtml(b.name)}</td>
            <td>${escHtml(b.address)}</td>
            <td>${escHtml(b.city)}</td>
            <td>${escHtml(b.province)}</td>
            <td style="text-align:center">${coords}</td>
            <td style="text-align:center">${legalBadge}</td>
            <td>${escHtml(b.phone)}</td>
            <td>${activeBadge(b.isActive)}</td>
            <td class="td-actions">
                <button class="btn btn-outline btn-sm" data-branch-id="${b.id}">Modifica</button>
            </td>
        </tr>`;
    }).join('');

    tbody.querySelectorAll('[data-branch-id]').forEach(btn => {
        btn.addEventListener('click', () => openBranchDrawer(btn.dataset.branchId));
    });
}

function openBranchDrawer(branchId = null) {
    currentBranchId = branchId;

    const title  = document.getElementById('branchDrawerTitle');
    const delBtn = document.getElementById('branchDrawerDelete');

    document.getElementById('branchForm')?.reset();
    if (title)  title.textContent    = branchId ? 'Modifica sede' : 'Aggiungi sede';
    if (delBtn) delBtn.style.display = branchId ? '' : 'none';

    if (branchId) {
        const b = allBranches.find(x => x.id === branchId);
        if (b) {
            const set = (id, val) => { const el = document.getElementById(id); if (el) el.value = val ?? ''; };
            const chk = (id, val) => { const el = document.getElementById(id); if (el) el.checked = !!val; };
            set('bName',      b.name);
            set('bLegalName', b.legalName);
            set('bAddress',   b.address);
            set('bCity',      b.city);
            set('bZip',       b.zipCode);
            set('bProvince',  b.province);
            set('bPhone',     b.phone);
            set('bEmail',     b.email);
            set('bWhatsapp',  b.whatsappNumber);
            set('bSortOrder', b.sortOrder);
            chk('bActive',    b.isActive);
            set('bLat',  b.latitude  != null ? b.latitude  : '');
            set('bLng',  b.longitude != null ? b.longitude : '');
            chk('bIsLegalAddress', b.isLegalAddress);
        }
    }

    document.getElementById('branchDrawerOverlay')?.classList.add('open');
    document.getElementById('branchDrawer')?.classList.add('open');
}

function closeBranchDrawer() {
    document.getElementById('branchDrawerOverlay')?.classList.remove('open');
    document.getElementById('branchDrawer')?.classList.remove('open');
    currentBranchId = null;
}

async function saveBranch(e) {
    e.preventDefault();

    const get = id => document.getElementById(id)?.value.trim() || null;
    const chk = id => document.getElementById(id)?.checked ?? false;

    const latVal = document.getElementById('bLat')?.value;
    const lngVal = document.getElementById('bLng')?.value;

    const payload = {
        name:            get('bName') ?? '',
        legalName:       get('bLegalName'),
        address:         get('bAddress'),
        zipCode:         get('bZip'),
        city:            get('bCity'),
        province:        get('bProvince'),
        latitude:        latVal !== '' && latVal != null ? Number(latVal) : null,
        longitude:       lngVal !== '' && lngVal != null ? Number(lngVal) : null,
        isLegalAddress:  chk('bIsLegalAddress'),
        phone:           get('bPhone'),
        email:           get('bEmail'),
        whatsappNumber:  get('bWhatsapp'),
        sortOrder:       parseInt(document.getElementById('bSortOrder')?.value ?? '0', 10) || 0,
        isActive:        chk('bActive'),
    };

    try {
        if (currentBranchId) {
            await apiFetch(`/api/admin/branches/${currentBranchId}`, { method: 'PUT', ...jsonOpts(payload) });
            showToast('Sede aggiornata.');
        } else {
            await apiFetch('/api/admin/branches', { method: 'POST', ...jsonOpts(payload) });
            showToast('Sede creata.');
        }
        closeBranchDrawer();
        await loadBranches();
        populateBranchSelect();
    } catch (err) {
        showToast(err?.message || 'Errore salvataggio.', 'error');
    }
}

async function deleteBranch() {
    if (!currentBranchId) return;
    if (!confirm('Eliminare questa sede? Anche i reparti associati potrebbero essere rimossi.')) return;

    try {
        await apiFetch(`/api/admin/branches/${currentBranchId}`, { method: 'DELETE' });
        showToast('Sede eliminata.');
        closeBranchDrawer();
        await loadBranches();
        populateBranchSelect();
    } catch {
        showToast('Errore eliminazione.', 'error');
    }
}

// ── Sede legale: pre-compila dati da profilo operatore ────────────────────────

async function onLegalAddressToggle() {
    const checked = document.getElementById('bIsLegalAddress')?.checked;
    if (!checked) return;

    try {
        if (!cachedProfile) cachedProfile = await apiFetch('/api/admin/profile');
        const p = cachedProfile;

        const set = (id, val) => { const el = document.getElementById(id); if (el) el.value = val ?? ''; };
        set('bAddress',  p.address);
        set('bZip',      p.zipCode);
        set('bCity',     p.city);
        set('bProvince', p.province);
        set('bLat',      p.latitude  != null ? p.latitude  : '');
        set('bLng',      p.longitude != null ? p.longitude : '');

        showToast('Dati indirizzo copiati dalla sede legale.');
    } catch {
        showToast('Impossibile caricare la sede legale.', 'error');
    }
}

// ── Geocodifica sede ──────────────────────────────────────────────────────────

async function geocodeBranch() {
    const address  = document.getElementById('bAddress')?.value.trim()  || '';
    const city     = document.getElementById('bCity')?.value.trim()     || '';
    const province = document.getElementById('bProvince')?.value.trim() || '';

    if (!city && !address) {
        showToast('Inserisci almeno la città per la geocodifica.', 'error');
        return;
    }

    const query = [address, city, province, 'Italy'].filter(Boolean).join(', ');
    const btn = document.getElementById('bGeocodeBtn');
    if (btn) btn.disabled = true;

    try {
        const res  = await fetch(
            `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&limit=1`,
            { headers: { 'Accept-Language': 'it', 'User-Agent': 'MyCars-Admin/1.0' } }
        );
        const data = await res.json();
        if (!data.length) {
            showToast('Indirizzo non trovato. Prova con città e provincia.', 'error');
            return;
        }
        const lat = parseFloat(data[0].lat).toFixed(7);
        const lng = parseFloat(data[0].lon).toFixed(7);
        const latEl = document.getElementById('bLat');
        const lngEl = document.getElementById('bLng');
        if (latEl) latEl.value = lat;
        if (lngEl) lngEl.value = lng;
        showToast(`Coordinate trovate: ${lat}, ${lng}`);
    } catch {
        showToast('Errore geocoding. Controlla la connessione.', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ── Departments ───────────────────────────────────────────────────────────────

async function loadDepartments() {
    const tbody = document.getElementById('deptTableBody');
    if (!tbody) return;

    try {
        allDepartments = await apiFetch('/api/admin/departments');
        renderDepartments();
    } catch {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;color:var(--text-muted);padding:2.5rem">Errore caricamento</td></tr>`;
    }
}

function renderDepartments() {
    const tbody  = document.getElementById('deptTableBody');
    const search = (document.getElementById('deptSearch')?.value ?? '').toLowerCase();

    const filtered = search
        ? allDepartments.filter(d => (d.name ?? '').toLowerCase().includes(search))
        : allDepartments;

    if (!filtered.length) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;color:var(--text-muted);padding:2.5rem">Nessun reparto trovato</td></tr>`;
        return;
    }

    tbody.innerHTML = filtered.map(d => {
        const branch = allBranches.find(b => b.id === d.branchId);
        return `
        <tr>
            <td class="td-main">${escHtml(d.name)}</td>
            <td>${branch ? escHtml(branch.name) : '—'}</td>
            <td style="color:var(--text-light)">${escHtml(d.description)}</td>
            <td>${activeBadge(d.isActive)}</td>
            <td class="td-actions">
                <button class="btn btn-outline btn-sm" data-dept-id="${d.id}">Modifica</button>
            </td>
        </tr>`;
    }).join('');

    tbody.querySelectorAll('[data-dept-id]').forEach(btn => {
        btn.addEventListener('click', () => openDeptDrawer(btn.dataset.deptId));
    });
}

function populateBranchSelect(selectedId = null) {
    const sel = document.getElementById('dBranch');
    if (!sel) return;
    const prev = selectedId ?? sel.value;
    sel.innerHTML = '<option value="">— Nessuna sede —</option>' +
        allBranches.map(b =>
            `<option value="${b.id}"${b.id === prev ? ' selected' : ''}>${escHtml(b.name)}</option>`
        ).join('');
}

function openDeptDrawer(deptId = null) {
    currentDeptId = deptId;

    const title  = document.getElementById('deptDrawerTitle');
    const delBtn = document.getElementById('deptDrawerDelete');

    document.getElementById('deptForm')?.reset();
    if (title)  title.textContent    = deptId ? 'Modifica reparto' : 'Aggiungi reparto';
    if (delBtn) delBtn.style.display = deptId ? '' : 'none';

    populateBranchSelect();

    if (deptId) {
        const d = allDepartments.find(x => x.id === deptId);
        if (d) {
            const set = (id, val) => { const el = document.getElementById(id); if (el) el.value = val ?? ''; };
            const chk = (id, val) => { const el = document.getElementById(id); if (el) el.checked = !!val; };
            set('dName',            d.name);
            set('dDescription',     d.description);
            set('dResponsibleName', d.responsibleName);
            set('dPhone',           d.phone);
            set('dEmail',           d.email);
            set('dSortOrder',       d.sortOrder);
            chk('dActive',          d.isActive);
            populateBranchSelect(d.branchId);
        }
    }

    document.getElementById('deptDrawerOverlay')?.classList.add('open');
    document.getElementById('deptDrawer')?.classList.add('open');
}

function closeDeptDrawer() {
    document.getElementById('deptDrawerOverlay')?.classList.remove('open');
    document.getElementById('deptDrawer')?.classList.remove('open');
    currentDeptId = null;
}

async function saveDept(e) {
    e.preventDefault();

    const get = id => document.getElementById(id)?.value.trim() || null;
    const chk = id => document.getElementById(id)?.checked ?? false;
    const branchVal = document.getElementById('dBranch')?.value || null;

    const payload = {
        name:            get('dName') ?? '',
        description:     get('dDescription'),
        responsibleName: get('dResponsibleName'),
        phone:           get('dPhone'),
        email:           get('dEmail'),
        branchId:        branchVal || null,
        sortOrder:       parseInt(document.getElementById('dSortOrder')?.value ?? '0', 10) || 0,
        isActive:        chk('dActive'),
    };

    try {
        if (currentDeptId) {
            await apiFetch(`/api/admin/departments/${currentDeptId}`, { method: 'PUT', ...jsonOpts(payload) });
            showToast('Reparto aggiornato.');
        } else {
            await apiFetch('/api/admin/departments', { method: 'POST', ...jsonOpts(payload) });
            showToast('Reparto creato.');
        }
        closeDeptDrawer();
        loadDepartments();
    } catch (err) {
        showToast(err?.message || 'Errore salvataggio.', 'error');
    }
}

async function deleteDept() {
    if (!currentDeptId) return;
    if (!confirm('Eliminare questo reparto?')) return;

    try {
        await apiFetch(`/api/admin/departments/${currentDeptId}`, { method: 'DELETE' });
        showToast('Reparto eliminato.');
        closeDeptDrawer();
        loadDepartments();
    } catch {
        showToast('Errore eliminazione.', 'error');
    }
}

// ── Init ──────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', async () => {
    await loadBranches();
    await loadDepartments();

    // Branch events
    document.getElementById('addBranchBtn')?.addEventListener('click', () => openBranchDrawer());
    document.getElementById('branchDrawerClose')?.addEventListener('click',   closeBranchDrawer);
    document.getElementById('branchDrawerCancel')?.addEventListener('click',  closeBranchDrawer);
    document.getElementById('branchDrawerOverlay')?.addEventListener('click', closeBranchDrawer);
    document.getElementById('branchDrawerDelete')?.addEventListener('click',  deleteBranch);
    document.getElementById('branchForm')?.addEventListener('submit', saveBranch);
    document.getElementById('bGeocodeBtn')?.addEventListener('click', geocodeBranch);
    document.getElementById('bIsLegalAddress')?.addEventListener('change', onLegalAddressToggle);

    let branchDebounce;
    document.getElementById('branchSearch')?.addEventListener('input', () => {
        clearTimeout(branchDebounce);
        branchDebounce = setTimeout(renderBranches, 200);
    });

    // Dept events
    document.getElementById('addDeptBtn')?.addEventListener('click', () => openDeptDrawer());
    document.getElementById('deptDrawerClose')?.addEventListener('click',   closeDeptDrawer);
    document.getElementById('deptDrawerCancel')?.addEventListener('click',  closeDeptDrawer);
    document.getElementById('deptDrawerOverlay')?.addEventListener('click', closeDeptDrawer);
    document.getElementById('deptDrawerDelete')?.addEventListener('click',  deleteDept);
    document.getElementById('deptForm')?.addEventListener('submit', saveDept);

    let deptDebounce;
    document.getElementById('deptSearch')?.addEventListener('input', () => {
        clearTimeout(deptDebounce);
        deptDebounce = setTimeout(renderDepartments, 200);
    });
});
