<template>
  <ion-page>
    <div class="dealer-header">
      <div class="dealer-bar">
        <div class="dealer-logo-wrap">
          <div class="dealer-logo-icon">
            <img v-if="op.profile?.logoUrl" :src="op.profile.logoUrl" alt="logo" />
            <ion-icon v-else :icon="carOutline" style="color:#fff;font-size:20px" />
          </div>
          <div>
            <div class="dealer-name">{{ op.profile?.businessName ?? 'MyCars' }}</div>
            <div class="dealer-sub">News &amp; Comunicazioni</div>
          </div>
        </div>
      </div>

      <!-- Filtri tipo news -->
      <div class="news-filter-row">
        <button
          v-for="f in filters" :key="f.value ?? 'all'"
          class="nfbtn"
          :class="store.activeType === f.value ? 'active' : 'inactive'"
          @click="store.setType(f.value)"
        >{{ f.label }}</button>
      </div>
    </div>

    <ion-content>
      <div class="news-scroll">
        <div
          v-for="item in store.items"
          :key="item.id"
          class="ncard"
          @click="$router.push(`/news/${item.id}`)"
        >
          <div class="ncard-img" :style="newsImgStyle(item.newsType)">
            <img v-if="item.coverImageUrl" :src="item.coverImageUrl" :alt="item.title" style="width:100%;height:100%;object-fit:cover" />
            <span class="ncard-type" :class="`ntype-${item.newsType}`">{{ newsTypeLabel(item.newsType) }}</span>
          </div>
          <div class="ncard-body">
            <div class="ncard-title">{{ item.title }}</div>
            <div v-if="item.excerpt" class="ncard-excerpt">{{ item.excerpt }}</div>
            <div class="ncard-footer">
              <span class="ncard-date">{{ fmtDate(item.publishedAt) }}</span>
              <span class="ncard-more">Scopri <ion-icon :icon="arrowForwardOutline" /></span>
            </div>
          </div>
        </div>

        <div v-if="store.loading && store.items.length === 0" class="loading-center">
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
import { onMounted } from 'vue'
import {
  IonPage, IonContent, IonIcon, IonSpinner,
  IonInfiniteScroll, IonInfiniteScrollContent,
} from '@ionic/vue'
import { carOutline, arrowForwardOutline } from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'
import { useNewsStore } from '@/stores/news'

const op    = useOperatorStore()
const store = useNewsStore()

const filters = [
  { value: null,             label: 'Tutte' },
  { value: 'nuovo_arrivo',   label: '✦ Nuovi Arrivi' },
  { value: 'promo',          label: '% Promozioni' },
  { value: 'evento',         label: '🗓 Eventi' },
  { value: 'finanziamento',  label: '💰 Finanziamenti' },
  { value: 'service',        label: '🔧 Service' },
]

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

function newsImgStyle(type: string) {
  return { background: newsGradients[type] ?? newsGradients.generic }
}
function newsTypeLabel(type: string) {
  return newsLabels[type] ?? 'Comunicazione'
}
function fmtDate(iso: string | null) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('it-IT', { day: 'numeric', month: 'short', year: 'numeric' })
}
async function onInfinite(ev: CustomEvent) {
  await store.fetchNextPage()
  ;(ev.target as HTMLIonInfiniteScrollElement).complete()
}

onMounted(() => { if (store.items.length === 0) store.fetchNews(true) })
</script>

<style scoped>
.news-filter-row {
  display: flex; gap: 7px; overflow-x: auto; padding-bottom: 2px;
}
.nfbtn {
  height: 32px; padding: 0 13px; border: none; cursor: pointer;
  border-radius: var(--mc-r-pill);
  font-family: var(--mc-font-heading); font-size: 12px; font-weight: 600;
  white-space: nowrap; flex-shrink: 0;
}
.nfbtn.active   { background: #fff; color: var(--dealer-primary); }
.nfbtn.inactive { background: rgba(255,255,255,.14); color: rgba(255,255,255,.7); }

.news-scroll { padding: 14px 16px 12px; display: flex; flex-direction: column; gap: 12px; }
.ncard-img { position: relative; }
.loading-center { display: flex; justify-content: center; padding: 40px; }
</style>
