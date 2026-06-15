'use strict';

let _appCodes           = [];
let _primaryCodeId      = null;
let _optionalServices   = [];

const DEFAULT_OPTIONAL_SERVICES = [
    { key: 'damage_waiver', label: 'Riduzione Franchigia Danni/Furto', pricePerDay: 15.00, priceFlat: null },
    { key: 'second_driver', label: 'Secondo Conducente',               pricePerDay:  5.00, priceFlat: null },
    { key: 'child_seat',    label: 'Seggiolino bimbo',                 pricePerDay: null,  priceFlat: 10.00 },
    { key: 'snow_chains',   label: 'Catene da neve',                   pricePerDay: null,  priceFlat: 15.00 },
    { key: 'gps',           label: 'Navigatore GPS',                   pricePerDay:  5.00, priceFlat: null  },
    { key: 'out_of_hours',  label: 'Consegna Fuori Orario/Sede',       pricePerDay: null,  priceFlat: 20.00 },
    { key: 'straps_kit',    label: 'Kit Cinghie Fissaggio (Furgoni)',  pricePerDay: null,  priceFlat: 10.00 },
    { key: 'hand_truck',    label: 'Carrello a mano (Furgoni)',        pricePerDay: null,  priceFlat: 10.00 },
];

// ── Helpers ───────────────────────────────────────────────────────────────────

const getVal = id => document.getElementById(id)?.value.trim() || null;
const getNum = id => { const v = document.getElementById(id)?.value; return v !== '' && v != null ? Number(v) : null; };
const setVal = (id, v) => { const el = document.getElementById(id); if (el) el.value = v ?? ''; };

// ── Load profile ──────────────────────────────────────────────────────────────

async function loadProfile() {
    try {
        const [p, codes] = await Promise.all([
            apiFetch('/api/admin/profile'),
            apiFetch('/api/admin/app-codes'),
        ]);
        _appCodes = codes;

        // Dati aziendali
        setVal('pBusinessName', p.businessName);
        setVal('pPhone',        p.phone);
        setVal('pWhatsapp',     p.whatsappNumber);
        setVal('pEmail',        p.email);
        setVal('pWebsite',      p.websiteUrl);
        setVal('pTagline',      p.tagline);

        // Fiscale
        setVal('pVat',    p.vatNumber);
        setVal('pFiscal', p.fiscalCode);
        setVal('pRea',    p.reaNumber);

        // Localizzazione
        setVal('pAddress',  p.address);
        setVal('pCity',     p.city);
        setVal('pProvince', p.province);
        setVal('pZip',      p.zipCode);
        setVal('pLat',      p.latitude  != null ? p.latitude  : '');
        setVal('pLng',      p.longitude != null ? p.longitude : '');

        // Branding
        setColorField('pColorPrimary',   'swatchPrimary',   'pColorPrimaryText',   p.primaryColor);
        setColorField('pColorSecondary', 'swatchSecondary', 'pColorSecondaryText', p.secondaryColor);
        setColorField('pColorAccent',    'swatchAccent',    'pColorAccentText',    p.accentColor);

        // Immagini
        if (p.logoUrl)      { const i = document.getElementById('logoPreview');  if (i) { i.src = p.logoUrl;      i.style.display = 'block'; } }
        if (p.coverImageUrl){ const i = document.getElementById('coverPreview'); if (i) { i.src = p.coverImageUrl; i.style.display = 'block'; } }

        // App mobile
        document.getElementById('infoSlug').textContent   = p.slug     ?? '—';
        document.getElementById('infoActive').textContent = p.isActive  ? 'Attivo' : 'Inattivo';

        const avatar = document.getElementById('userAvatar');
        if (avatar) avatar.textContent = (p.businessName ?? '?')[0].toUpperCase();
        const uname = document.getElementById('userName');
        if (uname) uname.textContent = p.businessName ?? '—';

        renderAppCodes(_appCodes);
        loadRentalSettings(p);
        loadRentalConditions(p);
        loadRentalServicesCatalog(p);
        loadSmtpSettings(p);
        loadPrivacyPolicy(p);

        document.getElementById('loadingMsg').style.display    = 'none';
        document.getElementById('profileContent').style.display = 'flex';

    } catch (err) {
        const el = document.getElementById('loadingMsg');
        if (el) el.textContent = 'Errore nel caricamento del profilo.';
        showToast(err.message || 'Errore caricamento.', 'error');
    }
}

// ── Save sections ─────────────────────────────────────────────────────────────

async function saveSection(payload, btnId) {
    const btn = document.getElementById(btnId);
    if (btn) btn.disabled = true;
    try {
        await apiFetch('/api/admin/profile', {
            method:  'PUT',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(payload),
        });
        showToast('Salvato.');
    } catch (err) {
        showToast(err.message || 'Errore salvataggio.', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

function saveAnagrafica() {
    return saveSection({
        businessName:   getVal('pBusinessName'),
        phone:          getVal('pPhone'),
        whatsappNumber: getVal('pWhatsapp'),
        email:          getVal('pEmail'),
        websiteUrl:     getVal('pWebsite'),
        tagline:        getVal('pTagline'),
    }, 'saveAnagraficaBtn');
}

function saveFiscale() {
    return saveSection({
        vatNumber:  getVal('pVat'),
        fiscalCode: getVal('pFiscal'),
        reaNumber:  getVal('pRea'),
    }, 'saveFiscaleBtn');
}

function saveLocalizzazione() {
    return saveSection({
        address:   getVal('pAddress'),
        city:      getVal('pCity'),
        province:  getVal('pProvince'),
        zipCode:   getVal('pZip'),
        latitude:  getNum('pLat'),
        longitude: getNum('pLng'),
    }, 'saveLocalizzazioneBtn');
}

function saveBranding() {
    return saveSection({
        primaryColor:   getVal('pColorPrimaryText'),
        secondaryColor: getVal('pColorSecondaryText'),
        accentColor:    getVal('pColorAccentText'),
    }, 'saveBrandingBtn');
}

// ── Geocoding ─────────────────────────────────────────────────────────────────

async function geocode() {
    const address  = getVal('pAddress') || '';
    const city     = getVal('pCity')    || '';
    const province = getVal('pProvince')|| '';
    const query    = [address, city, province, 'Italy'].filter(Boolean).join(', ');

    if (!city && !address) { showToast('Inserisci almeno la città.', 'error'); return; }

    const btn = document.getElementById('geocodeBtn');
    if (btn) btn.disabled = true;
    try {
        const res  = await fetch(
            `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&limit=1`,
            { headers: { 'Accept-Language': 'it', 'User-Agent': 'MyCars-Admin/1.0' } }
        );
        const data = await res.json();
        if (!data.length) { showToast('Indirizzo non trovato.', 'error'); return; }
        const lat = parseFloat(data[0].lat).toFixed(7);
        const lng = parseFloat(data[0].lon).toFixed(7);
        setVal('pLat', lat); setVal('pLng', lng);
        showToast(`Coordinate: ${lat}, ${lng}`);
    } catch { showToast('Errore geocoding.', 'error'); }
    finally { if (btn) btn.disabled = false; }
}

// ── File upload ───────────────────────────────────────────────────────────────

async function handleUpload(type) {
    const fileInput   = document.getElementById(`${type}File`);
    const previewEl   = document.getElementById(`${type}Preview`);
    const progressEl  = document.getElementById(`${type}Progress`);
    const progressBar = document.getElementById(`${type}ProgressBar`);
    const errorEl     = document.getElementById(`${type}Error`);
    const file        = fileInput?.files?.[0];
    if (!file) return;

    errorEl?.classList.remove('show');
    const reader = new FileReader();
    reader.onload = e => { if (previewEl) { previewEl.src = e.target.result; previewEl.style.display = 'block'; } };
    reader.readAsDataURL(file);

    const formData = new FormData();
    formData.append('file', file);
    if (progressEl) progressEl.style.display = '';
    if (progressBar) progressBar.style.width = '30%';

    try {
        const res = await fetch(`/api/admin/profile/${type}`, {
            method: 'POST', body: formData, credentials: 'same-origin',
        });
        if (progressBar) progressBar.style.width = '100%';
        if (!res.ok) {
            const data = await res.json().catch(() => null);
            throw new Error(data?.message ?? 'Errore upload.');
        }
        const data = await res.json();
        if (previewEl) previewEl.src = data.url;
        showToast(`${type === 'logo' ? 'Logo' : 'Copertina'} aggiornata.`);
    } catch (err) {
        if (errorEl) { errorEl.textContent = err.message; errorEl.classList.add('show'); }
        if (previewEl) previewEl.style.display = 'none';
    } finally {
        setTimeout(() => {
            if (progressEl) progressEl.style.display = 'none';
            if (progressBar) progressBar.style.width = '0%';
        }, 800);
    }
}

// Drag & drop
['logo', 'cover'].forEach(type => {
    const area = document.getElementById(`${type}Area`);
    if (!area) return;
    area.addEventListener('dragover',  e => { e.preventDefault(); area.classList.add('drag-over'); });
    area.addEventListener('dragleave', () => area.classList.remove('drag-over'));
    area.addEventListener('drop', e => {
        e.preventDefault(); area.classList.remove('drag-over');
        const input = document.getElementById(`${type}File`);
        if (e.dataTransfer.files.length) {
            const dt = new DataTransfer(); dt.items.add(e.dataTransfer.files[0]); input.files = dt.files;
            handleUpload(type);
        }
    });
});

// ── App Codes ─────────────────────────────────────────────────────────────────

function renderAppCodes(codes) {
    const primary = codes.find(c => c.isPrimary && c.isActive);
    const extras  = codes.filter(c => !c.isPrimary && c.isActive);

    _primaryCodeId = primary?.id ?? null;

    const box   = document.getElementById('primaryCodeBox');
    const usage = document.getElementById('primaryCodeUsage');
    const hint  = document.getElementById('deepLinkHint');
    if (primary) {
        if (box)   box.textContent   = primary.code;
        if (usage) usage.textContent = `Usato da ${primary.useCount} dispositiv${primary.useCount === 1 ? 'o' : 'i'}`;
        if (hint)  hint.textContent  = `mycars://connect?code=${primary.code}`;
    }

    const list = document.getElementById('extraCodesList');
    if (!list) return;
    if (!extras.length) {
        list.innerHTML = `<div style="font-size:13px;color:var(--text-light);padding:4px 0">Nessun codice aggiuntivo.</div>`;
        return;
    }
    list.innerHTML = extras.map(c => `
        <div style="display:flex;align-items:center;gap:8px;background:var(--surface);
                    border:1px solid var(--border);border-radius:var(--r-sm);padding:8px 12px">
            <div style="flex:1;min-width:0">
                <code style="font-size:14px;font-weight:700;color:var(--navy)">${escHtml(c.code)}</code>
                ${c.label ? `<span style="font-size:12px;color:var(--text-light);margin-left:8px">${escHtml(c.label)}</span>` : ''}
            </div>
            <span style="font-size:11px;color:var(--text-light);white-space:nowrap">${c.useCount} utilizzi</span>
            <button class="btn btn-outline btn-sm"
                    style="color:var(--red);border-color:transparent;padding:4px 8px;min-width:0"
                    onclick="deleteCode('${c.id}')" title="Elimina">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width:13px;height:13px"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>
            </button>
        </div>
    `).join('');
}

function escHtml(str) {
    return (str ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function copyPrimaryCode() {
    const code = document.getElementById('primaryCodeBox')?.textContent?.trim();
    if (!code || code === '—') return;
    navigator.clipboard.writeText(code).then(() => showToast('Codice copiato!'));
}

function showCreateCodeForm() {
    const form = document.getElementById('createCodeForm');
    const btn  = document.getElementById('addCodeBtn');
    if (form) form.style.display = 'flex';
    if (btn)  btn.style.display  = 'none';
    document.getElementById('newCodeInput')?.focus();
}

function cancelCreateCode() {
    const form = document.getElementById('createCodeForm');
    const btn  = document.getElementById('addCodeBtn');
    if (form) { form.style.display = 'none'; form.querySelectorAll('input').forEach(i => i.value = ''); }
    if (btn)  btn.style.display = '';
    document.getElementById('codeCreateError')?.classList.remove('show');
}

async function submitCreateCode() {
    const errEl   = document.getElementById('codeCreateError');
    const codeVal = document.getElementById('newCodeInput')?.value.trim().toUpperCase();
    const label   = document.getElementById('newCodeLabel')?.value.trim() || null;
    const expires = document.getElementById('newCodeExpires')?.value;
    const maxUses = document.getElementById('newCodeMaxUses')?.value;

    errEl?.classList.remove('show');
    if (!codeVal) { showErr(errEl, 'Il codice è obbligatorio.'); return; }
    if (!/^[A-Z0-9][A-Z0-9_-]{2,31}$/.test(codeVal)) {
        showErr(errEl, 'Formato non valido: solo lettere maiuscole, numeri, _ e – (min 3 caratteri).');
        return;
    }
    try {
        const created = await apiFetch('/api/admin/app-codes', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({
                code: codeVal, label,
                expiresAt: expires ? expires + 'T23:59:59Z' : null,
                maxUses:   maxUses ? parseInt(maxUses, 10) : null,
            }),
        });
        _appCodes.push(created);
        renderAppCodes(_appCodes);
        cancelCreateCode();
        showToast('Codice creato.');
    } catch (err) { showErr(errEl, err?.message || 'Errore creazione codice.'); }
}

async function deleteCode(id) {
    if (!confirm('Eliminare questo codice?')) return;
    try {
        await apiFetch(`/api/admin/app-codes/${id}`, { method: 'DELETE' });
        _appCodes = _appCodes.filter(c => c.id !== id);
        renderAppCodes(_appCodes);
        showToast('Codice eliminato.');
    } catch (err) { showToast(err?.message || 'Errore eliminazione.', 'error'); }
}

function showErr(el, msg) { if (!el) return; el.textContent = msg; el.classList.add('show'); }

// ── Password form ─────────────────────────────────────────────────────────────

function initPasswordForm() {
    const form   = document.getElementById('changePasswordForm');
    const errBox = document.getElementById('pwdError');
    form?.addEventListener('submit', async e => {
        e.preventDefault();
        errBox.classList.remove('show');
        const newPwd  = document.getElementById('newPassword').value;
        const confPwd = document.getElementById('confirmPassword').value;
        if (newPwd !== confPwd) {
            errBox.textContent = 'Le password non coincidono.'; errBox.classList.add('show'); return;
        }
        showToast('Funzionalità disponibile prossimamente.', 'error');
    });
}

// ── Health dashboard ──────────────────────────────────────────────────────────

window._healthLoaded = false;

async function loadSystemInfo() {
    window._healthLoaded = true;
    const grid = document.getElementById('healthGrid');
    if (!grid) return;

    // Skeleton
    grid.innerHTML = Array(5).fill(`
        <div class="health-card">
            <div class="health-card-header">
                <div class="health-dot load"></div>
                <div class="health-name" style="background:var(--surface);border-radius:4px;width:80px;height:14px"></div>
            </div>
        </div>`).join('');

    try {
        const s = await apiFetch('/api/admin/system');

        const cards = [
            {
                name:   'Database',
                ok:     s.database.ok,
                detail: s.database.provider + (s.database.host ? ` · ${s.database.host}` : ''),
                error:  s.database.error,
            },
            {
                name:   'SMTP / Email',
                ok:     s.smtp.ok,
                detail: s.smtp.ok
                    ? `${s.smtp.host}:${s.smtp.port}` + (s.smtp.from ? `\n${s.smtp.from}` : '')
                    : 'Non configurato',
            },
            {
                name:   'Push VAPID',
                ok:     s.vapid.ok,
                detail: s.vapid.ok
                    ? `Subject: ${s.vapid.subject}`
                    : 'Chiavi VAPID non configurate',
            },
            {
                name:   'File storage',
                ok:     s.storage.ok,
                detail: s.storage.path ?? '—',
            },
            {
                name:   'ASP.NET Core',
                ok:     true,
                detail: `${s.aspnet.runtime} · ${s.aspnet.environment}\nUptime: ${fmtUptime(s.aspnet.uptimeSec)}`,
            },
        ];

        grid.innerHTML = cards.map(c => {
            const cls    = c.ok ? 'ok' : 'err';
            const label  = c.ok ? 'OK' : 'Errore';
            return `
                <div class="health-card">
                    <div class="health-card-header">
                        <div class="health-dot ${cls}"></div>
                        <div class="health-name">${escHtml(c.name)}</div>
                    </div>
                    <span class="health-status ${cls}">${label}</span>
                    <div class="health-detail">${escHtml(c.detail ?? '').replace(/\n/g, '<br>')}</div>
                    ${c.error ? `<div class="health-error">${escHtml(c.error)}</div>` : ''}
                </div>`;
        }).join('');

        // Info sistema
        const tbody = document.getElementById('sysInfoTable');
        if (tbody) tbody.innerHTML = `
            <tr><td style="padding:7px 20px 7px 0;color:var(--text-light);font-size:13px">Versione</td>
                <td style="padding:7px 0;font-size:13px;font-weight:600">MyCars 1.0</td></tr>
            <tr><td style="padding:7px 20px 7px 0;color:var(--text-light);font-size:13px">Runtime</td>
                <td style="padding:7px 0;font-size:13px;font-weight:600">${escHtml(s.aspnet.runtime)}</td></tr>
            <tr><td style="padding:7px 20px 7px 0;color:var(--text-light);font-size:13px">Ambiente</td>
                <td style="padding:7px 0;font-size:13px;font-weight:600">${escHtml(s.aspnet.environment)}</td></tr>
            <tr><td style="padding:7px 20px 7px 0;color:var(--text-light);font-size:13px">Uptime</td>
                <td style="padding:7px 0;font-size:13px;font-weight:600">${escHtml(fmtUptime(s.aspnet.uptimeSec))}</td></tr>
            <tr><td style="padding:7px 20px 7px 0;color:var(--text-light);font-size:13px">Database</td>
                <td style="padding:7px 0;font-size:13px;font-weight:600">${escHtml(s.database.provider)}</td></tr>
            <tr style="border-bottom:none">
                <td style="padding:7px 20px 7px 0;color:var(--text-light);font-size:13px">Endpoint hash</td>
                <td style="padding:7px 0;font-size:13px">
                    <a href="/api/admin/hash?password=test" target="_blank" style="color:var(--blue);text-decoration:underline">/api/admin/hash</a>
                    <span style="color:var(--text-light);font-size:12px;display:block">Solo Development</span>
                </td>
            </tr>`;

    } catch (err) {
        grid.innerHTML = `
            <div class="health-card" style="grid-column:1/-1">
                <div class="health-card-header">
                    <div class="health-dot err"></div>
                    <div class="health-name">Errore caricamento</div>
                </div>
                <div class="health-error">${escHtml(err.message)}</div>
            </div>`;
    }
}

function fmtUptime(sec) {
    const h = Math.floor(sec / 3600);
    const m = Math.floor((sec % 3600) / 60);
    if (h > 0) return `${h}h ${m}m`;
    return `${m}m`;
}

// ── Rental settings ───────────────────────────────────────────────────────────

function loadRentalSettings(profile) {
    const setChk = (id, v) => { const el = document.getElementById(id); if (el) el.checked = !!v; };
    setChk('rsModuleEnabled', profile.rentalModuleEnabled);
    setChk('rsPhotosEnabled', profile.rentalPhotosEnabled);
    setChk('rsContractPdf',   profile.rentalContractPdfEnabled);
    setChk('rsShowPrices',    profile.rentalShowPrices);
}

async function saveRentalSettings() {
    const btn = document.getElementById('saveRentalSettingsBtn');
    if (btn) btn.disabled = true;
    const payload = {
        rentalModuleEnabled:      document.getElementById('rsModuleEnabled')?.checked ?? false,
        rentalPhotosEnabled:      document.getElementById('rsPhotosEnabled')?.checked ?? false,
        rentalContractPdfEnabled: document.getElementById('rsContractPdf')?.checked ?? false,
        rentalShowPrices:         document.getElementById('rsShowPrices')?.checked ?? false,
    };
    try {
        await apiFetch('/api/admin/profile/rental-settings', {
            method:  'PUT',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(payload),
        });
        showToast('Impostazioni noleggio salvate.');
    } catch (err) {
        showToast(err.message || 'Errore salvataggio impostazioni noleggio.', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ── Rental conditions ─────────────────────────────────────────────────────────

function loadRentalConditions(profile) {
    const c = profile.rentalConditions || {};
    const setChk = (id, v) => { const el = document.getElementById(id); if (el) el.checked = !!v; };
    setVal('rcMinAge',          c.minDriverAge     ?? 21);
    setVal('rcMinLicense',      c.minLicenseYears  ?? 2);
    setVal('rcFuelPolicy',      c.fuelPolicy       ?? 'full_to_full');
    setVal('rcDepositDefault',  c.depositDefault   != null ? c.depositDefault : '');
    setChk('rcCreditCardRequired', c.creditCardRequired ?? false);
    setVal('rcCleaningNote',    c.cleaningPenaltyNote ?? '');

    const methods = c.acceptedPaymentMethods ?? ['credit_card', 'debit_card', 'cash'];
    setChk('rcPayCredit',   methods.includes('credit_card'));
    setChk('rcPayDebit',    methods.includes('debit_card'));
    setChk('rcPayCash',     methods.includes('cash'));
    setChk('rcPayTransfer', methods.includes('transfer'));
}

async function saveRentalConditions() {
    const btn = document.getElementById('saveRentalConditionsBtn');
    if (btn) btn.disabled = true;

    const methods = [];
    if (document.getElementById('rcPayCredit')?.checked)   methods.push('credit_card');
    if (document.getElementById('rcPayDebit')?.checked)    methods.push('debit_card');
    if (document.getElementById('rcPayCash')?.checked)     methods.push('cash');
    if (document.getElementById('rcPayTransfer')?.checked) methods.push('transfer');

    const payload = {
        minDriverAge:          getNum('rcMinAge'),
        minLicenseYears:       getNum('rcMinLicense'),
        fuelPolicy:            getVal('rcFuelPolicy'),
        depositDefault:        getNum('rcDepositDefault'),
        creditCardRequired:    document.getElementById('rcCreditCardRequired')?.checked ?? false,
        acceptedPaymentMethods: methods,
        cleaningPenaltyNote:   getVal('rcCleaningNote'),
    };
    try {
        await apiFetch('/api/admin/profile/rental-conditions', {
            method:  'PUT',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(payload),
        });
        showToast('Condizioni di noleggio salvate.');
    } catch (err) {
        showToast(err.message || 'Errore salvataggio condizioni.', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ── Rental services catalog ────────────────────────────────────────────────────

function loadRentalServicesCatalog(profile) {
    const cat      = profile.rentalServicesCatalog || {};
    const included = cat.included ?? ['rca', 'roadside_24h', 'maintenance'];
    const setChk   = (id, v) => { const el = document.getElementById(id); if (el) el.checked = !!v; };

    setChk('scIncRca',         included.includes('rca'));
    setChk('scIncRoadside',    included.includes('roadside_24h'));
    setChk('scIncMaintenance', included.includes('maintenance'));

    _optionalServices = (cat.optional ?? DEFAULT_OPTIONAL_SERVICES).map(s => ({ ...s }));
    renderOptionalServices();
}

function renderOptionalServices() {
    const list = document.getElementById('optionalServicesList');
    if (!list) return;

    list.innerHTML = _optionalServices.map((_, idx) => `
        <div class="svc-row" data-idx="${idx}">
            <input type="text"   class="form-input svc-label" placeholder="Nome servizio">
            <input type="number" class="form-input svc-day"   placeholder="—" min="0" step="0.5" style="text-align:right">
            <input type="number" class="form-input svc-flat"  placeholder="—" min="0" step="0.5" style="text-align:right">
            <button type="button" class="btn btn-outline btn-sm svc-remove"
                    style="color:var(--red);border-color:transparent;min-width:0;padding:5px 8px"
                    data-idx="${idx}" title="Rimuovi">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width:13px;height:13px"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6M14 11v6"/></svg>
            </button>
        </div>`).join('');

    // Imposta valori in modo sicuro (no XSS in attributi value)
    list.querySelectorAll('.svc-row').forEach((row, idx) => {
        const svc = _optionalServices[idx];
        if (!svc) return;
        const lbl  = row.querySelector('.svc-label');
        const day  = row.querySelector('.svc-day');
        const flat = row.querySelector('.svc-flat');
        if (lbl)  lbl.value  = svc.label        || '';
        if (day)  day.value  = svc.pricePerDay  ?? '';
        if (flat) flat.value = svc.priceFlat    ?? '';
    });

    list.querySelectorAll('.svc-remove').forEach(btn => {
        btn.addEventListener('click', () => removeOptionalService(parseInt(btn.dataset.idx)));
    });
}

function syncServicesFromDom() {
    document.querySelectorAll('#optionalServicesList .svc-row').forEach((row, idx) => {
        if (!_optionalServices[idx]) return;
        const dayVal  = row.querySelector('.svc-day')?.value;
        const flatVal = row.querySelector('.svc-flat')?.value;
        _optionalServices[idx].label       = row.querySelector('.svc-label')?.value?.trim() || '';
        _optionalServices[idx].pricePerDay = dayVal  ? parseFloat(dayVal)  : null;
        _optionalServices[idx].priceFlat   = flatVal ? parseFloat(flatVal) : null;
    });
}

function addOptionalService() {
    syncServicesFromDom();
    _optionalServices.push({ key: `custom_${Date.now()}`, label: '', pricePerDay: null, priceFlat: null });
    renderOptionalServices();
    const rows = document.querySelectorAll('#optionalServicesList .svc-row');
    rows[rows.length - 1]?.querySelector('.svc-label')?.focus();
}

function removeOptionalService(idx) {
    syncServicesFromDom();
    _optionalServices.splice(idx, 1);
    renderOptionalServices();
}

async function saveRentalServicesCatalog() {
    syncServicesFromDom();
    const btn = document.getElementById('saveServicesCatalogBtn');
    if (btn) btn.disabled = true;

    const included = [];
    if (document.getElementById('scIncRca')?.checked)         included.push('rca');
    if (document.getElementById('scIncRoadside')?.checked)    included.push('roadside_24h');
    if (document.getElementById('scIncMaintenance')?.checked) included.push('maintenance');

    const optional = _optionalServices
        .filter(s => s.label.trim() !== '')
        .map(s => ({
            key:         s.key,
            label:       s.label,
            pricePerDay: s.pricePerDay,
            priceFlat:   s.priceFlat,
        }));

    try {
        await apiFetch('/api/admin/profile/rental-services-catalog', {
            method:  'PUT',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ included, optional }),
        });
        showToast('Catalogo servizi salvato.');
    } catch (err) {
        showToast(err.message || 'Errore salvataggio catalogo servizi.', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ── SMTP settings ─────────────────────────────────────────────────────────────

function loadSmtpSettings(profile) {
    setVal('smtpHost',      profile.smtpHost);
    setVal('smtpPort',      profile.smtpPort != null ? profile.smtpPort : '');
    setVal('smtpUsername',  profile.smtpUsername);
    setVal('smtpFromEmail', profile.smtpFromEmail);
    setVal('smtpFromName',  profile.smtpFromName);
    const ssl = document.getElementById('smtpUseSsl');
    if (ssl) ssl.checked = profile.smtpUseSsl !== false;
    // password: never populated (read-back)
    const pwd = document.getElementById('smtpPassword');
    if (pwd) pwd.placeholder = profile.smtpPassword ? '••••••••' : 'lascia vuoto per non modificarla';
}

async function saveSmtpSettings() {
    const btn = document.getElementById('saveSmtpBtn');
    if (btn) btn.disabled = true;
    const payload = {
        host:      getVal('smtpHost'),
        port:      getNum('smtpPort') ?? 587,
        useSsl:    document.getElementById('smtpUseSsl')?.checked ?? true,
        username:  getVal('smtpUsername'),
        password:  getVal('smtpPassword'),
        fromEmail: getVal('smtpFromEmail'),
        fromName:  getVal('smtpFromName'),
    };
    try {
        await apiFetch('/api/admin/profile/smtp', {
            method:  'PUT',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(payload),
        });
        showToast('Configurazione email salvata.');
        const pwd = document.getElementById('smtpPassword');
        if (pwd) { pwd.value = ''; pwd.placeholder = '••••••••'; }
    } catch (err) {
        showToast(err.message || 'Errore salvataggio SMTP.', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ── Privacy Policy editor ─────────────────────────────────────────────────────

let ppDirty = false;

function loadPrivacyPolicy(profile) {
    const ed = document.getElementById('ppEditor');
    if (ed) ed.innerHTML = profile.privacyPolicyHtml || '';
    ppDirty = false;
}

function ppCmd(cmd) {
    document.getElementById('ppEditor')?.focus();
    document.execCommand(cmd, false, null);
}

function ppHeading(tag) {
    document.getElementById('ppEditor')?.focus();
    document.execCommand('formatBlock', false, tag);
}

function ppLink() {
    const url = prompt('URL del link:');
    if (!url) return;
    document.getElementById('ppEditor')?.focus();
    document.execCommand('createLink', false, url);
}

async function savePrivacyPolicy() {
    const btn    = document.getElementById('savePrivacyBtn');
    const status = document.getElementById('ppStatus');
    const html   = document.getElementById('ppEditor')?.innerHTML ?? '';

    if (btn) btn.disabled = true;
    if (status) status.textContent = '';
    try {
        await apiFetch('/api/admin/profile/privacy-policy', {
            method:  'PUT',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ html }),
        });
        ppDirty = false;
        showToast('Privacy Policy salvata.');
        if (status) status.textContent = 'Salvato.';
    } catch (err) {
        showToast(err.message || 'Errore salvataggio Privacy Policy.', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ── Primary code edit ─────────────────────────────────────────────────────────

function startEditPrimaryCode() {
    const current = document.getElementById('primaryCodeBox')?.textContent?.trim();
    const input   = document.getElementById('primaryCodeInput');
    if (input) input.value = (current && current !== '—') ? current : '';
    document.getElementById('codeDisplayMode').style.display = 'none';
    document.getElementById('codeEditMode').style.display    = 'flex';
    document.getElementById('codeEditError')?.classList.remove('show');
    input?.focus();
}

function cancelEditPrimaryCode() {
    document.getElementById('codeDisplayMode').style.display = 'flex';
    document.getElementById('codeEditMode').style.display    = 'none';
    document.getElementById('codeEditError')?.classList.remove('show');
}

async function savePrimaryCode() {
    const errEl = document.getElementById('codeEditError');
    const input = document.getElementById('primaryCodeInput');
    const newCode = input?.value.trim().toUpperCase();
    errEl?.classList.remove('show');

    if (!newCode || newCode.length < 3) {
        showErr(errEl, 'Il codice deve avere almeno 3 caratteri.');
        return;
    }
    if (!_primaryCodeId) {
        showErr(errEl, 'Codice principale non trovato.');
        return;
    }

    try {
        const updated = await apiFetch(`/api/admin/app-codes/${_primaryCodeId}`, {
            method:  'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ code: newCode }),
        });
        const idx = _appCodes.findIndex(c => c.id === _primaryCodeId);
        if (idx >= 0) _appCodes[idx] = updated;
        renderAppCodes(_appCodes);
        cancelEditPrimaryCode();
        showToast('Codice principale aggiornato.');
    } catch (err) {
        showErr(errEl, err.message || 'Errore aggiornamento codice.');
    }
}

// ── Init ──────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadProfile();
    initPasswordForm();

    document.getElementById('saveAnagraficaBtn')    ?.addEventListener('click', saveAnagrafica);
    document.getElementById('saveFiscaleBtn')        ?.addEventListener('click', saveFiscale);
    document.getElementById('saveLocalizzazioneBtn') ?.addEventListener('click', saveLocalizzazione);
    document.getElementById('saveBrandingBtn')       ?.addEventListener('click', saveBranding);
    document.getElementById('geocodeBtn')            ?.addEventListener('click', geocode);
    document.getElementById('refreshHealthBtn')      ?.addEventListener('click', () => { window._healthLoaded = false; loadSystemInfo(); });
    document.getElementById('saveRentalSettingsBtn')    ?.addEventListener('click', saveRentalSettings);
    document.getElementById('saveRentalConditionsBtn')  ?.addEventListener('click', saveRentalConditions);
    document.getElementById('saveServicesCatalogBtn')   ?.addEventListener('click', saveRentalServicesCatalog);
    document.getElementById('addServiceBtn')            ?.addEventListener('click', addOptionalService);
    document.getElementById('saveSmtpBtn')              ?.addEventListener('click', saveSmtpSettings);
});
