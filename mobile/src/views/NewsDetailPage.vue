<template>
  <ion-page>
    <ion-content v-if="store.detail" :fullscreen="true" style="--padding-bottom: calc(var(--ion-tab-bar-height, 56px) + var(--ion-safe-area-bottom, 0px))">

      <!-- Hero — contiene topbar, badge, titolo e share -->
      <div class="news-hero" :style="heroStyle">
        <div class="hero-overlay" />

        <!-- Topbar: back a sinistra, share a destra -->
        <div class="hero-topbar">
          <button class="action-btn" @click="$router.back()">
            <ion-icon :icon="arrowBackOutline" />
          </button>
          <button class="action-btn" @click="shareNews">
            <ion-icon :icon="shareOutline" />
          </button>
        </div>

        <!-- Badge + titolo al fondo dell'hero -->
        <div class="hero-bottom">
          <span class="ncard-type" :class="`ntype-${store.detail.newsType}`">
            {{ newsTypeLabel(store.detail.newsType) }}
          </span>
          <h1 class="hero-title">{{ store.detail.title }}</h1>
        </div>
      </div>

      <!-- Corpo della news -->
      <div style="padding:14px 16px 40px">
        <div v-if="store.detail.publishedAt" style="font-size:12px;color:var(--mc-text-light);margin-bottom:12px">
          {{ fmtDate(store.detail.publishedAt) }}
        </div>
        <div v-if="store.detail.excerpt" style="font-size:14px;color:var(--mc-text-mid);line-height:1.6;margin-bottom:14px;font-weight:500">
          {{ store.detail.excerpt }}
        </div>
        <div
          v-if="store.detail.body"
          class="news-body"
          v-html="bodyHtml"
        />
        <a
          v-if="store.detail.linkUrl"
          :href="store.detail.linkUrl"
          target="_blank"
          class="btn-primary"
          style="margin-top:24px;text-decoration:none"
        >
          Scopri di più
        </a>
      </div>
    </ion-content>

    <div v-else-if="store.loading" style="display:flex;align-items:center;justify-content:center;height:100%">
      <ion-spinner name="crescent" />
    </div>
  </ion-page>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { IonPage, IonContent, IonIcon, IonSpinner } from '@ionic/vue'
import { arrowBackOutline, shareOutline } from 'ionicons/icons'
import { Share } from '@capacitor/share'
import { useNewsStore } from '@/stores/news'
import { useOperatorStore } from '@/stores/operator'

const route = useRoute()
const store = useNewsStore()
const op    = useOperatorStore()

const bodyHtml = computed(() => {
  const body = store.detail?.body
  if (!body) return ''
  const base = op.apiBase
  if (!base) return body
  return body
    .replace(/src="(\/[^"]+)"/g, `src="${base}$1"`)
    .replace(/src='(\/[^']+)'/g, `src='${base}$1'`)
})

const newsGradients: Record<string, string> = {
  nuovo_arrivo:  'linear-gradient(135deg,#700000,#B01020)',
  promo:         'linear-gradient(135deg,#8B1010,#D62828)',
  evento:        'linear-gradient(135deg,#0D3B6E,#2E75B6)',
  finanziamento: 'linear-gradient(135deg,#005C35,#00A86B)',
  service:       'linear-gradient(135deg,#374151,#6B7280)',
  generic:       'linear-gradient(135deg,#152B47,#1E3A5F)',
}
const newsLabels: Record<string, string> = {
  nuovo_arrivo:  '✦ Nuovo Arrivo',
  promo:         '% Promozione',
  evento:        '🗓 Evento',
  finanziamento: '💰 Finanziamento',
  service:       '🔧 Service',
  generic:       'Comunicazione',
}

const heroStyle = computed(() => {
  const type = store.detail?.newsType ?? 'generic'
  if (store.detail?.coverImageUrl)
    return { backgroundImage: `url(${store.detail.coverImageUrl})`, backgroundSize: 'cover', backgroundPosition: 'center' }
  return { background: newsGradients[type] ?? newsGradients.generic }
})

function newsTypeLabel(type: string) { return newsLabels[type] ?? 'Comunicazione' }
function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric' })
}

async function shareNews() {
  const detail = store.detail
  if (!detail) return
  try {
    await Share.share({
      title:  detail.title,
      text:   detail.excerpt ?? detail.title,
      dialogTitle: 'Condividi questa news',
    })
  } catch {
    // L'utente ha annullato o share non disponibile — nessuna azione
  }
}

onMounted(() => store.fetchDetail(route.params.id as string))
</script>

<style scoped>
.news-hero {
  min-height: 260px;
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}
.hero-overlay {
  position: absolute; inset: 0;
  background: linear-gradient(
    to bottom,
    rgba(0,0,0,.45) 0%,
    transparent 35%,
    rgba(0,0,0,.45) 65%,
    rgba(0,0,0,.75) 100%
  );
}

/* Topbar: back sinistra, share destra */
.hero-topbar {
  position: relative; z-index: 20;
  display: flex; justify-content: space-between; align-items: center;
  padding: 50px 14px 0;
}
.action-btn {
  width: 36px; height: 36px;
  background: rgba(0,0,0,.38); backdrop-filter: blur(10px);
  border: 1px solid rgba(255,255,255,.15); border-radius: 50%; cursor: pointer;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.action-btn ion-icon { color: #fff; font-size: 18px; }

/* Badge + titolo sul fondo dell'hero */
.hero-bottom {
  position: relative; z-index: 20;
  padding: 0 16px 18px;
  display: flex; flex-direction: column; gap: 8px;
}
.hero-title {
  font-family: var(--mc-font-heading);
  font-size: 20px;
  font-weight: 800;
  color: #fff;
  line-height: 1.25;
  margin: 0;
  text-shadow: 0 1px 6px rgba(0,0,0,.45);
}

.news-body { font-size: 14px; color: var(--mc-text-mid); line-height: 1.7; }
.news-body p { margin-bottom: 12px; }
.news-body h2, .news-body h3 { font-family: var(--mc-font-heading); color: var(--mc-text); margin: 16px 0 8px; }
.news-body :deep(img) { max-width: 100%; height: auto; display: block; margin: 8px 0; border-radius: 6px; }
</style>
