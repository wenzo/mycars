'use strict';

const PAGE_SIZE = 20;

const CONDITION_BADGE = {
    nuovo:        '<span class="badge badge-nuovo">Nuovo</span>',
    usato:        '<span class="badge badge-usato">Usato</span>',
    km_0:         '<span class="badge badge-km0">Km 0</span>',
    conto_vendita:'<span class="badge badge-usato">C/V</span>',
    epoca:        '<span class="badge badge-usato">Epoca</span>',
};

const CONDITION_LABEL = {
    autovettura: 'Autovettura', motoveicolo: 'Motoveicolo',
    autocarro: 'Autocarro', autocaravan: 'Autocaravan',
};

// ── Helpers ───────────────────────────────────────────────────────────────────

function escHtml(str) {
    return (str ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

const jsonOpts = body => ({
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
});

// ── Lista veicoli ─────────────────────────────────────────────────────────────

async function loadVehicles(page = 0) {
    const tbody = document.getElementById('vehiclesTableBody');
    if (!tbody) return;

    const search    = document.getElementById('vehicleSearch')?.value.trim()  || '';
    const condition = document.getElementById('conditionFilter')?.value       || '';
    const published = document.getElementById('publishedFilter')?.value       || '';
    const nuovoArr  = document.getElementById('nuovoArrivoFilter')?.value     || '';
    const prontaCon = document.getElementById('prontaConsegnaFilter')?.value  || '';

    const params = new URLSearchParams({ page, pageSize: PAGE_SIZE });
    if (condition)        params.set('condition',      condition);
    if (published !== '') params.set('isPublished',    published);
    if (nuovoArr  !== '') params.set('isNuovoArrivo',  nuovoArr);
    if (prontaCon !== '') params.set('prontaConsegna', prontaCon);

    try {
        const data = await apiFetch(`/api/admin/vehicles?${params}`);
        const { items, totalCount } = data;

        const filtered = search
            ? items.filter(v =>
                (v.model        ?? '').toLowerCase().includes(search.toLowerCase()) ||
                (v.version      ?? '').toLowerCase().includes(search.toLowerCase()) ||
                (v.internalCode ?? '').toLowerCase().includes(search.toLowerCase()))
            : items;

        if (!filtered.length) {
            tbody.innerHTML = `<tr><td colspan="8" style="text-align:center;color:var(--text-muted);padding:2.5rem">Nessun veicolo trovato</td></tr>`;
        } else {
            tbody.innerHTML = filtered.map(v => {
                const label  = [v.model, v.version].filter(Boolean).join(' ');
                const price  = v.price != null ? `€ ${Number(v.price).toLocaleString('it-IT')}` : '—';
                const date   = new Date(v.createdAt).toLocaleDateString('it-IT');
                const stato  = v.isPublished
                    ? '<span class="badge badge-published">Pubblicato</span>'
                    : '<span class="badge badge-draft">Bozza</span>';
                const badges = [
                    v.isNuovoArrivo  ? '<span class="badge badge-new">Nuovo Arrivo</span>'      : '',
                    v.prontaConsegna ? '<span class="badge badge-testdrive">P. Consegna</span>' : '',
                ].filter(Boolean).join(' ');
                return `
                    <tr>
                        <td style="font-size:12px;color:var(--text-light)">${escHtml(v.internalCode)}</td>
                        <td class="td-main">${escHtml(label)}</td>
                        <td>${CONDITION_BADGE[v.condition] ?? v.condition}</td>
                        <td>${price}</td>
                        <td>${stato}</td>
                        <td>${badges || '—'}</td>
                        <td>${date}</td>
                        <td class="td-actions">
                            <button class="btn btn-outline btn-sm" data-id="${v.id}">Modifica</button>
                            <button class="btn btn-outline btn-sm" data-notify="${v.id}" data-label="${escHtml(label)}" title="Invia notifica push">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width:13px;height:13px" aria-hidden="true"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>
                            </button>
                        </td>
                    </tr>`;
            }).join('');

            tbody.querySelectorAll('[data-id]').forEach(btn =>
                btn.addEventListener('click', () => openDrawer(btn.dataset.id)));

            tbody.querySelectorAll('[data-notify]').forEach(btn =>
                btn.addEventListener('click', () =>
                    openPushModal(btn.dataset.notify, btn.dataset.label)));
        }

        const info = document.getElementById('vehiclesPaginationInfo');
        const prev = document.getElementById('vehiclesPrev');
        const next = document.getElementById('vehiclesNext');
        if (info) info.textContent = `${totalCount} veicoli`;
        if (prev) { prev.disabled = page === 0; prev.onclick = () => loadVehicles(page - 1); }
        if (next) { next.disabled = (page + 1) * PAGE_SIZE >= totalCount; next.onclick = () => loadVehicles(page + 1); }

    } catch {
        tbody.innerHTML = `<tr><td colspan="8" style="text-align:center;color:var(--text-muted);padding:2.5rem">Errore caricamento</td></tr>`;
    }
}

// ── Stato drawer ──────────────────────────────────────────────────────────────

let currentVehicleId = null;

// ── Drawer open / close ───────────────────────────────────────────────────────

async function openDrawer(vehicleId = null) {
    currentVehicleId = vehicleId;

    const title  = document.getElementById('vehicleDrawerTitle');
    const delBtn = document.getElementById('vehicleDrawerDelete');

    document.getElementById('vehicleForm')?.reset();
    // default: pubblicato checked
    const pubChk = document.getElementById('vPublished');
    if (pubChk && !vehicleId) pubChk.checked = true;

    if (title)  title.textContent    = vehicleId ? 'Modifica veicolo' : 'Aggiungi veicolo';
    if (delBtn) delBtn.style.display = vehicleId ? '' : 'none';

    if (vehicleId) {
        try {
            const v = await apiFetch(`/api/admin/vehicles/${vehicleId}`);
            const set = (id, val) => { const el = document.getElementById(id); if (el) el.value = val ?? ''; };
            const chk = (id, val) => { const el = document.getElementById(id); if (el) el.checked = !!val; };
            set('vBranch',      v.branchId);
            set('vType',        v.vehicleType);
            set('vCondition',   v.condition);
            set('vInternalCode',v.internalCode);
            set('vTarga',       v.targa);
            set('vBrand',       v.brandName);
            set('vModel',       v.model);
            set('vVersion',     v.version);
            set('vRegYear',     v.registrationYear);
            set('vFuel',        v.fuel);
            set('vTransmission',v.transmission);
            set('vHp',          v.horsepowerCv);
            set('vMileage',     v.mileageKm);
            set('vColor',       v.color);
            set('vPrice',       v.price);
            set('vPrevPrice',   v.previousPrice);
            set('vDescription', v.description);
            chk('vPublished',      v.isPublished);
            chk('vProntaConsegna', v.prontaConsegna);
            chk('vNuovoArrivo',    v.isNuovoArrivo);
            chk('vNegotiable',     v.negotiable);
        } catch (err) {
            showToast(err.message || 'Errore caricamento veicolo.', 'error');
        }
    }

    document.getElementById('vehicleDrawerOverlay')?.classList.add('open');
    document.getElementById('vehicleDrawer')?.classList.add('open');
}

function closeDrawer() {
    document.getElementById('vehicleDrawerOverlay')?.classList.remove('open');
    document.getElementById('vehicleDrawer')?.classList.remove('open');
    currentVehicleId = null;
}

// ── Salvataggio ───────────────────────────────────────────────────────────────

async function saveVehicle(e) {
    e.preventDefault();

    const saveBtn = document.getElementById('vehicleDrawerSave');
    if (saveBtn) saveBtn.disabled = true;

    const getVal  = id => document.getElementById(id)?.value.trim() || null;
    const getNum  = id => { const v = document.getElementById(id)?.value; return v ? Number(v) : null; };
    const isChk   = id => document.getElementById(id)?.checked ?? false;

    const branchId = document.getElementById('vBranch')?.value || null;

    const payload = {
        vehicleType:     document.getElementById('vType')?.value      || 'autovettura',
        brandName:       getVal('vBrand')       ?? '',
        branchId:        branchId,
        internalCode:    getVal('vInternalCode') ?? '',
        targa:           getVal('vTarga'),
        model:           getVal('vModel')        ?? '',
        version:         getVal('vVersion'),
        condition:       document.getElementById('vCondition')?.value || 'usato',
        fuel:            document.getElementById('vFuel')?.value        || null,
        transmission:    document.getElementById('vTransmission')?.value || null,
        horsepowerCv:    getNum('vHp'),
        registrationYear: getNum('vRegYear'),
        mileageKm:       getNum('vMileage') ?? 0,
        color:           getVal('vColor'),
        price:           getNum('vPrice'),
        previousPrice:   getNum('vPrevPrice'),
        negotiable:      isChk('vNegotiable'),
        isPublished:     isChk('vPublished'),
        prontaConsegna:  isChk('vProntaConsegna'),
        isNuovoArrivo:   isChk('vNuovoArrivo'),
        description:     getVal('vDescription'),
    };

    // Converti stringhe vuote a null per i campi enum
    if (!payload.fuel)         payload.fuel         = null;
    if (!payload.transmission) payload.transmission = null;

    try {
        let saved;
        if (currentVehicleId) {
            saved = await apiFetch(`/api/admin/vehicles/${currentVehicleId}`, { method: 'PUT', ...jsonOpts(payload) });
            showToast('Veicolo aggiornato.');
        } else {
            saved = await apiFetch('/api/admin/vehicles', { method: 'POST', ...jsonOpts(payload) });
            showToast('Veicolo creato.');
        }
        const savedId    = saved?.id ?? currentVehicleId;
        const savedLabel = [payload.model, payload.version].filter(Boolean).join(' ');
        closeDrawer();
        loadVehicles();
        if (savedId) setTimeout(() => openPushModal(savedId, savedLabel), 300);
    } catch (err) {
        showToast(err.message || 'Errore salvataggio.', 'error');
    } finally {
        if (saveBtn) saveBtn.disabled = false;
    }
}

// ── Eliminazione ──────────────────────────────────────────────────────────────

async function deleteVehicle() {
    if (!currentVehicleId) return;
    if (!confirm('Eliminare questo veicolo? L\'operazione non è reversibile.')) return;

    try {
        await apiFetch(`/api/admin/vehicles/${currentVehicleId}`, { method: 'DELETE' });
        showToast('Veicolo eliminato.');
        closeDrawer();
        loadVehicles();
    } catch (err) {
        showToast(err.message || 'Errore eliminazione.', 'error');
    }
}

// ── Bootstrap dati per select ─────────────────────────────────────────────────

async function loadBranches() {
    try {
        const branches = await apiFetch('/api/admin/branches');
        const sel = document.getElementById('vBranch');
        if (!sel) return;
        sel.innerHTML = '<option value="">— Seleziona sede —</option>' +
            branches.map(b => `<option value="${b.id}">${escHtml(b.name)}</option>`).join('');
    } catch { /* silenzioso */ }
}

async function loadBrandsDatalist() {
    try {
        const brands  = await apiFetch('/api/admin/brands');
        const dl = document.getElementById('brandsList');
        if (!dl) return;
        dl.innerHTML = brands.map(b => `<option value="${escHtml(b.name)}">`).join('');
    } catch { /* silenzioso */ }
}

// ── Push Notification Modal ───────────────────────────────────────────────────

let currentPushVehicleId = null;

function openPushModal(vehicleId, label = '') {
    currentPushVehicleId = vehicleId;

    const titleEl = document.getElementById('pnTitle');
    const bodyEl  = document.getElementById('pnBody');
    const result  = document.getElementById('pnResult');

    if (titleEl) titleEl.value = label ? `Nuovo veicolo: ${label}` : 'Nuovo veicolo disponibile';
    if (bodyEl)  bodyEl.value  = label ? `${label} è disponibile in concessionaria.` : '';
    if (result)  { result.style.display = 'none'; result.textContent = ''; }

    const nowRadio = document.getElementById('pnNow');
    if (nowRadio) { nowRadio.checked = true; toggleScheduleFields(); }

    document.getElementById('pushModalOverlay')?.classList.add('open');
    document.getElementById('pushModal')?.classList.add('open');
}

function closePushModal() {
    document.getElementById('pushModalOverlay')?.classList.remove('open');
    document.getElementById('pushModal')?.classList.remove('open');
    currentPushVehicleId = null;
}

function toggleScheduleFields() {
    const isScheduled = document.getElementById('pnScheduled')?.checked;
    const fields = document.getElementById('pnScheduleFields');
    if (fields) fields.style.display = isScheduled ? '' : 'none';
}

async function sendPushNotification() {
    if (!currentPushVehicleId) return;

    const title   = document.getElementById('pnTitle')?.value.trim();
    const body    = document.getElementById('pnBody')?.value.trim();
    const isSchd  = document.getElementById('pnScheduled')?.checked;
    const dateVal = document.getElementById('pnDate')?.value;
    const timeVal = document.getElementById('pnTime')?.value || '09:00';
    const result  = document.getElementById('pnResult');
    const sendBtn = document.getElementById('pushModalSend');

    if (!title || !body) { showToast('Titolo e testo sono obbligatori.', 'error'); return; }

    let sendAt = null;
    if (isSchd) {
        if (!dateVal) { showToast('Scegli la data di invio.', 'error'); return; }
        sendAt = new Date(`${dateVal}T${timeVal}:00`).toISOString();
        if (new Date(sendAt) <= new Date()) { showToast('La data pianificata deve essere futura.', 'error'); return; }
    }

    if (sendBtn) sendBtn.disabled = true;

    try {
        const res = await apiFetch(`/api/admin/vehicles/${currentPushVehicleId}/notify`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ title, body, sendAt }),
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
                result.textContent = 'Nessun dispositivo iscritto alle notifiche veicoli.';
            } else {
                result.style.background = '#ecfdf5';
                result.style.color = '#065f46';
                result.textContent = `✓ Notifica inviata a ${res.sent} di ${res.total} dispositivi.`;
            }
        }
        setTimeout(closePushModal, 2000);
    } catch (err) {
        showToast(err.message || 'Errore invio notifica.', 'error');
    } finally {
        if (sendBtn) sendBtn.disabled = false;
    }
}

// ── Init ──────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadVehicles();
    loadBranches();
    loadBrandsDatalist();

    document.getElementById('addVehicleBtn')?.addEventListener('click', () => openDrawer());
    document.getElementById('vehicleDrawerClose')?.addEventListener('click',   closeDrawer);
    document.getElementById('vehicleDrawerCancel')?.addEventListener('click',  closeDrawer);
    document.getElementById('vehicleDrawerOverlay')?.addEventListener('click', closeDrawer);
    document.getElementById('vehicleDrawerDelete')?.addEventListener('click',  deleteVehicle);
    document.getElementById('vehicleForm')?.addEventListener('submit', saveVehicle);

    // Push modal
    document.getElementById('pushModalClose')?.addEventListener('click', closePushModal);
    document.getElementById('pushModalSkip')?.addEventListener('click',  closePushModal);
    document.getElementById('pushModalOverlay')?.addEventListener('click', closePushModal);
    document.getElementById('pushModalSend')?.addEventListener('click', sendPushNotification);
    document.querySelectorAll('input[name="pnWhen"]').forEach(r =>
        r.addEventListener('change', toggleScheduleFields));

    let debounce;
    document.getElementById('vehicleSearch')?.addEventListener('input', () => {
        clearTimeout(debounce);
        debounce = setTimeout(() => loadVehicles(0), 300);
    });
    ['conditionFilter', 'publishedFilter', 'nuovoArrivoFilter', 'prontaConsegnaFilter'].forEach(id => {
        document.getElementById(id)?.addEventListener('change', () => loadVehicles(0));
    });
});
