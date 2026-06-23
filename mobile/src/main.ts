import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'

import { IonicVue } from '@ionic/vue'
import { Capacitor } from '@capacitor/core'

import '@ionic/vue/css/core.css'
import '@ionic/vue/css/normalize.css'
import '@ionic/vue/css/structure.css'
import '@ionic/vue/css/typography.css'
import '@ionic/vue/css/padding.css'
import '@ionic/vue/css/flex-utils.css'
import '@ionic/vue/css/text-alignment.css'

import './theme/variables.css'
import './theme/app.css'

const pinia = createPinia()
const app   = createApp(App)
  .use(IonicVue, { mode: 'ios' })
  .use(pinia)
  .use(router)

// Carica il profilo dealer salvato prima di montare l'app
import { useOperatorStore } from './stores/operator'
router.isReady().then(() => {
  const opStore = useOperatorStore()
  opStore.load()
  app.mount('#app')
  opStore.refreshProfile()
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/sw.js').catch(() => {})
    navigator.serviceWorker.addEventListener('message', e => {
      if (e.data?.type === 'sw-push')
        console.warn('[APP] Push ricevuto dal SW:', e.data.title)
    })
  }

  // Listener foreground sempre attivo su native, indipendentemente da pushOptIn
  if (Capacitor.isNativePlatform()) {
    import('@capacitor/push-notifications').then(({ PushNotifications }) => {
      PushNotifications.addListener('pushNotificationReceived', notification => {
        console.warn('[FCM] Notifica in foreground:', notification.title, notification.body)
      })
    }).catch(() => {})
  }

  // Native Android: aggiorna il token FCM ad ogni avvio se l'utente aveva dato il consenso
  if (Capacitor.isNativePlatform() && localStorage.getItem('pushOptIn') === 'true') {
    import('@capacitor/push-notifications').then(async ({ PushNotifications }) => {

      await PushNotifications.createChannel({
        id:          'default',
        name:        'Notifiche',
        description: 'Nuovi veicoli, promozioni e aggiornamenti richieste',
        importance:  5,
        visibility:  1,
        vibration:   true,
      })

      const regHandle = await PushNotifications.addListener('registration', async token => {
        await regHandle.remove()
        localStorage.setItem('fcmToken', token.value)
        await fetch(`${opStore.apiBase}/api/push/subscribe`, {
          method:  'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            endpoint:      token.value,
            p256dh:        '',
            auth:          '',
            operatorId:    opStore.profile?.operatorId,
            deviceType:    'android',
            topicGeneral:  true,
            topicVehicles: true,
            topicNews:     true,
          }),
        }).catch(() => {})
      })

      const errHandle = await PushNotifications.addListener('registrationError', async err => {
        await errHandle.remove()
        console.error('[FCM] Refresh token fallito:', err.error)
      })

      await PushNotifications.register()
    }).catch(() => {})
  }
})
