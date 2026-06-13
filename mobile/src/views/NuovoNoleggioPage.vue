<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-back-button default-href="/tabs/noleggio" />
        </ion-buttons>
        <ion-title>Nuovo Noleggio</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content :fullscreen="true">
      <form @submit.prevent="submit" class="form-container">

        <!-- Veicolo -->
        <ion-list-header><ion-label>Veicolo</ion-label></ion-list-header>
        <ion-list lines="full" class="ion-margin-bottom">
          <ion-item>
            <ion-label position="stacked">Veicolo *</ion-label>
            <ion-select v-model="form.vehicleId" placeholder="Seleziona veicolo" required>
              <ion-select-option
                v-for="v in rentalVehicles"
                :key="v.id"
                :value="v.id"
              >
                {{ v.brandName }} {{ v.model }} – {{ v.targa ?? v.internalCode }}
              </ion-select-option>
            </ion-select>
          </ion-item>
        </ion-list>

        <!-- Date -->
        <ion-list-header><ion-label>Periodo</ion-label></ion-list-header>
        <ion-list lines="full" class="ion-margin-bottom">
          <ion-item>
            <ion-label position="stacked">Data inizio *</ion-label>
            <ion-datetime-button datetime="start-dt" />
            <ion-modal :keep-contents-mounted="true">
              <ion-datetime
                id="start-dt"
                presentation="date"
                v-model="form.startDate"
                :min="today"
              />
            </ion-modal>
          </ion-item>
          <ion-item>
            <ion-label position="stacked">Data fine prevista *</ion-label>
            <ion-datetime-button datetime="end-dt" />
            <ion-modal :keep-contents-mounted="true">
              <ion-datetime
                id="end-dt"
                presentation="date"
                v-model="form.plannedEndDate"
                :min="form.startDate || today"
              />
            </ion-modal>
          </ion-item>
        </ion-list>

        <div class="availability-badge" v-if="availabilityChecked">
          <ion-badge :color="available ? 'success' : 'danger'">
            {{ available ? 'Disponibile' : 'Non disponibile nel periodo' }}
          </ion-badge>
        </div>

        <!-- Cliente -->
        <ion-list-header><ion-label>Cliente</ion-label></ion-list-header>
        <ion-list lines="full" class="ion-margin-bottom">
          <ion-item>
            <ion-label position="stacked">Nome e Cognome *</ion-label>
            <ion-input v-model="form.customerName" required placeholder="Mario Rossi" />
          </ion-item>
          <ion-item>
            <ion-label position="stacked">Telefono</ion-label>
            <ion-input v-model="form.customerPhone" type="tel" placeholder="+39 333 1234567" />
          </ion-item>
          <ion-item>
            <ion-label position="stacked">N. Patente</ion-label>
            <ion-input v-model="form.customerLicense" placeholder="IT1234567890" />
          </ion-item>
          <ion-item>
            <ion-label position="stacked">Codice Fiscale</ion-label>
            <ion-input v-model="form.customerFiscalCode" placeholder="RSSMRA80A01H501Z" />
          </ion-item>
        </ion-list>

        <!-- Economico (se abilitato) -->
        <template v-if="operatorProfile?.rentalShowPrices">
          <ion-list-header><ion-label>Condizioni economiche</ion-label></ion-list-header>
          <ion-list lines="full" class="ion-margin-bottom">
            <ion-item>
              <ion-label position="stacked">Importo concordato (€)</ion-label>
              <ion-input v-model.number="form.agreedPrice" type="number" step="0.01" min="0" />
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Deposito cauzionale (€)</ion-label>
              <ion-input v-model.number="form.depositAmount" type="number" step="0.01" min="0" />
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Modalità pagamento</ion-label>
              <ion-select v-model="form.paymentMethod" placeholder="Seleziona">
                <ion-select-option value="cash">Contanti</ion-select-option>
                <ion-select-option value="pos">POS / Carta</ion-select-option>
                <ion-select-option value="transfer">Bonifico</ion-select-option>
              </ion-select>
            </ion-item>
          </ion-list>
        </template>

        <!-- Note -->
        <ion-list lines="full" class="ion-margin-bottom">
          <ion-item>
            <ion-label position="stacked">Note</ion-label>
            <ion-textarea v-model="form.notes" rows="3" placeholder="Eventuali note…" />
          </ion-item>
        </ion-list>

        <div class="form-footer">
          <ion-button expand="block" type="submit" :disabled="submitting || !canSubmit">
            <ion-spinner v-if="submitting" name="crescent" slot="start" />
            Crea Noleggio
          </ion-button>
        </div>

      </form>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import {
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonListHeader,
  IonLabel, IonItem, IonInput, IonSelect, IonSelectOption, IonTextarea,
  IonButton, IonButtons, IonBackButton, IonBadge, IonSpinner,
  IonDatetime, IonDatetimeButton, IonModal,
  toastController,
} from '@ionic/vue'
import { useRentalsStore } from '@/stores/rentals'
import { useVehiclesStore } from '@/stores/vehicles'
import { useOperatorStore } from '@/stores/operator'

const router    = useRouter()
const store     = useRentalsStore()
const vehicles  = useVehiclesStore()
const opStore   = useOperatorStore()

const operatorProfile = computed(() => opStore.profile)

const today = new Date().toISOString().slice(0, 10)

const form = ref({
  vehicleId:         '',
  startDate:         today,
  plannedEndDate:    today,
  customerName:      '',
  customerPhone:     '',
  customerLicense:   '',
  customerFiscalCode:'',
  agreedPrice:       undefined as number | undefined,
  depositAmount:     undefined as number | undefined,
  paymentMethod:     undefined as string | undefined,
  notes:             '',
})

const submitting          = ref(false)
const available           = ref(true)
const availabilityChecked = ref(false)

// Carica solo veicoli noleggiabili
const rentalVehicles = computed(() =>
  vehicles.items.filter(v => v.forRental && !v.isSold)
)

const canSubmit = computed(() =>
  form.value.vehicleId &&
  form.value.customerName.trim() &&
  form.value.startDate &&
  form.value.plannedEndDate &&
  available.value
)

// Controlla disponibilità al cambio veicolo/date
watch(
  [() => form.value.vehicleId, () => form.value.startDate, () => form.value.plannedEndDate],
  async ([vid, start, end]) => {
    if (!vid || !start || !end || end < start) {
      availabilityChecked.value = false
      return
    }
    available.value = await store.checkAvailability(vid, start, end)
    availabilityChecked.value = true
  }
)

async function submit() {
  if (!canSubmit.value) return
  submitting.value = true
  try {
    const rental = await store.createRental({
      vehicleId:         form.value.vehicleId,
      startDate:         form.value.startDate,
      plannedEndDate:    form.value.plannedEndDate,
      customerName:      form.value.customerName.trim(),
      customerPhone:     form.value.customerPhone || undefined,
      customerLicense:   form.value.customerLicense || undefined,
      customerFiscalCode:form.value.customerFiscalCode || undefined,
      agreedPrice:       form.value.agreedPrice,
      depositAmount:     form.value.depositAmount,
      paymentMethod:     form.value.paymentMethod as any,
      notes:             form.value.notes || undefined,
    })
    const toast = await toastController.create({
      message:  'Noleggio creato con successo.',
      duration: 2000,
      color:    'success',
    })
    await toast.present()
    router.replace(`/noleggio/${rental.id}`)
  } catch (err: any) {
    const toast = await toastController.create({
      message:  err.message ?? 'Errore durante la creazione.',
      duration: 3000,
      color:    'danger',
    })
    await toast.present()
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.form-container { padding-bottom: 24px; }
.availability-badge {
  padding: 8px 16px;
  display: flex;
  justify-content: flex-end;
}
.form-footer {
  padding: 16px;
}
</style>
