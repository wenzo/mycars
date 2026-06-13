<template>
  <ion-page>
    <ion-content :fullscreen="true" :scroll-y="false">
      <div class="home-wrap">

        <div class="tile" :style="{ background: primaryColor }" @click="router.push('/tabs/vetrina')">
          <ion-icon :icon="carSportOutline" class="tile-icon" />
          <span class="tile-label">VETRINA</span>
        </div>

        <div class="tile" :style="{ background: secondaryColor }" @click="router.push('/tabs/noleggio')">
          <ion-icon :icon="keyOutline" class="tile-icon" />
          <span class="tile-label">NOLEGGIO</span>
        </div>

      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { IonPage, IonContent, IonIcon } from '@ionic/vue'
import { carSportOutline, keyOutline } from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'

const router = useRouter()
const op     = useOperatorStore()

const primaryColor   = computed(() => op.profile?.primaryColor   ?? '#1E3A5F')
const secondaryColor = computed(() => op.profile?.secondaryColor ?? '#2E75B6')
</script>

<style scoped>
.home-wrap {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.tile {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 20px;
  cursor: pointer;
  transition: filter 0.15s ease;
  -webkit-tap-highlight-color: transparent;
}
.tile:active {
  filter: brightness(0.82);
}

.tile-icon {
  font-size: 80px;
  color: rgba(255, 255, 255, 0.88);
}

.tile-label {
  font-size: 26px;
  font-weight: 800;
  color: #fff;
  letter-spacing: 4px;
}
</style>
