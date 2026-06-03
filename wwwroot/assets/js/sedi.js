'use strict';

// ── State ─────────────────────────────────────────────────────────────────────

let allBranches    = [];
let allDepartments = [];
let currentBranchId = null;
let currentDeptId   = null;

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

    tbody.innerHTML = filtered.map(b => `
        <tr>
            <td class="td-main">${escHtml(b.name)}</td>
            <td>${escHtml(b.address)}</td>
            <td>${escHtml(b.city)}</td>
            <td>${escHtml(b.province)}</td>
            <td>${escHtml(b.phone)}</td>
            <td>${activeBadge(b.isActive)}</td>
            <td class="td-actions">
                <button class="btn btn-outline btn-sm" data-branch-id="${b.id}">Modifica</button>
            </td>
        </tr>`).join('');

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

    const payload = {
        name:           get('bName') ?? '',
        legalName:      get('bLegalName'),
        address:        get('bAddress'),
        zipCode:        get('bZip'),
        city:           get('bCity'),
        province:       get('bProvince'),
        phone:          get('bPhone'),
        email:          get('bEmail'),
        whatsappNumber: get('bWhatsapp'),
        sortOrder:      parseInt(document.getElementById('bSortOrder')?.value ?? '0', 10) || 0,
        isActive:       chk('bActive'),
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
            set('dName',        d.name);
            set('dDescription', d.description);
            set('dSortOrder',   d.sortOrder);
            chk('dActive',      d.isActive);
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
        name:        get('dName') ?? '',
        description: get('dDescription'),
        branchId:    branchVal || null,
        sortOrder:   parseInt(document.getElementById('dSortOrder')?.value ?? '0', 10) || 0,
        isActive:    chk('dActive'),
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
