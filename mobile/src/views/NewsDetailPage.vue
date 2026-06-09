<template>
  <ion-page>
    <ion-content v-if="store.detail" :fullscreen="true">
      <!-- Hero -->
      <div class="news-hero" :style="heroStyle">
        <div class="hero-overlay" />
        <div class="hero-topbar">
          <button class="action-btn" @click="$router.back()">
            <ion-icon :icon="arrowBackOutline" />
          </button>
        </div>
        <div class="hero-bottom">
          <span class="ncard-type" :class="`ntype-${store.detail.newsType}`">
            {{ newsTypeLabel(store.detail.newsType) }}
          </span>
        </div>
      </div>

      <div style="padding:16px 16px 40px">
        <h1 style="font-family:var(--mc-font-heading);font-size:22px;font-weight:800;color:var(--mc-text);line-height:1.2;margin-bottom:8px">
          {{ store.detail.title }}
        </h1>
        <div v-if="store.detail.publishedAt" style="font-size:12px;color:var(--mc-text-light);margin-bottom:16px">
          {{ fmtDate(store.detail.publishedAt) }}
        </div>
        <div v-if="store.detail.excerpt" style="font-size:14px;color:var(--mc-text-mid);line-height:1.6;margin-bottom:16px;font-weight:500">
          {{ store.detail.excerpt }}
        </div>
        <div
          v-if="store.detail.body"
          class="news-body"
          v-html="store.detail.body"
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
import { arrowBackOutline } from 'ionicons/icons'
import { useNewsStore } from '@/stores/news'

const route = useRoute()
const store = useNewsStore()

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

onMounted(() => store.fetchDetail(route.params.id as string))
</script>

<style scoped>
.news-hero {
  height: 220px; position: relative; flex-shrink: 0;
}
.hero-overlay {
  position: absolute; inset: 0;
  background: linear-gradient(to bottom, rgba(0,0,0,.35) 0%, transparent 40%, rgba(0,0,0,.3) 100%);
}
.hero-topbar {
  position: absolute; top: 50px; left: 16px; z-index: 20;
}
.action-btn {
  width: 36px; height: 36px;
  background: rgba(0,0,0,.38); backdrop-filter: blur(10px);
  border: 1px solid rgba(255,255,255,.15); border-radius: 50%; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
}
.action-btn ion-icon { color: #fff; font-size: 18px; }
.hero-bottom {
  position: absolute; bottom: 14px; left: 14px; z-index: 20;
}
.news-body { font-size: 14px; color: var(--mc-text-mid); line-height: 1.7; }
.news-body p { margin-bottom: 12px; }
.news-body h2, .news-body h3 { font-family: var(--mc-font-heading); color: var(--mc-text); margin: 16px 0 8px; }
</style>
