<template>
  <ion-page>
    <div class="ricerca-header">
      <div style="display:flex;align-items:center;gap:10px;margin-bottom:12px">
        <button class="back-btn" @click="$router.back()">
          <ion-icon :icon="arrowBackOutline" />
        </button>
        <div>
          <div style="font-family:var(--mc-font-heading);font-size:18px;font-weight:700;color:#fff">Ricerca Avanzata</div>
          <div style="font-size:12px;color:rgba(255,255,255,.55)">Filtra il catalogo</div>
        </div>
      </div>
    </div>

    <ion-content>
      <div style="padding:14px 16px 0;display:flex;flex-direction:column;gap:6px">

        <!-- Condizione -->
        <div class="filter-section">
          <div class="frow" @click="openSheet('condition')">
            <div class="frow-icon"><ion-icon :icon="starOutline" /></div>
            <span class="frow-label">Stato</span>
            <div class="frow-value" :class="{ selected: local.condition }">
              {{ local.condition ?? 'Qualunque' }} <ion-icon :icon="chevronForwardOutline" />
            </div>
          </div>
          <div class="frow" @click="openSheet('fuel')">
            <div class="frow-icon"><ion-icon :icon="flashOutline" /></div>
            <span class="frow-label">Alimentazione</span>
            <div class="frow-value" :class="{ selected: local.fuel }">
              {{ local.fuel ?? 'Qualunque' }} <ion-icon :icon="chevronForwardOutline" />
            </div>
          </div>
          <div class="frow" @click="openSheet('transmission')">
            <div class="frow-icon"><ion-icon :icon="gitNetworkOutline" /></div>
            <span class="frow-label">Cambio</span>
            <div class="frow-value" :class="{ selected: local.transmission }">
              {{ local.transmission ?? 'Qualunque' }} <ion-icon :icon="chevronForwardOutline" />
            </div>
          </div>
        </div>

        <!-- Max prezzo -->
        <div class="filter-section">
          <div class="frow-slider">
            <div class="frow-top">
              <div class="frow-icon"><ion-icon :icon="cashOutline" /></div>
              <span class="frow-label">Max prezzo</span>
              <div class="slider-val">{{ local.maxPrice ? `€ ${fmtN(local.maxPrice)}` : 'Qualunque' }}</div>
            </div>
            <input type="range" min="0" max="120000" step="1000" :value="local.maxPrice ?? 120000"
              @input="(e) => local.maxPrice = +(e.target as HTMLInputElement).value || undefined"
              class="mc-range" />
          </div>

          <!-- Max km -->
          <div class="frow-slider">
            <div class="frow-top">
              <div class="frow-icon"><ion-icon :icon="speedometerOutline" /></div>
              <span class="frow-label">Max km</span>
              <div class="slider-val">{{ local.maxMileageKm ? fmtN(local.maxMileageKm) : 'Qualunque' }}</div>
            </div>
            <input type="range" min="0" max="300000" step="5000" :value="local.maxMileageKm ?? 300000"
              @input="(e) => local.maxMileageKm = +(e.target as HTMLInputElement).value || undefined"
              class="mc-range" />
          </div>
        </div>

        <!-- Toggle -->
        <div class="filter-section">
          <div class="frow">
            <div class="frow-icon green"><ion-icon :icon="arrowForwardOutline" /></div>
            <span class="frow-label">Pronta consegna</span>
            <button class="mc-toggle" :class="{ on: local.prontaConsegna }" @click="local.prontaConsegna = !local.prontaConsegna" />
          </div>
          <div class="frow" style="border-bottom:none">
            <div class="frow-icon amber"><ion-icon :icon="starOutline" /></div>
            <span class="frow-label">Solo nuovi arrivi</span>
            <button class="mc-toggle" :class="{ on: local.isNuovoArrivo }" @click="local.isNuovoArrivo = !local.isNuovoArrivo" />
          </div>
        </div>

        <div style="height:8px" />
      </div>
    </ion-content>

    <!-- CTA -->
    <div class="cta-bar">
      <button class="btn-reset" @click="resetLocal">Azzera</button>
      <button class="btn-primary" @click="applyAndBack">
        <ion-icon :icon="searchOutline" /> Cerca veicoli
      </button>
    </div>

    <!-- Bottom sheet -->
    <div class="sheet-overlay" :class="{ show: sheetOpen }" @click.self="sheetOpen = false">
      <div class="sheet">
        <div class="sheet-handle" />
        <div class="sheet-title">{{ sheetTitle }}</div>
        <div
          v-for="opt in sheetOptions"
          :key="opt"
          class="sheet-option"
          :class="{ selected: isSelected(opt) }"
          @click="selectOption(opt)"
        >
          <span>{{ opt }}</span>
          <div class="check" />
        </div>
      </div>
    </div>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { IonPage, IonContent, IonIcon } from '@ionic/vue'
import {
  arrowBackOutline, starOutline, flashOutline, gitNetworkOutline,
  cashOutline, speedometerOutline, arrowForwardOutline,
  searchOutline, chevronForwardOutline,
} from 'ionicons/icons'
import { useVehicleStore } from '@/stores/vehicles'
import type { VehicleFilters } from '@/stores/vehicles'

const router = useRouter()
const store  = useVehicleStore()

const local = ref<VehicleFilters & { transmission?: string }>({ ...store.filters })

const sheetOpen    = ref(false)
const currentField = ref<string>('')
const sheetTitle   = computed(() => ({
  condition:    'Stato veicolo',
  fuel:         'Alimentazione',
  transmission: 'Cambio',
}[currentField.value] ?? ''))

const sheetOptionsMap: Record<string, string[]> = {
  condition:    ['Qualunque', 'usato', 'nuovo', 'km0'],
  fuel:         ['Qualunque', 'Benzina', 'Diesel', 'Ibrida', 'Elettrica', 'GPL', 'Metano'],
  transmission: ['Qualunque', 'Manuale', 'Automatico'],
}
const sheetOptions = computed(() => sheetOptionsMap[currentField.value] ?? [])

function openSheet(field: string) { currentField.value = field; sheetOpen.value = true }
function isSelected(opt: string) {
  const v = (local.value as any)[currentField.value]
  return opt === 'Qualunque' ? !v : v === opt
}
function selectOption(opt: string) {
  (local.value as any)[currentField.value] = opt === 'Qualunque' ? undefined : opt
  sheetOpen.value = false
}
function resetLocal() { local.value = {} }
function applyAndBack() {
  store.applyFilters(local.value)
  router.back()
}
function fmtN(v: number) { return new Intl.NumberFormat('it-IT').format(v) }
</script>

<style scoped>
.ricerca-header { background: var(--dealer-primary); padding: 10px 20px 18px; flex-shrink: 0; }
.back-btn {
  width: 34px; height: 34px; border-radius: 50%;
  background: rgba(255,255,255,.14); border: none; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
}
.back-btn ion-icon { color: #fff; font-size: 18px; }

.filter-section { background: #fff; border-radius: var(--mc-r); padding: 4px 0; box-shadow: var(--mc-shadow-sm); }
.frow {
  display: flex; align-items: center; padding: 13px 16px;
  border-bottom: 1px solid var(--mc-surface); cursor: pointer; gap: 12px;
}
.frow:last-child { border-bottom: none; }
.frow-label { font-size: 14px; font-weight: 500; color: var(--mc-text); flex: 1; }
.frow-value {
  font-size: 13px; color: var(--mc-text-light);
  display: flex; align-items: center; gap: 6px;
}
.frow-value.selected { color: var(--dealer-primary); font-weight: 600; }
.frow-icon {
  width: 28px; height: 28px; border-radius: 8px;
  background: var(--mc-surface2); display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.frow-icon ion-icon { font-size: 14px; color: var(--mc-blue); }
.frow-icon.green ion-icon { color: var(--mc-green); }
.frow-icon.amber ion-icon { color: var(--mc-amber); }

.frow-slider { padding: 13px 16px; display: flex; flex-direction: column; gap: 10px; border-bottom: 1px solid var(--mc-surface); }
.frow-slider:last-child { border-bottom: none; }
.frow-top { display: flex; align-items: center; gap: 12px; width: 100%; }
.slider-val { font-family: var(--mc-font-heading); font-size: 12px; font-weight: 700; color: var(--dealer-primary); min-width: 80px; text-align: right; }
.mc-range { width: 100%; accent-color: var(--dealer-primary); }

.cta-bar { padding: 12px 16px calc(12px + env(safe-area-inset-bottom, 0px)); background: #fff; border-top: 1px solid var(--mc-border); display: flex; gap: 10px; }
.btn-reset {
  height: 48px; padding: 0 18px; border: 2px solid var(--mc-border);
  background: transparent; border-radius: var(--mc-r-sm); cursor: pointer;
  font-family: var(--mc-font-heading); font-size: 14px; font-weight: 600; color: var(--mc-text-mid);
}

.sheet-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.45); z-index: 100;
  display: flex; align-items: flex-end;
  opacity: 0; pointer-events: none; transition: opacity .3s;
}
.sheet-overlay.show { opacity: 1; pointer-events: all; }
.sheet {
  background: #fff; border-radius: 24px 24px 0 0; width: 100%; padding: 0 0 20px;
  transform: translateY(100%); transition: transform .35s cubic-bezier(.4,0,.2,1);
}
.sheet-overlay.show .sheet { transform: translateY(0); }
.sheet-handle { width: 36px; height: 4px; background: var(--mc-border); border-radius: 2px; margin: 12px auto 8px; }
.sheet-title { font-family: var(--mc-font-heading); font-size: 16px; font-weight: 700; color: var(--mc-text); padding: 4px 20px 12px; border-bottom: 1px solid var(--mc-surface); }
.sheet-option { display: flex; align-items: center; justify-content: space-between; padding: 14px 20px; cursor: pointer; }
.sheet-option span { font-size: 14px; color: var(--mc-text); font-weight: 500; }
.check { width: 20px; height: 20px; border-radius: 50%; border: 2px solid var(--mc-border); display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.sheet-option.selected .check { background: var(--dealer-primary); border-color: var(--dealer-primary); }
.sheet-option.selected .check::after { content: ''; width: 6px; height: 10px; border: 2px solid #fff; border-top: none; border-left: none; transform: rotate(45deg) translateY(-1px); display: block; }
.sheet-option.selected span { color: var(--dealer-primary); font-weight: 700; }
</style>
