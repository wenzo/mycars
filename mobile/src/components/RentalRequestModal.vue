<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-sheet">
      <div class="sheet-handle" />

      <!-- Intestazione veicolo -->
      <div class="vehicle-banner">
        <div class="vb-icon">🔑</div>
        <div class="vb-info">
          <div class="vb-title">{{ vehicle.brandName }} {{ vehicle.model }}</div>
          <div v-if="vehicle.version" class="vb-sub">{{ vehicle.version }}</div>
        </div>
        <div v-if="rentalStartPrice && op.profile?.rentalShowPrices" class="vb-price">
          € {{ fmtPrice(rentalStartPrice) }}<span>{{ rentalStartUnit }}</span>
        </div>
      </div>

      <!-- Successo -->
      <div v-if="success" class="success-state">
        <div class="success-icon">✓</div>
        <div class="success-title">Richiesta inviata!</div>
        <div class="success-sub">Ti contatteremo al più presto per confermare disponibilità e condizioni.</div>
        <div v-if="trackingCode" class="tracking-box">
          <div class="tracking-label">Il tuo codice di tracciamento</div>
          <div class="tracking-code">{{ trackingCode }}</div>
          <div class="tracking-hint">Usalo nell'app per seguire lo stato della richiesta.<br>Ti abbiamo inviato anche un'email di conferma.</div>
        </div>
        <button class="btn-close-success" @click="$emit('close')">Chiudi</button>
      </div>

      <div v-else class="modal-body">

        <!-- Formula selector (se ci sono formule disponibili) -->
        <template v-if="availableFormulas.length > 0">
          <div class="section-label">Tipologia noleggio</div>
          <div class="formula-chips">
            <button
              v-for="fk in availableFormulas"
              :key="fk"
              class="formula-chip"
              :class="{ active: selectedFormula === fk }"
              @click="selectedFormula = fk"
            >{{ FORMULA_LABELS[fk] ?? fk }}</button>
          </div>
        </template>

        <!-- Periodo -->
        <div class="section-label">Periodo richiesto</div>
        <div class="date-row">
          <div class="date-field">
            <label class="field-label">Data inizio *</label>
            <input v-model="form.startDate" class="mc-input" type="date" :min="today" />
          </div>
          <div class="date-field">
            <label class="field-label">Data fine *</label>
            <input v-model="form.endDate" class="mc-input" type="date" :min="form.startDate || today" />
          </div>
        </div>

        <!-- Riepilogo costi stimati -->
        <div v-if="estimatedDays !== null && op.profile?.rentalShowPrices" class="cost-card">
          <div class="cost-row">
            <span>Durata</span>
            <strong>{{ estimatedDays }} giorn{{ estimatedDays === 1 ? 'o' : 'i' }}</strong>
          </div>
          <div v-if="baseEstimatedCost" class="cost-row">
            <span>Noleggio base</span>
            <strong>€ {{ fmtPrice(baseEstimatedCost) }}</strong>
          </div>
          <div v-if="optionsCostModal > 0" class="cost-row">
            <span>Servizi opzionali</span>
            <strong>+ € {{ fmtPrice(optionsCostModal) }}</strong>
          </div>
          <div v-if="estimatedCost" class="cost-row cost-total">
            <span>Totale stimato</span>
            <strong>€ {{ fmtPrice(estimatedCost) }}</strong>
          </div>
          <div v-if="vehicle.rentalDepositOverride" class="cost-row">
            <span>Deposito cauzionale</span>
            <strong>€ {{ fmtPrice(vehicle.rentalDepositOverride) }}</strong>
          </div>
          <div class="cost-note">Il costo definitivo sarà confermato dal concessionario</div>
        </div>

        <!-- Opzioni selezionate -->
        <div v-if="selectedOptions?.length" class="opts-summary">
          <div class="opts-title">Servizi aggiuntivi inclusi</div>
          <div v-for="o in selectedOptions" :key="o.key" class="opts-item">
            <span>✓ {{ o.label }}</span>
            <span v-if="op.profile?.rentalShowPrices" class="opts-price">
              {{ o.pricePerDay ? `+€${o.pricePerDay}/g` : o.priceFlat ? `+€${o.priceFlat}` : '' }}
            </span>
          </div>
        </div>

        <!-- Dati cliente -->
        <div class="section-label">I tuoi dati</div>
        <input v-model="form.fullName" class="mc-input" type="text" placeholder="Nome e cognome *" />
        <div class="date-row">
          <input v-model="form.phone" class="mc-input" type="tel"   placeholder="Telefono" style="flex:1" />
          <input v-model="form.email" class="mc-input" type="email" placeholder="Email"    style="flex:1" />
        </div>
        <textarea v-model="form.notes" class="mc-input mc-textarea"
          placeholder="Note (es. chilometraggio illimitato, guida aggiuntiva…)" />

        <!-- Privacy -->
        <label class="privacy-row">
          <input type="checkbox" v-model="form.privacyAccepted" />
          <span>Ho letto e accetto la <a href="#" @click.prevent="openPolicy">Privacy Policy</a> *</span>
        </label>

        <div v-if="error" class="error-msg">{{ error }}</div>

        <button class="btn-submit" :disabled="sending" @click="submit">
          <ion-spinner v-if="sending" name="crescent" style="width:18px;height:18px" />
          <template v-else>Invia richiesta noleggio</template>
        </button>
      </div>
    </div>

    <!-- Privacy policy sheet -->
    <div v-if="showPolicy" class="policy-overlay" @click.self="showPolicy = false">
      <div class="policy-sheet">
        <div class="sheet-handle" />
        <div class="policy-title">Privacy Policy</div>
        <div v-if="policyLoading" style="text-align:center;padding:40px">
          <ion-spinner name="crescent" />
        </div>
        <div v-else-if="policyHtml" class="policy-content" v-html="policyHtml" />
        <div v-else class="policy-empty">Privacy policy non disponibile.</div>
        <button class="policy-close-btn" @click="showPolicy = false">Chiudi</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { IonSpinner } from '@ionic/vue'
import { useOperatorStore } from '@/stores/operator'
import { useRentalClientStore, type SelectedOption } from '@/stores/rentalClient'

interface FormulaData {
  price: number
  kmIncluded: number | null
  priceExtraKm: number | null
}

const props = defineProps<{
  vehicle: {
    id?: string | null
    brandName?: string | null
    model?: string | null
    version?: string | null
    rentalPrice?: number | null
    rentalFormulas?: Record<string, FormulaData> | null
    rentalDepositOverride?: number | null
  }
  initialStartDate?: string
  initialEndDate?: string
  initialFormula?: string
  selectedOptions?: SelectedOption[]
}>()
const emit = defineEmits<{ close: [] }>()

const op          = useOperatorStore()
const clientStore = useRentalClientStore()
const sending     = ref(false)
const success     = ref(false)
const error       = ref('')
const trackingCode = ref<string | null>(null)

const today = new Date().toISOString().slice(0, 10)

// ── Formula selector ──────────────────────────────────────────────────────────
const FORMULA_ORDER = ['daily', 'weekend', 'weekly', 'monthly', 'mid_term']
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

const availableFormulas = computed(() =>
  FORMULA_ORDER.filter(k => props.vehicle.rentalFormulas?.[k]?.price != null)
)
const selectedFormula = ref(props.initialFormula ?? availableFormulas.value[0] ?? 'daily')

const currentFormula = computed<FormulaData | null>(() =>
  props.vehicle.rentalFormulas?.[selectedFormula.value] ?? null
)

const rentalStartPrice = computed<number | null>(() => {
  if (props.vehicle.rentalFormulas) {
    const daily = props.vehicle.rentalFormulas.daily?.price
    if (daily) return daily
    const first = availableFormulas.value[0]
    if (first) return props.vehicle.rentalFormulas[first]?.price ?? null
  }
  return props.vehicle.rentalPrice ?? null
})
const rentalStartUnit = computed(() => {
  if (props.vehicle.rentalFormulas) {
    const daily = props.vehicle.rentalFormulas.daily?.price
    if (daily) return '/giorno'
    const first = availableFormulas.value[0]
    if (first) return FORMULA_UNITS[first] ?? '/mese'
  }
  return '/mese'
})

// ── Form ──────────────────────────────────────────────────────────────────────
const form = ref({
  startDate:       props.initialStartDate ?? '',
  endDate:         props.initialEndDate   ?? '',
  fullName:        '',
  phone:           '',
  email:           '',
  notes:           '',
  privacyAccepted: false,
})

const estimatedDays = computed<number | null>(() => {
  if (!form.value.startDate || !form.value.endDate) return null
  const ms = new Date(form.value.endDate).getTime() - new Date(form.value.startDate).getTime()
  if (ms <= 0) return null
  return Math.ceil(ms / 86_400_000)
})

const baseEstimatedCost = computed<number | null>(() => {
  if (!estimatedDays.value) return null
  const days = estimatedDays.value
  if (currentFormula.value?.price) {
    const price = currentFormula.value.price
    switch (selectedFormula.value) {
      case 'weekly':   return Math.ceil(days / 7) * price
      case 'monthly':
      case 'mid_term': return Math.ceil(days / 30) * price
      case 'weekend':  return price
      default:         return days * price
    }
  }
  if (props.vehicle.rentalPrice) return Math.round((props.vehicle.rentalPrice / 30) * days)
  return null
})

const optionsCostModal = computed(() => {
  if (!props.selectedOptions?.length || !estimatedDays.value) return 0
  const days = estimatedDays.value
  return props.selectedOptions.reduce((sum, o) => {
    if (o.pricePerDay) return sum + o.pricePerDay * days
    if (o.priceFlat)   return sum + o.priceFlat
    return sum
  }, 0)
})

const estimatedCost = computed<number | null>(() => {
  const base = baseEstimatedCost.value
  if (base === null && optionsCostModal.value === 0) return null
  return (base ?? 0) + optionsCostModal.value
})

function fmtPrice(v: number | null | undefined) {
  return v != null ? new Intl.NumberFormat('it-IT').format(v) : '—'
}

// ── Privacy policy ────────────────────────────────────────────────────────────
const showPolicy    = ref(false)
const policyHtml    = ref<string | null>(null)
const policyLoading = ref(false)

async function openPolicy() {
  showPolicy.value = true
  if (policyHtml.value !== null) return
  policyLoading.value = true
  policyHtml.value = await op.fetchPrivacyPolicy()
  policyLoading.value = false
}

// ── Submit ────────────────────────────────────────────────────────────────────
async function submit() {
  error.value = ''
  if (!form.value.startDate || !form.value.endDate) { error.value = 'Seleziona il periodo di noleggio.'; return }
  if (!form.value.fullName.trim())                  { error.value = 'Il nome è obbligatorio.'; return }
  if (!form.value.phone && !form.value.email)       { error.value = 'Inserisci almeno telefono o email.'; return }
  if (!form.value.privacyAccepted)                  { error.value = 'Accetta la privacy policy per procedere.'; return }

  const days = estimatedDays.value
  const cost = estimatedCost.value
  const formulaLabel = FORMULA_LABELS[selectedFormula.value] ?? selectedFormula.value

  const msgParts = [
    `Richiesta noleggio: ${props.vehicle.brandName ?? ''} ${props.vehicle.model ?? ''}`.trim(),
    `Tipologia: ${formulaLabel}`,
    `Periodo: dal ${form.value.startDate} al ${form.value.endDate}${days ? ` (${days} giorni)` : ''}`,
  ]
  if (cost) msgParts.push(`Costo stimato: € ${fmtPrice(cost)}`)
  if (props.selectedOptions?.length) {
    const optsText = props.selectedOptions.map(o => {
      const p = o.pricePerDay ? `€${o.pricePerDay}/g` : o.priceFlat ? `€${o.priceFlat}` : ''
      return `- ${o.label}${p ? ` (${p})` : ''}`
    }).join('\n')
    msgParts.push(`Servizi aggiuntivi:\n${optsText}`)
  }
  if (form.value.notes.trim()) msgParts.push(`Note: ${form.value.notes.trim()}`)

  sending.value = true
  try {
    const res = await fetch(`${op.apiBase}/api/public/${op.slug}/leads`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        fullName:          form.value.fullName.trim(),
        phone:             form.value.phone || undefined,
        email:             form.value.email || undefined,
        message:           msgParts.join('\n'),
        privacyAccepted:   true,
        marketingAccepted: false,
        leadType:          'rental',
        vehicleId:         props.vehicle.id ?? undefined,
        preferredDate:     form.value.startDate,
      }),
    })
    if (!res.ok) throw new Error()

    const data = await res.json()
    trackingCode.value = data.trackingCode ?? null

    // Salva richiesta in localStorage per "I miei noleggi"
    clientStore.addRequest({
      vehicleId:    props.vehicle.id ?? null,
      vehicleName:  `${props.vehicle.brandName ?? ''} ${props.vehicle.model ?? ''}`.trim(),
      formula:      selectedFormula.value,
      formulaLabel,
      startDate:    form.value.startDate,
      endDate:      form.value.endDate,
      days:         days ?? null,
      estimatedCost: cost ?? null,
      options:      props.selectedOptions ?? [],
      notes:        form.value.notes.trim(),
      trackingCode: trackingCode.value,
    })
    // Pre-compila il profilo cliente se non già salvato
    if (!clientStore.profile.fullName && form.value.fullName) {
      clientStore.saveProfile({
        fullName: form.value.fullName.trim(),
        phone:    form.value.phone || '',
        email:    form.value.email || '',
        city:     '',
      })
    }

    success.value = true
    if (!trackingCode.value) setTimeout(() => emit('close'), 2500)
  } catch {
    error.value = 'Errore di rete. Riprova.'
  } finally {
    sending.value = false
  }
}
</script>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.5); z-index: 200;
  display: flex; align-items: flex-end;
}
.modal-sheet {
  background: #fff; border-radius: 24px 24px 0 0; width: 100%;
  max-height: 92vh; overflow-y: auto;
  padding-bottom: calc(20px + env(safe-area-inset-bottom, 0px));
}
.sheet-handle {
  width: 36px; height: 4px; background: var(--mc-border);
  border-radius: 2px; margin: 12px auto 0;
}

/* Intestazione veicolo */
.vehicle-banner {
  display: flex; align-items: center; gap: 12px;
  padding: 14px 20px;
  border-bottom: 1px solid var(--mc-surface);
}
.vb-icon { font-size: 28px; flex-shrink: 0; }
.vb-info { flex: 1; min-width: 0; }
.vb-title { font-family: var(--mc-font-heading); font-size: 16px; font-weight: 700; color: var(--mc-text); }
.vb-sub   { font-size: 12px; color: var(--mc-text-light); margin-top: 1px; }
.vb-price { font-family: var(--mc-font-heading); font-size: 17px; font-weight: 800; color: var(--dealer-primary); white-space: nowrap; }
.vb-price span { font-size: 11px; font-weight: 600; color: var(--mc-text-light); }

/* Corpo */
.modal-body { padding: 16px 20px; display: flex; flex-direction: column; gap: 10px; }

.section-label {
  font-family: var(--mc-font-heading); font-size: 11px; font-weight: 700;
  color: var(--mc-text-light); text-transform: uppercase; letter-spacing: .06em;
  margin-top: 4px;
}

/* Formula chips */
.formula-chips { display: flex; gap: 6px; flex-wrap: wrap; }
.formula-chip {
  padding: 7px 14px; border-radius: 20px;
  border: 1.5px solid var(--mc-border);
  background: #fff; font-size: 12px; font-weight: 600;
  color: var(--mc-text-mid); cursor: pointer; transition: all .15s;
}
.formula-chip.active {
  background: var(--dealer-primary); color: #fff; border-color: var(--dealer-primary);
}

.date-row { display: flex; gap: 8px; }
.date-field { flex: 1; display: flex; flex-direction: column; gap: 4px; }
.field-label { font-size: 11px; font-weight: 600; color: var(--mc-text-mid); }

/* Riepilogo costi */
.cost-card {
  background: #F0F4FF; border-radius: 12px;
  padding: 12px 14px; display: flex; flex-direction: column; gap: 6px;
}
.cost-row {
  display: flex; justify-content: space-between; align-items: center;
  font-size: 13px; color: var(--mc-text-mid);
}
.cost-row strong { color: var(--mc-text); }
.cost-total { border-top: 1px solid rgba(0,0,0,.08); padding-top: 6px; }
.cost-total strong { font-family: var(--mc-font-heading); font-size: 16px; color: var(--dealer-primary); }
.cost-note { font-size: 10.5px; color: var(--mc-text-light); font-style: italic; }

.mc-input {
  width: 100%; height: 48px; border: 2px solid var(--mc-border);
  border-radius: var(--mc-r-sm); padding: 0 14px;
  font-family: var(--mc-font-body); font-size: 14px; color: var(--mc-text);
  background: #fff; outline: none; box-sizing: border-box;
}
.mc-input:focus { border-color: var(--dealer-primary); }
.mc-textarea { height: 80px; padding: 12px 14px; resize: none; }

.opts-summary {
  background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 10px;
  padding: 10px 13px; display: flex; flex-direction: column; gap: 5px;
}
.opts-title { font-size: 10.5px; font-weight: 700; color: #15803d; text-transform: uppercase; letter-spacing: .05em; margin-bottom: 3px; }
.opts-item  { display: flex; justify-content: space-between; font-size: 12.5px; color: #166534; }
.opts-price { font-weight: 600; }

.privacy-row {
  display: flex; align-items: flex-start; gap: 10px;
  font-size: 12px; color: var(--mc-text-mid); cursor: pointer;
}
.privacy-row input { margin-top: 2px; accent-color: var(--dealer-primary); }
.privacy-row a { color: var(--dealer-primary); }

.error-msg { color: var(--mc-red); font-size: 12px; }

.btn-submit {
  width: 100%; height: 50px;
  background: var(--dealer-primary); color: #fff;
  border: none; border-radius: 14px;
  font-family: var(--mc-font-heading); font-size: 15px; font-weight: 700;
  cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 8px;
  margin-top: 4px;
}
.btn-submit:disabled { opacity: .6; cursor: not-allowed; }

/* Successo */
.success-state {
  display: flex; flex-direction: column; align-items: center;
  padding: 36px 20px 28px; gap: 10px;
}
.success-icon {
  width: 60px; height: 60px; border-radius: 50%;
  background: #E8F5E9; color: #2E7D32;
  font-size: 30px; display: flex; align-items: center; justify-content: center;
}
.success-title {
  font-family: var(--mc-font-heading); font-size: 20px; font-weight: 700; color: var(--mc-text);
}
.success-sub { font-size: 13px; color: var(--mc-text-mid); text-align: center; max-width: 260px; line-height: 1.5; }

.tracking-box {
  margin-top: 8px; width: 100%; max-width: 300px;
  background: #f0f4ff; border: 2px dashed #2E75B6; border-radius: 12px;
  padding: 16px 20px; text-align: center; display: flex; flex-direction: column; gap: 6px;
}
.tracking-label {
  font-size: 10.5px; font-weight: 700; color: var(--dealer-primary);
  text-transform: uppercase; letter-spacing: .07em;
}
.tracking-code {
  font-family: monospace; font-size: 26px; font-weight: 700;
  letter-spacing: 4px; color: #1E3A5F;
}
.tracking-hint { font-size: 11.5px; color: var(--mc-text-mid); line-height: 1.5; }
.btn-close-success {
  margin-top: 8px; width: 100%; max-width: 280px; height: 46px;
  background: var(--mc-surface); color: var(--mc-text);
  border: none; border-radius: 12px;
  font-family: var(--mc-font-heading); font-size: 14px; font-weight: 700;
  cursor: pointer;
}

/* Privacy policy sheet */
.policy-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.5); z-index: 300;
  display: flex; align-items: flex-end;
}
.policy-sheet {
  background: #fff; border-radius: 24px 24px 0 0; width: 100%;
  max-height: 85vh; overflow-y: auto;
  padding-bottom: calc(16px + env(safe-area-inset-bottom, 0px));
  display: flex; flex-direction: column;
}
.policy-title {
  font-family: var(--mc-font-heading); font-size: 18px; font-weight: 700;
  color: var(--mc-text); padding: 14px 20px;
  border-bottom: 1px solid var(--mc-surface);
}
.policy-content {
  padding: 16px 20px; font-size: 13px; color: var(--mc-text-mid);
  line-height: 1.65; flex: 1; overflow-y: auto;
}
.policy-empty { padding: 40px 20px; text-align: center; color: var(--mc-text-light); font-size: 13px; }
.policy-close-btn {
  margin: 12px 20px 0; height: 46px;
  background: var(--mc-surface); color: var(--mc-text);
  border: none; border-radius: 12px;
  font-family: var(--mc-font-heading); font-size: 14px; font-weight: 700;
  cursor: pointer;
}
</style>
