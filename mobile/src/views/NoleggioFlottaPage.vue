<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-back-button default-href="/tabs/noleggio" />
        </ion-buttons>
        <ion-title>Flotta Noleggio</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content :fullscreen="true">
      <ion-refresher slot="fixed" @ionRefresh="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>

      <div v-if="loading && rentalFleet.length === 0" class="center-spinner">
        <ion-spinner name="crescent" />
      </div>

      <ion-list lines="full" v-else>
        <ion-item v-for="v in rentalFleet" :key="v.id">
          <ion-thumbnail slot="start" v-if="v.coverImageUrl">
            <img :src="resolveUrl(v.coverImageUrl)" />
          </ion-thumbnail>
          <ion-label>
            <h2>{{ v.brandName }} {{ v.model }}</h2>
            <p v-if="v.version">{{ v.version }}</p>
            <p>
              <span v-if="v.vehicleTarga" class="targa">{{ v.vehicleTarga }}</span>
              <span class="rental-price" v-if="v.rentalPrice">
                € {{ v.rentalPrice }}/gg
              </span>
            </p>
          </ion-label>
          <div slot="end" class="status-chip" :class="statusClass(v.id)">
            {{ statusLabel(v.id) }}
          </div>
        </ion-item>
      </ion-list>

      <div class="empty-state" v-if="!loading && rentalFleet.length === 0">
        <ion-icon :icon="carOutline" class="empty-icon" />
        <p>Nessun veicolo noleggiabile</p>
        <p class="hint">Abilita il flag "Noleggiabile" nella scheda di un veicolo.</p>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonItem,
  IonLabel, IonThumbnail, IonIcon, IonButtons, IonBackButton, IonRefresher,
  IonRefresherContent, IonSpinner,
} from '@ionic/vue'
import { carOutline } from 'ionicons/icons'
import { useVehiclesStore } from '@/stores/vehicles'
import { useRentalsStore } from '@/stores/rentals'
import { useOperatorStore } from '@/stores/operator'

const vehiclesStore = useVehiclesStore()
const rentalsStore  = useRentalsStore()
const opStore       = useOperatorStore()

const loading = computed(() => vehiclesStore.loading)

// Veicoli con forRental = true
const rentalFleet = computed(() =>
  vehiclesStore.items.filter(v => v.forRental)
)

// Mappa vehicleId → stato noleggio corrente
const activeRentalMap = computed(() => {
  const map: Record<string, string> = {}
  for (const r of rentalsStore.items) {
    if (r.status === 'active' || r.status === 'booked') {
      map[r.vehicleId] = r.status
    }
  }
  return map
})

function statusClass(vehicleId: string) {
  const s = activeRentalMap.value[vehicleId]
  return s === 'active' ? 'chip-active' : s === 'booked' ? 'chip-booked' : 'chip-free'
}

function statusLabel(vehicleId: string) {
  const s = activeRentalMap.value[vehicleId]
  return s === 'active' ? 'In corso' : s === 'booked' ? 'Prenotato' : 'Disponibile'
}

function resolveUrl(url: string | null) {
  if (!url) return ''
  if (url.startsWith('http')) return url
  return (import.meta.env.VITE_API_BASE_URL ?? '') + url
}

onMounted(async () => {
  await Promise.all([
    vehiclesStore.fetchVehicles(),
    rentalsStore.fetchRentals('active'),
  ])
})

async function refresh(ev: CustomEvent) {
  await Promise.all([vehiclesStore.fetchVehicles(), rentalsStore.fetchRentals('active')])
  ;(ev.target as HTMLIonRefresherElement).complete()
}
</script>

<style scoped>
.center-spinner { display: flex; justify-content: center; padding: 60px; }
.targa {
  font-weight: 600;
  font-size: 12px;
  background: #eee;
  padding: 1px 5px;
  border-radius: 3px;
  margin-right: 6px;
}
.rental-price { font-size: 12px; color: var(--dealer-primary, #1E3A5F); }
.status-chip {
  font-size: 11px;
  font-weight: 600;
  padding: 3px 8px;
  border-radius: 10px;
}
.chip-free   { background: #e8f5e9; color: #2e7d32; }
.chip-active { background: #e3f2fd; color: #1565c0; }
.chip-booked { background: #fff8e1; color: #f57f17; }
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 60px 24px;
  color: var(--ion-color-medium);
}
.empty-icon { font-size: 52px; margin-bottom: 12px; opacity: 0.4; }
.hint { font-size: 13px; text-align: center; }
</style>
