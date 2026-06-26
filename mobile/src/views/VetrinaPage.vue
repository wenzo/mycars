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
        <button class="header-icon-btn" @click="goToProfilo">
          <ion-icon :icon="personCircleOutline" />
        </button>
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

      <!-- Trigger ricerca: tocca per aprire il modal -->
      <div class="search-row">
        <button class="search-trigger" :class="{ 'has-query': searchText }" @click="openModal">
          <ion-spinner v-if="store.loading && searchText" name="crescent" class="s-spinner" />
          <span v-else class="ai-spark">✦</span>
          <span class="trigger-label" :class="{ 'query-active': searchText }">
            {{ searchText || 'Cerca in linguaggio naturale…' }}
          </span>
          <ion-icon
            v-if="searchText"
            :icon="closeOutline"
            class="trigger-clear"
            @click.stop="clearSearch"
          />
        </button>
        <button class="search-adv-btn" @click="$router.push('/tabs/ricerca')">
          <ion-icon :icon="funnelOutline" />
          Filtra
        </button>
      </div>

      <div v-if="!searchText" class="ai-hint">
        Powered by AI · tocca la barra per cercare in linguaggio naturale
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
      <div class="veicoli-scroll" :class="layout === 'grid' ? 'grid-2col' : 'list-col'">
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

    <!-- ── Modal di ricerca AI ──────────────────────────────────────── -->
    <ion-modal
      :is-open="modalOpen"
      :breakpoints="[0, 0.88]"
      :initial-breakpoint="0.88"
      handle-behavior="cycle"
      @did-dismiss="onModalDismiss"
      @did-present="focusTextarea"
    >
      <ion-header>
        <ion-toolbar class="modal-toolbar">
          <ion-title>
            <span class="modal-title-text"><span class="ai-spark-md">✦</span> Ricerca AI</span>
          </ion-title>
          <ion-buttons slot="end">
            <ion-button @click="closeModal">
              <ion-icon :icon="closeOutline" />
            </ion-button>
          </ion-buttons>
        </ion-toolbar>
      </ion-header>

      <ion-content class="modal-content-scroll">
        <div class="modal-inner">
          <!-- Textarea principale -->
          <ion-textarea
            ref="textareaEl"
            v-model="modalText"
            :placeholder="examplesPlaceholder"
            :rows="5"
            auto-grow
            class="ai-textarea"
          />

          <!-- Esempi cliccabili -->
          <p class="examples-label">Esempi di ricerca:</p>
          <div class="examples-chips">
            <button
              v-for="ex in examples"
              :key="ex"
              class="example-chip"
              @click="modalText = ex"
            >
              {{ ex }}
            </button>
          </div>

          <!-- Errore mic -->
          <div v-if="micError" class="mic-error-msg">
            <ion-icon :icon="warningOutline" /> {{ micError }}
          </div>
        </div>
      </ion-content>

      <!-- Footer con mic + pulsante Cerca -->
      <div class="modal-footer">
        <button
          v-if="speechSupported"
          class="mic-fab"
          :class="{ recording: isRecording }"
          @click="toggleRecording"
          :aria-label="isRecording ? 'Interrompi dettatura' : 'Cerca con la voce'"
        >
          <ion-icon :icon="isRecording ? stopCircleOutline : micOutline" />
          <span>{{ isRecording ? 'Stop' : 'Voce' }}</span>
          <span v-if="isRecording" class="mic-ring" />
        </button>
        <div v-else class="mic-unavailable">
          <ion-icon :icon="micOffOutline" />
          <span>Voce non disponibile</span>
        </div>

        <button
          class="submit-btn"
          :disabled="!modalText.trim()"
          @click="submitFromModal"
        >
          <ion-icon :icon="searchOutline" />
          Cerca
        </button>
      </div>
    </ion-modal>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import {
  IonPage, IonContent, IonIcon, IonSpinner,
  IonInfiniteScroll, IonInfiniteScrollContent,
  IonModal, IonHeader, IonToolbar, IonTitle,
  IonButtons, IonButton, IonTextarea,
} from '@ionic/vue'
import {
  carOutline, funnelOutline, gridOutline, listOutline,
  personCircleOutline, closeOutline,
  micOutline, micOffOutline, stopCircleOutline,
  searchOutline, warningOutline,
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

function clearSearch() {
  searchText.value = ''
  store.applyFilters({ vehicleType: activeType.value })
}

async function onInfinite(ev: CustomEvent) {
  await store.fetchNextPage()
  ;(ev.target as HTMLIonInfiniteScrollElement).complete()
}

onMounted(() => {
  if (!store.initialized) store.applyFilters({ vehicleType: activeType.value })
  initSpeech()
})

// ── Modal ─────────────────────────────────────────────────────────────────────

const modalOpen  = ref(false)
const modalText  = ref('')
const textareaEl = ref<InstanceType<typeof IonTextarea> | null>(null)

const examples = [
  'SUV diesel automatico per famiglia, massimo 25.000€',
  'Auto non più vecchia di 2 anni, pochi km, Euro 6',
  'Berlina adatta al traino di roulotte, almeno 130 CV',
  'Auto piccola per città, IVA detraibile',
  'Ibrida o elettrica con tetto apribile',
]

const examplesPlaceholder =
  'Descrivi il veicolo che cerchi…\nes. "SUV diesel automatico per famiglia con 3 figli, sotto 25.000€, non più vecchia di 3 anni"'

function openModal() {
  modalText.value = searchText.value
  micError.value  = ''
  modalOpen.value = true
}

function closeModal() {
  modalOpen.value = false
}

function onModalDismiss() {
  modalOpen.value  = false
  stopRecording()
}

async function focusTextarea() {
  await nextTick()
  const el = textareaEl.value?.$el?.querySelector('textarea')
  el?.focus()
}

function submitFromModal() {
  const q = modalText.value.trim()
  searchText.value = q
  closeModal()
  if (q) store.applyFilters({ vehicleType: activeType.value, q })
  else   store.applyFilters({ vehicleType: activeType.value })
}

// ── Dettatura vocale ──────────────────────────────────────────────────────────

const speechSupported = ref(false)
const isRecording     = ref(false)
const micError        = ref('')
let recognition: any  = null

function initSpeech() {
  const SR = (window as any).SpeechRecognition ?? (window as any).webkitSpeechRecognition
  if (!SR) return
  speechSupported.value = true
  recognition = new SR()
  recognition.lang            = 'it-IT'
  recognition.continuous      = false
  recognition.interimResults  = false
  recognition.maxAlternatives = 1
}

function toggleRecording() {
  if (isRecording.value) { stopRecording(); return }
  startRecording()
}

function startRecording() {
  if (!recognition) return
  micError.value    = ''
  modalText.value   = ''

  recognition.onresult = (event: any) => {
    const transcript = event.results[0]?.[0]?.transcript ?? ''
    if (transcript) modalText.value = transcript
    isRecording.value = false
  }
  recognition.onerror = (event: any) => {
    isRecording.value = false
    if (event.error === 'not-allowed' || event.error === 'service-not-allowed')
      micError.value = 'Permesso microfono negato. Controlla le impostazioni.'
    else if (event.error === 'network')
      micError.value = 'La dettatura richiede connessione HTTPS.'
    else if (event.error === 'no-speech')
      micError.value = 'Nessun audio rilevato. Riprova.'
    else
      micError.value = `Dettatura non disponibile (${event.error ?? 'errore'}).`
  }
  recognition.onend = () => { isRecording.value = false }

  try {
    recognition.start()
    isRecording.value = true
  } catch {
    micError.value    = 'Impossibile avviare la dettatura.'
    isRecording.value = false
  }
}

function stopRecording() {
  if (!recognition || !isRecording.value) return
  try { recognition.stop() } catch { /* già ferma */ }
  isRecording.value = false
}

onUnmounted(() => stopRecording())
</script>

<style scoped>
/* ── Header ──────────────────────────────────────────────────────────────── */
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

/* ── Search trigger ──────────────────────────────────────────────────────── */
.search-row { display: flex; gap: 8px; align-items: center; }

.search-trigger {
  flex: 1; height: 44px; min-width: 0;
  display: flex; align-items: center; gap: 8px; padding: 0 10px 0 12px;
  border-radius: var(--mc-r-sm);
  background: rgba(255,255,255,.13);
  border: 1.5px solid rgba(255,255,255,.22);
  cursor: pointer; text-align: left;
  transition: border-color .2s, box-shadow .2s;
}
.search-trigger:active,
.search-trigger:focus-within {
  border-color: rgba(255,255,255,.55);
  box-shadow: 0 0 0 3px rgba(255,255,255,.08);
}
.search-trigger.has-query {
  border-color: rgba(255,255,255,.4);
}

.ai-spark {
  font-size: 14px; color: rgba(255,255,255,.75); flex-shrink: 0;
  animation: spark-fade 3s ease-in-out infinite;
}
@keyframes spark-fade {
  0%, 100% { opacity: .6; }
  50%       { opacity: 1; }
}
.s-spinner { width: 16px; height: 16px; color: rgba(255,255,255,.7); flex-shrink: 0; }

.trigger-label {
  flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
  font-size: 13.5px; color: rgba(255,255,255,.48);
}
.trigger-label.query-active { color: #fff; }

.trigger-clear {
  flex-shrink: 0; color: rgba(255,255,255,.55); font-size: 17px;
  padding: 4px;
}

.search-adv-btn {
  height: 44px; padding: 0 14px;
  background: var(--dealer-secondary); border: none;
  border-radius: var(--mc-r-sm); cursor: pointer;
  font-family: var(--mc-font-heading); font-size: 12px; font-weight: 600;
  color: #fff; display: flex; align-items: center; gap: 5px; flex-shrink: 0;
}

.ai-hint {
  margin-top: 7px;
  font-size: 10.5px; color: rgba(255,255,255,.38);
  text-align: center; font-style: italic;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}

/* ── Stats bar ───────────────────────────────────────────────────────────── */
.stats-bar {
  display: flex; align-items: center; justify-content: space-between;
  padding: 10px 18px; background: var(--mc-surface);
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

/* ── Modal ───────────────────────────────────────────────────────────────── */
.modal-toolbar {
  --background: var(--mc-surface);
  --border-color: var(--mc-border);
}
.modal-title-text {
  display: flex; align-items: center; gap: 6px;
  font-family: var(--mc-font-heading); font-weight: 700; font-size: 16px;
}
.ai-spark-md {
  font-size: 16px; color: var(--dealer-primary);
  animation: spark-fade 3s ease-in-out infinite;
}

.modal-content-scroll {
  --background: var(--mc-surface);
}
.modal-inner {
  padding: 16px 16px 8px;
  display: flex; flex-direction: column; gap: 16px;
}

.ai-textarea {
  --background: var(--mc-surface-alt, #f5f5f5);
  --border-radius: 12px;
  --padding-start: 14px;
  --padding-end: 14px;
  --padding-top: 12px;
  --padding-bottom: 12px;
  font-size: 15px;
  border: 1.5px solid var(--mc-border);
  border-radius: 12px;
  min-height: 120px;
}

.examples-label {
  margin: 0 0 6px;
  font-size: 11.5px; font-weight: 600; color: var(--mc-text-light);
  text-transform: uppercase; letter-spacing: .5px;
}
.examples-chips {
  display: flex; flex-wrap: wrap; gap: 8px;
}
.example-chip {
  padding: 7px 12px;
  background: var(--mc-surface-alt, #f0f0f0);
  border: 1px solid var(--mc-border);
  border-radius: 20px; cursor: pointer;
  font-size: 12.5px; color: var(--mc-text-mid);
  text-align: left; transition: background .15s, border-color .15s;
}
.example-chip:active {
  background: var(--dealer-primary);
  border-color: var(--dealer-primary);
  color: #fff;
}

.mic-error-msg {
  display: flex; align-items: center; gap: 6px;
  padding: 10px 14px; border-radius: 10px;
  background: #fff3cd; border: 1px solid #ffc107;
  color: #856404; font-size: 13px;
}

/* ── Modal footer ────────────────────────────────────────────────────────── */
.modal-footer {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 16px calc(12px + var(--ion-safe-area-bottom, 0px));
  background: var(--mc-surface);
  border-top: 1px solid var(--mc-border);
  gap: 12px;
}

.mic-fab {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  gap: 2px; padding: 8px 14px;
  background: var(--mc-surface-alt, #f0f0f0);
  border: 1.5px solid var(--mc-border);
  border-radius: 12px; cursor: pointer;
  font-size: 11px; color: var(--mc-text-mid);
  position: relative; transition: background .2s, border-color .2s;
  min-width: 64px;
}
.mic-fab ion-icon { font-size: 22px; }
.mic-fab.recording {
  background: #fff0f0;
  border-color: #ff4444;
  color: #cc0000;
}
/* Alone pulsante */
.mic-ring {
  position: absolute; inset: -6px;
  border-radius: 14px;
  border: 2px solid rgba(255, 60, 60, .5);
  animation: mic-ring-anim 1.1s ease-out infinite;
  pointer-events: none;
}
@keyframes mic-ring-anim {
  0%   { transform: scale(1);   opacity: .7; }
  100% { transform: scale(1.15); opacity: 0; }
}

.mic-unavailable {
  display: flex; flex-direction: column; align-items: center; gap: 2px;
  font-size: 11px; color: var(--mc-text-light);
  min-width: 64px; opacity: .5;
}
.mic-unavailable ion-icon { font-size: 20px; }

.submit-btn {
  flex: 1; height: 50px;
  display: flex; align-items: center; justify-content: center; gap: 8px;
  background: var(--dealer-primary); color: #fff; border: none;
  border-radius: 12px; cursor: pointer;
  font-family: var(--mc-font-heading); font-size: 15px; font-weight: 700;
  transition: opacity .15s;
}
.submit-btn:disabled { opacity: .4; cursor: default; }
.submit-btn ion-icon { font-size: 18px; }
</style>
