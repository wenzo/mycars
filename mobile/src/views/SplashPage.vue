<template>
  <ion-page>
    <ion-content :fullscreen="true" :scroll-y="false">
      <div class="splash" :class="{ ready: visible }" :style="bgStyle">

        <!-- Logo o icona generica -->
        <div class="splash-logo-wrap">
          <img v-if="logoUrl" :src="logoUrl" class="splash-logo-img" alt="logo" />
          <div v-else class="splash-logo-fallback">
            <ion-icon :icon="carSportOutline" />
          </div>
        </div>

        <!-- Nome azienda -->
        <div class="splash-name">{{ businessName }}</div>

        <!-- Slogan (solo se presente) -->
        <div v-if="tagline" class="splash-tagline">"{{ tagline }}"</div>

        <!-- Loader puntini -->
        <div class="splash-dots">
          <span /><span /><span />
        </div>

      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { IonPage, IonContent, IonIcon, toastController } from '@ionic/vue'
import { carSportOutline } from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'

const router = useRouter()
const op     = useOperatorStore()
const visible = ref(false)

const logoUrl     = computed(() => op.resolveUrl(op.profile?.logoUrl))
const businessName = computed(() => op.profile?.businessName ?? 'MyCars')
const tagline     = computed(() => op.profile?.tagline ?? null)

const bgStyle = computed(() => {
  const primary   = op.profile?.primaryColor   ?? '#1E3A5F'
  const secondary = op.profile?.secondaryColor ?? '#2E75B6'
  return {
    background: `linear-gradient(150deg, ${primary} 0%, ${secondary} 100%)`,
  }
})

onMounted(async () => {
  // Piccolo delay per triggherare l'animazione di entrata
  requestAnimationFrame(() => { visible.value = true })

  await new Promise(r => setTimeout(r, 2200))

  if (op.isConnected) {
    router.replace('/home')
  } else {
    const toast = await toastController.create({
      message: 'App non ancora associata a nessun operatore. Inserisci il codice del tuo concessionario.',
      duration: 4500,
      position: 'top',
      color: 'warning',
      cssClass: 'splash-toast',
      buttons: [{ icon: 'close', role: 'cancel' }],
    })
    router.replace('/tabs/impostazioni')
    await toast.present()
  }
})
</script>

<style scoped>
.splash {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0;
  padding: 40px 32px;
  opacity: 0;
  transform: scale(0.96);
  transition: opacity 0.45s ease, transform 0.45s ease;
}
.splash.ready {
  opacity: 1;
  transform: scale(1);
}

/* Logo */
.splash-logo-wrap {
  margin-bottom: 28px;
}
.splash-logo-img {
  width: 110px;
  height: 110px;
  object-fit: contain;
  border-radius: 24px;
  background: rgba(255,255,255,.12);
  padding: 10px;
  box-shadow: 0 8px 32px rgba(0,0,0,.25);
}
.splash-logo-fallback {
  width: 110px;
  height: 110px;
  border-radius: 24px;
  background: rgba(255,255,255,.15);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 32px rgba(0,0,0,.25);
}
.splash-logo-fallback ion-icon {
  font-size: 54px;
  color: #fff;
}

/* Nome */
.splash-name {
  font-family: var(--mc-font-heading);
  font-size: 28px;
  font-weight: 800;
  color: #fff;
  text-align: center;
  letter-spacing: -0.3px;
  text-shadow: 0 2px 8px rgba(0,0,0,.2);
  margin-bottom: 10px;
}

/* Slogan */
.splash-tagline {
  font-size: 14px;
  font-style: italic;
  color: rgba(255,255,255,.75);
  text-align: center;
  line-height: 1.5;
  max-width: 260px;
  margin-bottom: 0;
}

/* Loader puntini */
.splash-dots {
  position: absolute;
  bottom: 56px;
  display: flex;
  gap: 8px;
}
.splash-dots span {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: rgba(255,255,255,.5);
  animation: dot-pulse 1.2s ease-in-out infinite;
}
.splash-dots span:nth-child(2) { animation-delay: 0.2s; }
.splash-dots span:nth-child(3) { animation-delay: 0.4s; }

@keyframes dot-pulse {
  0%, 80%, 100% { opacity: 0.3; transform: scale(0.8); }
  40%           { opacity: 1;   transform: scale(1.1); }
}
</style>
