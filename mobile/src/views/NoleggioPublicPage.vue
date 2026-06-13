<template>
  <ion-page>
    <div class="dealer-header">
      <div class="dealer-bar">
        <div class="dealer-logo-wrap">
          <div class="dealer-logo-icon">
            <img v-if="op.profile?.logoUrl" :src="op.resolveUrl(op.profile.logoUrl) ?? undefined" alt="logo" />
            <ion-icon v-else :icon="keyOutline" style="color:#fff;font-size:20px" />
          </div>
          <div>
            <div class="dealer-name">{{ op.profile?.businessName ?? 'MyCars' }}</div>
            <div class="dealer-sub">Noleggio veicoli</div>
          </div>
        </div>
      </div>

      <!-- Tipo veicolo switcher -->
      <div class="tipo-switcher">
        <button
          v-for="t in tipi" :key="t.value"
          class="tipo-btn"
          :class="activeType === t.value ? 'active' : 'inactive'"
          @click="selectType(t.value)"
        >{{ t.label }}</button>
      </div>

      <!-- Searchbar -->
      <div class="search-row">
        <div class="search-box">
          <ion-icon :icon="searchOutline" />
          <input v-model="searchText" placeholder="Cerca marca, modello…" />
        </div>
      </div>
    </div>

    <!-- Stats bar -->
    <div class="stats-bar">
      <div class="stats-count">
        <span>{{ filteredItems.length }}</span> disponibili a noleggio
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

    <ion-content>
      <ion-refresher slot="fixed" @ionRefresh="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>

      <div v-if="loading" class="loading-row">
        <ion-spinner name="crescent" />
      </div>

      <div
        v-else-if="filteredItems.length > 0"
        class="veicoli-scroll"
        :class="layout === 'grid' ? 'grid-2col' : 'list-col'"
      >
        <VehicleCard
          v-for="v in filteredItems"
          :key="v.id"
          :vehicle="v"
          :layout="layout"
          :rental-mode="true"
          @click="$router.push(`/veicolo/${v.id}?from=noleggio`)"
        />
      </div>

      <div v-else class="empty-state">
        <ion-icon :icon="keyOutline" class="empty-icon" />
        <p>Nessun veicolo disponibile a noleggio al momento.</p>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  IonPage, IonContent, IonIcon, IonSpinner,
  IonRefresher, IonRefresherContent,
} from '@ionic/vue'
import { keyOutline, searchOutline, gridOutline, listOutline } from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'
import VehicleCard from '@/components/VehicleCard.vue'

const op = useOperatorStore()

const items      = ref<any[]>([])
const loading    = ref(false)
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
  if (!q) return items.value
  return items.value.filter((v: any) =>
    v.brandName?.toLowerCase().includes(q) ||
    v.model?.toLowerCase().includes(q) ||
    v.version?.toLowerCase().includes(q)
  )
})

async function load() {
  if (!op.slug) return
  loading.value = true
  try {
    const params = new URLSearchParams({
      forRental:   'true',
      vehicleType: activeType.value,
      pageSize:    '50',
    })
    const res = await fetch(`${op.apiBase}/api/public/${op.slug}/vehicles?${params}`)
    if (!res.ok) throw new Error()
    const data = await res.json()
    items.value = data.items ?? []
  } catch {
    items.value = []
  } finally {
    loading.value = false
  }
}

function selectType(type: string) {
  activeType.value = type
  searchText.value = ''
  load()
}

async function refresh(ev: CustomEvent) {
  await load()
  ;(ev.target as HTMLIonRefresherElement).complete()
}

onMounted(load)
</script>

<style scoped>
.tipo-switcher { display: flex; gap: 7px; margin-bottom: 14px; }
.tipo-btn {
  flex: 1; height: 38px; border: none; cursor: pointer;
  border-radius: var(--mc-r-sm);
  font-family: var(--mc-font-heading); font-size: 10.5px; font-weight: 600;
}
.tipo-btn.active   { background: var(--dealer-secondary); color: #fff; }
.tipo-btn.inactive { background: rgba(255,255,255,.12); color: rgba(255,255,255,.65); }

.search-row { display: flex; }
.search-box {
  flex: 1; height: 42px;
  background: rgba(255,255,255,.14); border: 1.5px solid rgba(255,255,255,.18);
  border-radius: var(--mc-r-sm);
  display: flex; align-items: center; gap: 8px; padding: 0 12px;
}
.search-box ion-icon { color: rgba(255,255,255,.6); font-size: 16px; flex-shrink: 0; }
.search-box input {
  flex: 1; background: transparent; border: none; outline: none;
  color: #fff; font-size: 13.5px;
}
.search-box input::placeholder { color: rgba(255,255,255,.5); }

.stats-bar {
  display: flex; align-items: center; justify-content: space-between;
  padding: 10px 18px 6px; background: var(--mc-surface);
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

.veicoli-scroll { padding: 12px; }
.grid-2col { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
.list-col  { display: flex; flex-direction: column; gap: 10px; }

.loading-row { display: flex; justify-content: center; padding: 48px; }

.empty-state {
  display: flex; flex-direction: column; align-items: center;
  padding: 60px 24px; color: var(--mc-text-light); text-align: center;
}
.empty-icon { font-size: 52px; margin-bottom: 14px; opacity: 0.35; }
.empty-state p { font-size: 14px; line-height: 1.5; max-width: 240px; }
</style>
