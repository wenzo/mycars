<template>
  <ion-page>
    <ion-content>

      <!-- Hero copertina + info concessionario -->
      <div class="dealer-hero">
        <!-- Cover image o gradiente fallback -->
        <div class="cover-bg">
          <img
            v-if="op.profile?.coverImageUrl"
            :src="op.resolveUrl(op.profile.coverImageUrl)!"
            alt="copertina"
            class="cover-img"
          />
          <div v-else class="cover-placeholder" />
          <div class="cover-scrim" />
        </div>

        <!-- Info concessionario sovrapposta alla cover -->
        <div class="dealer-info-overlay">
          <div class="dealer-logo-circle">
            <img v-if="op.profile?.logoUrl" :src="op.resolveUrl(op.profile.logoUrl)!" alt="logo" />
            <ion-icon v-else :icon="carOutline" style="color:#fff;font-size:26px" />
          </div>
          <div>
            <div class="dealer-name">{{ op.profile?.businessName ?? 'MyCars' }}</div>
            <div v-if="op.profile?.tagline" class="dealer-tagline">{{ op.profile.tagline }}</div>
          </div>
        </div>
      </div>

      <div style="padding:14px 16px 80px;display:flex;flex-direction:column;gap:12px">

        <div v-if="loading" style="display:flex;justify-content:center;padding:40px">
          <ion-spinner name="crescent" />
        </div>

        <template v-else>

          <!-- Reparti con contatti diretti -->
          <div v-for="dept in activeDepts" :key="dept.id" class="dept-card">
            <div class="dept-card-header">
              <div>
                <div class="dept-card-title">{{ dept.name }}</div>
                <div v-if="dept.description" class="dept-card-sub">{{ dept.description }}</div>
                <div v-if="dept.responsibleName" class="dept-responsible">
                  <ion-icon :icon="personOutline" /> {{ dept.responsibleName }}
                </div>
              </div>
              <span v-if="branchNameForDept(dept.branchId)" class="dept-branch-badge">
                {{ branchNameForDept(dept.branchId) }}
              </span>
            </div>

            <!-- Contatti a due colonne con icone-pulsante inline -->
            <div class="dept-contacts" v-if="dept.phone || dept.email">

              <!-- Colonna telefono -->
              <div v-if="dept.phone" class="dept-contact-col" :class="{ 'col-full': !dept.email }">
                <ion-icon :icon="callOutline" class="label-icon red" />
                <span class="contact-value">{{ dept.phone }}</span>
                <button class="icon-btn icon-btn-red" @click="call(dept.phone)" title="Chiama">
                  <ion-icon :icon="callOutline" />
                </button>
                <button class="icon-btn icon-btn-green" @click="whatsapp(dept.phone)" title="WhatsApp">
                  <ion-icon :icon="logoWhatsapp" />
                </button>
              </div>

              <!-- Divisore verticale -->
              <div v-if="dept.phone && dept.email" class="dept-contact-divider" />

              <!-- Colonna email -->
              <div v-if="dept.email" class="dept-contact-col" :class="{ 'col-full': !dept.phone }">
                <ion-icon :icon="mailOutline" class="label-icon blue" />
                <span class="contact-value">{{ dept.email }}</span>
                <button class="icon-btn icon-btn-blue" @click="sendEmail(dept.email)" title="Invia email">
                  <ion-icon :icon="mailOutline" />
                </button>
              </div>

            </div>
          </div>

          <!-- WhatsApp e sito (da profilo operatore) -->
          <div class="mc-section" style="padding:0" v-if="op.profile?.whatsappNumber || op.profile?.websiteUrl">
            <div v-if="op.profile?.whatsappNumber" class="srow" @click="whatsapp(op.profile.whatsappNumber)">
              <div class="srow-icon green"><ion-icon :icon="logoWhatsapp" /></div>
              <div style="flex:1">
                <div style="font-size:14px;font-weight:500;color:var(--mc-text)">{{ op.profile.whatsappNumber }}</div>
                <div style="font-size:11px;color:var(--mc-text-light)">WhatsApp</div>
              </div>
              <ion-icon :icon="openOutline" style="color:var(--mc-green);font-size:16px" />
            </div>
            <div v-if="op.profile?.websiteUrl" class="srow" @click="openUrl(op.profile.websiteUrl)" style="border-top:1px solid var(--mc-surface)">
              <div class="srow-icon navy"><ion-icon :icon="globeOutline" /></div>
              <div style="flex:1">
                <div style="font-size:14px;font-weight:500;color:var(--mc-text)">{{ op.profile.websiteUrl }}</div>
                <div style="font-size:11px;color:var(--mc-text-light)">Sito ufficiale</div>
              </div>
              <ion-icon :icon="openOutline" style="color:var(--mc-blue);font-size:16px" />
            </div>
          </div>
        </template>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { IonPage, IonContent, IonIcon, IonSpinner } from '@ionic/vue'
import {
  carOutline, callOutline, mailOutline, globeOutline,
  openOutline, logoWhatsapp, personOutline,
} from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'

interface Branch {
  id: string; name: string
  address: string | null; city: string | null; province: string | null
  isLegalAddress: boolean; isActive: boolean
}
interface Department {
  id: string; branchId: string | null; name: string; description: string | null
  responsibleName: string | null; phone: string | null; email: string | null
  isActive: boolean
}

const op          = useOperatorStore()
const branches    = ref<Branch[]>([])
const departments = ref<Department[]>([])
const loading     = ref(false)

const activeDepts = computed(() =>
  departments.value.filter(d => d.isActive)
)

function branchNameForDept(branchId: string | null): string | null {
  if (!branchId) return null
  return branches.value.find(b => b.id === branchId)?.name ?? null
}

function call(phone: string)       { window.open(`tel:${phone}`) }
function whatsapp(num: string)     { window.open(`https://wa.me/${num.replace(/\D/g,'')}`) }
function sendEmail(addr: string)   { window.open(`mailto:${addr}`) }
function openUrl(url: string)      { window.open(url, '_blank') }

onMounted(async () => {
  if (!op.slug) return
  loading.value = true
  try {
    const [bRes, dRes] = await Promise.all([
      fetch(`${op.apiBase}/api/public/${op.slug}/branches`),
      fetch(`${op.apiBase}/api/public/${op.slug}/departments`),
    ])
    if (bRes.ok) branches.value    = await bRes.json()
    if (dRes.ok) departments.value = await dRes.json()
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
/* Hero cover */
.dealer-hero   { position: relative; height: 220px; flex-shrink: 0; }
.cover-bg      { position: absolute; inset: 0; overflow: hidden; }
.cover-img     { width: 100%; height: 100%; object-fit: cover; }
.cover-placeholder { width: 100%; height: 100%; background: linear-gradient(145deg, var(--dealer-primary) 0%, var(--dealer-secondary) 100%); }
.cover-scrim   { position: absolute; inset: 0; background: linear-gradient(to bottom, rgba(0,0,0,.15) 0%, rgba(0,0,0,.55) 100%); }
.dealer-info-overlay {
  position: absolute; bottom: 0; left: 0; right: 0; z-index: 5;
  display: flex; align-items: center; gap: 12px;
  padding: 14px 16px calc(14px + env(safe-area-inset-top, 0px));
}
.dealer-logo-circle {
  width: 54px; height: 54px; border-radius: 14px; flex-shrink: 0;
  background: rgba(255,255,255,.18); backdrop-filter: blur(10px);
  border: 2px solid rgba(255,255,255,.35);
  display: flex; align-items: center; justify-content: center; overflow: hidden;
}
.dealer-logo-circle img { width: 100%; height: 100%; object-fit: contain; }
.dealer-name    { font-family: var(--mc-font-heading); font-size: 18px; font-weight: 800; color: #fff; }
.dealer-tagline { font-size: 12px; color: rgba(255,255,255,.75); margin-top: 2px; }

/* Dept card */
.dept-card        { background: var(--mc-white); border-radius: var(--mc-r); overflow: hidden; box-shadow: var(--mc-shadow-sm); }
.dept-card-header {
  background: var(--dealer-primary); padding: 12px 16px;
  display: flex; align-items: flex-start; justify-content: space-between; gap: 8px;
}
.dept-card-title  { font-family: var(--mc-font-heading); font-size: 15px; font-weight: 700; color: #fff; }
.dept-card-sub    { font-size: 11px; color: rgba(255,255,255,.65); margin-top: 2px; }
.dept-responsible { font-size: 11.5px; color: rgba(255,255,255,.8); margin-top: 4px; display: flex; align-items: center; gap: 4px; }
.dept-responsible ion-icon { font-size: 12px; }
.dept-branch-badge {
  background: rgba(255,255,255,.18); color: #fff;
  font-family: var(--mc-font-heading); font-size: 10px; font-weight: 700;
  padding: 3px 8px; border-radius: 10px; white-space: nowrap; flex-shrink: 0;
}
/* Contatti a due colonne */
.dept-contacts {
  display: flex; align-items: stretch;
  border-top: 1px solid var(--mc-surface);
}
.dept-contact-col {
  flex: 1; display: flex; flex-direction: row;
  align-items: center; gap: 6px;
  padding: 10px 12px;
  min-width: 0;
}
.dept-contact-col.col-full { flex: 1; }
.dept-contact-divider {
  width: 1px; background: var(--mc-border); flex-shrink: 0; align-self: stretch; margin: 8px 0;
}
.label-icon { font-size: 15px; flex-shrink: 0; }
.label-icon.red  { color: var(--mc-red); }
.label-icon.blue { color: var(--mc-blue); }
.contact-value {
  flex: 1; min-width: 0;
  font-size: 11.5px; font-weight: 600; color: var(--mc-text);
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.icon-btn {
  width: 40px; height: 40px; border-radius: 12px; border: none; cursor: pointer;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
  transition: opacity .15s;
}
.icon-btn:active { opacity: .75; }
.icon-btn ion-icon { font-size: 19px; color: #fff; }
.icon-btn-red   { background: var(--mc-red); }
.icon-btn-green { background: #25D366; }
.icon-btn-blue  { background: var(--mc-blue); }

.mc-section { background: var(--mc-white); border-radius: var(--mc-r); box-shadow: var(--mc-shadow-sm); overflow: hidden; }
.srow { display: flex; align-items: center; padding: 13px 16px; gap: 12px; cursor: pointer; }
.srow-icon { width: 32px; height: 32px; border-radius: 9px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.srow-icon.navy  { background: #E8EEF5; }
.srow-icon.navy  ion-icon { color: var(--mc-navy); font-size: 16px; }
.srow-icon.green { background: #E8F5E9; }
.srow-icon.green ion-icon { color: var(--mc-green); font-size: 16px; }
</style>
