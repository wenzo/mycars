<template>
  <ion-page>
    <div class="dealer-header">
      <div class="dealer-bar">
        <div class="dealer-logo-wrap">
          <div class="dealer-logo-icon">
            <img v-if="op.profile?.logoUrl" :src="op.resolveUrl(op.profile.logoUrl)" alt="logo" />
            <ion-icon v-else :icon="carOutline" style="color:#fff;font-size:20px" />
          </div>
          <div>
            <div class="dealer-name">{{ op.profile?.businessName ?? 'MyCars' }}</div>
            <div class="dealer-sub">Vetrina veicoli</div>
          </div>
        </div>
        <div style="display:flex;gap:8px">
          <button class="header-icon-btn" @click="goToProfilo">
            <ion-icon :icon="personCircleOutline" />
          </button>
        </div>
      </div>

      <!-- Tipo veicolo switcher -->
      <div class="tipo-switcher">
        <button
          v-for="t in tipi" :key="t.value"
          class="tipo-btn"
          :class="activeType === t.value ? 'active' : 'inactive'"
          @click="selectType(t.value)"
        >
          {{ t.label }}
        </button>
      </div>

      <!-- Searchbar -->
      <div class="search-row">
        <div class="search-box">
          <ion-icon :icon="searchOutline" />
          <input v-model="searchText" placeholder="Cerca marca, modello..." />
        </div>
        <button class="search-adv-btn" @click="$router.push('/tabs/ricerca')">
          <ion-icon :icon="funnelOutline" />
          Filtra
        </button>
      </div>
    </div>

    <!-- Stats bar -->
    <div class="stats-bar">
      <div class="stats-count">
        <span>{{ displayCount }}</span> veicoli
      </div>
      <div class="layout-toggle">
        <button class="ltbtn" :class="{ active: layout === 'grid' }" @click="layout = 'grid'">
          <ion-icon :icon="gridOutline" />
        </button>
        <button class="ltbtn" :class="{ active: layout === 'list' }" @click="layout = 'list'">
          <ion-icon :icon="listOutline" />
        </button>
      </div>
    </div>

    <ion-content style="--padding-bottom: calc(var(--ion-tab-bar-height, 56px) + var(--ion-safe-area-bottom, 0px))">
      <div
        class="veicoli-scroll"
        :class="layout === 'grid' ? 'grid-2col' : 'list-col'"
      >
        <VehicleCard
          v-for="v in filteredItems"
          :key="v.id"
          :vehicle="v"
          :layout="layout"
          @click="$router.push(`/tabs/veicolo/${v.id}`)"
        />

        <div v-if="store.loading" class="loading-row">
          <ion-spinner name="crescent" />
        </div>

        <ion-infinite-scroll @ionInfinite="onInfinite">
          <ion-infinite-scroll-content />
        </ion-infinite-scroll>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import {
  IonPage, IonContent, IonIcon, IonSpinner,
  IonInfiniteScroll, IonInfiniteScrollContent,
} from '@ionic/vue'
import {
  carOutline, searchOutline, funnelOutline,
  gridOutline, listOutline, personCircleOutline,
} from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'
import { useVehicleStore } from '@/stores/vehicles'
import VehicleCard from '@/components/VehicleCard.vue'

const router = useRouter()
const op     = useOperatorStore()
const store  = useVehicleStore()

function goToProfilo() {
  router.push('/tabs/profilo')
}

const searchText = ref('')
const layout     = ref<'grid' | 'list'>('grid')
const activeType = ref('autovettura')

const tipi = [
  { value: 'autovettura', label: 'Auto' },
  { value: 'motoveicolo', label: 'Moto' },
  { value: 'autocarro',   label: 'Truck' },
  { value: 'autocaravan', label: 'Caravan' },
]

const filteredItems = computed(() => {
  const q = searchText.value.trim().toLowerCase()
  if (!q) return store.items
  return store.items.filter(v =>
    v.brandName?.toLowerCase().includes(q) ||
    v.model?.toLowerCase().includes(q) ||
    v.version?.toLowerCase().includes(q)
  )
})

const displayCount = computed(() => filteredItems.value.length || store.items.length)

function selectType(type: string) {
  activeType.value = type
  searchText.value = ''
  store.applyFilters({ ...store.filters, vehicleType: type })
}

let searchTimer: ReturnType<typeof setTimeout>
watch(searchText, (q) => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    store.applyFilters({ vehicleType: activeType.value, search: q.trim() || undefined })
  }, 300)
})

async function onInfinite(ev: CustomEvent) {
  await store.fetchNextPage()
  ;(ev.target as HTMLIonInfiniteScrollElement).complete()
}

onMounted(() => {
  if (!store.initialized) {
    store.applyFilters({ vehicleType: activeType.value })
  }
})
</script>

<style scoped>
.tipo-switcher {
  display: flex; gap: 7px; margin-bottom: 14px;
}
.tipo-btn {
  flex: 1; height: 38px; border: none; cursor: pointer;
  border-radius: var(--mc-r-sm);
  font-family: var(--mc-font-heading);
  font-size: 10.5px; font-weight: 600;
}
.tipo-btn.active   { background: rgba(255,255,255,.92); color: var(--dealer-primary); font-weight: 700; }
.tipo-btn.inactive { background: rgba(255,255,255,.12); color: rgba(255,255,255,.65); }

.search-row { display: flex; gap: 8px; align-items: center; }
.search-box {
  flex: 1; height: 42px;
  background: rgba(255,255,255,.14);
  border: 1.5px solid rgba(255,255,255,.18);
  border-radius: var(--mc-r-sm);
  display: flex; align-items: center; gap: 8px; padding: 0 12px;
}
.search-box ion-icon { color: rgba(255,255,255,.6); font-size: 16px; flex-shrink: 0; }
.search-box input {
  flex: 1; background: transparent; border: none; outline: none;
  color: #fff; font-size: 13.5px;
}
.search-box input::placeholder { color: rgba(255,255,255,.5); }
.search-adv-btn {
  height: 42px; padding: 0 14px;
  background: var(--dealer-secondary); border: none;
  border-radius: var(--mc-r-sm); cursor: pointer;
  font-family: var(--mc-font-heading); font-size: 12px; font-weight: 600;
  color: #fff; display: flex; align-items: center; gap: 5px;
}

.stats-bar {
  display: flex; align-items: center; justify-content: space-between;
  padding: 10px 18px 10px; background: var(--mc-surface);
}
.stats-count { font-family: var(--mc-font-heading); font-size: 13px; font-weight: 600; color: var(--mc-text-mid); }
.stats-count span { color: var(--dealer-primary); font-weight: 700; }
.layout-toggle { display: flex; gap: 4px; }
.ltbtn {
  width: 30px; height: 30px; border: none; cursor: pointer;
  border-radius: 8px; background: transparent;
  display: flex; align-items: center; justify-content: center;
}
.ltbtn.active { background: var(--dealer-primary); }
.ltbtn.active ion-icon { color: #fff; }
.ltbtn ion-icon { color: var(--mc-text-light); font-size: 16px; }

.veicoli-scroll { padding: 10px 12px 12px; }
.grid-2col { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
.list-col   { display: flex; flex-direction: column; gap: 10px; }

.loading-row { display: flex; justify-content: center; padding: 20px; }
</style>
