/**
 * MyCars — Push Client v1
 *
 * Utilizzo minimo:
 *   await PushClient.init({ operatorId: 'uuid-operatore' });
 *   await PushClient.subscribe();
 *
 * Utilizzo con topic personalizzati:
 *   await PushClient.subscribe({ general: true, vehicles: true, news: false });
 *
 * Aggiorna i topic senza ri-iscriversi:
 *   await PushClient.updateTopics({ general: true, vehicles: false, news: true });
 *
 * Dis-iscrizione:
 *   await PushClient.unsubscribe();
 */

const PushClient = (() => {
    'use strict';

    const SW_URL      = '/sw.js';
    const SW_SCOPE    = '/';
    const TOPICS_KEY  = 'mycars_push_topics';
    const CONFIG_URL  = '/api/push/config';
    const SUB_URL     = '/api/push/subscribe';
    const UNSUB_URL   = '/api/push/unsubscribe';

    let _operatorId     = null;
    let _vapidPublicKey = null;
    let _swReg          = null;

    // ── Pubblico ──────────────────────────────────────────────────────────────

    /**
     * Inizializza il client.
     * @param {object} opts
     * @param {string}  opts.operatorId      UUID dell'operatore (obbligatorio)
     * @param {string} [opts.vapidPublicKey] Chiave VAPID pubblica (opzionale — viene scaricata automaticamente)
     */
    async function init({ operatorId, vapidPublicKey = null } = {}) {
        _operatorId = operatorId || null;

        if (vapidPublicKey) {
            _vapidPublicKey = vapidPublicKey;
        } else {
            try {
                const res  = await fetch(CONFIG_URL);
                const data = await res.json();
                _vapidPublicKey = data.vapidPublicKey || null;
            } catch (e) {
                console.warn('[PushClient] impossibile ottenere la VAPID public key:', e);
            }
        }
    }

    /** Ritorna true se il browser supporta le push notification. */
    function isSupported() {
        return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
    }

    /**
     * Stato corrente dell'iscrizione.
     * @returns {'unsupported'|'denied'|'subscribed'|'unsubscribed'}
     */
    async function getState() {
        if (!isSupported())                      return 'unsupported';
        if (Notification.permission === 'denied') return 'denied';

        try {
            const reg = await navigator.serviceWorker.getRegistration(SW_SCOPE);
            if (!reg) return 'unsubscribed';
            const sub = await reg.pushManager.getSubscription();
            return sub ? 'subscribed' : 'unsubscribed';
        } catch {
            return 'unsubscribed';
        }
    }

    /**
     * Chiede il permesso e iscrive il dispositivo.
     * @param {object} [topics] { general, vehicles, news } — default tutti true
     * @returns {PushSubscription}
     */
    async function subscribe(topics = null) {
        _assertSupported();
        _assertKey();

        topics = _normTopics(topics);

        // Registra il SW
        const reg = await _ensureSW();
        await navigator.serviceWorker.ready;

        // Richiedi permesso se necessario
        if (Notification.permission !== 'granted') {
            const perm = await Notification.requestPermission();
            if (perm !== 'granted') throw new Error('Permesso notifiche negato dall\'utente.');
        }

        // Ottieni o crea la subscription browser
        let sub = await reg.pushManager.getSubscription();
        if (!sub) {
            sub = await reg.pushManager.subscribe({
                userVisibleOnly:      true,
                applicationServerKey: _urlBase64ToUint8Array(_vapidPublicKey),
            });
        }

        // Invia al server
        await _postSubscription(sub, topics);
        _saveTopics(topics);

        return sub;
    }

    /** Aggiorna i topic senza richiedere nuovamente il permesso. */
    async function updateTopics(topics) {
        _assertSupported();

        const reg = await navigator.serviceWorker.getRegistration(SW_SCOPE);
        if (!reg) throw new Error('Nessun service worker registrato.');

        const sub = await reg.pushManager.getSubscription();
        if (!sub) throw new Error('Nessuna iscrizione push attiva. Iscriviti prima.');

        topics = _normTopics(topics);
        await _postSubscription(sub, topics);
        _saveTopics(topics);
    }

    /** Cancella l'iscrizione dal browser e dal server. */
    async function unsubscribe() {
        const reg = await navigator.serviceWorker.getRegistration(SW_SCOPE);
        if (!reg) return;

        const sub = await reg.pushManager.getSubscription();
        if (!sub) return;

        // Prima avvisa il server
        try {
            await fetch(UNSUB_URL, {
                method:  'DELETE',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify({ endpoint: sub.endpoint }),
            });
        } catch (e) {
            console.warn('[PushClient] errore comunicazione unsubscribe al server:', e);
        }

        await sub.unsubscribe();
        localStorage.removeItem(TOPICS_KEY);
    }

    /** Legge i topic salvati localmente (default: tutti attivi). */
    function getStoredTopics() {
        try {
            const raw = localStorage.getItem(TOPICS_KEY);
            return raw ? JSON.parse(raw) : _defaultTopics();
        } catch {
            return _defaultTopics();
        }
    }

    // ── Privato ───────────────────────────────────────────────────────────────

    function _defaultTopics() {
        return { general: true, vehicles: true, news: true };
    }

    function _normTopics(t) {
        const def = _defaultTopics();
        if (!t) return def;
        return {
            general:  t.general  !== undefined ? !!t.general  : def.general,
            vehicles: t.vehicles !== undefined ? !!t.vehicles : def.vehicles,
            news:     t.news     !== undefined ? !!t.news     : def.news,
        };
    }

    function _saveTopics(t) {
        localStorage.setItem(TOPICS_KEY, JSON.stringify(t));
    }

    function _assertSupported() {
        if (!isSupported()) throw new Error('Push notifications non supportate in questo browser.');
    }

    function _assertKey() {
        if (!_vapidPublicKey) throw new Error('VAPID public key non disponibile. Chiama init() prima.');
    }

    async function _ensureSW() {
        if (!_swReg) {
            _swReg = await navigator.serviceWorker.register(SW_URL, { scope: SW_SCOPE });
        }
        return _swReg;
    }

    async function _postSubscription(sub, topics) {
        const key  = sub.getKey('p256dh');
        const auth = sub.getKey('auth');

        const payload = {
            endpoint:      sub.endpoint,
            p256dh:        key  ? _bufToBase64(key)  : '',
            auth:          auth ? _bufToBase64(auth) : '',
            operatorId:    _operatorId,
            topicGeneral:  topics.general,
            topicVehicles: topics.vehicles,
            topicNews:     topics.news,
            deviceType:    /Mobi|Android|iPhone|iPad/i.test(navigator.userAgent) ? 'mobile' : 'web',
        };

        const res = await fetch(SUB_URL, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(payload),
        });

        if (!res.ok) {
            const body = await res.json().catch(() => ({}));
            throw new Error(body.message || `Errore server: ${res.status}`);
        }
    }

    /** Converte ArrayBuffer → Base64 standard */
    function _bufToBase64(buffer) {
        return btoa(String.fromCharCode(...new Uint8Array(buffer)));
    }

    /** Converte Base64url (chiave VAPID) → Uint8Array per pushManager.subscribe */
    function _urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
        const base64  = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const raw     = atob(base64);
        return Uint8Array.from([...raw].map(c => c.charCodeAt(0)));
    }

    // ── API pubblica ──────────────────────────────────────────────────────────

    return { init, isSupported, getState, subscribe, unsubscribe, updateTopics, getStoredTopics };
})();
