/* EasyCars — Service Worker */
'use strict';

self.addEventListener('push', event => {
  let data = { title: 'EasyCars', body: '' };
  try {
    if (event.data) data = { ...data, ...event.data.json() };
  } catch {
    if (event.data) data.body = event.data.text();
  }

  event.waitUntil(
    self.registration.showNotification(data.title, {
      body:               data.body,
      icon:               data.icon  || '/favicon.png',
      badge:              data.badge || '/favicon.png',
      data:               { url: data.url || '/' },
      tag:                'easycars-' + (data.tag || 'msg'),
      renotify:           true,
      requireInteraction: false,
      vibrate:            [200, 100, 200],
    })
  );
});

self.addEventListener('notificationclick', event => {
  event.notification.close();
  const url = event.notification.data?.url || '/';
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(list => {
      for (const c of list) {
        if (new URL(c.url).pathname === new URL(url, self.location.origin).pathname && 'focus' in c)
          return c.focus();
      }
      if (clients.openWindow) return clients.openWindow(url);
    })
  );
});

self.addEventListener('pushsubscriptionchange', event => {
  const opts = event.oldSubscription?.options ?? { userVisibleOnly: true };
  event.waitUntil(
    self.registration.pushManager.subscribe(opts)
      .then(sub => fetch('/api/push/subscribe', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({
          endpoint: sub.endpoint,
          p256dh:   bufToBase64(sub.getKey('p256dh')),
          auth:     bufToBase64(sub.getKey('auth')),
        }),
      }))
      .catch(() => {})
  );
});

function bufToBase64(buf) {
  return buf ? btoa(String.fromCharCode(...new Uint8Array(buf))) : '';
}
