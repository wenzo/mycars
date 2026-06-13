<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-sheet">
      <div class="sheet-handle" />
      <div class="modal-title">{{ titles[props.leadType] }}</div>

      <!-- Stato successo -->
      <div v-if="success" class="success-state">
        <div class="success-icon">✓</div>
        <div class="success-title">Richiesta inviata!</div>
        <div class="success-sub">Ti contatteremo al più presto.</div>
      </div>

      <div v-else class="modal-body">
        <input v-model="form.fullName"  class="mc-input" type="text"  placeholder="Nome e cognome *" />
        <input v-model="form.phone"     class="mc-input" type="tel"   placeholder="Telefono" />
        <input v-model="form.email"     class="mc-input" type="email" placeholder="Email" />
        <textarea v-model="form.message" class="mc-input mc-textarea" placeholder="Messaggio (opzionale)" />

        <div v-if="leadType === 'test_drive'" style="display:flex;gap:8px">
          <input v-model="form.preferredDate" class="mc-input" type="date" style="flex:1" />
          <select v-model="form.preferredTime" class="mc-input" style="flex:1">
            <option value="">Orario preferito</option>
            <option v-for="t in times" :key="t" :value="t">{{ t }}</option>
          </select>
        </div>

        <!-- Privacy -->
        <label class="privacy-row">
          <input type="checkbox" v-model="form.privacyAccepted" />
          <span>Ho letto e accetto la <a href="#" @click.prevent>Privacy Policy</a> *</span>
        </label>
        <label class="privacy-row">
          <input type="checkbox" v-model="form.marketingAccepted" />
          <span>Acconsento alle comunicazioni commerciali</span>
        </label>

        <div v-if="error" style="color:var(--mc-red);font-size:12px">{{ error }}</div>

        <button
          class="btn-primary btn-danger"
          :disabled="sending"
          style="margin-top:4px"
          @click="submit"
        >
          <ion-spinner v-if="sending" name="crescent" style="width:18px;height:18px" />
          <template v-else>{{ submitLabel }}</template>
        </button>
      </div>
    </div>
  </div>
</template>


<script setup lang="ts">
import { ref, computed } from 'vue'
import { IonSpinner } from '@ionic/vue'
import { useOperatorStore } from '@/stores/operator'
import { useVehicleStore } from '@/stores/vehicles'

const props = defineProps<{
  leadType: 'info' | 'test_drive' | 'price_update'
  initialMessage?: string
}>()
const emit = defineEmits<{ close: [] }>()

const op      = useOperatorStore()
const vStore  = useVehicleStore()
const sending = ref(false)
const success = ref(false)
const error   = ref('')

const form = ref({
  fullName: '', phone: '', email: '', message: props.initialMessage ?? '',
  privacyAccepted: false, marketingAccepted: false,
  preferredDate: '', preferredTime: '',
})

const titles: Record<string, string> = {
  info:          'Richiesta informazioni',
  test_drive:    'Prenota Test Drive',
  price_update:  'Aggiornamento prezzo',
}
const submitLabel = computed(() => ({
  info:         'Invia richiesta',
  test_drive:   'Prenota Test Drive',
  price_update: 'Richiedi aggiornamento',
})[props.leadType])

const times = ['9:00','10:00','11:00','12:00','15:00','16:00','17:00','18:00']

async function submit() {
  error.value = ''
  if (!form.value.fullName.trim()) { error.value = 'Il nome è obbligatorio.'; return }
  if (!form.value.phone && !form.value.email) { error.value = 'Inserisci telefono o email.'; return }
  if (!form.value.privacyAccepted) { error.value = 'Accetta la privacy policy per procedere.'; return }

  sending.value = true
  try {
    const body = {
      fullName:          form.value.fullName.trim(),
      phone:             form.value.phone || undefined,
      email:             form.value.email || undefined,
      message:           form.value.message || undefined,
      privacyAccepted:   true,
      marketingAccepted: form.value.marketingAccepted,
      leadType:          props.leadType,
      vehicleId:         vStore.detail?.id,
      preferredDate:     form.value.preferredDate || undefined,
      preferredTime:     form.value.preferredTime || undefined,
    }
    const res = await fetch(`${op.apiBase}/api/public/${op.slug}/leads`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    if (!res.ok) throw new Error('Errore invio richiesta')
    success.value = true
    setTimeout(() => emit('close'), 2000)
  } catch (e: any) {
    error.value = e?.message ?? 'Errore di rete'
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
  max-height: 90vh; overflow-y: auto;
  padding: 0 0 calc(20px + env(safe-area-inset-bottom, 0px));
}
.sheet-handle { width: 36px; height: 4px; background: var(--mc-border); border-radius: 2px; margin: 12px auto 8px; }
.modal-title {
  font-family: var(--mc-font-heading); font-size: 18px; font-weight: 700; color: var(--mc-text);
  padding: 4px 20px 14px; border-bottom: 1px solid var(--mc-surface);
}
.modal-body { padding: 16px 20px; display: flex; flex-direction: column; gap: 10px; }
.mc-input {
  width: 100%; height: 48px; border: 2px solid var(--mc-border);
  border-radius: var(--mc-r-sm); padding: 0 14px;
  font-family: var(--mc-font-body); font-size: 14px; color: var(--mc-text);
  background: #fff; outline: none;
}
.mc-input:focus { border-color: var(--dealer-primary); }
.mc-textarea { height: 90px; padding: 12px 14px; resize: none; }
.privacy-row {
  display: flex; align-items: flex-start; gap: 10px;
  font-size: 12px; color: var(--mc-text-mid); cursor: pointer;
}
.privacy-row input { margin-top: 2px; accent-color: var(--dealer-primary); }
.privacy-row a { color: var(--dealer-primary); }
.success-state {
  display: flex; flex-direction: column; align-items: center;
  padding: 40px 20px 48px; gap: 10px;
}
.success-icon {
  width: 56px; height: 56px; border-radius: 50%;
  background: #E8F5E9; color: #2E7D32;
  font-size: 28px; display: flex; align-items: center; justify-content: center;
}
.success-title {
  font-family: var(--mc-font-heading); font-size: 18px; font-weight: 700; color: var(--mc-text);
}
.success-sub { font-size: 13px; color: var(--mc-text-mid); }
</style>
