<template>
  <ion-page>
    <div class="dealer-header">
      <div class="dealer-bar">
        <div class="dealer-logo-wrap">
          <div class="dealer-logo-icon">
            <img v-if="op.profile?.logoUrl" :src="op.resolveUrl(op.profile.logoUrl)" alt="logo" />
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
          class="ncard ncard-list"
          @click="$router.push(`/news/${item.id}`)"
        >
          <div class="ncard-img" :style="newsImgStyle(item.newsType)">
            <img v-if="item.coverImageUrl" :src="op.resolveUrl(item.coverImageUrl)" :alt="item.title" />
          </div>
          <div class="ncard-body">
            <span class="ncard-type" :class="`ntype-${item.newsType}`">{{ newsTypeLabel(item.newsType) }}</span>
            <div class="ncard-title">{{ item.title }}</div>
            <div v-if="item.excerpt" class="ncard-excerpt">{{ item.excerpt }}</div>
            <div class="ncard-footer">
              <span class="ncard-date">{{ fmtDate(item.publishedAt) }}</span>
              <span class="ncard-more">Scopri <ion-icon :icon="arrowForwardOutline" /></span>
            </div>
          </div>
        </div>

        <div v-if="store.loading && store.items.length === 0" class="state-center">
          <ion-spinner name="crescent" />
        </div>

        <div v-else-if="store.error" class="state-center">
          <div style="font-size:13px;color:var(--mc-red);text-align:center">
            {{ store.error }}<br/>
            <button style="margin-top:8px;font-size:12px;color:var(--mc-blue);background:none;border:none;cursor:pointer" @click="store.fetchNews(true)">Riprova</button>
          </div>
        </div>

        <div v-else-if="!store.loading && store.items.length === 0" class="state-center">
          <ion-icon :icon="newspaperOutline" style="font-size:40px;color:var(--mc-border);margin-bottom:8px" />
          <div style="font-size:13px;color:var(--mc-text-light)">Nessuna news disponibile</div>
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
import { carOutline, arrowForwardOutline, newspaperOutline } from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'
import { useNewsStore } from '@/stores/news'

const op    = useOperatorStore()
const store = useNewsStore()

const filters = [
  { value: null,          label: 'Tutte' },
  { value: 'new_arrival', label: '✦ Nuovi Arrivi' },
  { value: 'promotion',   label: '% Promozioni' },
  { value: 'event',       label: '🗓 Eventi' },
  { value: 'financing',   label: '💰 Finanziamenti' },
  { value: 'service',     label: '🔧 Service' },
]

const newsGradients: Record<string, string> = {
  new_arrival: 'linear-gradient(135deg,#700000,#B01020)',
  promotion:   'linear-gradient(135deg,#8B1010,#D62828)',
  event:       'linear-gradient(135deg,#0D3B6E,#2E75B6)',
  financing:   'linear-gradient(135deg,#005C35,#00A86B)',
  service:     'linear-gradient(135deg,#374151,#6B7280)',
  generic:     'linear-gradient(135deg,#152B47,#1E3A5F)',
}

const newsLabels: Record<string, string> = {
  new_arrival: '✦ Nuovo Arrivo',
  promotion:   '% Promozione',
  event:       '🗓 Evento',
  financing:   '💰 Finanziamento',
  service:     '🔧 Service',
  generic:     'Comunicazione',
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

.news-scroll { padding: 14px 16px 80px; display: flex; flex-direction: column; gap: 12px; }

/* ── card base ── */
.ncard {
  background: #fff;
  border-radius: var(--mc-r);
  overflow: hidden;
  box-shadow: var(--mc-shadow-sm);
  cursor: pointer;
}

.ncard-img {
  position: relative;
  height: 160px;
  overflow: hidden;
}
.ncard-img img {
  position: absolute; inset: 0;
  width: 100%; height: 100%; object-fit: cover;
}

.ncard-type {
  position: absolute; top: 8px; left: 8px; z-index: 1;
  font-family: var(--mc-font-heading); font-size: 10px; font-weight: 700;
  color: #fff; background: rgba(0,0,0,.38);
  padding: 3px 8px; border-radius: 20px;
  backdrop-filter: blur(4px);
}

.ncard-body { padding: 12px 14px 14px; }

.ncard-title {
  font-family: var(--mc-font-heading);
  font-size: 15px; font-weight: 700;
  color: var(--mc-text);
  line-height: 1.35;
  margin-bottom: 5px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.ncard-excerpt {
  font-size: 12.5px; color: var(--mc-text-mid);
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  margin-bottom: 10px;
}

.ncard-footer {
  display: flex; align-items: center; justify-content: space-between;
  margin-top: 10px;
}
.ncard-date { font-size: 11px; color: var(--mc-text-light); }
.ncard-more {
  font-family: var(--mc-font-heading); font-size: 12px; font-weight: 700;
  color: var(--dealer-primary);
  display: flex; align-items: center; gap: 4px;
}

/* ── listview ── */
.ncard-list { display: flex; flex-direction: row; }
.ncard-list .ncard-img { display: none; }
.ncard-list .ncard-body {
  flex: 1; min-width: 0;
  padding: 10px 12px 10px;
  display: flex; flex-direction: column;
}
.ncard-list .ncard-type {
  position: static;
  align-self: flex-start;
  backdrop-filter: none;
  font-size: 9px;
  margin-bottom: 5px;
}
.ncard-list .ntype-financing   { background: #00966D; }
.ncard-list .ntype-promotion   { background: #C82020; }
.ncard-list .ntype-event       { background: #2061A8; }
.ncard-list .ntype-new_arrival { background: #A01020; }
.ncard-list .ntype-service     { background: #5A6270; }
.ncard-list .ntype-generic     { background: #1A304A; }
.ncard-list .ncard-title {
  font-size: 13.5px;
  -webkit-line-clamp: 3;
  margin-bottom: 4px;
}
.ncard-list .ncard-excerpt { display: none; }
.ncard-list .ncard-footer { margin-top: auto; }

.state-center { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 60px 20px; }
</style>
