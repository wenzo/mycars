/* MyCars — Service Worker v1 */
'use strict';

// ── Push: mostra la notifica ──────────────────────────────────────────────────

self.addEventListener('push', event => {
    if (!event.data) return;

    let data;
    try   { data = event.data.json(); }
    catch { data = { title: 'MyCars', body: event.data.text() }; }

    const title = data.title || 'MyCars';
    const options = {
        body:               data.body    || '',
        icon:               data.icon    || '/assets/icons/icon-192.png',
        badge:              data.badge   || '/assets/icons/badge-72.png',
        image:              data.image   || undefined,
        data:               { url: data.url || '/' },
        tag:                'mycars-' + (data.tag || 'msg'),
        renotify:           true,
        requireInteraction: false,
        vibrate:            [200, 100, 200],
    };

    event.waitUntil(
        self.registration.showNotification(title, options)
    );
});

// ── Notification click: apre URL o mette a fuoco la finestra esistente ────────

self.addEventListener('notificationclick', event => {
    event.notification.close();

    const targetUrl = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(list => {
                // Cerca una finestra già aperta con quell'URL
                for (const client of list) {
                    const cu = new URL(client.url);
                    const tu = new URL(targetUrl, self.location.origin);
                    if (cu.pathname === tu.pathname && 'focus' in client) {
                        return client.focus();
                    }
                }
                // Altrimenti apri una nuova scheda
                if (clients.openWindow) return clients.openWindow(targetUrl);
            })
    );
});

// ── Push subscription change: ri-iscrizione automatica ───────────────────────
// Scatta quando il browser rinnova la subscription (raro ma possibile)

self.addEventListener('pushsubscriptionchange', event => {
    const opts = event.oldSubscription?.options ?? { userVisibleOnly: true };

    event.waitUntil(
        self.registration.pushManager.subscribe(opts)
            .then(sub => fetch('/api/push/subscribe', {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify(encodeSubscription(sub)),
            }))
            .catch(err => console.warn('[SW] pushsubscriptionchange fallito', err))
    );
});

// ── Helper ────────────────────────────────────────────────────────────────────

function encodeSubscription(sub) {
    const key  = sub.getKey('p256dh');
    const auth = sub.getKey('auth');
    return {
        endpoint: sub.endpoint,
        p256dh:   key  ? bufToBase64(key)  : '',
        auth:     auth ? bufToBase64(auth) : '',
    };
}

function bufToBase64(buffer) {
    return btoa(String.fromCharCode(...new Uint8Array(buffer)));
}
