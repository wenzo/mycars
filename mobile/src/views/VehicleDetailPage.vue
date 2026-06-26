<template>
  <ion-page>
    <div v-if="store.detail" style="display:flex;flex-direction:column;height:100%">

      <!-- Hero immagine -->
      <div class="hero" ref="heroEl">
        <div class="hero-gradient" />

        <img v-if="currentImg" :src="currentImg" alt="Foto veicolo" class="hero-img" />
        <div v-else class="hero-placeholder"><ion-icon :icon="carOutline" /></div>

        <!-- Topbar -->
        <div class="hero-topbar">
          <button class="action-btn" @click="$router.back()">
            <ion-icon :icon="arrowBackOutline" />
          </button>
          <div style="display:flex;gap:8px">
            <button class="action-btn" :class="{ 'action-btn--fav': isFav }" @click="toggleFav">
              <ion-icon :icon="isFav ? heart : heartOutline" />
            </button>
            <button class="action-btn" @click="shareVehicle">
              <ion-icon :icon="shareOutline" />
            </button>
          </div>
        </div>

        <!-- Badges -->
        <div class="hero-badges">
          <div style="display:flex;gap:6px;flex-wrap:wrap">
            <span v-if="store.detail.isNuovoArrivo" class="badge-nuovo-arrivo">✦ Nuovo Arrivo</span>
            <span v-if="store.detail.prontaConsegna" class="badge-pronta-consegna">⚡ Pronta Consegna</span>
            <span v-if="store.detail.imported" class="hero-attr-badge">🌍 Importata</span>
            <span v-if="store.detail.handicapAccessible" class="hero-attr-badge">♿ Accessibile</span>
          </div>
          <span v-if="store.detail.vatDeductible" class="hero-attr-badge badge-iva">IVA esp.</span>
        </div>

        <!-- Galleria dot indicator -->
        <div v-if="store.images.length > 1" class="img-dots">
          <span
            v-for="(_, i) in store.images"
            :key="i"
            class="img-dot"
            :class="{ active: imgIndex === i }"
            @click="imgIndex = i"
          />
        </div>
      </div>

      <!-- Contenuto scrollabile -->
      <ion-content style="--padding-bottom: calc(100px + var(--ion-tab-bar-height, 56px) + var(--ion-safe-area-bottom, 0px))">

        <!-- Card info principale -->
        <div class="info-card">
          <div class="info-brand">{{ store.detail.brandName }}</div>
          <div class="info-model">{{ store.detail.model }}</div>
          <div v-if="store.detail.version" class="info-version">{{ store.detail.version }}</div>
          <div class="price-row">
            <div v-if="store.detail.forSale || !store.detail.forRental" class="price-main">
              € {{ fmtPrice(store.detail.price) }}
              <span class="price-iva-label">{{ store.detail.vatDeductible ? 'IVA esp.' : 'IVA inc.' }}</span>
            </div>
            <div v-if="store.detail.previousPrice" class="price-old">
              € {{ fmtPrice(store.detail.previousPrice) }}
            </div>
            <div v-if="discount" class="price-discount">−{{ discount }}%</div>
          </div>
          <div v-if="store.detail.forRental && rentalStartPrice && op.profile?.rentalShowPrices" class="rental-row">
            <span class="rental-icon">🔑</span>
            <span class="rental-label">Noleggio da</span>
            <span class="rental-price">€ {{ fmtPrice(rentalStartPrice) }}{{ rentalStartUnit }}</span>
          </div>
        </div>

        <!-- Spec chips -->
        <div class="specs-row">
          <div v-if="store.detail.fuel"             class="dspec"><ion-icon :icon="flashOutline"    /><span>{{ store.detail.fuel }}</span></div>
          <div v-if="store.detail.mileageKm != null" class="dspec"><ion-icon :icon="speedometerOutline" /><span>{{ fmtKm(store.detail.mileageKm) }} km</span></div>
          <div v-if="store.detail.registrationYear"  class="dspec"><ion-icon :icon="calendarOutline" /><span>{{ fmtReg(store.detail.registrationMonth, store.detail.registrationYear) }}</span></div>
          <div v-if="store.detail.transmission"      class="dspec"><ion-icon :icon="gitNetworkOutline" /><span>{{ store.detail.transmission }}</span></div>
          <div v-if="store.detail.horsepowerCv"      class="dspec"><ion-icon :icon="thermometerOutline" /><span>{{ store.detail.horsepowerCv }} CV</span></div>
          <div v-if="store.detail.powerKw"           class="dspec"><ion-icon :icon="thermometerOutline" /><span>{{ store.detail.powerKw }} kW</span></div>
          <div v-if="store.detail.color"             class="dspec"><ion-icon :icon="colorPaletteOutline" /><span>{{ store.detail.color }}</span></div>
        </div>

        <!-- Sede veicolo -->
        <div v-if="store.detail.branchName" class="detail-branch-card">
          <div class="branch-icon-wrap">
            <ion-icon :icon="locationOutline" />
          </div>
          <div style="flex:1">
            <div style="font-family:var(--mc-font-heading);font-size:14px;font-weight:700;color:var(--mc-text)">
              {{ store.detail.branchName }}
            </div>
            <div style="font-size:12px;color:var(--mc-text-light)">
              {{ store.detail.city }}<span v-if="store.detail.province"> ({{ store.detail.province }})</span>
            </div>
          </div>
        </div>

        <!-- Simulatore costo noleggio -->
        <div v-if="fromRental && hasRentalPricing && op.profile?.rentalShowPrices" class="sim-card">
          <div class="sim-title">🔑 Simula il costo del noleggio</div>

          <!-- Formula selector (solo se ci sono formule configurate) -->
          <div v-if="availableFormulas.length > 1" class="formula-chips">
            <button
              v-for="fk in availableFormulas"
              :key="fk"
              class="formula-chip"
              :class="{ active: selectedFormula === fk }"
              @click="selectedFormula = fk"
            >{{ FORMULA_LABELS[fk] ?? fk }}</button>
          </div>

          <!-- Dettaglio formula selezionata -->
          <div v-if="currentFormulaData" class="formula-detail">
            <span class="fd-price">
              € {{ fmtPrice(currentFormulaData.price) }}<small>{{ FORMULA_UNITS[selectedFormula] }}</small>
            </span>
            <span v-if="currentFormulaData.kmIncluded" class="fd-km">
              · {{ fmtKm(currentFormulaData.kmIncluded) }} km inclusi
            </span>
          </div>

          <div class="sim-dates">
            <div class="sim-date-field">
              <label class="sim-label">Data inizio</label>
              <input v-model="simStart" class="sim-input" type="date" :min="today" @change="clampEnd" />
            </div>
            <div class="sim-date-sep">→</div>
            <div class="sim-date-field">
              <label class="sim-label">Data fine</label>
              <input v-model="simEnd" class="sim-input" type="date" :min="simStart || today" />
            </div>
          </div>

          <!-- Risultato calcolo -->
          <div v-if="simDays !== null" class="sim-result">
            <div class="sim-result-row">
              <span>Tariffa {{ (FORMULA_LABELS[selectedFormula] ?? selectedFormula).toLowerCase() }}</span>
              <span>€ {{ fmtPrice(simUnitPrice) }}{{ FORMULA_UNITS[selectedFormula] ?? '/giorno' }}</span>
            </div>
            <div class="sim-result-row">
              <span>Durata</span>
              <span>{{ simDays }} giorn{{ simDays === 1 ? 'o' : 'i' }}</span>
            </div>
            <div v-if="currentFormulaData?.kmIncluded" class="sim-result-row">
              <span>Km inclusi</span>
              <span>{{ fmtKm(currentFormulaData.kmIncluded) }} km</span>
            </div>
            <div v-if="optionsCost > 0" class="sim-result-row">
              <span>Servizi aggiuntivi</span>
              <span>+ € {{ fmtPrice(optionsCost) }}</span>
            </div>
            <div class="sim-result-total">
              <span>Totale stimato</span>
              <strong>€ {{ fmtPrice(totalWithOptions) }}</strong>
            </div>
            <div class="sim-note">Stima indicativa. Deposito e condizioni definitivi confermati dal concessionario.</div>
          </div>
          <div v-else class="sim-placeholder">
            Seleziona il periodo per calcolare il costo
          </div>
        </div>

        <!-- Sempre incluso -->
        <div v-if="fromRental && servicesCatalog?.included?.length" class="mc-section">
          <div class="mc-section-title">
            <ion-icon :icon="checkmarkCircleOutline" /> Sempre incluso
          </div>
          <div class="inclusi-chips">
            <div v-for="svc in servicesCatalog.included" :key="svc" class="incluso-chip">
              <ion-icon :icon="checkmarkOutline" />
              <span>{{ svc }}</span>
            </div>
          </div>
        </div>

        <!-- Aggiungi servizi opzionali -->
        <div v-if="fromRental && servicesCatalog?.optional?.length" class="mc-section">
          <div class="mc-section-title">
            <ion-icon :icon="addCircleOutline" /> Aggiungi servizi
          </div>
          <div class="optional-list">
            <div
              v-for="opt in servicesCatalog.optional"
              :key="opt.key"
              class="optional-item"
              @click="selectedOpts[opt.key] = !selectedOpts[opt.key]"
            >
              <div class="opt-body">
                <span class="opt-label">{{ opt.label }}</span>
                <span v-if="op.profile?.rentalShowPrices" class="opt-price">
                  + € {{ opt.pricePerDay ?? opt.priceFlat }}
                  {{ opt.pricePerDay ? '/ giorno' : 'una tantum' }}
                </span>
              </div>
              <span class="opt-check" :class="{ active: selectedOpts[opt.key] }">
                <ion-icon v-if="selectedOpts[opt.key]" :icon="checkmarkOutline" />
              </span>
            </div>
          </div>
        </div>

        <!-- Buono a sapersi -->
        <div v-if="fromRental && (rentalConditionsData || (depositAmount && op.profile?.rentalShowPrices))" class="mc-section">
          <div class="mc-section-title">
            <ion-icon :icon="informationCircleOutline" /> Buono a sapersi
          </div>
          <div class="cond-list">
            <div v-if="depositAmount && op.profile?.rentalShowPrices" class="cond-row">
              <ion-icon :icon="cardOutline" />
              <span class="cond-key">Deposito cauzionale</span>
              <span class="cond-val">€ {{ fmtPrice(depositAmount) }} su carta</span>
            </div>
            <div v-if="rentalConditionsData?.minDriverAge" class="cond-row">
              <ion-icon :icon="personOutline" />
              <span class="cond-key">Età minima</span>
              <span class="cond-val">
                {{ rentalConditionsData.minDriverAge }} anni
                <template v-if="rentalConditionsData.minLicenseYears">
                  · {{ rentalConditionsData.minLicenseYears }} anni patente
                </template>
              </span>
            </div>
            <div v-if="fuelPolicyLabel" class="cond-row">
              <ion-icon :icon="flashOutline" />
              <span class="cond-key">Carburante</span>
              <span class="cond-val">{{ fuelPolicyLabel }}</span>
            </div>
            <div v-if="op.profile?.city" class="cond-row">
              <ion-icon :icon="locationOutline" />
              <span class="cond-key">Ritiro</span>
              <span class="cond-val">
                {{ op.profile.city }}
                <template v-if="op.profile.province"> ({{ op.profile.province }})</template>
              </span>
            </div>
          </div>
        </div>

        <!-- Note veicolo per noleggio -->
        <div v-if="fromRental && store.detail.rentalVehicleNotes" class="mc-section">
          <div class="mc-section-title">
            <ion-icon :icon="documentTextOutline" /> Note per il noleggio
          </div>
          <div style="font-size:13px;color:var(--mc-text-mid);line-height:1.65">
            {{ store.detail.rentalVehicleNotes }}
          </div>
        </div>

        <!-- Descrizione -->
        <div v-if="store.detail.description" class="mc-section">
          <div class="mc-section-title">
            <ion-icon :icon="documentTextOutline" /> Descrizione
          </div>
          <div style="font-size:13px;color:var(--mc-text-mid);line-height:1.65">
            {{ store.detail.description }}
          </div>
        </div>

        <!-- Cross-promo: vendita → noleggio -->
        <div
          v-if="!fromRental && store.detail.forRental"
          class="cross-promo"
          @click="$router.replace({ path: `/tabs/veicolo/${store.detail.id}`, query: { from: 'noleggio' } })"
        >
          <span>🔑 Sei interessato al noleggio?</span>
          <span class="cross-promo-link">Scopri le condizioni →</span>
        </div>

        <!-- Cross-promo: noleggio → vendita -->
        <div
          v-if="fromRental && store.detail.forSale"
          class="cross-promo"
          @click="$router.replace({ path: `/tabs/veicolo/${store.detail.id}` })"
        >
          <span>🚗 Saresti interessato all'acquisto?</span>
          <span class="cross-promo-link">Vedi la scheda vendita →</span>
        </div>

      </ion-content>

      <!-- CTA bar fissa -->
      <div class="cta-bar">
        <button class="btn-secondary" @click="openLead('info')">
          <ion-icon :icon="callOutline" /> Contatta
        </button>
        <button v-if="fromRental" class="btn-primary btn-danger" @click="showRentalModal = true">
          <ion-icon :icon="keyOutline" />
          {{ (totalWithOptions > 0 && op.profile?.rentalShowPrices) ? `Richiedi · € ${fmtPrice(totalWithOptions)}` : 'Richiedi Noleggio' }}
        </button>
        <button v-else class="btn-primary btn-danger" @click="openLead('test_drive')">
          <ion-icon :icon="calendarOutline" /> Prenota Test Drive
        </button>
      </div>
    </div>

    <div v-else-if="store.loading" style="display:flex;align-items:center;justify-content:center;height:100%">
      <ion-spinner name="crescent" />
    </div>

    <!-- Lead modal -->
    <LeadModal v-if="showLead" :lead-type="leadType" :initial-message="leadInitialMessage" @close="showLead = false" />

    <!-- Rental request modal -->
    <RentalRequestModal
      v-if="showRentalModal && store.detail"
      :vehicle="store.detail"
      :initial-start-date="simStart"
      :initial-end-date="simEnd"
      :initial-formula="selectedFormula"
      :selected-options="selectedOptsList"
      @close="showRentalModal = false"
    />
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, watch, watchEffect, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { IonPage, IonContent, IonIcon, IonSpinner, createGesture } from '@ionic/vue'
import {
  arrowBackOutline, heart, heartOutline, shareOutline,
  carOutline, flashOutline, speedometerOutline, calendarOutline,
  gitNetworkOutline, thermometerOutline, colorPaletteOutline,
  locationOutline, documentTextOutline, callOutline, keyOutline,
  checkmarkCircleOutline, addCircleOutline, informationCircleOutline,
  cardOutline, personOutline, checkmarkOutline,
} from 'ionicons/icons'
import { Share } from '@capacitor/share'
import { Filesystem, Directory } from '@capacitor/filesystem'
import { CapacitorHttp } from '@capacitor/core'
import { useVehicleStore } from '@/stores/vehicles'
import { useOperatorStore } from '@/stores/operator'
import { useFavoritesStore } from '@/stores/favorites'
import LeadModal from '@/components/LeadModal.vue'
import RentalRequestModal from '@/components/RentalRequestModal.vue'

const route    = useRoute()
const store    = useVehicleStore()
const op       = useOperatorStore()
const favs     = useFavoritesStore()
const imgIndex = ref(0)
const heroEl   = ref<HTMLElement | null>(null)

let gesture: ReturnType<typeof createGesture> | null = null

watch(heroEl, (el) => {
  gesture?.destroy()
  gesture = null
  if (!el) return
  gesture = createGesture({
    el,
    gestureName: 'gallery-swipe',
    threshold: 10,
    onEnd(d) {
      if (Math.abs(d.deltaX) < 30 || Math.abs(d.deltaY) > 60) return
      const len = store.images.length
      if (len < 2) return
      if (d.deltaX < 0) imgIndex.value = Math.min(imgIndex.value + 1, len - 1)
      else               imgIndex.value = Math.max(imgIndex.value - 1, 0)
    },
  })
  gesture.enable()
})
onUnmounted(() => gesture?.destroy())

const showLead = ref(false)
const showRentalModal = ref(false)
const leadType = ref<'info' | 'test_drive' | 'price_update'>('info')
const leadInitialMessage = ref('')

const fromRental = computed(() => route.query.from === 'noleggio')

// ── Formule noleggio ──────────────────────────────────────────────────────────
const FORMULA_ORDER = ['daily', 'weekend', 'weekly', 'monthly', 'mid_term'] as const
const FORMULA_LABELS: Record<string, string> = {
  daily:    'Giornaliero',
  weekend:  'Weekend',
  weekly:   'Settimanale',
  monthly:  'Mensile',
  mid_term: 'Lungo Termine',
}
const FORMULA_UNITS: Record<string, string> = {
  daily:    '/giorno',
  weekend:  '/weekend',
  weekly:   '/settimana',
  monthly:  '/mese',
  mid_term: '/mese',
}

const rentalFormulas = computed(() => store.detail?.rentalFormulas ?? null)
const availableFormulas = computed(() =>
  FORMULA_ORDER.filter(k => (rentalFormulas.value as Record<string, any>)?.[k]?.price != null)
)
const hasRentalFormulas = computed(() => availableFormulas.value.length > 0)
const hasRentalPricing  = computed(() => hasRentalFormulas.value || !!store.detail?.rentalPrice)

const selectedFormula = ref('daily')
watchEffect(() => {
  if (availableFormulas.value.length > 0 && !availableFormulas.value.includes(selectedFormula.value as any)) {
    selectedFormula.value = availableFormulas.value[0]
  }
})

const currentFormulaData = computed(() => {
  return (rentalFormulas.value as Record<string, any>)?.[selectedFormula.value] ?? null
})

// Prezzo "da" mostrato nell'info-card
const rentalStartPrice = computed<number | null>(() => {
  if (rentalFormulas.value) {
    const daily = (rentalFormulas.value as Record<string, any>)?.daily?.price
    if (daily) return daily
    const first = availableFormulas.value[0]
    if (first) return (rentalFormulas.value as Record<string, any>)[first]?.price ?? null
  }
  return store.detail?.rentalPrice ?? null
})
const rentalStartUnit = computed(() => {
  if (rentalFormulas.value) {
    const daily = (rentalFormulas.value as Record<string, any>)?.daily?.price
    if (daily) return '/giorno'
    const first = availableFormulas.value[0]
    if (first) return FORMULA_UNITS[first] ?? '/mese'
  }
  return '/mese'
})

// ── Simulatore costo noleggio ─────────────────────────────────────────────────
const today    = new Date().toISOString().slice(0, 10)
const simStart = ref('')
const simEnd   = ref('')

const simDays = computed<number | null>(() => {
  if (!simStart.value || !simEnd.value) return null
  const ms = new Date(simEnd.value).getTime() - new Date(simStart.value).getTime()
  if (ms <= 0) return null
  return Math.ceil(ms / 86_400_000)
})

const simUnitPrice = computed<number>(() => {
  if (currentFormulaData.value?.price) return currentFormulaData.value.price
  const p = store.detail?.rentalPrice
  return p ? Math.round(p / 30) : 0
})

const simTotal = computed(() => {
  if (simDays.value === null || !simUnitPrice.value) return 0
  const days = simDays.value
  switch (selectedFormula.value) {
    case 'weekly':   return Math.ceil(days / 7) * simUnitPrice.value
    case 'monthly':
    case 'mid_term': return Math.ceil(days / 30) * simUnitPrice.value
    case 'weekend':  return simUnitPrice.value
    default:         return days * simUnitPrice.value
  }
})

function clampEnd() {
  if (simEnd.value && simEnd.value < simStart.value) simEnd.value = simStart.value
}

// ── Catalogo servizi e condizioni noleggio ────────────────────────────────────
interface RentalOptSvc {
  key: string; label: string; pricePerDay?: number | null; priceFlat?: number | null
}
interface SvcCatalog { included?: string[]; optional?: RentalOptSvc[] }
interface RentalConds {
  minDriverAge?: number; minLicenseYears?: number; fuelPolicy?: string
  depositDefault?: number; creditCardRequired?: boolean
}

const selectedOpts = ref<Record<string, boolean>>({})

const servicesCatalog = computed<SvcCatalog | null>(() => {
  const raw = op.profile?.rentalServicesCatalog as SvcCatalog | null
  return (raw?.included?.length || raw?.optional?.length) ? raw : null
})

const rentalConditionsData = computed<RentalConds | null>(() => {
  const raw = op.profile?.rentalConditions as RentalConds | null
  return (raw?.minDriverAge || raw?.depositDefault || raw?.fuelPolicy) ? raw : null
})

const depositAmount = computed(() =>
  store.detail?.rentalDepositOverride ??
  (rentalConditionsData.value?.depositDefault ?? null)
)

const optionsCost = computed(() => {
  const opts = servicesCatalog.value?.optional
  if (!opts?.length) return 0
  const days = simDays.value ?? 0
  return opts.reduce((sum, o) => {
    if (!selectedOpts.value[o.key]) return sum
    if (o.pricePerDay) return sum + o.pricePerDay * days
    if (o.priceFlat)   return sum + o.priceFlat
    return sum
  }, 0)
})

const totalWithOptions = computed(() => simTotal.value + optionsCost.value)

const selectedOptsList = computed(() =>
  (servicesCatalog.value?.optional ?? [])
    .filter(o => selectedOpts.value[o.key])
    .map(o => ({ key: o.key, label: o.label, pricePerDay: o.pricePerDay ?? null, priceFlat: o.priceFlat ?? null }))
)

const fuelPolicyLabel = computed(() => {
  const map: Record<string, string> = {
    full_full: 'Pieno / Pieno', full_any: 'Pieno / Qualsiasi', same: 'Stesso livello',
  }
  return map[rentalConditionsData.value?.fuelPolicy ?? ''] ?? null
})

const currentImg = computed(() => {
  const raw = store.images[imgIndex.value]?.url ?? store.detail?.coverImageUrl ?? null
  return op.resolveUrl(raw)
})
const discount = computed(() => {
  const v = store.detail
  if (!v?.price || !v?.previousPrice) return null
  return Math.round((1 - v.price / v.previousPrice) * 100)
})

function fmtPrice(v: number | null) { return v ? new Intl.NumberFormat('it-IT').format(v) : '—' }
function fmtKm(v: number)           { return new Intl.NumberFormat('it-IT').format(v) }
function fmtReg(month: number | null | undefined, year: number | null | undefined) {
  if (!year) return ''
  if (!month) return String(year)
  return `${String(month).padStart(2, '0')}/${year}`
}
function openLead(type: 'info' | 'test_drive' | 'price_update') {
  leadType.value = type
  if (type === 'info' && store.detail) {
    const { brandName, model, price } = store.detail
    const priceStr = price ? ` - € ${new Intl.NumberFormat('it-IT').format(price)}` : ''
    leadInitialMessage.value = `Si richiedono informazioni sul veicolo ${brandName} ${model}${priceStr}`
  } else {
    leadInitialMessage.value = ''
  }
  showLead.value = true
}

// ── Preferiti ─────────────────────────────────────────────────────────────────
const isFav = computed(() => !!store.detail && favs.isFavorite(store.detail.id))

function toggleFav() {
  if (!store.detail) return
  const v = store.detail
  favs.toggle({
    id:            v.id,
    brandName:     v.brandName,
    model:         v.model,
    version:       v.version ?? undefined,
    coverImageUrl: store.images[0]?.url ?? undefined,
  })
}

// ── Condivisione ──────────────────────────────────────────────────────────────
async function shareVehicle() {
  const v = store.detail
  if (!v) return
  const title = `${v.brandName} ${v.model}${v.version ? ' ' + v.version : ''}`
  const parts: string[] = [title]
  if (v.price)             parts.push(`€ ${fmtPrice(v.price)}`)
  if (v.fuel)              parts.push(v.fuel)
  if (v.registrationYear)  parts.push(String(v.registrationYear))
  if (v.mileageKm != null) parts.push(`${fmtKm(v.mileageKm)} km`)

  const options: Parameters<typeof Share.share>[0] = {
    title,
    text:        parts.join(' · '),
    dialogTitle: 'Condividi veicolo',
  }

  // Scarica l'immagine via HTTP nativo (bypassa il CORS della WebView)
  const imgUrl = currentImg.value
  if (imgUrl) {
    try {
      const res  = await CapacitorHttp.get({ url: imgUrl, responseType: 'blob' })
      // Su native, CapacitorHttp restituisce i dati binari già in base64
      const data: string = typeof res.data === 'string'
        ? res.data
        : btoa(String.fromCharCode(...new Uint8Array(res.data)))
      const ct  = (res.headers['content-type'] ?? res.headers['Content-Type'] ?? '') as string
      const ext = ct.includes('png') ? 'png' : 'jpg'
      const { uri } = await Filesystem.writeFile({
        path:      `share_vehicle.${ext}`,
        data,
        directory: Directory.Cache,
      })
      options.files = [uri]
    } catch {
      // Fallback: condivide senza immagine
    }
  }

  await Share.share(options)
}

onMounted(() => store.fetchDetail(route.params.id as string))
</script>

<style scoped>
.hero {
  position: relative; height: 252px; flex-shrink: 0; overflow: hidden;
  background: linear-gradient(150deg,#0F1C2E 0%,#1a2744 55%,#243558 100%);
}
.hero-img     { width: 100%; height: 100%; object-fit: cover; }
.hero-gradient {
  position: absolute; inset: 0; z-index: 5;
  background: linear-gradient(to bottom, rgba(0,0,0,.35) 0%, transparent 40%, transparent 60%, rgba(0,0,0,.4) 100%);
}
.hero-placeholder {
  width: 100%; height: 100%; display: flex; align-items: center; justify-content: center;
}
.hero-placeholder ion-icon { font-size: 80px; color: rgba(255,255,255,.2); }
.hero-topbar {
  position: absolute; top: 50px; left: 0; right: 0; z-index: 20;
  display: flex; align-items: center; justify-content: space-between; padding: 8px 16px;
}
.action-btn {
  width: 36px; height: 36px;
  background: rgba(0,0,0,.52);
  border: 1px solid rgba(255,255,255,.15); border-radius: 50%; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: background .2s, border-color .2s;
}
.action-btn ion-icon { color: #fff; font-size: 18px; }
.action-btn--fav { background: rgba(214,40,40,.75); border-color: rgba(214,40,40,.5); }
.hero-badges {
  position: absolute; bottom: 14px; left: 14px; right: 14px; z-index: 15;
  display: flex; justify-content: space-between; align-items: center;
}
.img-dots {
  position: absolute; bottom: 8px; left: 0; right: 0; z-index: 15;
  display: flex; justify-content: center; gap: 5px;
}
.img-dot {
  width: 6px; height: 6px; border-radius: 50%;
  background: rgba(255,255,255,.4); cursor: pointer; transition: background .2s;
}
.img-dot.active { background: #fff; width: 18px; border-radius: 3px; }

.info-card {
  background: #fff; margin: -22px 16px 0;
  border-radius: 18px; padding: 30px 18px 14px;
  box-shadow: 0 6px 28px rgba(30,58,95,.13); position: relative; z-index: 5;
}
.info-brand   { font-size: 11.5px; font-weight: 600; color: var(--mc-text-light); text-transform: uppercase; letter-spacing: .07em; margin-bottom: 3px; }
.info-model   { font-family: var(--mc-font-heading); font-size: 22px; font-weight: 800; color: var(--mc-text); line-height: 1.1; margin-bottom: 3px; }
.info-version { font-size: 13px; color: var(--mc-text-mid); margin-bottom: 13px; }
.price-row      { display: flex; align-items: baseline; gap: 10px; flex-wrap: wrap; }
.price-main     { font-family: var(--mc-font-heading); font-size: 28px; font-weight: 800; color: var(--dealer-primary); display: flex; align-items: baseline; gap: 6px; }
.price-iva-label { font-size: 11px; font-weight: 600; color: var(--mc-text-light); }
.price-old      { font-family: var(--mc-font-heading); font-size: 14px; color: var(--mc-text-light); text-decoration: line-through; }
.price-discount {
  background: #FFF0F0; color: var(--mc-red);
  font-family: var(--mc-font-heading); font-size: 11px; font-weight: 700;
  padding: 3px 8px; border-radius: 10px;
}
.rental-row   { display: flex; align-items: center; gap: 6px; margin-top: 8px; }
.rental-icon  { font-size: 14px; }
.rental-label { font-size: 12px; font-weight: 600; color: var(--mc-text-mid); }
.rental-price { font-family: var(--mc-font-heading); font-size: 16px; font-weight: 700; color: var(--mc-blue); }
.hero-attr-badge {
  background: rgba(0,0,0,.45); backdrop-filter: blur(8px);
  color: #fff; font-size: 10px; font-weight: 700;
  padding: 3px 8px; border-radius: 8px; border: 1px solid rgba(255,255,255,.2);
}
.badge-iva { background: rgba(34,139,34,.6); }

.specs-row {
  display: flex; gap: 8px; padding: 10px 16px; overflow-x: auto; flex-shrink: 0;
}
.dspec {
  display: flex; align-items: center; gap: 5px;
  background: #fff; border: 1.5px solid var(--mc-border);
  border-radius: 22px; padding: 6px 12px; white-space: nowrap; flex-shrink: 0;
}
.dspec ion-icon { font-size: 13px; color: var(--mc-blue); flex-shrink: 0; }
.dspec span     { font-size: 12px; font-weight: 600; color: var(--mc-text-mid); }

.detail-branch-card {
  margin: 6px 16px; background: #fff; border-radius: var(--mc-r);
  padding: 12px 16px; box-shadow: var(--mc-shadow-sm);
  display: flex; align-items: center; gap: 12px;
}
.branch-icon-wrap {
  width: 38px; height: 38px; border-radius: 10px;
  background: #FFF0F0; display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.branch-icon-wrap ion-icon { font-size: 18px; color: var(--dealer-secondary); }

/* ── Simulatore costo noleggio ── */
.sim-card {
  margin: 6px 16px; background: #F0F4FF; border-radius: var(--mc-r);
  padding: 14px 16px; display: flex; flex-direction: column; gap: 12px;
}
.sim-title {
  font-family: var(--mc-font-heading); font-size: 13px; font-weight: 700;
  color: var(--dealer-primary);
}

/* Formula chips */
.formula-chips { display: flex; gap: 6px; flex-wrap: wrap; }
.formula-chip {
  padding: 6px 12px; border-radius: 20px;
  border: 1.5px solid var(--mc-border);
  background: #fff; font-size: 12px; font-weight: 600;
  color: var(--mc-text-mid); cursor: pointer; transition: all .15s;
}
.formula-chip.active {
  background: var(--dealer-primary); color: #fff; border-color: var(--dealer-primary);
}
.formula-detail { display: flex; align-items: center; gap: 8px; }
.fd-price {
  font-family: var(--mc-font-heading); font-size: 20px; font-weight: 800;
  color: var(--dealer-primary); display: flex; align-items: baseline; gap: 3px;
}
.fd-price small { font-size: 11px; font-weight: 600; color: var(--mc-text-light); }
.fd-km { font-size: 12px; color: var(--mc-text-mid); }

.sim-dates { display: flex; align-items: flex-end; gap: 8px; }
.sim-date-field { flex: 1; display: flex; flex-direction: column; gap: 4px; }
.sim-label { font-size: 11px; font-weight: 600; color: var(--mc-text-mid); }
.sim-input {
  width: 100%; height: 42px; border: 1.5px solid var(--mc-border);
  border-radius: var(--mc-r-sm); padding: 0 10px;
  font-size: 13px; color: var(--mc-text); background: #fff; outline: none;
  box-sizing: border-box;
}
.sim-input:focus { border-color: var(--dealer-primary); }
.sim-date-sep { font-size: 16px; color: var(--mc-text-light); padding-bottom: 10px; flex-shrink: 0; }

.sim-result {
  background: #fff; border-radius: 10px; padding: 12px 14px;
  display: flex; flex-direction: column; gap: 7px;
}
.sim-result-row {
  display: flex; justify-content: space-between;
  font-size: 13px; color: var(--mc-text-mid);
}
.sim-result-total {
  display: flex; justify-content: space-between; align-items: center;
  border-top: 1.5px solid var(--mc-border); padding-top: 8px; margin-top: 2px;
  font-size: 13px; color: var(--mc-text-mid);
}
.sim-result-total strong {
  font-family: var(--mc-font-heading); font-size: 22px; font-weight: 800;
  color: var(--dealer-primary);
}
.sim-note {
  font-size: 10.5px; color: var(--mc-text-light); font-style: italic; line-height: 1.4;
}
.sim-placeholder {
  font-size: 13px; color: var(--mc-text-light); text-align: center;
  padding: 4px 0;
}

/* Sezione testo generica */
.mc-section {
  margin: 6px 16px; background: #fff; border-radius: var(--mc-r);
  padding: 14px 16px; box-shadow: var(--mc-shadow-sm);
}
.mc-section-title {
  font-family: var(--mc-font-heading); font-size: 13px; font-weight: 700;
  color: var(--mc-text); margin-bottom: 8px;
  display: flex; align-items: center; gap: 6px;
}
.mc-section-title ion-icon { color: var(--dealer-primary); }

/* Cross-promo banner */
.cross-promo {
  margin: 6px 16px; background: #fff; border: 1.5px solid var(--mc-border);
  border-radius: var(--mc-r); padding: 12px 16px;
  display: flex; justify-content: space-between; align-items: center;
  cursor: pointer; gap: 8px;
  font-size: 13px; color: var(--mc-text-mid);
}
.cross-promo-link { font-weight: 700; color: var(--dealer-primary); white-space: nowrap; }

/* ── Sempre incluso ── */
.inclusi-chips { display: flex; gap: 8px; flex-wrap: wrap; }
.incluso-chip {
  display: flex; align-items: center; gap: 5px;
  background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 20px;
  padding: 5px 11px; font-size: 11.5px; font-weight: 600; color: #15803d;
}
.incluso-chip ion-icon { font-size: 13px; flex-shrink: 0; }

/* ── Opzionali ── */
.optional-list { display: flex; flex-direction: column; }
.optional-item {
  display: flex; align-items: center; gap: 12px;
  padding: 11px 0; border-bottom: 1px solid var(--mc-surface); cursor: pointer;
}
.optional-item:last-child { border-bottom: none; }
.opt-body { flex: 1; }
.opt-label { display: block; font-size: 13px; color: var(--mc-text); font-weight: 500; }
.opt-price { font-size: 11.5px; color: var(--mc-text-mid); margin-top: 2px; }
.opt-check {
  width: 22px; height: 22px; border-radius: 6px; border: 2px solid var(--mc-border);
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
  transition: all .15s;
}
.opt-check.active { background: var(--dealer-primary); border-color: var(--dealer-primary); }
.opt-check.active ion-icon { font-size: 13px; color: #fff; }

/* ── Buono a sapersi ── */
.cond-list { display: flex; flex-direction: column; }
.cond-row {
  display: flex; align-items: center; gap: 10px;
  padding: 9px 0; border-bottom: 1px solid var(--mc-surface); font-size: 12.5px;
}
.cond-row:last-child { border-bottom: none; }
.cond-row ion-icon { font-size: 15px; color: var(--mc-text-light); flex-shrink: 0; }
.cond-key { color: var(--mc-text-mid); flex: 1; }
.cond-val { color: var(--mc-text); font-weight: 600; text-align: right; max-width: 55%; }

.cta-bar {
  background: #fff; border-top: 1px solid var(--mc-border);
  padding: 12px 16px calc(12px + env(safe-area-inset-bottom, 0px));
  display: flex; gap: 10px;
}
</style>
