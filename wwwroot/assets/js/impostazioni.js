'use strict';

// ── Helpers ───────────────────────────────────────────────────────────────────

const getVal  = id => document.getElementById(id)?.value.trim() || null;
const getNum  = id => { const v = document.getElementById(id)?.value; return v !== '' && v != null ? Number(v) : null; };
const setVal  = (id, v) => { const el = document.getElementById(id); if (el) el.value = v ?? ''; };
const setColor = (id, v) => { const el = document.getElementById(id); if (el) el.value = v ?? '#000000'; };

// ── Carica profilo ────────────────────────────────────────────────────────────

async function loadProfile() {
    try {
        const p = await apiFetch('/api/admin/profile');

        // Anagrafica
        setVal('pBusinessName',   p.businessName);
        setVal('pPhone',          p.phone);
        setVal('pEmail',          p.email);
        setVal('pWebsite',        p.websiteUrl);
        setVal('pWhatsapp',       p.whatsappNumber);

        // Fiscale
        setVal('pVat',            p.vatNumber);
        setVal('pFiscal',         p.fiscalCode);
        setVal('pRea',            p.reaNumber);

        // Localizzazione
        setVal('pAddress',        p.address);
        setVal('pCity',           p.city);
        setVal('pProvince',       p.province);
        setVal('pZip',            p.zipCode);
        setVal('pLat',            p.latitude != null ? p.latitude : '');
        setVal('pLng',            p.longitude != null ? p.longitude : '');

        // Branding
        setColor('pColorPrimary',   p.primaryColor   || '#1E3A5F');
        setColor('pColorSecondary', p.secondaryColor || '#2A4D73');
        setColor('pColorAccent',    p.accentColor    || '#D62828');
        if (typeof updateColorPreviews === 'function') updateColorPreviews();

        // Immagini
        setLogoPreview(p.logoUrl);
        setCoverPreview(p.coverImageUrl);

    } catch (err) {
        showToast(err.message || 'Errore caricamento profilo.', 'error');
    }
}

// ── Salvataggio sezioni ───────────────────────────────────────────────────────

async function saveSection(payload, btnId) {
    const btn = document.getElementById(btnId);
    if (btn) btn.disabled = true;
    try {
        await apiFetch('/api/admin/profile', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
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
        email:          getVal('pEmail'),
        websiteUrl:     getVal('pWebsite'),
        whatsappNumber: getVal('pWhatsapp'),
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
        primaryColor:   getVal('pColorPrimary'),
        secondaryColor: getVal('pColorSecondary'),
        accentColor:    getVal('pColorAccent'),
    }, 'saveBrandingBtn');
}

// ── Geocoding via Nominatim ───────────────────────────────────────────────────

async function geocode() {
    const address  = getVal('pAddress') || '';
    const city     = getVal('pCity')    || '';
    const province = getVal('pProvince')|| '';

    const query = [address, city, province, 'Italy'].filter(Boolean).join(', ');
    if (!city && !address) {
        showToast('Inserisci almeno la città per cercare le coordinate.', 'error');
        return;
    }

    const btn = document.getElementById('geocodeBtn');
    if (btn) btn.disabled = true;

    try {
        const res = await fetch(
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
        setVal('pLat', lat);
        setVal('pLng', lng);
        showToast(`Coordinate trovate: ${lat}, ${lng}`);
    } catch {
        showToast('Errore geocoding. Controlla la connessione.', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ── Immagini: logo ────────────────────────────────────────────────────────────

function setLogoPreview(url) {
    const img  = document.getElementById('logoImg');
    const ph   = document.getElementById('logoPlaceholder');
    const rBtn = document.getElementById('logoRemoveBtn');
    if (url) {
        img.src = url; img.style.display = ''; ph.style.display = 'none'; if (rBtn) rBtn.style.display = '';
    } else {
        img.src = ''; img.style.display = 'none'; ph.style.display = ''; if (rBtn) rBtn.style.display = 'none';
    }
}

async function uploadLogo(file) {
    const form = new FormData();
    form.append('file', file);
    try {
        const res = await apiFetch('/api/admin/profile/logo', { method: 'POST', body: form });
        setLogoPreview(res.url);
        showToast('Logo aggiornato.');
    } catch (err) { showToast(err.message || 'Errore upload logo.', 'error'); }
}

// ── Immagini: copertina ───────────────────────────────────────────────────────

function setCoverPreview(url) {
    const img  = document.getElementById('coverImg');
    const ph   = document.getElementById('coverPlaceholder');
    const rBtn = document.getElementById('coverRemoveBtn');
    if (url) {
        img.src = url; img.style.display = ''; ph.style.display = 'none'; if (rBtn) rBtn.style.display = '';
    } else {
        img.src = ''; img.style.display = 'none'; ph.style.display = ''; if (rBtn) rBtn.style.display = 'none';
    }
}

async function uploadCover(file) {
    const form = new FormData();
    form.append('file', file);
    try {
        const res = await apiFetch('/api/admin/profile/cover', { method: 'POST', body: form });
        setCoverPreview(res.url);
        showToast('Copertina aggiornata.');
    } catch (err) { showToast(err.message || 'Errore upload copertina.', 'error'); }
}

// ── Cambio password ───────────────────────────────────────────────────────────

function initPasswordForm() {
    const form   = document.getElementById('changePasswordForm');
    const errBox = document.getElementById('pwdError');
    form?.addEventListener('submit', async e => {
        e.preventDefault();
        errBox.classList.remove('show');
        const newPwd  = document.getElementById('newPassword').value;
        const confPwd = document.getElementById('confirmPassword').value;
        if (newPwd !== confPwd) {
            errBox.textContent = 'Le password non coincidono.';
            errBox.classList.add('show');
            return;
        }
        showToast('Funzionalità disponibile prossimamente.', 'error');
    });
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

    // Logo
    const logoInput = document.getElementById('logoInput');
    document.getElementById('logoUploadBtn')?.addEventListener('click', () => logoInput?.click());
    document.getElementById('logoArea')?.addEventListener('click', () => { if (!document.getElementById('logoImg')?.src) logoInput?.click(); });
    logoInput?.addEventListener('change', e => { const f = e.target.files?.[0]; if (f) { uploadLogo(f); e.target.value = ''; } });

    // Copertina
    const coverInput = document.getElementById('coverInput');
    document.getElementById('coverUploadBtn')?.addEventListener('click', () => coverInput?.click());
    coverInput?.addEventListener('change', e => { const f = e.target.files?.[0]; if (f) { uploadCover(f); e.target.value = ''; } });
});
