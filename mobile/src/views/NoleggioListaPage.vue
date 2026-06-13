<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-back-button default-href="/tabs/noleggio" />
        </ion-buttons>
        <ion-title>Tutti i noleggi</ion-title>
        <ion-buttons slot="end">
          <ion-button router-link="/tabs/noleggio/nuovo">
            <ion-icon :icon="addOutline" slot="icon-only" />
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
      <ion-toolbar>
        <ion-segment :value="activeFilter" @ionChange="onFilterChange">
          <ion-segment-button value="">Tutti</ion-segment-button>
          <ion-segment-button value="booked">Prenotati</ion-segment-button>
          <ion-segment-button value="active">In corso</ion-segment-button>
          <ion-segment-button value="closed">Conclusi</ion-segment-button>
        </ion-segment>
      </ion-toolbar>
    </ion-header>

    <ion-content :fullscreen="true">
      <ion-refresher slot="fixed" @ionRefresh="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>

      <ion-list lines="full" v-if="!loading || items.length > 0">
        <rental-list-item
          v-for="r in items"
          :key="r.id"
          :rental="r"
          @click="goToDetail(r.id)"
        />
      </ion-list>

      <div class="empty-state" v-if="!loading && items.length === 0">
        <ion-icon :icon="keyOutline" class="empty-icon" />
        <p>Nessun noleggio trovato</p>
      </div>

      <ion-infinite-scroll @ionInfinite="loadMore($event)" :disabled="!hasMore">
        <ion-infinite-scroll-content loading-text="Caricamento…" />
      </ion-infinite-scroll>

      <ion-loading :is-open="loading && items.length === 0" message="Caricamento…" />
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonLabel,
  IonButton, IonButtons, IonBackButton, IonIcon, IonSegment, IonSegmentButton,
  IonRefresher, IonRefresherContent, IonInfiniteScroll, IonInfiniteScrollContent,
  IonLoading,
} from '@ionic/vue'
import { addOutline, keyOutline } from 'ionicons/icons'
import { useRentalsStore, type RentalStatus } from '@/stores/rentals'
import RentalListItem from '@/components/RentalListItem.vue'

const router = useRouter()
const store  = useRentalsStore()

const loading      = computed(() => store.loading)
const items        = computed(() => store.items)
const hasMore      = computed(() => store.hasMore)
const activeFilter = ref<RentalStatus | ''>('')

onMounted(() => store.fetchRentals(activeFilter.value))

async function onFilterChange(ev: CustomEvent) {
  activeFilter.value = ev.detail.value
  await store.fetchRentals(activeFilter.value)
}

async function refresh(ev: CustomEvent) {
  await store.fetchRentals(activeFilter.value)
  ;(ev.target as HTMLIonRefresherElement).complete()
}

async function loadMore(ev: CustomEvent) {
  await store.fetchNextPage(activeFilter.value)
  ;(ev.target as HTMLIonInfiniteScrollElement).complete()
}

function goToDetail(id: string) {
  router.push(`/noleggio/${id}`)
}
</script>

<style scoped>
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 60px 24px;
  color: var(--ion-color-medium);
}
.empty-icon { font-size: 52px; margin-bottom: 12px; opacity: 0.4; }
</style>
