<template>
  <ion-page>
    <div class="dealer-header">
      <div class="dealer-bar">
        <div class="dealer-logo-wrap">
          <div class="dealer-logo-icon">
            <img v-if="op.profile?.logoUrl" :src="op.resolveUrl(op.profile.logoUrl)" alt="logo" />
            <ion-icon v-else :icon="carOutline" style="color:#fff;font-size:20px" />
          </div>
          <div>
            <div class="dealer-name">{{ op.profile?.businessName ?? 'MyCars' }}</div>
            <div class="dealer-sub">Vetrina veicoli</div>
          </div>
        </div>
        <div style="display:flex;gap:8px">
          <button class="header-icon-btn" @click="goToProfilo">
            <ion-icon :icon="personCircleOutline" />
          </button>
        </div>
      </div>

      <!-- Tipo veicolo switcher -->
      <div class="tipo-switcher">
        <button
          v-for="t in tipi" :key="t.value"
          class="tipo-btn"
          :class="activeType === t.value ? 'active' : 'inactive'"
          @click="selectType(t.value)"
        >
          {{ t.label }}
        </button>
      </div>

      <!-- Barra ricerca conversazionale AI -->
      <div class="search-row">
        <div class="search-ai-box" :class="{ 'is-recording': isRecording }">

          <!-- Icona sinistra: spinner se caricamento, altrimenti sparkle AI -->
          <div class="search-left-icon">
            <ion-spinner v-if="store.loading && searchText.trim()" name="crescent" class="search-spinner" />
            <span v-else class="ai-spark">✦</span>
          </div>

          <input
            ref="inputEl"
            v-model="searchText"
            :placeholder="isRecording ? 'In ascolto…' : 'Cerca in linguaggio naturale…'"
            @keydown.enter="submitSearch"
            class="search-input"
          />

          <!-- Pulsante clear (se c'è testo) -->
          <button v-if="searchText && !isRecording" class="search-icon-btn" @click="clearSearch">
            <ion-icon :icon="closeOutline" />
          </button>

          <!-- Pulsante microfono -->
          <button
            v-if="speechSupported"
            class="search-icon-btn mic-btn"
            :class="{ recording: isRecording }"
            @click="toggleRecording"
            :aria-label="isRecording ? 'Interrompi dettatura' : 'Cerca con la voce'"
          >
            <ion-icon :icon="isRecording ? stopCircleOutline : micOutline" />
            <span v-if="isRecording" class="mic-pulse" />
          </button>
        </div>

        <button class="search-adv-btn" @click="$router.push('/tabs/ricerca')">
          <ion-icon :icon="funnelOutline" />
          Filtra
        </button>
      </div>

      <!-- Etichetta AI (solo quando campo vuoto) -->
      <div v-if="!searchText && !isRecording" class="ai-hint">
        <span>Powered by AI · prova: "SUV diesel sotto 15.000€ per famiglia"</span>
      </div>
    </div>

    <!-- Stats bar -->
    <div class="stats-bar">
      <div class="stats-count">
        <span>{{ displayCount }}</span> veicoli
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

    <ion-content style="--padding-bottom: calc(var(--ion-tab-bar-height, 56px) + var(--ion-safe-area-bottom, 0px))">
      <div
        class="veicoli-scroll"
        :class="layout === 'grid' ? 'grid-2col' : 'list-col'"
      >
        <VehicleCard
          v-for="v in store.items"
          :key="v.id"
          :vehicle="v"
          :layout="layout"
          @click="$router.push(`/tabs/veicolo/${v.id}`)"
        />

        <div v-if="store.loading" class="loading-row">
          <ion-spinner name="crescent" />
        </div>

        <ion-infinite-scroll @ionInfinite="onInfinite">
          <ion-infinite-scroll-content />
        </ion-infinite-scroll>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import {
  IonPage, IonContent, IonIcon, IonSpinner,
  IonInfiniteScroll, IonInfiniteScrollContent,
} from '@ionic/vue'
import {
  carOutline, funnelOutline,
  gridOutline, listOutline, personCircleOutline,
  closeOutline, micOutline, stopCircleOutline,
} from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'
import { useVehicleStore } from '@/stores/vehicles'
import VehicleCard from '@/components/VehicleCard.vue'

const router = useRouter()
const op     = useOperatorStore()
const store  = useVehicleStore()

function goToProfilo() { router.push('/tabs/profilo') }

const searchText  = ref('')
const layout      = ref<'grid' | 'list'>('grid')
const activeType  = ref('autovettura')
const inputEl     = ref<HTMLInputElement | null>(null)

const tipi = [
  { value: 'autovettura', label: 'Auto' },
  { value: 'motoveicolo', label: 'Moto' },
  { value: 'autocarro',   label: 'Truck' },
  { value: 'autocaravan', label: 'Caravan' },
]

const displayCount = computed(() => store.totalCount || store.items.length)

function selectType(type: string) {
  activeType.value = type
  searchText.value = ''
  stopRecording()
  store.applyFilters({ vehicleType: type })
}

function submitSearch() {
  const q = searchText.value.trim()
  if (q) store.applyFilters({ vehicleType: activeType.value, q })
  else   store.applyFilters({ vehicleType: activeType.value })
}

function clearSearch() {
  searchText.value = ''
  store.applyFilters({ vehicleType: activeType.value })
}

// Debounce su digitazione manuale
let searchTimer: ReturnType<typeof setTimeout>
watch(searchText, (val) => {
  clearTimeout(searchTimer)
  const q = val.trim()
  searchTimer = setTimeout(() => {
    if (q) store.applyFilters({ vehicleType: activeType.value, q })
    else   store.applyFilters({ vehicleType: activeType.value })
  }, 500)
})

async function onInfinite(ev: CustomEvent) {
  await store.fetchNextPage()
  ;(ev.target as HTMLIonInfiniteScrollElement).complete()
}

onMounted(() => {
  if (!store.initialized) store.applyFilters({ vehicleType: activeType.value })
  initSpeech()
})

// ── Dettatura vocale ─────────────────────────────────────────────────────────

type SpeechRecognitionType = typeof window extends { SpeechRecognition: infer T } ? T : any

const speechSupported = ref(false)
const isRecording     = ref(false)
let recognition: SpeechRecognitionType | null = null

function initSpeech() {
  const SR = (window as any).SpeechRecognition ?? (window as any).webkitSpeechRecognition
  if (!SR) return
  speechSupported.value = true

  recognition = new SR()
  recognition.lang           = 'it-IT'
  recognition.continuous     = false
  recognition.interimResults = false
  recognition.maxAlternatives = 1

  recognition.onresult = (event: any) => {
    const transcript = event.results[0]?.[0]?.transcript ?? ''
    if (transcript) {
      searchText.value = transcript
      submitSearch()
    }
    isRecording.value = false
  }

  recognition.onerror = () => { isRecording.value = false }
  recognition.onend   = () => { isRecording.value = false }
}

function toggleRecording() {
  if (!recognition) return
  if (isRecording.value) {
    stopRecording()
  } else {
    startRecording()
  }
}

function startRecording() {
  if (!recognition) return
  searchText.value  = ''
  isRecording.value = true
  try { recognition.start() } catch { isRecording.value = false }
}

function stopRecording() {
  if (!recognition || !isRecording.value) return
  try { recognition.stop() } catch { /* già ferma */ }
  isRecording.value = false
}

onUnmounted(() => stopRecording())
</script>

<style scoped>
.tipo-switcher {
  display: flex; gap: 7px; margin-bottom: 12px;
}
.tipo-btn {
  flex: 1; height: 38px; border: none; cursor: pointer;
  border-radius: var(--mc-r-sm);
  font-family: var(--mc-font-heading);
  font-size: 10.5px; font-weight: 600;
}
.tipo-btn.active   { background: rgba(255,255,255,.92); color: var(--dealer-primary); font-weight: 700; }
.tipo-btn.inactive { background: rgba(255,255,255,.12); color: rgba(255,255,255,.65); }

/* ── Barra ricerca AI ───────────────────────────────────────────────────── */
.search-row { display: flex; gap: 8px; align-items: center; }

.search-ai-box {
  flex: 1; height: 44px;
  display: flex; align-items: center; gap: 6px; padding: 0 10px 0 12px;
  border-radius: var(--mc-r-sm);
  background: rgba(255,255,255,.13);
  /* Bordo con sfumatura per richiamare il tema AI */
  border: 1.5px solid rgba(255,255,255,.22);
  transition: border-color .25s, box-shadow .25s;
  position: relative;
}
/* Glow leggero quando l'utente interagisce (focus-within) */
.search-ai-box:focus-within {
  border-color: rgba(255,255,255,.55);
  box-shadow: 0 0 0 3px rgba(255,255,255,.08);
}
/* Stato di registrazione: bordo pulsante colorato */
.search-ai-box.is-recording {
  border-color: rgba(255, 90, 90, .8);
  box-shadow: 0 0 0 3px rgba(255, 80, 80, .18);
}

/* Icona sinistra */
.search-left-icon {
  display: flex; align-items: center; flex-shrink: 0; width: 18px; justify-content: center;
}
.ai-spark {
  font-size: 14px; color: rgba(255,255,255,.75); line-height: 1;
  /* Piccola animazione shimmer per indicare AI */
  animation: spark-fade 3s ease-in-out infinite;
}
@keyframes spark-fade {
  0%, 100% { opacity: .6; }
  50%       { opacity: 1; }
}
.search-spinner { width: 16px; height: 16px; color: rgba(255,255,255,.7); }

/* Input */
.search-input {
  flex: 1; background: transparent; border: none; outline: none;
  color: #fff; font-size: 13.5px; min-width: 0;
}
.search-input::placeholder { color: rgba(255,255,255,.48); }

/* Pulsanti icona (clear / mic) */
.search-icon-btn {
  background: transparent; border: none; padding: 0; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  width: 28px; height: 28px; flex-shrink: 0;
  color: rgba(255,255,255,.55); font-size: 17px;
  border-radius: 50%;
  transition: color .15s, background .15s;
  position: relative;
}
.search-icon-btn:active { color: #fff; background: rgba(255,255,255,.12); }

/* Microfono attivo */
.mic-btn.recording {
  color: #ff6b6b;
}
/* Alone pulsante attorno al microfono durante registrazione */
.mic-pulse {
  position: absolute; inset: -4px;
  border-radius: 50%;
  border: 1.5px solid rgba(255, 90, 90, .6);
  animation: mic-ring 1s ease-out infinite;
  pointer-events: none;
}
@keyframes mic-ring {
  0%   { transform: scale(1);   opacity: .8; }
  100% { transform: scale(1.6); opacity: 0; }
}

/* Pulsante Filtra */
.search-adv-btn {
  height: 44px; padding: 0 14px;
  background: var(--dealer-secondary); border: none;
  border-radius: var(--mc-r-sm); cursor: pointer;
  font-family: var(--mc-font-heading); font-size: 12px; font-weight: 600;
  color: #fff; display: flex; align-items: center; gap: 5px; flex-shrink: 0;
}

/* Hint AI sotto la barra */
.ai-hint {
  margin-top: 7px;
  font-size: 10.5px; color: rgba(255,255,255,.38);
  text-align: center; font-style: italic;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}

/* ── Stats bar ───────────────────────────────────────────────────────────── */
.stats-bar {
  display: flex; align-items: center; justify-content: space-between;
  padding: 10px 18px 10px; background: var(--mc-surface);
}
.stats-count { font-family: var(--mc-font-heading); font-size: 13px; font-weight: 600; color: var(--mc-text-mid); }
.stats-count span { color: var(--dealer-primary); font-weight: 700; }
.layout-toggle { display: flex; gap: 4px; }
.ltbtn {
  width: 30px; height: 30px; border: none; cursor: pointer;
  border-radius: 8px; background: transparent;
  display: flex; align-items: center; justify-content: center;
}
.ltbtn.active { background: var(--dealer-primary); }
.ltbtn.active ion-icon { color: #fff; }
.ltbtn ion-icon { color: var(--mc-text-light); font-size: 16px; }

/* ── Lista veicoli ───────────────────────────────────────────────────────── */
.veicoli-scroll { padding: 10px 12px 12px; }
.grid-2col { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
.list-col   { display: flex; flex-direction: column; gap: 10px; }
.loading-row { display: flex; justify-content: center; padding: 20px; }
</style>
