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
                const brand  = brandMap[v.brandId] || '';
                const label  = [brand, v.model, v.version].filter(Boolean).join(' ');
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

// ── Brand map (id → name) ─────────────────────────────────────────────────────

let brandMap = {};

// ── Stato drawer ──────────────────────────────────────────────────────────────

let currentVehicleId   = null;
let currentCoverUrl    = null;

// ── Stato noleggio ─────────────────────────────────────────────────────────────

const FORMULA_KEYS = ['daily', 'weekend', 'weekly', 'monthly', 'mid_term'];
const FORMULA_META = {
    daily:    { priceLabel: 'Prezzo (€/giorno)', kmLabel: 'Km inclusi/giorno' },
    weekend:  { priceLabel: 'Prezzo (€ totale)', kmLabel: 'Km totali weekend' },
    weekly:   { priceLabel: 'Prezzo (€/giorno)', kmLabel: 'Km inclusi totali' },
    monthly:  { priceLabel: 'Prezzo (€/mese)',   kmLabel: 'Km inclusi/mese'   },
    mid_term: { priceLabel: 'Prezzo (€/mese)',   kmLabel: 'Km inclusi/mese'   },
};

function emptyFormulas() {
    return Object.fromEntries(FORMULA_KEYS.map(k => [k, { price: null, kmIncluded: null, priceExtraKm: null }]));
}

let _rentalFormulas = emptyFormulas();
let _activeFormula  = 'daily';

function syncFormulaToState() {
    const p   = document.getElementById('vFPrice')?.value;
    const km  = document.getElementById('vFKm')?.value;
    const xkm = document.getElementById('vFExtraKm')?.value;
    _rentalFormulas[_activeFormula] = {
        price:        p   ? parseFloat(p)    : null,
        kmIncluded:   km  ? parseInt(km, 10) : null,
        priceExtraKm: xkm ? parseFloat(xkm) : null,
    };
}

function applyFormulaTab(formula) {
    _activeFormula = formula;
    document.querySelectorAll('.formula-tab').forEach(btn =>
        btn.classList.toggle('active', btn.dataset.formula === formula));
    const meta = FORMULA_META[formula] || FORMULA_META.daily;
    const pl = document.getElementById('labelFPrice');
    const kl = document.getElementById('labelFKm');
    if (pl) pl.textContent = meta.priceLabel;
    if (kl) kl.textContent = meta.kmLabel;
    const f  = _rentalFormulas[formula] || {};
    const pe = document.getElementById('vFPrice');
    const ke = document.getElementById('vFKm');
    const xe = document.getElementById('vFExtraKm');
    if (pe) pe.value = f.price        ?? '';
    if (ke) ke.value = f.kmIncluded   ?? '';
    if (xe) xe.value = f.priceExtraKm ?? '';
}

function switchFormulaTab(formula) {
    syncFormulaToState();
    applyFormulaTab(formula);
}

function loadRentalData(v) {
    _rentalFormulas = emptyFormulas();
    const src = v.rentalFormulas ?? {};
    FORMULA_KEYS.forEach(key => {
        if (src[key]) _rentalFormulas[key] = {
            price:        src[key].price        ?? null,
            kmIncluded:   src[key].kmIncluded   ?? null,
            priceExtraKm: src[key].priceExtraKm ?? null,
        };
    });
    applyFormulaTab('daily');

    const r   = v.rentalRedemption ?? {};
    const chk = (id, val) => { const el = document.getElementById(id); if (el) el.checked = !!val; };
    const set = (id, val) => { const el = document.getElementById(id); if (el) el.value = val ?? ''; };
    chk('vRedemptionEnabled', r.enabled);
    set('vRedemptionPrice',   r.salePrice);
    set('vRedemptionPct',     r.canoiDiscountPct);
    set('vRedemptionNotes',   r.notes);
    const rdFields = document.getElementById('redemptionFields');
    if (rdFields) rdFields.style.display = r.enabled ? 'flex' : 'none';

    set('vRentalDeposit',      v.rentalDepositOverride);
    set('vRentalVehicleNotes', v.rentalVehicleNotes);
}

function resetRentalData() {
    _rentalFormulas = emptyFormulas();
    applyFormulaTab('daily');
    ['vRentalDeposit','vRentalVehicleNotes','vRedemptionPrice','vRedemptionPct','vRedemptionNotes'].forEach(id => {
        const el = document.getElementById(id); if (el) el.value = '';
    });
    const chk = document.getElementById('vRedemptionEnabled');
    if (chk) chk.checked = false;
    const rdFields = document.getElementById('redemptionFields');
    if (rdFields) rdFields.style.display = 'none';
}

// ── Drawer open / close ───────────────────────────────────────────────────────

async function openDrawer(vehicleId = null) {
    currentVehicleId = vehicleId;

    const title  = document.getElementById('vehicleDrawerTitle');
    const delBtn = document.getElementById('vehicleDrawerDelete');

    document.getElementById('vehicleForm')?.reset();
    // default: pubblicato checked
    const pubChk = document.getElementById('vPublished');
    if (pubChk && !vehicleId) pubChk.checked = true;
    resetRentalData();
    { const rs = document.getElementById('rentalSection'); if (rs) rs.style.display = 'none'; }

    if (title)  title.textContent    = vehicleId ? 'Modifica veicolo' : 'Aggiungi veicolo';
    if (delBtn) delBtn.style.display = vehicleId ? '' : 'none';

    // Reset sezione immagini
    document.getElementById('vImagesSection').style.display = vehicleId ? '' : 'none';
    setCover(null);
    renderGallery([]);

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
            set('vRegMonth',    v.registrationMonth ?? '');
            set('vRegYear',     v.registrationYear);
            set('vFuel',        v.fuel);
            set('vTransmission',v.transmission);
            set('vHp',          v.horsepowerCv);
            set('vKw',          v.powerKw);
            set('vMileage',     v.mileageKm);
            set('vColor',       v.color);
            set('vPrice',       v.price);
            set('vPrevPrice',   v.previousPrice);
            set('vDescription', v.description);
            chk('vPublished',          v.isPublished);
            chk('vProntaConsegna',     v.prontaConsegna);
            chk('vNuovoArrivo',        v.isNuovoArrivo);
            chk('vVatDeductible',      v.vatDeductible);
            chk('vImported',           v.imported);
            chk('vHandicapAccessible', v.handicapAccessible);
            chk('vForSale',            v.forSale ?? true);
            chk('vForRental',          v.forRental);
            loadRentalData(v);
            { const rs = document.getElementById('rentalSection'); if (rs) rs.style.display = v.forRental ? 'flex' : 'none'; }
            setCover(v.coverImageUrl ?? null);
        } catch (err) {
            showToast(err.message || 'Errore caricamento veicolo.', 'error');
        }

        // Carica galleria in background
        apiFetch(`/api/admin/vehicles/${vehicleId}/images`)
            .then(imgs => renderGallery(imgs))
            .catch(() => {});
    }

    document.getElementById('vehicleDrawerOverlay')?.classList.add('open');
    document.getElementById('vehicleDrawer')?.classList.add('open');
}

function closeDrawer() {
    document.getElementById('vehicleDrawerOverlay')?.classList.remove('open');
    document.getElementById('vehicleDrawer')?.classList.remove('open');
    currentVehicleId = null;
    currentCoverUrl  = null;
}

// ── Immagini: copertina ───────────────────────────────────────────────────────

function setCover(url) {
    currentCoverUrl = url || null;
    const img        = document.getElementById('vCoverImg');
    const placeholder= document.getElementById('vCoverPlaceholder');
    const removeBtn  = document.getElementById('vCoverRemoveBtn');
    if (url) {
        img.src              = url;
        img.style.display    = '';
        placeholder.style.display = 'none';
        if (removeBtn) removeBtn.style.display = '';
    } else {
        img.src              = '';
        img.style.display    = 'none';
        placeholder.style.display = '';
        if (removeBtn) removeBtn.style.display = 'none';
    }
}

async function uploadCover(file) {
    if (!currentVehicleId) return;
    const form = new FormData();
    form.append('file', file);
    try {
        const res = await apiFetch(`/api/admin/vehicles/${currentVehicleId}/cover`, { method: 'POST', body: form });
        setCover(res.url);
        showToast('Copertina aggiornata.');
        loadVehicles();
    } catch (err) {
        showToast(err.message || 'Errore upload copertina.', 'error');
    }
}

async function removeCover() {
    if (!currentVehicleId) return;
    if (!confirm('Rimuovere la foto di copertina?')) return;
    try {
        await apiFetch(`/api/admin/vehicles/${currentVehicleId}/cover`, { method: 'DELETE' });
        setCover(null);
        showToast('Copertina rimossa.');
        loadVehicles();
    } catch (err) {
        showToast(err.message || 'Errore rimozione copertina.', 'error');
    }
}

// ── Immagini: galleria ────────────────────────────────────────────────────────

function renderGallery(images) {
    const grid = document.getElementById('vGalleryGrid');
    if (!grid) return;
    if (!images || !images.length) {
        grid.innerHTML = '<span style="color:var(--text-light);font-size:13px">Nessuna foto in galleria.</span>';
        return;
    }
    grid.innerHTML = images.map(img => `
        <div class="img-gallery-item">
            <img src="${escHtml(img.url)}" alt="">
            <button class="img-gallery-del" data-image-id="${img.id}" aria-label="Elimina foto" title="Elimina">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
            </button>
        </div>`).join('');
    grid.querySelectorAll('.img-gallery-del').forEach(btn =>
        btn.addEventListener('click', () => deleteGalleryImage(btn.dataset.imageId)));
}

async function uploadGalleryImages(files) {
    if (!currentVehicleId || !files.length) return;
    const uploaded = [];
    for (const file of files) {
        try {
            const form = new FormData();
            form.append('file', file);
            const img = await apiFetch(`/api/admin/vehicles/${currentVehicleId}/images`, { method: 'POST', body: form });
            uploaded.push(img);
        } catch (err) {
            showToast(err.message || 'Errore upload foto.', 'error');
        }
    }
    if (uploaded.length) {
        showToast(`${uploaded.length} foto aggiunta/e.`);
        const imgs = await apiFetch(`/api/admin/vehicles/${currentVehicleId}/images`).catch(() => []);
        renderGallery(imgs);
    }
}

async function deleteGalleryImage(imageId) {
    if (!currentVehicleId) return;
    if (!confirm('Eliminare questa foto dalla galleria?')) return;
    try {
        await apiFetch(`/api/admin/vehicles/${currentVehicleId}/images/${imageId}`, { method: 'DELETE' });
        const imgs = await apiFetch(`/api/admin/vehicles/${currentVehicleId}/images`).catch(() => []);
        renderGallery(imgs);
        showToast('Foto eliminata.');
    } catch (err) {
        showToast(err.message || 'Errore eliminazione.', 'error');
    }
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

    // Noleggio: costruisci formule e riscatto
    const forRental = isChk('vForRental');
    let rentalFormulas   = null;
    let rentalRedemption = null;
    if (forRental) {
        syncFormulaToState();
        const fResult = {};
        Object.entries(_rentalFormulas).forEach(([key, f]) => {
            if (f.price != null) fResult[key] = f;
        });
        if (Object.keys(fResult).length) rentalFormulas = fResult;
        if (isChk('vRedemptionEnabled')) {
            rentalRedemption = {
                enabled:          true,
                salePrice:        getNum('vRedemptionPrice'),
                canoiDiscountPct: getNum('vRedemptionPct'),
                notes:            getVal('vRedemptionNotes'),
            };
        }
    }

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
        powerKw:         getNum('vKw'),
        registrationMonth: getNum('vRegMonth'),
        registrationYear:  getNum('vRegYear'),
        mileageKm:       getNum('vMileage') ?? 0,
        color:           getVal('vColor'),
        price:           getNum('vPrice'),
        previousPrice:   getNum('vPrevPrice'),
        vatDeductible:      isChk('vVatDeductible'),
        imported:           isChk('vImported'),
        handicapAccessible: isChk('vHandicapAccessible'),
        forSale:              isChk('vForSale'),
        forRental,
        rentalPrice:          null,
        rentalFormulas,
        rentalRedemption,
        rentalDepositOverride: forRental ? getNum('vRentalDeposit') : null,
        rentalVehicleNotes:    forRental ? getVal('vRentalVehicleNotes') : null,
        isPublished:        isChk('vPublished'),
        prontaConsegna:     isChk('vProntaConsegna'),
        isNuovoArrivo:      isChk('vNuovoArrivo'),
        description:        getVal('vDescription'),
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
        const brands = await apiFetch('/api/admin/brands');
        brands.forEach(b => { brandMap[b.id] = b.name; });
        const dl = document.getElementById('brandsList');
        if (dl) dl.innerHTML = brands.map(b => `<option value="${escHtml(b.name)}">`).join('');
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

    // Apertura diretta da dashboard (?open=<id>)
    const autoOpen = new URLSearchParams(location.search).get('open');
    if (autoOpen) openDrawer(autoOpen);
    loadBrandsDatalist();

    document.getElementById('addVehicleBtn')?.addEventListener('click', () => openDrawer());
    document.getElementById('vehicleDrawerClose')?.addEventListener('click',   closeDrawer);
    document.getElementById('vehicleDrawerCancel')?.addEventListener('click',  closeDrawer);
    document.getElementById('vehicleDrawerOverlay')?.addEventListener('click', closeDrawer);
    document.getElementById('vehicleDrawerDelete')?.addEventListener('click',  deleteVehicle);
    document.getElementById('vehicleForm')?.addEventListener('submit', saveVehicle);

    // Copertina
    const coverInput = document.getElementById('vCoverInput');
    document.getElementById('vCoverUploadBtn')?.addEventListener('click', () => coverInput?.click());
    document.getElementById('vCoverArea')?.addEventListener('click', () => { if (!currentCoverUrl) coverInput?.click(); });
    coverInput?.addEventListener('change', e => {
        const f = e.target.files?.[0];
        if (f) { uploadCover(f); e.target.value = ''; }
    });
    document.getElementById('vCoverRemoveBtn')?.addEventListener('click', removeCover);

    // Galleria
    const galleryInput = document.getElementById('vGalleryInput');
    document.getElementById('vGalleryAddBtn')?.addEventListener('click', () => galleryInput?.click());
    galleryInput?.addEventListener('change', e => {
        const files = Array.from(e.target.files || []);
        if (files.length) { uploadGalleryImages(files); e.target.value = ''; }
    });

    // Sezione noleggio — mostra/nascondi al toggle "In noleggio"
    document.getElementById('vForRental')?.addEventListener('change', e => {
        const section = document.getElementById('rentalSection');
        if (section) section.style.display = e.target.checked ? 'flex' : 'none';
        if (!e.target.checked) resetRentalData();
    });

    // Tab formule noleggio
    document.querySelectorAll('.formula-tab').forEach(btn =>
        btn.addEventListener('click', () => switchFormulaTab(btn.dataset.formula)));

    // Toggle riscatto
    document.getElementById('vRedemptionEnabled')?.addEventListener('change', e => {
        const fields = document.getElementById('redemptionFields');
        if (fields) fields.style.display = e.target.checked ? 'flex' : 'none';
    });

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
