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
        <div v-if="vehicle.rentalPrice" class="vb-price">
          € {{ fmtPrice(vehicle.rentalPrice) }}<span>/mese</span>
        </div>
      </div>

      <!-- Successo -->
      <div v-if="success" class="success-state">
        <div class="success-icon">✓</div>
        <div class="success-title">Richiesta inviata!</div>
        <div class="success-sub">Ti contatteremo al più presto per confermare disponibilità e condizioni.</div>
      </div>

      <div v-else class="modal-body">

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
        <div v-if="estimatedDays !== null" class="cost-card">
          <div class="cost-row">
            <span>Durata</span>
            <strong>{{ estimatedDays }} giorn{{ estimatedDays === 1 ? 'o' : 'i' }}</strong>
          </div>
          <div v-if="vehicle.rentalPrice" class="cost-row cost-total">
            <span>Costo stimato</span>
            <strong>€ {{ fmtPrice(estimatedCost!) }}</strong>
          </div>
          <div class="cost-note">Il costo definitivo sarà confermato dal concessionario</div>
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
          <span>Ho letto e accetto la <a href="#" @click.prevent>Privacy Policy</a> *</span>
        </label>

        <div v-if="error" class="error-msg">{{ error }}</div>

        <button class="btn-submit" :disabled="sending" @click="submit">
          <ion-spinner v-if="sending" name="crescent" style="width:18px;height:18px" />
          <template v-else>Invia richiesta noleggio</template>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { IonSpinner } from '@ionic/vue'
import { useOperatorStore } from '@/stores/operator'

const props = defineProps<{
  vehicle: {
    id?: string | null
    brandName?: string | null
    model?: string | null
    version?: string | null
    rentalPrice?: number | null
  }
  initialStartDate?: string
  initialEndDate?: string
}>()
const emit = defineEmits<{ close: [] }>()

const op      = useOperatorStore()
const sending = ref(false)
const success = ref(false)
const error   = ref('')

const today = new Date().toISOString().slice(0, 10)

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

const estimatedCost = computed<number | null>(() => {
  if (!estimatedDays.value || !props.vehicle.rentalPrice) return null
  return Math.round((props.vehicle.rentalPrice / 30) * estimatedDays.value)
})

function fmtPrice(v: number | null | undefined) {
  return v != null ? new Intl.NumberFormat('it-IT').format(v) : '—'
}

async function submit() {
  error.value = ''
  if (!form.value.startDate || !form.value.endDate) { error.value = 'Seleziona il periodo di noleggio.'; return }
  if (!form.value.fullName.trim())                  { error.value = 'Il nome è obbligatorio.'; return }
  if (!form.value.phone && !form.value.email)       { error.value = 'Inserisci almeno telefono o email.'; return }
  if (!form.value.privacyAccepted)                  { error.value = 'Accetta la privacy policy per procedere.'; return }

  const days = estimatedDays.value
  const cost = estimatedCost.value

  const msgParts = [
    `Richiesta noleggio: ${props.vehicle.brandName ?? ''} ${props.vehicle.model ?? ''}`.trim(),
    `Periodo: dal ${form.value.startDate} al ${form.value.endDate}${days ? ` (${days} giorni)` : ''}`,
  ]
  if (cost) msgParts.push(`Costo stimato: € ${fmtPrice(cost)}`)
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
        leadType:          'info',
        vehicleId:         props.vehicle.id ?? undefined,
        preferredDate:     form.value.startDate,
      }),
    })
    if (!res.ok) throw new Error()
    success.value = true
    setTimeout(() => emit('close'), 2500)
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
  padding: 14px 20px 14px;
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
  padding: 48px 20px; gap: 10px;
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
</style>
