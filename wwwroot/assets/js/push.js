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
            vapidEl.textContent = stats.vapid_enabled ? 'Attivo' : 'Non configurato';
            vapidEl.style.color = stats.vapid_enabled ? 'var(--green)' : 'var(--amber)';
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

// ── Pianificazione toggle ─────────────────────────────────────────────────────

function toggleScheduleFields() {
    const isScheduled = document.getElementById('pushScheduled')?.checked;
    const fields = document.getElementById('pushScheduleFields');
    if (fields) fields.style.display = isScheduled ? '' : 'none';
    const btn = document.getElementById('pushSendBtn');
    if (btn) btn.textContent = isScheduled ? 'Pianifica notifica' : 'Invia notifica';
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

    // Pianificazione
    const isScheduled = document.getElementById('pushScheduled')?.checked;
    const dateVal     = document.getElementById('pushDate')?.value;
    const timeVal     = document.getElementById('pushTime')?.value || '09:00';
    let sendAt = null;
    if (isScheduled) {
        if (!dateVal) {
            errBox.textContent = 'Scegli la data di invio.';
            errBox.classList.add('show');
            return;
        }
        sendAt = new Date(`${dateVal}T${timeVal}:00`).toISOString();
        if (new Date(sendAt) <= new Date()) {
            errBox.textContent = 'La data pianificata deve essere futura.';
            errBox.classList.add('show');
            return;
        }
    }

    if (target === 'vehicle' && !vehicleId) {
        errBox.textContent = 'Cerca e seleziona un veicolo per targa prima di inviare.';
        errBox.classList.add('show');
        return;
    }
    if (target === 'email' && !userEmail) {
        errBox.textContent = "Inserisci l'email dell'utente destinatario.";
        errBox.classList.add('show');
        return;
    }

    const origLabel = btn.textContent;
    btn.disabled    = true;
    btn.textContent = isScheduled ? 'Pianificazione…' : 'Invio…';

    try {
        const data = await apiFetch('/api/admin/push/send', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ title, body, imageUrl, target, vehicleId, userEmail, sendAt }),
        });

        result.style.display = 'block';
        if (data.scheduled) {
            const dt = new Date(data.sendAt).toLocaleString('it-IT');
            document.getElementById('resultText').textContent = `✓ Notifica pianificata per ${dt}.`;
            showToast('Notifica pianificata.', 'success');
        } else {
            document.getElementById('resultText').textContent =
                data.message ?? `Notifica inviata a ${data.sent} / ${data.total} dispositivi.`;
            showToast(`Inviata a ${data.sent} dispositivi.`, 'success');
        }
        loadNotifications();

    } catch (err) {
        errBox.textContent = err.message || 'Errore durante l\'invio.';
        errBox.classList.add('show');
    } finally {
        btn.disabled    = false;
        btn.textContent = origLabel;
    }
}

// ── Storico notifiche ─────────────────────────────────────────────────────────

const TOPIC_LABEL = { general: 'Generale', news: 'News', vehicles: 'Veicoli' };

async function loadNotifications() {
    const tbody = document.getElementById('notifTableBody');
    if (!tbody) return;
    try {
        const items = await apiFetch('/api/admin/push/notifications?limit=50');
        if (!items.length) {
            tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;color:var(--text-muted);padding:2rem">Nessuna notifica</td></tr>';
            return;
        }
        tbody.innerHTML = items.map(n => {
            const dt     = new Date(n.scheduledAt).toLocaleString('it-IT');
            const topic  = TOPIC_LABEL[n.topic] ?? n.topic;
            let statusBadge;
            if (n.error) {
                statusBadge = `<span class="badge badge-lost" title="${escHtml(n.error)}">Errore</span>`;
            } else if (n.sentAt) {
                statusBadge = '<span class="badge badge-published">Inviata</span>';
            } else {
                statusBadge = '<span class="badge badge-new">Pianificata</span>';
            }
            const canDelete = !n.sentAt && !n.error;
            return `<tr>
                <td class="td-main">${escHtml(n.title)}</td>
                <td>${escHtml(topic)}</td>
                <td style="white-space:nowrap;font-size:13px">${dt}</td>
                <td>${statusBadge}</td>
                <td class="td-actions">
                    ${canDelete
                        ? `<button class="btn btn-outline btn-sm" data-del="${n.id}" style="color:var(--red);border-color:var(--red)">Annulla</button>`
                        : ''}
                </td>
            </tr>`;
        }).join('');
        tbody.querySelectorAll('[data-del]').forEach(btn =>
            btn.addEventListener('click', () => cancelNotification(btn.dataset.del)));
    } catch {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;color:var(--text-muted);padding:2rem">Errore caricamento</td></tr>';
    }
}

function escHtml(str) {
    return (str ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

async function cancelNotification(id) {
    if (!confirm('Annullare questa notifica pianificata?')) return;
    try {
        await apiFetch(`/api/admin/push/notifications/${id}`, { method: 'DELETE' });
        showToast('Notifica annullata.');
        loadNotifications();
    } catch (err) {
        showToast(err.message || 'Errore annullamento.', 'error');
    }
}

// ── Init ──────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadStats();
    loadNotifications();

    document.getElementById('pushTitle')?.addEventListener('input', updatePreview);
    document.getElementById('pushBody')?.addEventListener('input',  updatePreview);

    document.querySelectorAll('input[name="target"]')
        .forEach(r => r.addEventListener('change', toggleTargetPicker));

    document.querySelectorAll('input[name="pushWhen"]')
        .forEach(r => r.addEventListener('change', toggleScheduleFields));

    document.getElementById('searchTargaBtn')
        ?.addEventListener('click', searchByTarga);

    document.getElementById('pushTarga')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); searchByTarga(); }
    });

    document.getElementById('refreshNotifBtn')?.addEventListener('click', loadNotifications);

    document.getElementById('pushForm')?.addEventListener('submit', sendPush);
});
