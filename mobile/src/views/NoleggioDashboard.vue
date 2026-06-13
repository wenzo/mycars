<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-title>Noleggio</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="goToFlotta">
            <ion-icon :icon="carOutline" slot="icon-only" />
          </ion-button>
          <ion-button router-link="/tabs/noleggio/nuovo">
            <ion-icon :icon="addOutline" slot="icon-only" />
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content :fullscreen="true">
      <ion-refresher slot="fixed" @ionRefresh="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>

      <!-- KPI cards -->
      <div class="kpi-row" v-if="dashboard">
        <div class="kpi-card kpi-active">
          <div class="kpi-value">{{ dashboard.activeCount }}</div>
          <div class="kpi-label">In corso</div>
        </div>
        <div class="kpi-card kpi-booked">
          <div class="kpi-value">{{ dashboard.bookedCount }}</div>
          <div class="kpi-label">Prenotati</div>
        </div>
        <div class="kpi-card kpi-return">
          <div class="kpi-value">{{ dashboard.returningTodayCount }}</div>
          <div class="kpi-label">Rientri oggi</div>
        </div>
      </div>

      <!-- Rientri oggi -->
      <ion-list-header v-if="dashboard && dashboard.returningToday.length > 0">
        <ion-label>Rientri previsti oggi</ion-label>
      </ion-list-header>
      <ion-list v-if="dashboard && dashboard.returningToday.length > 0" lines="full">
        <rental-list-item
          v-for="r in dashboard.returningToday"
          :key="r.id"
          :rental="r"
          @click="goToDetail(r.id)"
        />
      </ion-list>

      <!-- Lista noleggi attivi -->
      <ion-list-header>
        <ion-label>Noleggi in corso</ion-label>
      </ion-list-header>

      <ion-list lines="full" v-if="!loading && activeRentals.length > 0">
        <rental-list-item
          v-for="r in activeRentals"
          :key="r.id"
          :rental="r"
          @click="goToDetail(r.id)"
        />
      </ion-list>

      <div class="empty-state" v-if="!loading && activeRentals.length === 0">
        <ion-icon :icon="keyOutline" class="empty-icon" />
        <p>Nessun noleggio attivo</p>
        <ion-button fill="outline" router-link="/tabs/noleggio/nuovo">
          Nuovo noleggio
        </ion-button>
      </div>

      <!-- Sezione prenotazioni -->
      <ion-list-header>
        <ion-label>Prossime prenotazioni</ion-label>
        <ion-button fill="clear" size="small" @click="filterStatus = 'booked'; goToLista()">
          Vedi tutte
        </ion-button>
      </ion-list-header>
      <ion-list lines="full" v-if="bookedRentals.length > 0">
        <rental-list-item
          v-for="r in bookedRentals.slice(0, 3)"
          :key="r.id"
          :rental="r"
          @click="goToDetail(r.id)"
        />
      </ion-list>

      <ion-loading :is-open="loading && !dashboard" message="Caricamento…" />
    </ion-content>

    <!-- Tab per filtrare la lista -->
    <ion-footer>
      <ion-toolbar>
        <ion-segment :value="listTab" @ionChange="onTabChange($event)" mode="md">
          <ion-segment-button value="dashboard">Dashboard</ion-segment-button>
          <ion-segment-button value="lista">Tutti</ion-segment-button>
        </ion-segment>
      </ion-toolbar>
    </ion-footer>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonListHeader,
  IonLabel, IonButton, IonButtons, IonIcon, IonRefresher, IonRefresherContent,
  IonLoading, IonFooter, IonSegment, IonSegmentButton,
} from '@ionic/vue'
import { addOutline, carOutline, keyOutline } from 'ionicons/icons'
import { useRentalsStore, STATUS_LABELS, STATUS_COLORS, type RentalStatus } from '@/stores/rentals'
import RentalListItem from '@/components/RentalListItem.vue'

const router = useRouter()
const store  = useRentalsStore()

const loading     = computed(() => store.loading)
const dashboard   = computed(() => store.dashboard)
const listTab     = ref('dashboard')
const filterStatus = ref<RentalStatus | ''>('')

const activeRentals = computed(() =>
  store.items.filter(r => r.status === 'active')
)
const bookedRentals = computed(() =>
  store.items.filter(r => r.status === 'booked')
)

onMounted(async () => {
  await Promise.all([
    store.fetchDashboard(),
    store.fetchRentals(),
  ])
})

async function refresh(ev: CustomEvent) {
  await Promise.all([store.fetchDashboard(), store.fetchRentals()])
  ;(ev.target as HTMLIonRefresherElement).complete()
}

function goToDetail(id: string) {
  router.push(`/noleggio/${id}`)
}
function goToFlotta() {
  router.push('/tabs/noleggio/flotta')
}
function goToLista() {
  router.push('/tabs/noleggio/lista')
}

function onTabChange(ev: CustomEvent) {
  if (ev.detail.value === 'lista') router.push('/tabs/noleggio/lista')
}
</script>

<style scoped>
.kpi-row {
  display: flex;
  gap: 12px;
  padding: 16px 16px 8px;
}
.kpi-card {
  flex: 1;
  background: var(--ion-card-background, #fff);
  border-radius: 12px;
  padding: 14px 8px;
  text-align: center;
  box-shadow: 0 2px 8px rgba(0,0,0,0.08);
}
.kpi-value {
  font-size: 28px;
  font-weight: 700;
  line-height: 1;
}
.kpi-label {
  font-size: 11px;
  color: var(--ion-color-medium);
  margin-top: 4px;
}
.kpi-active .kpi-value  { color: var(--ion-color-success); }
.kpi-booked .kpi-value  { color: var(--ion-color-warning); }
.kpi-return .kpi-value  { color: var(--ion-color-danger); }
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 40px 24px;
  color: var(--ion-color-medium);
}
.empty-icon {
  font-size: 52px;
  margin-bottom: 12px;
  opacity: 0.4;
}
</style>
