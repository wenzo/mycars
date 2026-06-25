<template>
  <ion-page>

    <!-- ── Intestazione dealer (identica a VetrinaPage, colore secondario) ─ -->
    <div class="dealer-header nol-header">
      <div class="dealer-bar">
        <div class="dealer-logo-wrap">
          <div class="dealer-logo-icon">
            <img v-if="op.profile?.logoUrl" :src="op.resolveUrl(op.profile.logoUrl) ?? undefined" alt="logo" />
            <ion-icon v-else :icon="keyOutline" style="color:#fff;font-size:20px" />
          </div>
          <div>
            <div class="dealer-name">{{ op.profile?.businessName ?? 'MyCars' }}</div>
            <div class="dealer-sub">Noleggio veicoli</div>
          </div>
        </div>
        <div style="display:flex;gap:8px">
          <button class="header-icon-btn" @click="$router.push('/tabs/profilo')">
            <ion-icon :icon="personCircleOutline" />
          </button>
        </div>
      </div>

      <!-- Tipo veicolo switcher -->
      <div class="tipo-switcher">
        <button
          v-for="t in tipi"
          :key="t.value"
          class="tipo-btn"
          :class="activeType === t.value ? 'active' : 'inactive'"
          @click="selectType(t.value)"
        >{{ t.label }}</button>
      </div>

      <!-- Searchbar -->
      <div class="search-row">
        <div class="search-box">
          <ion-icon :icon="searchOutline" />
          <input v-model="searchText" placeholder="Cerca marca, modello…" @focus="activeTab = 'esplora'" />
        </div>
      </div>
    </div>

    <!-- ── Contenu to scrollabile ────────────────────────────────────────── -->
    <ion-content style="--padding-bottom: calc(var(--ion-tab-bar-height, 56px) + var(--ion-safe-area-bottom, 0px))">

      <!-- ═══ ESPLORA ═══════════════════════════════════════════════════════ -->
      <template v-if="activeTab === 'esplora'">
        <ion-refresher slot="fixed" @ionRefresh="refreshEsplora($event)">
          <ion-refresher-content />
        </ion-refresher>

        <div class="stats-bar">
          <div class="stats-count">
            <span>{{ filteredItems.length }}</span> veicoli disponibili
          </div>
          <div class="layout-toggle">
            <button class="ltbtn" :class="{ active: layout === 'grid' }" @click="layout = 'grid'">
              <ion-icon :icon="gridOutline" />
            </button>
            <button class="ltbtn" :class="{ active: layout === 'list' }" @click="layout = 'list'">
              <ion-icon :icon="listOutline" />
            </button>
          </div>
        </div>

        <div v-if="loadingEsplora" class="loading-row">
          <ion-spinner name="crescent" />
        </div>

        <div
          v-else-if="filteredItems.length > 0"
          class="veicoli-scroll"
          :class="layout === 'grid' ? 'grid-2col' : 'list-col'"
        >
          <VehicleCard
            v-for="v in filteredItems"
            :key="v.id"
            :vehicle="v"
            :layout="layout"
            :rental-mode="true"
            @click="$router.push(`/tabs/veicolo/${v.id}?from=noleggio`)"
          />
        </div>

        <div v-else class="empty-state">
          <ion-icon :icon="keyOutline" class="empty-icon" />
          <p>Nessun veicolo disponibile a noleggio al momento.</p>
        </div>
      </template>

      <!-- ═══ I MIEI NOLEGGI ════════════════════════════════════════════════ -->
      <template v-else-if="activeTab === 'noleggi'">

        <!-- Cerca per codice -->
        <div class="track-search-wrap">
          <div class="track-search-label">Hai già un codice? Cerca la tua richiesta</div>
          <div class="track-search-row">
            <input
              v-model="trackInput"
              class="track-input"
              placeholder="es. NLG-A3X9K2"
              @input="trackInput = trackInput.toUpperCase()"
              @keydown.enter="searchByCode"
            />
            <button class="track-btn" :disabled="trackSearching" @click="searchByCode">
              <ion-spinner v-if="trackSearching" name="crescent" style="width:16px;height:16px" />
              <span v-else>Cerca</span>
            </button>
          </div>
          <div v-if="trackError" class="track-error">{{ trackError }}</div>
          <div v-if="trackResult" class="track-result">
            <div class="tr-code">{{ trackResult.trackingCode }}</div>
            <div class="tr-status" :class="trackStatusClass(trackResult.status)">{{ trackResult.statusLabel }}</div>
            <div v-if="trackResult.preferredDate" class="tr-date">Data: {{ fmtDate(trackResult.preferredDate) }}</div>
          </div>
        </div>

        <div v-if="clientStore.requests.length === 0" class="empty-state" style="padding-top:32px">
          <ion-icon :icon="keyOutline" class="empty-icon" />
          <p>Non hai ancora inviato richieste di noleggio.</p>
          <button class="link-btn" @click="activeTab = 'esplora'">Esplora i veicoli →</button>
        </div>

        <div v-else class="requests-list">
          <div class="section-label">Richieste inviate</div>
          <div
            v-for="r in clientStore.requests"
            :key="r.localId"
            class="req-card"
            @click="openRequest(r)"
          >
            <div class="req-icon-wrap">
              <ion-icon :icon="keyOutline" class="req-icon" />
            </div>
            <div class="req-body">
              <div class="req-name">{{ r.vehicleName }}</div>
              <div class="req-dates">{{ fmtDate(r.startDate) }} → {{ fmtDate(r.endDate) }}</div>
              <div class="req-formula">{{ r.formulaLabel }}</div>
              <div v-if="r.trackingCode" class="req-tracking">
                <span class="req-tracking-code">{{ r.trackingCode }}</span>
                <span v-if="liveStatuses[r.trackingCode]" class="req-live-badge" :class="trackStatusClass(liveStatuses[r.trackingCode].status)">
                  {{ liveStatuses[r.trackingCode].statusLabel }}
                </span>
              </div>
            </div>
            <div class="req-right">
              <span
                v-if="r.trackingCode && liveStatuses[r.trackingCode]"
                class="req-badge"
                :class="trackStatusClass(liveStatuses[r.trackingCode].status)"
              >{{ liveStatuses[r.trackingCode].statusLabel }}</span>
              <span v-else class="req-badge badge-pending">In attesa</span>
              <ion-icon :icon="chevronForwardOutline" class="req-chevron" />
            </div>
          </div>

          <div class="req-note">
            <ion-icon :icon="informationCircleOutline" />
            Il concessionario ti contatterà per confermare disponibilità e condizioni.
          </div>
        </div>
      </template>

      <!-- ═══ COMUNICAZIONI ═════════════════════════════════════════════════ -->
      <template v-else-if="activeTab === 'comunicazioni'">
        <ion-refresher slot="fixed" @ionRefresh="refreshComm($event)">
          <ion-refresher-content />
        </ion-refresher>

        <div v-if="loadingComm" class="loading-row">
          <ion-spinner name="crescent" />
        </div>

        <div v-else-if="filteredComm.length === 0" class="empty-state">
          <ion-icon :icon="notificationsOutline" class="empty-icon" />
          <p>Nessuna comunicazione disponibile.</p>
        </div>

        <div v-else class="comm-list">
          <div
            v-for="item in filteredComm"
            :key="item.id"
            class="comm-card"
            @click="$router.push(`/tabs/news/${item.id}`)"
          >
            <div class="comm-icon-wrap" :class="commIconClass(item.newsType)">
              <ion-icon :icon="commIcon(item.newsType)" />
            </div>
            <div class="comm-body">
              <div class="comm-title">{{ item.title }}</div>
              <div class="comm-excerpt">{{ item.excerpt }}</div>
              <div class="comm-meta">
                <span class="comm-type-badge" :class="commIconClass(item.newsType)">
                  {{ commTypeLabel(item.newsType) }}
                </span>
                <span class="comm-date">{{ fmtDate(item.publishedAt?.slice(0,10)) }}</span>
              </div>
            </div>
          </div>
        </div>
      </template>


    </ion-content>

    <!-- ── Modal dettaglio richiesta ──────────────────────────────────────── -->
    <ion-modal :is-open="!!selectedRequest" @didDismiss="selectedRequest = null">
      <ion-header>
        <ion-toolbar>
          <ion-title>Dettaglio richiesta</ion-title>
          <ion-buttons slot="end">
            <ion-button @click="selectedRequest = null">Chiudi</ion-button>
          </ion-buttons>
        </ion-toolbar>
      </ion-header>
      <ion-content v-if="selectedRequest">
        <div class="req-detail-wrap">

          <!-- Status bar -->
          <div class="req-detail-status">
            <span
              v-if="selectedRequest.trackingCode && liveStatuses[selectedRequest.trackingCode]"
              class="badge-status-lg"
              :class="trackStatusClass(liveStatuses[selectedRequest.trackingCode].status)"
            >{{ liveStatuses[selectedRequest.trackingCode].statusLabel }}</span>
            <span v-else class="badge-pending-lg">In attesa di conferma</span>
          </div>

          <!-- Tracking code -->
          <div v-if="selectedRequest.trackingCode" class="detail-tracking-box">
            <div class="dtb-label">Codice richiesta</div>
            <div class="dtb-code">{{ selectedRequest.trackingCode }}</div>
          </div>

          <!-- Veicolo -->
          <div class="req-detail-card">
            <div class="rdc-icon">
              <ion-icon :icon="keyOutline" />
            </div>
            <div class="rdc-info">
              <div class="rdc-name">{{ selectedRequest.vehicleName }}</div>
              <div class="rdc-sub">{{ selectedRequest.formulaLabel }}</div>
            </div>
          </div>

          <!-- Dettagli -->
          <div class="req-detail-rows">
            <div class="rdr-row">
              <ion-icon :icon="calendarOutline" />
              <span class="rdr-key">Ritiro</span>
              <span class="rdr-val">{{ fmtDate(selectedRequest.startDate) }}</span>
            </div>
            <div class="rdr-row">
              <ion-icon :icon="calendarOutline" />
              <span class="rdr-key">Riconsegna</span>
              <span class="rdr-val">{{ fmtDate(selectedRequest.endDate) }}</span>
            </div>
            <div v-if="selectedRequest.days" class="rdr-row">
              <ion-icon :icon="timeOutline" />
              <span class="rdr-key">Durata</span>
              <span class="rdr-val">{{ selectedRequest.days }} giorni</span>
            </div>
            <div v-if="selectedRequest.estimatedCost" class="rdr-row">
              <ion-icon :icon="cardOutline" />
              <span class="rdr-key">Costo stimato</span>
              <span class="rdr-val">€ {{ fmtPrice(selectedRequest.estimatedCost) }}</span>
            </div>
          </div>

          <!-- Opzioni -->
          <div v-if="selectedRequest.options?.length" class="req-detail-opts">
            <div class="rdo-title">Servizi aggiuntivi richiesti</div>
            <div v-for="opt in selectedRequest.options" :key="opt.key" class="rdo-item">
              <ion-icon :icon="checkmarkCircleOutline" />
              <span>{{ opt.label }}</span>
              <span class="rdo-price">
                {{ opt.pricePerDay ? `+€${opt.pricePerDay}/g` : opt.priceFlat ? `+€${opt.priceFlat}` : '' }}
              </span>
            </div>
          </div>

          <!-- Note -->
          <div v-if="selectedRequest.notes" class="req-detail-notes">
            <div class="rdn-title">Note</div>
            <p>{{ selectedRequest.notes }}</p>
          </div>

          <div class="req-detail-info">
            <ion-icon :icon="informationCircleOutline" />
            Il concessionario ti contatterà per confermare disponibilità e condizioni finali.
          </div>

          <!-- Contatta dealer -->
          <div class="req-detail-actions">
            <ion-button v-if="op.profile?.phone" expand="block" fill="outline" :href="'tel:' + op.profile.phone">
              <ion-icon :icon="callOutline" slot="start" />
              Chiama il concessionario
            </ion-button>
            <ion-button v-if="op.profile?.whatsappNumber" expand="block" color="success"
              :href="'https://wa.me/' + op.profile.whatsappNumber">
              <ion-icon :icon="logoWhatsapp" slot="start" />
              WhatsApp
            </ion-button>
          </div>
        </div>
      </ion-content>
    </ion-modal>

  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import {
  IonPage, IonContent, IonIcon, IonSpinner, IonButton, IonButtons,
  IonRefresher, IonRefresherContent, IonModal, IonHeader, IonToolbar, IonTitle,
  onIonViewWillEnter,
} from '@ionic/vue'
import {
  keyOutline, searchOutline, gridOutline, listOutline, notificationsOutline,
  chevronForwardOutline, informationCircleOutline, calendarOutline, timeOutline,
  cardOutline, checkmarkCircleOutline, callOutline, mailOutline, locationOutline,
  businessOutline, logoWhatsapp, megaphoneOutline, newspaperOutline,
  personCircleOutline, closeOutline as closeOutlineIcon,
} from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'
import { useRentalClientStore, type LocalRentalRequest } from '@/stores/rentalClient'
import { useFavoritesStore } from '@/stores/favorites'
import VehicleCard from '@/components/VehicleCard.vue'

const route       = useRoute()
const op          = useOperatorStore()
const clientStore = useRentalClientStore()
const favs        = useFavoritesStore()

// ── Tab navigation ────────────────────────────────────────────────────────────
type TabId = 'esplora' | 'noleggi' | 'comunicazioni'
const activeTab = ref<TabId>('esplora')


const pendingCount = computed(() => clientStore.requests.length)

const tabs = computed(() => [
  { id: 'noleggi'       as TabId, label: 'Noleggi', icon: keyOutline,           badge: pendingCount.value },
  { id: 'comunicazioni' as TabId, label: 'News',    icon: notificationsOutline, badge: unreadComm.value },
])

const badgeCount = computed(() => pendingCount.value + unreadComm.value)

// ── Esplora ───────────────────────────────────────────────────────────────────
const items       = ref<any[]>([])
const loadingEsplora = ref(false)
const searchText  = ref('')
const layout      = ref<'grid' | 'list'>('grid')
const activeType  = ref('autovettura')

const tipi = [
  { value: 'autovettura', label: 'Auto' },
  { value: 'autocarro',   label: 'Truck' },
  { value: 'autocaravan', label: 'Camper' },
  { value: 'motoveicolo', label: 'Moto' },
]

const filteredItems = computed(() => {
  const q = searchText.value.trim().toLowerCase()
  if (!q) return items.value
  return items.value.filter((v: any) =>
    v.brandName?.toLowerCase().includes(q) ||
    v.model?.toLowerCase().includes(q) ||
    v.version?.toLowerCase().includes(q)
  )
})

async function loadEsplora() {
  if (!op.slug) return
  loadingEsplora.value = true
  try {
    const params = new URLSearchParams({
      forRental:   'true',
      vehicleType: activeType.value,
      pageSize:    '50',
    })
    const res = await fetch(`${op.apiBase}/api/public/${op.slug}/vehicles?${params}`)
    if (!res.ok) throw new Error()
    const data = await res.json()
    items.value = data.items ?? []
  } catch {
    items.value = []
  } finally {
    loadingEsplora.value = false
  }
}

function selectType(type: string) {
  activeTab.value  = 'esplora'
  activeType.value = type
  searchText.value = ''
  loadEsplora()
}

async function refreshEsplora(ev: CustomEvent) {
  await loadEsplora()
  ;(ev.target as HTMLIonRefresherElement).complete()
}

// ── I miei noleggi ────────────────────────────────────────────────────────────
const selectedRequest = ref<LocalRentalRequest | null>(null)
const liveStatuses = ref<Record<string, { status: string; statusLabel: string }>>({})

const trackInput    = ref('')
const trackSearching = ref(false)
const trackError    = ref('')
const trackResult   = ref<{ trackingCode: string; status: string; statusLabel: string; preferredDate?: string } | null>(null)

function openRequest(r: LocalRentalRequest) {
  selectedRequest.value = r
}

function trackStatusClass(status: string) {
  if (status === 'new')       return 'badge-pending'
  if (status === 'contacted') return 'badge-contacted'
  if (status === 'closed')    return 'badge-closed'
  return 'badge-lost'
}

async function loadLiveStatuses() {
  const codes = clientStore.requests
    .map(r => r.trackingCode)
    .filter((c): c is string => !!c)
  if (!codes.length || !op.slug) return

  const results = await Promise.allSettled(
    codes.map(code =>
      fetch(`${op.apiBase}/api/public/${op.slug}/rental-requests/${code}`)
        .then(r => r.ok ? r.json() : null)
    )
  )
  const updated: Record<string, { status: string; statusLabel: string }> = {}
  results.forEach((r, i) => {
    if (r.status === 'fulfilled' && r.value) {
      updated[codes[i]] = { status: r.value.status, statusLabel: r.value.statusLabel }
    }
  })
  liveStatuses.value = updated
}

async function searchByCode() {
  const code = trackInput.value.trim()
  if (!code) return
  trackError.value  = ''
  trackResult.value = null
  trackSearching.value = true
  try {
    const res = await fetch(`${op.apiBase}/api/public/${op.slug}/rental-requests/${encodeURIComponent(code)}`)
    if (res.status === 404) { trackError.value = 'Codice non trovato. Verifica e riprova.'; return }
    if (!res.ok) throw new Error()
    trackResult.value = await res.json()
  } catch {
    trackError.value = 'Errore di rete. Riprova.'
  } finally {
    trackSearching.value = false
  }
}

// ── Comunicazioni ─────────────────────────────────────────────────────────────
const newsItems   = ref<any[]>([])
const loadingComm = ref(false)
const filtroComm  = ref('tutti')
const unreadComm  = ref(0)

const tipiComm = [
  { value: 'tutti',      label: 'Tutte' },
  { value: 'promo',      label: 'Promo' },
  { value: 'news',       label: 'News' },
  { value: 'avviso',     label: 'Avvisi' },
]

const filteredComm = computed(() => {
  if (filtroComm.value === 'tutti') return newsItems.value
  return newsItems.value.filter((n: any) => n.newsType === filtroComm.value)
})

function commIcon(type: string) {
  if (type === 'promo') return megaphoneOutline
  return newspaperOutline
}

function commIconClass(type: string) {
  if (type === 'promo') return 'comm-promo'
  if (type === 'avviso') return 'comm-avviso'
  return 'comm-news'
}

function commTypeLabel(type: string) {
  const map: Record<string, string> = {
    promo: 'Promozione', news: 'News', avviso: 'Avviso',
  }
  return map[type] ?? 'Comunicazione'
}

async function loadComm() {
  if (!op.slug) return
  loadingComm.value = true
  try {
    const res = await fetch(`${op.apiBase}/api/public/${op.slug}/news?pageSize=30`)
    if (!res.ok) throw new Error()
    const data = await res.json()
    newsItems.value = data.items ?? []
    unreadComm.value = Math.min(newsItems.value.length, 0) // badge only for actual unread
  } catch {
    newsItems.value = []
  } finally {
    loadingComm.value = false
  }
}

async function refreshComm(ev: CustomEvent) {
  await loadComm()
  ;(ev.target as HTMLIonRefresherElement).complete()
}

// ── Utils ─────────────────────────────────────────────────────────────────────
function fmtDate(d: string | undefined) {
  if (!d) return '—'
  const [y, m, dd] = d.split('-')
  if (!y || !m || !dd) return d
  return `${dd}/${m}/${y}`
}

function fmtPrice(v: number) {
  return new Intl.NumberFormat('it-IT').format(v)
}

onMounted(async () => {
  await Promise.all([loadEsplora(), loadComm()])
  loadLiveStatuses()
})

onIonViewWillEnter(() => {
  const tab = route.query.tab as string | undefined
  if (tab === 'noleggi')       activeTab.value = 'noleggi'
  else if (tab === 'comunicazioni') activeTab.value = 'comunicazioni'
})
</script>

<style scoped>
/* ── Override colore header (dealer-secondary invece di dealer-primary) ──── */
.nol-header { background: var(--dealer-secondary); }

/* ── Tipo switcher + search (identici a VetrinaPage) ────────────────────── */
.tipo-switcher { display: flex; gap: 7px; margin-bottom: 14px; }
.tipo-btn {
  flex: 1; height: 38px; border: none; cursor: pointer;
  border-radius: var(--mc-r-sm);
  font-family: var(--mc-font-heading); font-size: 10.5px; font-weight: 600;
}
.tipo-btn.active   { background: rgba(255,255,255,.92); color: var(--dealer-secondary); font-weight: 700; }
.tipo-btn.inactive { background: rgba(255,255,255,.12); color: rgba(255,255,255,.65); }
.search-row { display: flex; gap: 8px; align-items: center; }
.search-box {
  flex: 1; height: 42px;
  background: rgba(255,255,255,.14); border: 1.5px solid rgba(255,255,255,.18);
  border-radius: var(--mc-r-sm);
  display: flex; align-items: center; gap: 8px; padding: 0 12px;
}
.search-box ion-icon { color: rgba(255,255,255,.6); font-size: 16px; flex-shrink: 0; }
.search-box input {
  flex: 1; background: transparent; border: none; outline: none;
  color: #fff; font-size: 13.5px;
}
.search-box input::placeholder { color: rgba(255,255,255,.5); }
.header-icon-btn.active { background: rgba(255,255,255,.28); }

/* ── Stats bar ───────────────────────────────────────────────────────────── */
.stats-bar {
  display: flex; align-items: center; justify-content: space-between;
  padding: 9px 16px 6px; background: var(--mc-surface);
}
.stats-count { font-size: 12.5px; font-weight: 600; color: var(--mc-text-mid); }
.stats-count span { color: var(--dealer-secondary); font-weight: 700; }
.layout-toggle { display: flex; gap: 4px; }
.ltbtn {
  width: 28px; height: 28px; border: none; cursor: pointer;
  border-radius: 7px; background: transparent;
  display: flex; align-items: center; justify-content: center;
}
.ltbtn.active { background: var(--dealer-secondary); }
.ltbtn.active ion-icon { color: #fff; }
.ltbtn ion-icon { color: var(--mc-text-light); font-size: 15px; }

.veicoli-scroll { padding: 10px 12px; }
.grid-2col { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
.list-col  { display: flex; flex-direction: column; gap: 10px; }

.loading-row { display: flex; justify-content: center; padding: 48px; }
.empty-state {
  display: flex; flex-direction: column; align-items: center;
  padding: 60px 24px; color: var(--mc-text-light); text-align: center;
}
.empty-icon { font-size: 48px; margin-bottom: 12px; opacity: 0.35; }
.empty-state p { font-size: 13.5px; line-height: 1.5; max-width: 240px; }
.link-btn {
  background: none; border: none; color: var(--dealer-secondary);
  font-size: 13.5px; font-weight: 700; cursor: pointer; margin-top: 8px;
}

/* ── Cerca per codice ────────────────────────────────────────────────────── */
.track-search-wrap {
  margin: 14px 16px 4px; background: #fff; border-radius: var(--mc-r);
  border: 1px solid var(--mc-border); padding: 14px 16px;
}
.track-search-label { font-size: 12px; font-weight: 600; color: var(--mc-text-mid); margin-bottom: 8px; }
.track-search-row { display: flex; gap: 8px; }
.track-input {
  flex: 1; height: 42px; border: 2px solid var(--mc-border); border-radius: var(--mc-r-sm);
  padding: 0 12px; font-family: monospace; font-size: 15px; font-weight: 600;
  color: var(--mc-text); background: #fff; outline: none; text-transform: uppercase;
}
.track-input:focus { border-color: var(--dealer-secondary); }
.track-btn {
  height: 42px; padding: 0 16px; background: var(--dealer-secondary); color: #fff;
  border: none; border-radius: var(--mc-r-sm);
  font-family: var(--mc-font-heading); font-size: 13px; font-weight: 700;
  cursor: pointer; display: flex; align-items: center; gap: 6px;
}
.track-btn:disabled { opacity: .6; cursor: not-allowed; }
.track-error { font-size: 12px; color: var(--mc-red); margin-top: 6px; }
.track-result {
  margin-top: 10px; padding: 10px 12px; background: #f0f4ff;
  border-radius: 8px; display: flex; align-items: center; gap: 10px; flex-wrap: wrap;
}
.tr-code { font-family: monospace; font-size: 14px; font-weight: 700; color: #1E3A5F; flex: 1; }
.tr-status { font-size: 11px; font-weight: 700; padding: 3px 10px; border-radius: 20px; }
.tr-date { font-size: 11px; color: var(--mc-text-mid); width: 100%; }

/* ── I miei noleggi ──────────────────────────────────────────────────────── */
.requests-list { padding: 14px 16px; }
.section-label {
  font-size: 10.5px; font-weight: 700; color: var(--mc-text-light);
  text-transform: uppercase; letter-spacing: .07em; margin-bottom: 10px;
}
.req-card {
  background: #fff; border-radius: var(--mc-r); border: 1px solid var(--mc-border);
  padding: 12px 14px; display: flex; align-items: center; gap: 12px;
  margin-bottom: 10px; cursor: pointer;
}
.req-icon-wrap {
  width: 44px; height: 44px; border-radius: 12px;
  background: color-mix(in srgb, var(--dealer-secondary) 12%, transparent);
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.req-icon { font-size: 20px; color: var(--dealer-secondary); }
.req-body { flex: 1; min-width: 0; }
.req-name   { font-family: var(--mc-font-heading); font-size: 14px; font-weight: 700; color: var(--mc-text); }
.req-dates  { font-size: 11.5px; color: var(--mc-text-mid); margin-top: 2px; }
.req-formula { font-size: 11px; color: var(--mc-text-light); margin-top: 1px; }
.req-tracking { display: flex; align-items: center; gap: 6px; margin-top: 3px; }
.req-tracking-code { font-family: monospace; font-size: 11px; font-weight: 700; color: var(--dealer-secondary); }
.req-live-badge { font-size: 9px; font-weight: 700; padding: 1px 6px; border-radius: 10px; }
.req-right { display: flex; flex-direction: column; align-items: flex-end; gap: 6px; flex-shrink: 0; }
.req-badge { font-size: 9.5px; font-weight: 700; padding: 3px 8px; border-radius: 20px; }
.badge-pending   { background: #fef3c7; color: #92400e; }
.badge-contacted { background: #dbeafe; color: #1d4ed8; }
.badge-closed    { background: #d1fae5; color: #065f46; }
.badge-lost      { background: #fee2e2; color: #991b1b; }
.req-chevron { font-size: 16px; color: var(--mc-border); }
.req-note {
  display: flex; align-items: flex-start; gap: 8px;
  font-size: 12px; color: var(--mc-text-light); line-height: 1.5;
  padding: 12px 14px; background: #f8fafc; border-radius: 10px; margin-top: 6px;
}
.req-note ion-icon { font-size: 15px; flex-shrink: 0; margin-top: 1px; }

/* ── Comunicazioni ───────────────────────────────────────────────────────── */
.comm-list { padding: 12px 16px; display: flex; flex-direction: column; gap: 10px; }
.comm-card {
  background: #fff; border-radius: var(--mc-r); border: 1px solid var(--mc-border);
  padding: 12px 14px; display: flex; gap: 12px; cursor: pointer;
}
.comm-icon-wrap {
  width: 40px; height: 40px; border-radius: 10px; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
}
.comm-news   { background: #eff6ff; color: #2563eb; }
.comm-promo  { background: #fef3c7; color: #d97706; }
.comm-avviso { background: #fef2f2; color: #dc2626; }
.comm-icon-wrap ion-icon { font-size: 18px; }
.comm-body { flex: 1; min-width: 0; }
.comm-title   { font-size: 13.5px; font-weight: 700; color: var(--mc-text); line-height: 1.3; }
.comm-excerpt {
  font-size: 12px; color: var(--mc-text-mid); margin-top: 3px; line-height: 1.4;
  display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;
}
.comm-meta { display: flex; align-items: center; gap: 8px; margin-top: 6px; }
.comm-type-badge {
  font-size: 9.5px; font-weight: 700; padding: 2px 7px; border-radius: 8px;
}
.comm-date { font-size: 10.5px; color: var(--mc-text-light); }

/* ── Modal dettaglio richiesta ───────────────────────────────────────────── */
.req-detail-wrap { padding: 16px; }
.req-detail-status { text-align: center; padding: 12px 0 8px; }
.badge-pending-lg {
  background: #fef3c7; color: #92400e;
  font-size: 12px; font-weight: 700; padding: 6px 14px; border-radius: 20px;
}
.badge-status-lg { font-size: 12px; font-weight: 700; padding: 6px 14px; border-radius: 20px; }
.detail-tracking-box {
  background: #f0f4ff; border: 2px dashed #2E75B6; border-radius: 10px;
  padding: 12px 16px; margin: 6px 0 10px; text-align: center;
}
.dtb-label { font-size: 10px; font-weight: 700; color: var(--dealer-secondary); text-transform: uppercase; letter-spacing: .07em; margin-bottom: 4px; }
.dtb-code { font-family: monospace; font-size: 22px; font-weight: 700; letter-spacing: 3px; color: #1E3A5F; }
.req-detail-card {
  background: #fff; border-radius: var(--mc-r); border: 1px solid var(--mc-border);
  padding: 14px; display: flex; align-items: center; gap: 12px; margin: 10px 0;
}
.rdc-icon {
  width: 48px; height: 48px; border-radius: 12px;
  background: color-mix(in srgb, var(--dealer-secondary) 12%, transparent);
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.rdc-icon ion-icon { font-size: 22px; color: var(--dealer-secondary); }
.rdc-name { font-family: var(--mc-font-heading); font-size: 15px; font-weight: 700; color: var(--mc-text); }
.rdc-sub  { font-size: 12px; color: var(--mc-text-mid); margin-top: 2px; }
.req-detail-rows {
  background: #fff; border-radius: var(--mc-r); border: 1px solid var(--mc-border); overflow: hidden;
}
.rdr-row {
  display: flex; align-items: center; gap: 10px;
  padding: 11px 14px; border-bottom: 1px solid var(--mc-surface);
  font-size: 13px;
}
.rdr-row:last-child { border-bottom: none; }
.rdr-row ion-icon { font-size: 15px; color: var(--mc-text-light); flex-shrink: 0; }
.rdr-key  { color: var(--mc-text-mid); flex: 1; }
.rdr-val  { color: var(--mc-text); font-weight: 600; }
.req-detail-opts {
  background: #fff; border-radius: var(--mc-r); border: 1px solid var(--mc-border);
  padding: 12px 14px; margin-top: 10px;
}
.rdo-title { font-size: 11px; font-weight: 700; color: var(--mc-text-mid); text-transform: uppercase; letter-spacing:.05em; margin-bottom: 8px; }
.rdo-item {
  display: flex; align-items: center; gap: 8px;
  font-size: 13px; color: var(--mc-text); padding: 4px 0;
}
.rdo-item ion-icon { color: var(--ion-color-success); font-size: 15px; flex-shrink: 0; }
.rdo-price { color: var(--mc-text-mid); margin-left: auto; font-size: 12px; }
.req-detail-notes {
  background: #fff; border-radius: var(--mc-r); border: 1px solid var(--mc-border);
  padding: 12px 14px; margin-top: 10px; font-size: 13px; color: var(--mc-text-mid);
}
.rdn-title { font-weight: 700; color: var(--mc-text); margin-bottom: 6px; font-size: 12px; text-transform: uppercase; }
.req-detail-info {
  display: flex; align-items: flex-start; gap: 8px;
  font-size: 12px; color: var(--mc-text-light); line-height: 1.5;
  padding: 12px 14px; background: #f8fafc; border-radius: 10px; margin-top: 12px;
}
.req-detail-info ion-icon { font-size: 15px; flex-shrink: 0; margin-top: 1px; }
.req-detail-actions { display: flex; flex-direction: column; gap: 10px; margin-top: 16px; padding-bottom: 24px; }
</style>
