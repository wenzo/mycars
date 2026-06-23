<template>
  <ion-page>
    <ion-content :fullscreen="true" :scroll-y="false">
      <div class="home-bg" :style="bgStyle">

        <!-- Dealer identity -->
        <div class="home-brand">
          <div class="home-logo-wrap">
            <img v-if="logoUrl" :src="logoUrl" class="home-logo-img" alt="logo" />
            <div v-else class="home-logo-fallback">
              <ion-icon :icon="carSportOutline" />
            </div>
          </div>
          <div class="home-name">{{ businessName }}</div>
          <div v-if="tagline" class="home-tagline">{{ tagline }}</div>
        </div>

        <!-- Services card -->
        <div class="home-card">
          <p class="home-card-label">Scegli un servizio</p>
          <div class="home-tiles">

            <button class="htile" @click="router.push('/tabs/vetrina')">
              <div class="htile-icon" :style="{ background: primaryColor }">
                <ion-icon :icon="carSportOutline" />
              </div>
              <span class="htile-name">Vetrina</span>
              <span class="htile-desc">Auto in vendita</span>
            </button>

            <button v-if="op.profile?.rentalModuleEnabled" class="htile" @click="router.push('/tabs/noleggio')">
              <div class="htile-icon" :style="{ background: secondaryColor }">
                <ion-icon :icon="keyOutline" />
              </div>
              <span class="htile-name">Noleggio</span>
              <span class="htile-desc">Soluzioni di affitto</span>
            </button>

          </div>
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

const logoUrl        = computed(() => op.resolveUrl(op.profile?.logoUrl))
const businessName   = computed(() => op.profile?.businessName ?? 'MyCars')
const tagline        = computed(() => op.profile?.tagline ?? null)
const primaryColor   = computed(() => op.profile?.primaryColor   ?? '#1E3A5F')
const secondaryColor = computed(() => op.profile?.secondaryColor ?? '#2E75B6')

const bgStyle = computed(() => {
  const p = op.profile?.primaryColor   ?? '#1E3A5F'
  const s = op.profile?.secondaryColor ?? '#2E75B6'
  return { background: `linear-gradient(150deg, ${p} 0%, ${s} 100%)` }
})
</script>

<style scoped>
.home-bg {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 32px;
  padding: 40px 24px max(32px, calc(env(safe-area-inset-bottom) + 24px));
}

/* ── Dealer brand ─────────────────────────────────────────────────────────── */
.home-brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
}

.home-logo-wrap { margin-bottom: 4px; }

.home-logo-img {
  width: 88px;
  height: 88px;
  object-fit: contain;
  border-radius: 20px;
  background: rgba(255,255,255,.15);
  padding: 8px;
  box-shadow: 0 6px 24px rgba(0,0,0,.25);
}

.home-logo-fallback {
  width: 88px;
  height: 88px;
  border-radius: 20px;
  background: rgba(255,255,255,.15);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 6px 24px rgba(0,0,0,.2);
}
.home-logo-fallback ion-icon {
  font-size: 44px;
  color: #fff;
}

.home-name {
  font-family: var(--mc-font-heading);
  font-size: 24px;
  font-weight: 800;
  color: #fff;
  text-align: center;
  letter-spacing: -0.3px;
  text-shadow: 0 2px 8px rgba(0,0,0,.2);
}

.home-tagline {
  font-size: 13px;
  font-style: italic;
  color: rgba(255,255,255,.72);
  text-align: center;
  line-height: 1.5;
  max-width: 240px;
}

/* ── Services card ────────────────────────────────────────────────────────── */
.home-card {
  width: 100%;
  max-width: 360px;
  background: rgba(255,255,255,.96);
  border-radius: 24px;
  padding: 20px 16px;
  box-shadow: 0 16px 48px rgba(0,0,0,.28);
}

.home-card-label {
  font-family: var(--mc-font-heading);
  font-size: 11px;
  font-weight: 600;
  color: rgba(0,0,0,.38);
  text-transform: uppercase;
  letter-spacing: .08em;
  text-align: center;
  margin: 0 0 14px;
}

.home-tiles {
  display: flex;
  gap: 10px;
}

/* ── Single tile ──────────────────────────────────────────────────────────── */
.htile {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  padding: 24px 12px 22px;
  border: none;
  background: var(--mc-surface, #f5f6f8);
  border-radius: 18px;
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  transition: transform 0.14s ease, box-shadow 0.14s ease;
}
.htile:active {
  transform: scale(0.94);
}

.htile-icon {
  width: 72px;
  height: 72px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 16px rgba(0,0,0,.22);
}
.htile-icon ion-icon {
  font-size: 36px;
  color: #fff;
}

.htile-name {
  font-family: var(--mc-font-heading);
  font-size: 15px;
  font-weight: 800;
  color: var(--mc-text, #1a1a2e);
}

.htile-desc {
  font-size: 11px;
  color: var(--mc-text-mid, #777);
  text-align: center;
  line-height: 1.35;
}
</style>
