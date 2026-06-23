<template>
  <ion-page>
    <ion-content :fullscreen="true">
      <div style="padding: 58px 0 0">

        <div style="padding: 0 20px 14px; border-bottom: 1px solid var(--mc-border)">
          <div style="font-family:var(--mc-font-heading);font-size:22px;font-weight:800;color:var(--mc-text)">
            Impostazioni
          </div>
        </div>

        <div style="padding: 14px 16px; display: flex; flex-direction: column; gap: 8px;">

          <!-- Push banner (solo web, non su app nativa) -->
          <div v-if="!pushEnabled && !isNative" class="push-banner">
            <div class="push-banner-icon">
              <ion-icon :icon="notificationsOutline" />
            </div>
            <div style="flex:1">
              <div style="font-family:var(--mc-font-heading);font-size:13px;font-weight:700;color:#fff;margin-bottom:4px">
                Abilita le notifiche push
              </div>
              <div style="font-size:11.5px;color:rgba(255,255,255,.65);line-height:1.4;margin-bottom:10px">
                Ricevi aggiornamenti su nuovi veicoli, promozioni e le tue richieste di test drive.
              </div>
              <button class="push-allow-btn" @click="requestPush">Consenti notifiche</button>
            </div>
          </div>

          <!-- Concessionaria attiva -->
          <div class="section-label">Concessionaria attiva</div>
          <div class="mc-section" style="padding:0">
            <div v-if="op.profile" class="dealer-chip">
              <div class="dealer-chip-logo">
                <img
                  v-if="op.profile.logoUrl && !logoFailed"
                  :src="op.resolveUrl(op.profile.logoUrl)"
                  alt="logo"
                  @error="logoFailed = true"
                />
                <ion-icon v-else :icon="carOutline" style="color:#fff;font-size:20px" />
              </div>
              <div style="flex:1">
                <div style="font-family:var(--mc-font-heading);font-size:14px;font-weight:700;color:var(--mc-text);margin-bottom:3px">
                  {{ op.profile.businessName }}
                </div>
                <span class="dealer-code-chip">
                  <ion-icon :icon="qrCodeOutline" style="font-size:9px" />
                  {{ op.profile.publicCode }}
                </span>
              </div>
              <button class="chip-remove" @click="confirmDisconnect">
                <ion-icon :icon="closeOutline" />
              </button>
            </div>
          </div>

          <!-- Aggiungi / connetti -->
          <div class="section-label">{{ op.profile ? 'Cambia concessionaria' : 'Collegati a una concessionaria' }}</div>
          <div class="mc-section">
            <div style="font-size:12px;color:var(--mc-text-mid);font-weight:500;margin-bottom:9px">
              Inserisci il codice fornito dal tuo venditore, oppure scansiona il QR code
            </div>
            <div style="display:flex;gap:8px;margin-bottom:9px">
              <input
                v-model="codeInput"
                class="code-input"
                type="text"
                placeholder="es. MARIO24"
                maxlength="32"
                @keyup.enter="connect"
              />
              <button class="qr-btn">
                <ion-icon :icon="qrCodeOutline" />
              </button>
            </div>
            <button class="btn-primary" :disabled="connecting" @click="connect">
              <ion-icon v-if="!connecting" :icon="sendOutline" />
              <ion-spinner v-else name="crescent" style="width:18px;height:18px" />
              Collegati alla concessionaria
            </button>
            <div v-if="connectError" style="color:var(--mc-red);font-size:12px;margin-top:8px">
              {{ connectError }}
            </div>
          </div>

          <!-- Notifiche -->
          <div class="section-label">Notifiche</div>
          <div class="mc-section" style="padding:0">
            <div class="srow" @click="togglePush">
              <div class="srow-icon navy"><ion-icon :icon="notificationsOutline" /></div>
              <div class="srow-info">
                <div class="srow-label">Notifiche push</div>
                <div class="srow-sub">Nuovi veicoli, promozioni e aggiornamenti richieste</div>
              </div>
              <button class="mc-toggle" :class="{ on: pushEnabled }" @click.stop="togglePush" />
            </div>
            <div v-if="pushError" style="padding:0 16px 12px;font-size:12px;color:var(--mc-text-light)">
              {{ pushError }}
            </div>
          </div>

          <!-- Privacy -->
          <div class="section-label">Privacy &amp; Legale</div>
          <div class="mc-section" style="padding:0">
            <div class="srow" @click="openUrl(op.profile?.websiteUrl)">
              <div class="srow-icon navy"><ion-icon :icon="shieldOutline" /></div>
              <div class="srow-info"><div class="srow-label">Privacy Policy</div></div>
              <ion-icon :icon="chevronForwardOutline" style="color:var(--mc-border)" />
            </div>
            <div class="srow" style="border-bottom:none">
              <div class="srow-icon red"><ion-icon :icon="trashOutline" /></div>
              <div class="srow-info">
                <div class="srow-label" style="color:var(--mc-red)">Cancella dati dispositivo</div>
                <div class="srow-sub">Rimuove preferiti e cronologia locale</div>
              </div>
            </div>
          </div>

          <!-- App info -->
          <div class="section-label">App</div>
          <div class="mc-section" style="padding:0">
            <div class="srow" style="border-bottom:none;cursor:default">
              <div class="srow-icon gray"><ion-icon :icon="informationCircleOutline" /></div>
              <div class="srow-info">
                <div class="srow-label">Versione app</div>
                <div class="srow-sub">1.0.0 (build 1)</div>
              </div>
            </div>
          </div>

          <div style="text-align:center;font-size:12px;color:var(--mc-text-light);padding:10px 0 4px">
            MyCars · Multi-tenant dealer app
          </div>

        </div>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { IonPage, IonContent, IonIcon, IonSpinner } from '@ionic/vue'
import {
  carOutline, notificationsOutline, qrCodeOutline,
  sendOutline, shieldOutline, trashOutline,
  chevronForwardOutline, closeOutline, informationCircleOutline,
} from 'ionicons/icons'
import { Capacitor } from '@capacitor/core'
import { useOperatorStore } from '@/stores/operator'

const op           = useOperatorStore()
const codeInput    = ref('')
const connecting   = ref(false)
const connectError = ref('')
const pushEnabled  = ref(false)
const pushError    = ref('')
const logoFailed   = ref(false)

const isNative = Capacitor.isNativePlatform()
const SW_PATH = '/sw.js'

function urlBase64ToUint8Array(base64: string) {
  const pad = '='.repeat((4 - base64.length % 4) % 4)
  const b64 = (base64 + pad).replace(/-/g, '+').replace(/_/g, '/')
  const raw = atob(b64)
  return Uint8Array.from([...raw].map(c => c.charCodeAt(0)))
}

async function connect() {
  if (!codeInput.value.trim()) return
  connecting.value   = true
  connectError.value = ''
  logoFailed.value   = false
  try {
    await op.connectByCode(codeInput.value.trim())
    codeInput.value = ''
  } catch (e: any) {
    connectError.value = e?.message ?? 'Errore di connessione'
  } finally {
    connecting.value = false
  }
}

function confirmDisconnect() {
  if (confirm('Disconnettere la concessionaria?')) {
    logoFailed.value = false
    op.disconnect()
  }
}

async function checkPushStatus() {
  if (isNative) {
    pushEnabled.value = localStorage.getItem('pushOptIn') === 'true'
    return
  }
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) return
  try {
    const reg = await navigator.serviceWorker.getRegistration(SW_PATH)
    if (!reg) return
    const sub = await reg.pushManager.getSubscription()
    pushEnabled.value = sub !== null
  } catch { /* browser senza supporto o permesso negato */ }
}

function togglePush() {
  if (pushEnabled.value) {
    disablePush()
  } else {
    requestPush()
  }
}

async function disablePush() {
  pushError.value = ''

  if (isNative) {
    localStorage.setItem('pushOptIn', 'false')
    pushEnabled.value = false
    return
  }

  try {
    const reg = await navigator.serviceWorker.getRegistration(SW_PATH)
    if (reg) {
      const sub = await reg.pushManager.getSubscription()
      if (sub) {
        // Rimuove la subscription dal backend prima di de-registrarla localmente
        await fetch(`${op.apiBase}/api/push/unsubscribe`, {
          method:  'DELETE',
          headers: { 'Content-Type': 'application/json' },
          body:    JSON.stringify({ endpoint: sub.endpoint }),
        }).catch(() => { /* non blocca il flusso */ })
        await sub.unsubscribe()
      }
    }
    pushEnabled.value = false
  } catch (e: any) {
    pushError.value = e?.message ?? 'Errore durante la disattivazione.'
  }
}

async function requestPush() {
  pushError.value = ''

  if (isNative) {
    localStorage.setItem('pushOptIn', 'true')
    pushEnabled.value = true
    pushError.value = 'Verifica che le notifiche siano attive in Impostazioni → App → EasyCars → Notifiche.'
    return
  }

  if (!('Notification' in window)) {
    pushError.value = 'Le notifiche non sono supportate su questo dispositivo.'
    return
  }

  // Web: service worker + VAPID
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    pushError.value = 'Le notifiche push non sono supportate da questo browser.'
    return
  }
  try {
    const permission = await Notification.requestPermission()
    if (permission !== 'granted') {
      pushError.value = 'Permesso notifiche negato. Abilitalo nelle impostazioni del browser.'
      return
    }
    // register() installa il SW se non presente; ready attende che sia attivo
    await navigator.serviceWorker.register(SW_PATH)
    const reg  = await navigator.serviceWorker.ready
    const base = op.apiBase
    const cfgRes = await fetch(`${base}/api/push/config`)
    if (!cfgRes.ok) throw new Error('Configurazione push non disponibile.')
    const { vapidPublicKey } = await cfgRes.json()
    const sub = await reg.pushManager.subscribe({
      userVisibleOnly:      true,
      applicationServerKey: urlBase64ToUint8Array(vapidPublicKey),
    })
    const json    = sub.toJSON() as { endpoint: string; keys: { p256dh: string; auth: string } }
    const saveRes = await fetch(`${base}/api/push/subscribe`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        endpoint:      json.endpoint,
        p256dh:        json.keys.p256dh,
        auth:          json.keys.auth,
        operatorId:    op.profile?.operatorId,
        deviceType:    'web',
        topicGeneral:  true,
        topicVehicles: true,
        topicNews:     true,
      }),
    })
    if (!saveRes.ok) throw new Error('Errore registrazione subscription sul server.')
    pushEnabled.value = true
  } catch (e: any) {
    pushError.value = e?.message ?? 'Errore durante la registrazione.'
  }
}

function openUrl(url: string | null | undefined) {
  if (url) window.open(url, '_blank')
}

onMounted(checkPushStatus)
</script>

<style scoped>
.push-banner {
  background: linear-gradient(135deg, var(--dealer-primary), var(--mc-navy-mid));
  border-radius: var(--mc-r); padding: 14px 16px;
  display: flex; align-items: flex-start; gap: 12px;
  box-shadow: var(--mc-shadow);
}
.push-banner-icon {
  width: 38px; height: 38px; background: rgba(255,255,255,.15);
  border-radius: 10px; display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.push-banner-icon ion-icon { color: #fff; font-size: 18px; }
.push-allow-btn {
  height: 32px; padding: 0 14px; background: #fff; border: none;
  border-radius: 20px; cursor: pointer;
  font-family: var(--mc-font-heading); font-size: 12px; font-weight: 700;
  color: var(--dealer-primary);
}

.section-label {
  font-family: var(--mc-font-heading); font-size: 11px; font-weight: 700;
  color: var(--mc-text-light); text-transform: uppercase; letter-spacing: .08em;
  padding: 4px 4px 2px;
}
.dealer-chip {
  display: flex; align-items: center; gap: 12px; padding: 13px 16px;
}
.dealer-chip-logo {
  width: 40px; height: 40px; background: var(--dealer-secondary);
  border-radius: 10px; display: flex; align-items: center; justify-content: center;
  overflow: hidden; flex-shrink: 0;
}
.dealer-chip-logo img { width: 100%; height: 100%; object-fit: cover; }
.dealer-code-chip {
  display: inline-flex; align-items: center; gap: 4px;
  font-family: var(--mc-font-heading); font-size: 11px; font-weight: 700;
  color: var(--mc-blue); background: var(--mc-surface2); padding: 2px 8px; border-radius: 6px;
}
.chip-remove {
  width: 28px; height: 28px; border-radius: 50%;
  background: var(--mc-surface); border: none; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
}
.chip-remove ion-icon { color: var(--mc-text-light); font-size: 14px; }

.code-input {
  flex: 1; height: 44px; border: 2px solid var(--mc-border);
  border-radius: var(--mc-r-sm); padding: 0 14px;
  font-family: var(--mc-font-heading); font-size: 15px; font-weight: 700;
  color: var(--mc-text); background: #fff;
  letter-spacing: .1em; text-transform: uppercase; outline: none;
}
.code-input:focus { border-color: var(--dealer-primary); }
.qr-btn {
  width: 44px; height: 44px; border-radius: var(--mc-r-sm);
  background: var(--dealer-primary); border: none; cursor: pointer;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.qr-btn ion-icon { color: #fff; font-size: 20px; }

.srow {
  display: flex; align-items: center; padding: 13px 16px; gap: 12px;
  border-bottom: 1px solid var(--mc-surface); cursor: pointer;
}
.srow-icon {
  width: 32px; height: 32px; border-radius: 9px;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.srow-icon ion-icon { font-size: 16px; }
.srow-icon.navy { background: #E8EEF5; } .srow-icon.navy ion-icon { color: var(--mc-navy); }
.srow-icon.red  { background: #FFF0F0; } .srow-icon.red  ion-icon { color: var(--mc-red); }
.srow-icon.gray { background: var(--mc-surface2); } .srow-icon.gray ion-icon { color: var(--mc-text-light); }
.srow-info { flex: 1; }
.srow-label { font-size: 14px; font-weight: 500; color: var(--mc-text); }
.srow-sub   { font-size: 11px; color: var(--mc-text-light); margin-top: 1px; line-height: 1.35; }
</style>
