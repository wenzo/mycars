<template>
  <ion-item button :detail="true">
    <ion-label>
      <h2>{{ rental.customerName }}</h2>
      <p>{{ rental.vehicleBrand }} {{ rental.vehicleModel }}
        <span v-if="rental.vehicleTarga"> · {{ rental.vehicleTarga }}</span>
      </p>
      <p class="dates">{{ formatDate(rental.startDate) }} → {{ formatDate(rental.plannedEndDate) }}</p>
    </ion-label>
    <ion-badge slot="end" :color="STATUS_COLORS[rental.status as RentalStatus]">
      {{ STATUS_LABELS[rental.status as RentalStatus] }}
    </ion-badge>
  </ion-item>
</template>

<script setup lang="ts">
import { IonItem, IonLabel, IonBadge } from '@ionic/vue'
import { type Rental, type RentalStatus, STATUS_LABELS, STATUS_COLORS } from '@/stores/rentals'

defineProps<{ rental: Rental }>()

function formatDate(d: string) {
  if (!d) return ''
  const [y, m, dd] = d.split('-')
  return `${dd}/${m}/${y}`
}
</script>

<style scoped>
.dates { font-size: 12px; color: var(--ion-color-medium); }
</style>
