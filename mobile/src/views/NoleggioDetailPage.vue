<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-back-button default-href="/tabs/noleggio" />
        </ion-buttons>
        <ion-title>{{ rental ? rental.customerName : 'Noleggio' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button v-if="rental?.status === 'active'" @click="sendReminder" title="Promemoria push">
            <ion-icon :icon="notificationsOutline" slot="icon-only" />
          </ion-button>
          <ion-button
            v-if="profile?.rentalContractPdfEnabled && rental"
            @click="openContract"
            title="Stampa contratto"
          >
            <ion-icon :icon="documentTextOutline" slot="icon-only" />
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content :fullscreen="true">
      <div v-if="loading" class="center-spinner">
        <ion-spinner name="crescent" />
      </div>

      <template v-else-if="rental">
        <!-- Status badge -->
        <div class="status-bar" :class="'status-' + rental.status">
          {{ STATUS_LABELS[rental.status as RentalStatus] }}
        </div>

        <!-- Veicolo -->
        <ion-card>
          <ion-card-header>
            <ion-card-subtitle>Veicolo</ion-card-subtitle>
            <ion-card-title>{{ rental.vehicleBrand }} {{ rental.vehicleModel }}</ion-card-title>
          </ion-card-header>
          <ion-card-content>
            <p v-if="rental.vehicleTarga"><strong>Targa:</strong> {{ rental.vehicleTarga }}</p>
          </ion-card-content>
        </ion-card>

        <!-- Periodo -->
        <ion-card>
          <ion-card-header>
            <ion-card-subtitle>Periodo</ion-card-subtitle>
          </ion-card-header>
          <ion-card-content>
            <div class="info-row">
              <span>Inizio</span>
              <span>{{ formatDate(rental.startDate) }}</span>
            </div>
            <div class="info-row">
              <span>Fine prevista</span>
              <span>{{ formatDate(rental.plannedEndDate) }}</span>
            </div>
            <div class="info-row" v-if="rental.actualEndDate">
              <span>Rientro effettivo</span>
              <span>{{ formatDate(rental.actualEndDate) }}</span>
            </div>
            <div class="info-row">
              <span>Durata</span>
              <span>{{ duration }} gg</span>
            </div>
          </ion-card-content>
        </ion-card>

        <!-- Cliente -->
        <ion-card>
          <ion-card-header>
            <ion-card-subtitle>Cliente</ion-card-subtitle>
          </ion-card-header>
          <ion-card-content>
            <div class="info-row">
              <span>Nome</span>
              <strong>{{ rental.customerName }}</strong>
            </div>
            <div class="info-row" v-if="rental.customerPhone">
              <span>Telefono</span>
              <a :href="'tel:' + rental.customerPhone">{{ rental.customerPhone }}</a>
            </div>
            <div class="info-row" v-if="rental.customerLicense">
              <span>Patente</span>
              <span>{{ rental.customerLicense }}</span>
            </div>
            <div class="info-row" v-if="rental.customerFiscalCode">
              <span>C.F.</span>
              <span>{{ rental.customerFiscalCode }}</span>
            </div>
          </ion-card-content>
        </ion-card>

        <!-- Consegna / Rientro -->
        <ion-card v-if="rental.status !== 'booked'">
          <ion-card-header>
            <ion-card-subtitle>Condizioni veicolo</ion-card-subtitle>
          </ion-card-header>
          <ion-card-content>
            <div class="info-row" v-if="rental.kmDeparture">
              <span>Km partenza</span>
              <span>{{ rental.kmDeparture.toLocaleString() }} km</span>
            </div>
            <div class="info-row" v-if="rental.fuelDeparture">
              <span>Carburante partenza</span>
              <span>{{ FUEL_LABELS[rental.fuelDeparture as FuelLevel] }}</span>
            </div>
            <div class="info-row" v-if="rental.kmReturn">
              <span>Km rientro</span>
              <span>{{ rental.kmReturn.toLocaleString() }} km</span>
            </div>
            <div class="info-row" v-if="rental.fuelReturn">
              <span>Carburante rientro</span>
              <span>{{ FUEL_LABELS[rental.fuelReturn as FuelLevel] }}</span>
            </div>
          </ion-card-content>
        </ion-card>

        <!-- Economico -->
        <ion-card v-if="profile?.rentalShowPrices && (rental.agreedPrice || rental.depositAmount)">
          <ion-card-header>
            <ion-card-subtitle>Condizioni economiche</ion-card-subtitle>
          </ion-card-header>
          <ion-card-content>
            <div class="info-row" v-if="rental.agreedPrice">
              <span>Importo</span>
              <strong>€ {{ rental.agreedPrice.toFixed(2) }}</strong>
            </div>
            <div class="info-row" v-if="rental.depositAmount">
              <span>Deposito</span>
              <span>€ {{ rental.depositAmount.toFixed(2) }}
                <ion-badge :color="rental.depositReturned ? 'success' : 'warning'" class="dep-badge">
                  {{ rental.depositReturned ? 'Restituito' : 'Non restituito' }}
                </ion-badge>
              </span>
            </div>
            <div class="info-row" v-if="rental.paymentMethod">
              <span>Pagamento</span>
              <span>{{ PM_LABELS[rental.paymentMethod] }}</span>
            </div>
            <div class="info-row">
              <span>Pagato</span>
              <ion-badge :color="rental.isPaid ? 'success' : 'danger'">
                {{ rental.isPaid ? 'Sì' : 'No' }}
              </ion-badge>
            </div>
          </ion-card-content>
        </ion-card>

        <!-- Note -->
        <ion-card v-if="rental.notes">
          <ion-card-header>
            <ion-card-subtitle>Note</ion-card-subtitle>
          </ion-card-header>
          <ion-card-content>
            <p>{{ rental.notes }}</p>
          </ion-card-content>
        </ion-card>

        <!-- Foto (se abilitato) -->
        <template v-if="profile?.rentalPhotosEnabled">
          <ion-card>
            <ion-card-header>
              <ion-card-subtitle>Foto veicolo</ion-card-subtitle>
            </ion-card-header>
            <ion-card-content>
              <photo-section
                title="Alla consegna"
                phase="departure"
                :photos="departurePhotos"
                :rental-id="rental.id"
                :can-upload="rental.status !== 'cancelled'"
                @upload="onPhotoUpload"
                @delete="onPhotoDelete"
              />
              <photo-section
                title="Al rientro"
                phase="return"
                :photos="returnPhotos"
                :rental-id="rental.id"
                :can-upload="rental.status === 'active' || rental.status === 'closed'"
                @upload="onPhotoUpload"
                @delete="onPhotoDelete"
              />
            </ion-card-content>
          </ion-card>
        </template>

        <!-- Azioni -->
        <div class="actions-area">
          <!-- Attiva (da prenotato) -->
          <ion-button
            v-if="rental.status === 'booked'"
            expand="block"
            color="success"
            @click="openActivateModal"
          >
            <ion-icon :icon="checkmarkCircleOutline" slot="start" />
            Registra consegna
          </ion-button>

          <!-- Chiudi (da attivo) -->
          <ion-button
            v-if="rental.status === 'active'"
            expand="block"
            color="primary"
            @click="openCloseModal"
          >
            <ion-icon :icon="returnDownBackOutline" slot="start" />
            Registra rientro
          </ion-button>

          <!-- Annulla -->
          <ion-button
            v-if="rental.status === 'booked' || rental.status === 'active'"
            expand="block"
            fill="outline"
            color="danger"
            @click="confirmCancel"
          >
            Annulla noleggio
          </ion-button>
        </div>
      </template>
    </ion-content>

    <!-- Modal consegna -->
    <ion-modal :is-open="activateModal" @didDismiss="activateModal = false">
      <ion-header>
        <ion-toolbar>
          <ion-title>Registra Consegna</ion-title>
          <ion-buttons slot="end">
            <ion-button @click="activateModal = false">Chiudi</ion-button>
          </ion-buttons>
        </ion-toolbar>
      </ion-header>
      <ion-content>
        <ion-list>
          <ion-item>
            <ion-label position="stacked">Km alla partenza</ion-label>
            <ion-input v-model.number="activateForm.kmDeparture" type="number" min="0" />
          </ion-item>
          <ion-item>
            <ion-label position="stacked">Carburante partenza</ion-label>
            <ion-select v-model="activateForm.fuelDeparture" placeholder="Seleziona">
              <ion-select-option v-for="(label, key) in FUEL_LABELS" :key="key" :value="key">
                {{ label }}
              </ion-select-option>
            </ion-select>
          </ion-item>
        </ion-list>
        <div class="modal-footer">
          <ion-button expand="block" color="success" @click="doActivate">
            Conferma consegna
          </ion-button>
        </div>
      </ion-content>
    </ion-modal>

    <!-- Modal rientro -->
    <ion-modal :is-open="closeModal" @didDismiss="closeModal = false">
      <ion-header>
        <ion-toolbar>
          <ion-title>Registra Rientro</ion-title>
          <ion-buttons slot="end">
            <ion-button @click="closeModal = false">Chiudi</ion-button>
          </ion-buttons>
        </ion-toolbar>
      </ion-header>
      <ion-content>
        <ion-list>
          <ion-item>
            <ion-label position="stacked">Data rientro effettivo</ion-label>
            <ion-datetime-button datetime="actual-end-dt" />
            <ion-modal :keep-contents-mounted="true">
              <ion-datetime id="actual-end-dt" presentation="date" v-model="closeForm.actualEndDate" />
            </ion-modal>
          </ion-item>
          <ion-item>
            <ion-label position="stacked">Km al rientro</ion-label>
            <ion-input v-model.number="closeForm.kmReturn" type="number" min="0" />
          </ion-item>
          <ion-item>
            <ion-label position="stacked">Carburante rientro</ion-label>
            <ion-select v-model="closeForm.fuelReturn" placeholder="Seleziona">
              <ion-select-option v-for="(label, key) in FUEL_LABELS" :key="key" :value="key">
                {{ label }}
              </ion-select-option>
            </ion-select>
          </ion-item>
        </ion-list>
        <div class="modal-footer">
          <ion-button expand="block" color="primary" @click="doClose">
            Conferma rientro
          </ion-button>
        </div>
      </ion-content>
    </ion-modal>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import {
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardHeader,
  IonCardTitle, IonCardSubtitle, IonCardContent, IonList, IonItem, IonLabel,
  IonInput, IonSelect, IonSelectOption, IonButton, IonButtons, IonBackButton,
  IonBadge, IonIcon, IonModal, IonSpinner, IonDatetime, IonDatetimeButton,
  alertController, toastController,
} from '@ionic/vue'
import {
  checkmarkCircleOutline, returnDownBackOutline,
  notificationsOutline, documentTextOutline,
} from 'ionicons/icons'
import {
  useRentalsStore,
  FUEL_LABELS, STATUS_LABELS, STATUS_COLORS,
  type RentalStatus, type FuelLevel,
} from '@/stores/rentals'
import { useOperatorStore } from '@/stores/operator'
import PhotoSection from '@/components/RentalPhotoSection.vue'

const route  = useRoute()
const store  = useRentalsStore()
const opStore = useOperatorStore()

const rentalId = route.params.id as string
const loading  = computed(() => store.loading)
const rental   = computed(() => store.detail)
const profile  = computed(() => opStore.profile)

const PM_LABELS: Record<string, string> = {
  cash: 'Contanti', pos: 'POS / Carta', transfer: 'Bonifico',
}

const departurePhotos = computed(() =>
  store.photos.filter(p => p.phase === 'departure')
)
const returnPhotos = computed(() =>
  store.photos.filter(p => p.phase === 'return')
)

const duration = computed(() => {
  if (!rental.value) return 0
  const a = new Date(rental.value.startDate)
  const b = new Date(rental.value.plannedEndDate)
  return Math.round((b.getTime() - a.getTime()) / 86400000) + 1
})

const activateModal  = ref(false)
const closeModal     = ref(false)
const activateForm   = ref({ kmDeparture: undefined as number | undefined, fuelDeparture: '' })
const closeForm      = ref({
  actualEndDate: new Date().toISOString().slice(0, 10),
  kmReturn: undefined as number | undefined,
  fuelReturn: '',
})

onMounted(async () => {
  await Promise.all([
    store.fetchDetail(rentalId),
    store.fetchPhotos(rentalId),
  ])
})

function formatDate(d: string) {
  if (!d) return '—'
  const [y, m, dd] = d.split('-')
  return `${dd}/${m}/${y}`
}

function openActivateModal() { activateModal.value = true }
function openCloseModal()    { closeModal.value = true }

async function doActivate() {
  try {
    await store.activateRental(
      rentalId,
      activateForm.value.kmDeparture,
      activateForm.value.fuelDeparture as FuelLevel || undefined
    )
    activateModal.value = false
    toast('Consegna registrata.')
  } catch (e: any) { toast(e.message, 'danger') }
}

async function doClose() {
  try {
    await store.closeRental(
      rentalId,
      closeForm.value.actualEndDate,
      closeForm.value.kmReturn,
      closeForm.value.fuelReturn as FuelLevel || undefined
    )
    closeModal.value = false
    toast('Rientro registrato.')
  } catch (e: any) { toast(e.message, 'danger') }
}

async function confirmCancel() {
  const alert = await alertController.create({
    header: 'Annulla noleggio',
    message: 'Sei sicuro di voler annullare questo noleggio?',
    buttons: [
      { text: 'No', role: 'cancel' },
      {
        text: 'Sì, annulla',
        role: 'destructive',
        handler: async () => {
          try {
            await store.cancelRental(rentalId)
            toast('Noleggio annullato.')
          } catch (e: any) { toast(e.message, 'danger') }
        },
      },
    ],
  })
  await alert.present()
}

async function sendReminder() {
  try {
    await store.sendReturnReminder(rentalId)
    toast('Promemoria inviato.')
  } catch { toast('Errore invio notifica.', 'danger') }
}

function openContract() {
  store.openContract(rentalId)
}

async function onPhotoUpload(payload: { file: File; phase: 'departure' | 'return' }) {
  try {
    await store.uploadPhoto(rentalId, payload.file, payload.phase)
    toast('Foto caricata.')
  } catch { toast('Errore upload foto.', 'danger') }
}

async function onPhotoDelete(photoId: string) {
  await store.deletePhoto(rentalId, photoId)
}

async function toast(message: string, color = 'success') {
  const t = await toastController.create({ message, duration: 2000, color })
  await t.present()
}
</script>

<style scoped>
.status-bar {
  padding: 10px 16px;
  font-weight: 700;
  font-size: 13px;
  letter-spacing: .04em;
  text-transform: uppercase;
  text-align: center;
}
.status-booked    { background: var(--ion-color-warning-tint);  color: var(--ion-color-warning-shade); }
.status-active    { background: var(--ion-color-success-tint);  color: var(--ion-color-success-shade); }
.status-closed    { background: var(--ion-color-medium-tint);   color: var(--ion-color-medium-shade);  }
.status-cancelled { background: var(--ion-color-danger-tint);   color: var(--ion-color-danger-shade);  }
.info-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 5px 0;
  border-bottom: 1px solid var(--ion-border-color, #eee);
}
.info-row:last-child { border-bottom: none; }
.info-row span:first-child { color: var(--ion-color-medium); font-size: 13px; }
.dep-badge { margin-left: 6px; }
.actions-area { padding: 16px; display: flex; flex-direction: column; gap: 10px; }
.center-spinner { display: flex; justify-content: center; padding: 60px; }
.modal-footer { padding: 16px; }
</style>
