'use strict';

let selectedVehicleId = null;

// ── Stats + VAPID key ─────────────────────────────────────────────────────────

async function loadStats() {
    try {
        const stats = await apiFetch('/api/admin/push/stats');

        document.getElementById('statTotal').textContent =
            stats.total_subscribers ?? '0';

        const vapidEl = document.getElementById('statVapid');
        if (vapidEl) {
            vapidEl.textContent  = stats.vapid_enabled ? 'Attivo' : 'Non configurato';
            vapidEl.style.color  = stats.vapid_enabled ? 'var(--green)' : 'var(--amber)';
        }

        const keyEl = document.getElementById('vapidPublicKeyDisplay');
        if (keyEl && stats.vapid_public_key) {
            keyEl.textContent = stats.vapid_public_key;
        }
    } catch {
        // silently fail
    }
}

// ── Vehicle search by targa ───────────────────────────────────────────────────

async function searchByTarga() {
    const targa = document.getElementById('pushTarga')?.value.trim().toUpperCase();
    if (!targa) return;

    const resultDiv  = document.getElementById('targaResult');
    const nameEl     = document.getElementById('targaVehicleName');
    const codeEl     = document.getElementById('targaVehicleCode');
    const hiddenId   = document.getElementById('pushVehicleId');

    resultDiv.style.display = 'none';
    selectedVehicleId       = null;
    if (hiddenId) hiddenId.value = '';

    try {
        const v = await apiFetch(`/api/admin/vehicles/by-targa?value=${encodeURIComponent(targa)}`);
        const label = [v.model, v.version].filter(Boolean).join(' ');
        nameEl.textContent  = label;
        codeEl.textContent  = `[${v.internalCode ?? v.targa}]`;
        resultDiv.style.display = 'block';
        selectedVehicleId       = v.id;
        if (hiddenId) hiddenId.value = v.id;
    } catch {
        nameEl.textContent  = 'Nessun veicolo trovato con questa targa.';
        codeEl.textContent  = '';
        resultDiv.style.display = 'block';
    }
}

// ── Live preview ──────────────────────────────────────────────────────────────

function updatePreview() {
    const title = document.getElementById('pushTitle')?.value || 'Titolo notifica';
    const body  = document.getElementById('pushBody')?.value  || 'Testo del messaggio';
    document.getElementById('previewTitle').textContent = title;
    document.getElementById('previewBody').textContent  = body;
}

// ── Target radio toggle ───────────────────────────────────────────────────────

function toggleTargetPicker() {
    const target = document.querySelector('input[name="target"]:checked')?.value;
    const vehicleGroup = document.getElementById('vehiclePickerGroup');
    const emailGroup   = document.getElementById('emailPickerGroup');
    if (vehicleGroup) vehicleGroup.style.display = target === 'vehicle' ? 'flex' : 'none';
    if (emailGroup)   emailGroup.style.display   = target === 'email'   ? 'block' : 'none';
}

// ── Send ──────────────────────────────────────────────────────────────────────

async function sendPush(e) {
    e.preventDefault();

    const errBox  = document.getElementById('pushError');
    const btn     = document.getElementById('pushSendBtn');
    const result  = document.getElementById('resultCard');
    errBox.classList.remove('show');

    const title     = document.getElementById('pushTitle')?.value.trim() || '';
    const body      = document.getElementById('pushBody')?.value.trim()  || '';
    const imageUrl  = document.getElementById('pushImage')?.value.trim() || null;
    const target    = document.querySelector('input[name="target"]:checked')?.value ?? 'all';
    const vehicleId = target === 'vehicle' ? (selectedVehicleId ?? null) : null;
    const userEmail = target === 'email'   ? (document.getElementById('pushUserEmail')?.value.trim() || '') : null;

    if (target === 'vehicle' && !vehicleId) {
        errBox.textContent = 'Cerca e seleziona un veicolo per targa prima di inviare.';
        errBox.classList.add('show');
        return;
    }
    if (target === 'email' && !userEmail) {
        errBox.textContent = 'Inserisci l\'email dell\'utente destinatario.';
        errBox.classList.add('show');
        return;
    }

    btn.disabled = true;
    btn.textContent = 'Invio…';

    try {
        const data = await apiFetch('/api/admin/push/send', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ title, body, imageUrl, target, vehicleId, userEmail }),
        });

        result.style.display = 'block';
        document.getElementById('resultText').textContent =
            data.message ?? `Notifica inviata a ${data.sent} / ${data.total} dispositivi.`;
        showToast(`Inviata a ${data.sent} dispositivi`, 'success');

    } catch {
        errBox.textContent = 'Errore durante l\'invio. VAPID configurato?';
        errBox.classList.add('show');
    } finally {
        btn.disabled    = false;
        btn.textContent = 'Invia notifica';
    }
}

// ── Init ──────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadStats();

    document.getElementById('pushTitle')?.addEventListener('input', updatePreview);
    document.getElementById('pushBody')?.addEventListener('input',  updatePreview);

    document.querySelectorAll('input[name="target"]')
        .forEach(r => r.addEventListener('change', toggleTargetPicker));

    document.getElementById('searchTargaBtn')
        ?.addEventListener('click', searchByTarga);

    document.getElementById('pushTarga')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); searchByTarga(); }
    });

    document.getElementById('pushForm')?.addEventListener('submit', sendPush);
});
