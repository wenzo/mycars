<template>
  <div class="vcard" :class="layout === 'list' ? 'vcard-list' : ''">
    <div class="vcard-img">
      <img v-if="vehicle.coverImageUrl" :src="op.resolveUrl(vehicle.coverImageUrl)!" :alt="vehicle.model" />
      <div v-else class="vcard-placeholder">
        <ion-icon :icon="carOutline" />
      </div>
      <span class="vcard-badge" :class="conditionBadgeClass">{{ conditionLabel }}</span>
      <span v-if="vehicle.isNuovoArrivo" class="vcard-badge vcard-badge-right badge-nuovo-arrivo">
        ✦ Arrivo
      </span>
    </div>
    <div class="vcard-body">
      <div class="vcard-brand">{{ vehicle.brandName }}</div>
      <div class="vcard-model">{{ vehicle.model }}<span v-if="vehicle.version"> {{ vehicle.version }}</span></div>
      <div class="vcard-specs">
        <span v-if="vehicle.fuel"             class="vcard-spec">{{ vehicle.fuel }}</span>
        <span v-if="vehicle.mileageKm != null" class="vcard-spec">{{ fmtKm(vehicle.mileageKm) }} km</span>
        <span v-if="vehicle.registrationYear"  class="vcard-spec">{{ vehicle.registrationYear }}</span>
        <span v-if="vehicle.prontaConsegna"    class="vcard-spec vcard-spec-pc">⚡ P.C.</span>
      </div>
      <!-- Modalità noleggio: mostra prezzo/giorno -->
      <template v-if="rentalMode">
        <div v-if="vehicle.rentalPrice" class="vcard-price vcard-price-rental">
          da € {{ dailyPrice }}<span class="vcard-price-label">/giorno</span>
        </div>
        <div v-else class="vcard-price" style="color:var(--mc-text-light);font-size:13px">
          Prezzo su richiesta
        </div>
      </template>
      <!-- Modalità vendita: mostra prezzo acquisto -->
      <template v-else>
        <div v-if="vehicle.price" class="vcard-price">
          € {{ fmtPrice(vehicle.price) }}<span class="vcard-price-label">{{ vehicle.vatDeductible ? ' IVA esp.' : ' IVA inc.' }}</span>
        </div>
        <div v-if="vehicle.forRental && vehicle.rentalPrice" class="vcard-rental">
          <span class="vcard-spec vcard-spec-rental">🔑 Noleggio € {{ fmtPrice(vehicle.rentalPrice) }}/mese</span>
        </div>
        <div v-if="!vehicle.price && !(vehicle.forRental && vehicle.rentalPrice)" class="vcard-price" style="color:var(--mc-text-light);font-size:13px">
          Prezzo su richiesta
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'


import { IonIcon } from '@ionic/vue'
import { carOutline } from 'ionicons/icons'
import type { VehicleCard } from '@/stores/vehicles'
import { useOperatorStore } from '@/stores/operator'

const op = useOperatorStore()

const props = defineProps<{
  vehicle: VehicleCard
  layout?: 'grid' | 'list'
  rentalMode?: boolean
}>()

const dailyPrice = computed(() => {
  if (!props.vehicle.rentalPrice) return '—'
  return new Intl.NumberFormat('it-IT').format(Math.round(props.vehicle.rentalPrice / 30))
})

const conditionBadgeClass = computed(() => {
  switch (props.vehicle.condition) {
    case 'nuovo':        return 'badge-nuovo'
    case 'km_0':         return 'badge-km0'
    case 'conto_vendita':return 'badge-conto'
    case 'epoca':        return 'badge-epoca'
    default:             return 'badge-usato'
  }
})
const conditionLabel = computed(() => {
  switch (props.vehicle.condition) {
    case 'nuovo':        return 'Nuovo'
    case 'km_0':         return 'KM 0'
    case 'conto_vendita':return 'C. Vendita'
    case 'epoca':        return 'Epoca'
    default:             return 'Usato'
  }
})

function fmtKm(v: number)    { return new Intl.NumberFormat('it-IT').format(v) }
function fmtPrice(v: number) { return new Intl.NumberFormat('it-IT').format(v) }
</script>

<style scoped>
.vcard-list {
  display: flex;
  flex-direction: row;
}
.vcard-list .vcard-img {
  width: 130px;
  height: unset;       /* clear fixed 110px from app.css; flex stretch sets the height */
  align-self: stretch;
  flex-shrink: 0;
}
.vcard-list .vcard-body {
  flex: 1;
}
.vcard-price-rental {
  color: var(--dealer-primary);
}
.vcard-placeholder {
  width: 100%; height: 100%;
  display: flex; align-items: center; justify-content: center;
  background: var(--mc-surface2);
}
.vcard-placeholder ion-icon {
  font-size: 40px; color: var(--mc-border);
}
</style>
