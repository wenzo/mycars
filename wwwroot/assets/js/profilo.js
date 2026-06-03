'use strict';

let profile  = null;
let appCodes = [];

// ── Load ──────────────────────────────────────────────────────────────────────

async function loadProfile() {
    try {
        [profile, appCodes] = await Promise.all([
            apiFetch('/api/admin/profile'),
            apiFetch('/api/admin/app-codes'),
        ]);
        populateForm(profile);
        renderAppCodes(appCodes);
        document.getElementById('loadingMsg').style.display  = 'none';
        document.getElementById('profileContent').style.display = '';
        document.getElementById('saveBtn').style.display     = '';
    } catch {
        document.getElementById('loadingMsg').textContent = 'Errore nel caricamento del profilo.';
    }
}

function populateForm(p) {
    setVal('businessName',   p.businessName);
    setVal('vatNumber',      p.vatNumber);
    setVal('fiscalCode',     p.fiscalCode);
    setVal('email',          p.email);
    setVal('phone',          p.phone);
    setVal('websiteUrl',     p.websiteUrl);
    setVal('whatsappNumber', p.whatsappNumber);

    setColorField('primary',   p.primaryColor);
    setColorField('secondary', p.secondaryColor);
    setColorField('accent',    p.accentColor);

    if (p.logoUrl) {
        const img = document.getElementById('logoPreview');
        img.src   = p.logoUrl;
        img.style.display = 'block';
    }
    if (p.coverImageUrl) {
        const img = document.getElementById('coverPreview');
        img.src   = p.coverImageUrl;
        img.style.display = 'block';
    }

    document.getElementById('infoSlug').textContent   = p.slug    ?? '—';
    document.getElementById('infoActive').textContent = p.isActive ? 'Attivo' : 'Inattivo';

    const avatar = document.getElementById('userAvatar');
    if (avatar) avatar.textContent = (p.businessName ?? '?')[0].toUpperCase();
    const name = document.getElementById('userName');
    if (name) name.textContent = p.businessName ?? '—';
}

function setVal(id, val) {
    const el = document.getElementById(id);
    if (el) el.value = val ?? '';
}

function setColorField(key, value) {
    const hex    = document.getElementById(`${key}ColorHex`);
    const picker = document.getElementById(`${key}Color`);
    const swatch = document.getElementById(`swatch${capitalize(key)}`);
    if (!hex || !picker || !swatch) return;
    const v = value ?? '';
    hex.value = v;
    if (/^#[0-9a-fA-F]{6}$/.test(v)) {
        picker.value            = v;
        swatch.style.background = v;
    }
}

function capitalize(s) { return s.charAt(0).toUpperCase() + s.slice(1); }

// ── Color picker sync ─────────────────────────────────────────────────────────

function updateSwatch(key) {
    const picker = document.getElementById(`${key}Color`);
    const hex    = document.getElementById(`${key}ColorHex`);
    const swatch = document.getElementById(`swatch${capitalize(key)}`);
    if (!picker || !hex || !swatch) return;
    hex.value = picker.value;
    swatch.style.background = picker.value;
}

function syncColorPicker(key) {
    const hex    = document.getElementById(`${key}ColorHex`);
    const picker = document.getElementById(`${key}Color`);
    const swatch = document.getElementById(`swatch${capitalize(key)}`);
    if (!hex || !picker || !swatch) return;
    const v = hex.value.trim();
    if (/^#[0-9a-fA-F]{6}$/.test(v)) {
        picker.value            = v;
        swatch.style.background = v;
    }
}

// ── Save profile ──────────────────────────────────────────────────────────────

async function saveProfile() {
    const errEl = document.getElementById('profileError');
    errEl.classList.remove('show');

    const body = {
        businessName:   document.getElementById('businessName').value.trim()   || null,
        vatNumber:      document.getElementById('vatNumber').value.trim()       || null,
        fiscalCode:     document.getElementById('fiscalCode').value.trim()      || null,
        email:          document.getElementById('email').value.trim()           || null,
        phone:          document.getElementById('phone').value.trim()           || null,
        websiteUrl:     document.getElementById('websiteUrl').value.trim()      || null,
        whatsappNumber: document.getElementById('whatsappNumber').value.trim()  || null,
        primaryColor:   document.getElementById('primaryColorHex').value.trim()    || null,
        secondaryColor: document.getElementById('secondaryColorHex').value.trim()  || null,
        accentColor:    document.getElementById('accentColorHex').value.trim()     || null,
    };

    try {
        profile = await apiFetch('/api/admin/profile', {
            method:  'PUT',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(body),
        });
        showToast('Profilo salvato', 'success');
    } catch (err) {
        errEl.textContent = err?.message || 'Errore durante il salvataggio.';
        errEl.classList.add('show');
    }
}

// ── File upload ───────────────────────────────────────────────────────────────

async function handleUpload(type) {
    const fileInput   = document.getElementById(`${type}File`);
    const previewEl   = document.getElementById(`${type}Preview`);
    const progressEl  = document.getElementById(`${type}Progress`);
    const progressBar = document.getElementById(`${type}ProgressBar`);
    const errorEl     = document.getElementById(`${type}Error`);

    const file = fileInput.files?.[0];
    if (!file) return;

    errorEl.classList.remove('show');

    const reader = new FileReader();
    reader.onload = e => { previewEl.src = e.target.result; previewEl.style.display = 'block'; };
    reader.readAsDataURL(file);

    const formData = new FormData();
    formData.append('file', file);
    progressEl.style.display = '';
    progressBar.style.width  = '30%';

    try {
        const res = await fetch(`/api/admin/profile/${type}`, {
            method: 'POST', body: formData, credentials: 'same-origin',
        });
        progressBar.style.width = '100%';
        if (!res.ok) {
            const data = await res.json().catch(() => null);
            throw new Error(data?.message ?? 'Errore durante l\'upload.');
        }
        const data = await res.json();
        previewEl.src = data.url;
        showToast(`${type === 'logo' ? 'Logo' : 'Copertina'} aggiornata`, 'success');
    } catch (err) {
        errorEl.textContent = err.message ?? 'Errore upload.';
        errorEl.classList.add('show');
        previewEl.style.display = 'none';
    } finally {
        setTimeout(() => { progressEl.style.display = 'none'; progressBar.style.width = '0%'; }, 800);
    }
}

['logo', 'cover'].forEach(type => {
    const area = document.getElementById(`${type}Area`);
    if (!area) return;
    area.addEventListener('dragover',  e => { e.preventDefault(); area.classList.add('drag-over'); });
    area.addEventListener('dragleave', () => area.classList.remove('drag-over'));
    area.addEventListener('drop', e => {
        e.preventDefault();
        area.classList.remove('drag-over');
        const input = document.getElementById(`${type}File`);
        if (e.dataTransfer.files.length) {
            const dt = new DataTransfer();
            dt.items.add(e.dataTransfer.files[0]);
            input.files = dt.files;
            handleUpload(type);
        }
    });
});

// ── App Codes ─────────────────────────────────────────────────────────────────

function renderAppCodes(codes) {
    const primary = codes.find(c => c.isPrimary && c.isActive);
    const extras  = codes.filter(c => !c.isPrimary && c.isActive);

    // Codice primario
    const box     = document.getElementById('primaryCodeBox');
    const usage   = document.getElementById('primaryCodeUsage');
    const hint    = document.getElementById('deepLinkHint');
    if (primary) {
        box.textContent   = primary.code;
        usage.textContent = `Usato da ${primary.useCount} dispositiv${primary.useCount === 1 ? 'o' : 'i'}`;
        if (hint) hint.textContent = `mycars://connect?code=${primary.code}`;
    }

    // Codici aggiuntivi
    const list = document.getElementById('extraCodesList');
    if (!list) return;
    if (!extras.length) {
        list.innerHTML = `<div style="font-size:13px;color:var(--text-light);padding:4px 0">Nessun codice aggiuntivo.</div>`;
        return;
    }
    list.innerHTML = extras.map(c => `
        <div style="display:flex;align-items:center;justify-content:space-between;gap:8px;
                    background:var(--surface);border:1px solid var(--border);
                    border-radius:var(--r-sm);padding:8px 12px">
            <div style="flex:1;min-width:0">
                <code style="font-size:14px;font-weight:700;color:var(--navy)">${escHtml(c.code)}</code>
                ${c.label ? `<span style="font-size:12px;color:var(--text-light);margin-left:8px">${escHtml(c.label)}</span>` : ''}
            </div>
            <span style="font-size:11px;color:var(--text-light);white-space:nowrap">${c.useCount} utilizzi</span>
            <button class="btn btn-outline btn-sm"
                    style="color:var(--red);border-color:transparent;padding:4px 8px;min-width:0"
                    onclick="deleteCode('${c.id}')" title="Elimina codice">
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

// ── Create code form ──────────────────────────────────────────────────────────

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

    const payload = {
        code:     codeVal,
        label,
        expiresAt: expires ? expires + 'T23:59:59Z' : null,
        maxUses:   maxUses ? parseInt(maxUses, 10) : null,
    };

    try {
        const created = await apiFetch('/api/admin/app-codes', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(payload),
        });
        appCodes.push(created);
        renderAppCodes(appCodes);
        cancelCreateCode();
        showToast('Codice creato.');
    } catch (err) {
        showErr(errEl, err?.message || 'Errore creazione codice.');
    }
}

async function deleteCode(id) {
    if (!confirm('Eliminare questo codice? Chi lo ha già usato potrà continuare ad accedere tramite il codice principale.')) return;

    try {
        await apiFetch(`/api/admin/app-codes/${id}`, { method: 'DELETE' });
        appCodes = appCodes.filter(c => c.id !== id);
        renderAppCodes(appCodes);
        showToast('Codice eliminato.');
    } catch (err) {
        showToast(err?.message || 'Errore eliminazione.', 'error');
    }
}

function showErr(el, msg) {
    if (!el) return;
    el.textContent = msg;
    el.classList.add('show');
}

// ── Init ──────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', loadProfile);
