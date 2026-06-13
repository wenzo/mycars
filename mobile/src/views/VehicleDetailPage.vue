<template>
  <ion-page>
    <div v-if="store.detail" style="display:flex;flex-direction:column;height:100%">

      <!-- Hero immagine -->
      <div class="hero">
        <div class="hero-gradient" />

        <img v-if="currentImg" :src="currentImg" alt="Foto veicolo" class="hero-img" />
        <div v-else class="hero-placeholder"><ion-icon :icon="carOutline" /></div>

        <!-- Topbar -->
        <div class="hero-topbar">
          <button class="action-btn" @click="$router.back()">
            <ion-icon :icon="arrowBackOutline" />
          </button>
          <div style="display:flex;gap:8px">
            <button class="action-btn"><ion-icon :icon="heartOutline" /></button>
            <button class="action-btn"><ion-icon :icon="shareOutline" /></button>
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
      <ion-content style="--padding-bottom:100px">

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
          <div v-if="store.detail.forRental && store.detail.rentalPrice" class="rental-row">
            <span class="rental-icon">🔑</span>
            <span class="rental-label">Noleggio</span>
            <span class="rental-price">€ {{ fmtPrice(store.detail.rentalPrice) }}/mese</span>
          </div>
        </div>

        <!-- Spec chips -->
        <div class="specs-row">
          <div v-if="store.detail.fuel"             class="dspec"><ion-icon :icon="flashOutline"    /><span>{{ store.detail.fuel }}</span></div>
          <div v-if="store.detail.mileageKm != null" class="dspec"><ion-icon :icon="speedometerOutline" /><span>{{ fmtKm(store.detail.mileageKm) }} km</span></div>
          <div v-if="store.detail.registrationYear"  class="dspec"><ion-icon :icon="calendarOutline" /><span>{{ store.detail.registrationYear }}</span></div>
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
        <div v-if="fromRental && store.detail.rentalPrice" class="sim-card">
          <div class="sim-title">🔑 Simula il costo del noleggio</div>

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
              <span>Tariffa giornaliera</span>
              <span>€ {{ fmtPrice(simDailyRate) }}/giorno</span>
            </div>
            <div class="sim-result-row">
              <span>Durata</span>
              <span>{{ simDays }} giorn{{ simDays === 1 ? 'o' : 'i' }}</span>
            </div>
            <div class="sim-result-total">
              <span>Totale stimato</span>
              <strong>€ {{ fmtPrice(simTotal) }}</strong>
            </div>
            <div class="sim-note">Stima indicativa. Deposito, km e condizioni saranno confermati dal concessionario.</div>
          </div>
          <div v-else class="sim-placeholder">
            Seleziona il periodo per calcolare il costo
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
          @click="$router.replace({ path: `/veicolo/${store.detail.id}`, query: { from: 'noleggio' } })"
        >
          <span>🔑 Sei interessato al noleggio?</span>
          <span class="cross-promo-link">Scopri le condizioni →</span>
        </div>

        <!-- Cross-promo: noleggio → vendita -->
        <div
          v-if="fromRental && store.detail.forSale"
          class="cross-promo"
          @click="$router.replace({ path: `/veicolo/${store.detail.id}` })"
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
          {{ simTotal > 0 ? `Richiedi · € ${fmtPrice(simTotal)}` : 'Richiedi Noleggio' }}
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
      @close="showRentalModal = false"
    />
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { IonPage, IonContent, IonIcon, IonSpinner } from '@ionic/vue'
import {
  arrowBackOutline, heartOutline, shareOutline,
  carOutline, flashOutline, speedometerOutline, calendarOutline,
  gitNetworkOutline, thermometerOutline, colorPaletteOutline,
  locationOutline, documentTextOutline, callOutline, keyOutline,
} from 'ionicons/icons'
import { useVehicleStore } from '@/stores/vehicles'
import { useOperatorStore } from '@/stores/operator'
import LeadModal from '@/components/LeadModal.vue'
import RentalRequestModal from '@/components/RentalRequestModal.vue'

const route    = useRoute()
const store    = useVehicleStore()
const op       = useOperatorStore()
const imgIndex = ref(0)
const showLead = ref(false)
const showRentalModal = ref(false)
const leadType = ref<'info' | 'test_drive' | 'price_update'>('info')
const leadInitialMessage = ref('')

const fromRental = computed(() => route.query.from === 'noleggio')

// ── Simulatore costo noleggio ─────────────────────────────────────────────────
const today    = new Date().toISOString().slice(0, 10)
const simStart = ref('')
const simEnd   = ref('')

const simDailyRate = computed(() => {
  const p = store.detail?.rentalPrice
  return p ? Math.round(p / 30) : 0
})
const simDays = computed<number | null>(() => {
  if (!simStart.value || !simEnd.value) return null
  const ms = new Date(simEnd.value).getTime() - new Date(simStart.value).getTime()
  if (ms <= 0) return null
  return Math.ceil(ms / 86_400_000)
})
const simTotal = computed(() =>
  simDays.value !== null ? simDailyRate.value * simDays.value : 0
)
function clampEnd() {
  if (simEnd.value && simEnd.value < simStart.value) simEnd.value = simStart.value
}

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
  background: rgba(0,0,0,.38); backdrop-filter: blur(10px);
  border: 1px solid rgba(255,255,255,.15); border-radius: 50%; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
}
.action-btn ion-icon { color: #fff; font-size: 18px; }
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
  border-radius: 18px; padding: 16px 18px 14px;
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

/* Riquadro condizioni noleggio (static fallback) */
.rental-info-card {
  margin: 6px 16px; background: #F0F4FF; border-radius: var(--mc-r);
  padding: 14px 16px; display: flex; flex-direction: column; gap: 8px;
}
.ric-title { font-family: var(--mc-font-heading); font-size: 13px; font-weight: 700; color: var(--dealer-primary); }
.ric-row {
  display: flex; justify-content: space-between; align-items: center;
  font-size: 13px; color: var(--mc-text-mid);
}
.ric-row strong { color: var(--mc-text); font-family: var(--mc-font-heading); }
.ric-note { font-size: 11px; color: var(--mc-text-light); font-style: italic; line-height: 1.4; }

/* Cross-promo banner */
.cross-promo {
  margin: 6px 16px; background: #fff; border: 1.5px solid var(--mc-border);
  border-radius: var(--mc-r); padding: 12px 16px;
  display: flex; justify-content: space-between; align-items: center;
  cursor: pointer; gap: 8px;
  font-size: 13px; color: var(--mc-text-mid);
}
.cross-promo-link { font-weight: 700; color: var(--dealer-primary); white-space: nowrap; }

.cta-bar {
  background: #fff; border-top: 1px solid var(--mc-border);
  padding: 12px 16px calc(12px + env(safe-area-inset-bottom, 0px));
  display: flex; gap: 10px;
}
</style>
