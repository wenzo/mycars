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
            <div class="dealer-sub">{{ op.profile?.email ?? '' }}</div>
          </div>
        </div>
      </div>
    </div>

    <ion-content>
      <div style="padding:14px 16px 80px;display:flex;flex-direction:column;gap:12px">

        <div v-if="loading" style="display:flex;justify-content:center;padding:40px">
          <ion-spinner name="crescent" />
        </div>

        <template v-else>
          <div v-for="branch in branches" :key="branch.id" class="branch-card">
            <div class="branch-header">
              <div>
                <div class="branch-title">{{ branch.name }}</div>
                <div class="branch-sub" v-if="branch.address">{{ branch.address }}, {{ branch.city }}</div>
              </div>
              <span v-if="branch.isLegalAddress" class="branch-badge">PRINCIPALE</span>
            </div>

            <div class="branch-body">
              <div v-if="branch.phone" class="cinfo-row">
                <div class="cinfo-icon red"><ion-icon :icon="callOutline" /></div>
                <div class="cinfo-text">
                  <strong>{{ branch.phone }}</strong>
                  Centralino
                </div>
              </div>
              <div v-if="branch.whatsappNumber" class="cinfo-row">
                <div class="cinfo-icon green"><ion-icon :icon="logoWhatsapp" /></div>
                <div class="cinfo-text">
                  <strong>WhatsApp</strong>
                  {{ branch.whatsappNumber }}
                </div>
              </div>
              <div v-if="branch.email" class="cinfo-row">
                <div class="cinfo-icon"><ion-icon :icon="mailOutline" /></div>
                <div class="cinfo-text">
                  <strong>{{ branch.email }}</strong>
                </div>
              </div>
            </div>

            <!-- Bottoni contatto -->
            <div class="contact-btns">
              <button v-if="branch.phone" class="cbtn cbtn-call" @click="call(branch.phone)">
                <ion-icon :icon="callOutline" /> Chiama
              </button>
              <button v-if="branch.whatsappNumber" class="cbtn cbtn-wa" @click="whatsapp(branch.whatsappNumber)">
                <ion-icon :icon="logoWhatsapp" /> WhatsApp
              </button>
              <button v-if="branch.email" class="cbtn cbtn-mail" @click="email(branch.email)">
                <ion-icon :icon="mailOutline" /> Email
              </button>
            </div>

            <!-- Reparti della sede -->
            <div v-if="deptsByBranch(branch.id).length" class="dept-list">
              <div
                v-for="dept in deptsByBranch(branch.id)"
                :key="dept.id"
                class="dept-row"
              >
                <div class="dept-dot" />
                <span class="dept-name">{{ dept.name }}</span>
                <span v-if="dept.description" class="dept-desc">{{ dept.description }}</span>
              </div>
            </div>
          </div>

          <!-- Sito web -->
          <div v-if="op.profile?.websiteUrl" class="mc-section" style="padding:0">
            <div class="srow" @click="openUrl(op.profile.websiteUrl)">
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
import { ref, onMounted } from 'vue'
import { IonPage, IonContent, IonIcon, IonSpinner } from '@ionic/vue'
import {
  carOutline, callOutline, mailOutline, globeOutline,
  openOutline, logoWhatsapp,
} from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'

interface Branch {
  id: string; name: string; slug: string
  address: string | null; city: string | null; province: string | null
  phone: string | null; email: string | null; whatsappNumber: string | null
  isLegalAddress: boolean; isActive: boolean
}
interface Department { id: string; branchId: string | null; name: string; description: string | null }

const op          = useOperatorStore()
const branches    = ref<Branch[]>([])
const departments = ref<Department[]>([])
const loading     = ref(false)

function deptsByBranch(branchId: string) {
  return departments.value.filter(d => d.branchId === branchId)
}
function call(phone: string)      { window.open(`tel:${phone}`) }
function whatsapp(num: string)    { window.open(`https://wa.me/${num.replace(/\D/g,'')}`) }
function email(addr: string)      { window.open(`mailto:${addr}`) }
function openUrl(url: string)     { window.open(url, '_blank') }

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
.branch-card { background: var(--mc-white); border-radius: var(--mc-r); overflow: hidden; box-shadow: var(--mc-shadow-sm); }
.branch-header {
  background: var(--dealer-primary); padding: 12px 16px;
  display: flex; align-items: center; justify-content: space-between;
}
.branch-title { font-family: var(--mc-font-heading); font-size: 14px; font-weight: 700; color: #fff; }
.branch-sub   { font-size: 11px; color: rgba(255,255,255,.6); margin-top: 2px; }
.branch-badge {
  background: rgba(255,255,255,.18); color: #fff;
  font-family: var(--mc-font-heading); font-size: 10px; font-weight: 700;
  padding: 3px 8px; border-radius: 10px;
}
.branch-body { padding: 14px 16px 6px; }
.cinfo-row { display: flex; align-items: flex-start; gap: 10px; margin-bottom: 11px; }
.cinfo-icon {
  width: 28px; height: 28px; border-radius: 8px;
  background: var(--mc-surface2); display: flex; align-items: center; justify-content: center;
  flex-shrink: 0; margin-top: 1px;
}
.cinfo-icon ion-icon { font-size: 13px; color: var(--mc-blue); }
.cinfo-icon.red   ion-icon { color: var(--mc-red); }
.cinfo-icon.green ion-icon { color: var(--mc-green); }
.cinfo-text { flex: 1; font-size: 13px; color: var(--mc-text-mid); line-height: 1.4; }
.cinfo-text strong { color: var(--mc-text); font-weight: 600; display: block; font-size: 13.5px; }
.contact-btns { display: flex; gap: 8px; padding: 4px 16px 14px; }
.cbtn {
  flex: 1; height: 42px; border-radius: var(--mc-r-sm); border: none; cursor: pointer;
  font-family: var(--mc-font-heading); font-size: 12px; font-weight: 700;
  display: flex; align-items: center; justify-content: center; gap: 6px;
}
.cbtn ion-icon { font-size: 15px; }
.cbtn-call { background: var(--dealer-primary); color: #fff; }
.cbtn-wa   { background: #25D366; color: #fff; }
.cbtn-mail { background: var(--mc-surface2); color: var(--mc-text-mid); }
.dept-list { border-top: 1px solid var(--mc-surface); }
.dept-row {
  padding: 10px 16px; display: flex; align-items: center; gap: 10px;
  border-bottom: 1px solid var(--mc-surface); cursor: pointer;
}
.dept-row:last-child { border-bottom: none; }
.dept-dot { width: 8px; height: 8px; border-radius: 50%; background: var(--mc-blue-light); flex-shrink: 0; }
.dept-name { font-size: 13px; font-weight: 600; color: var(--mc-text); flex: 1; }
.dept-desc { font-size: 11px; color: var(--mc-text-light); }
.srow { display: flex; align-items: center; padding: 13px 16px; gap: 12px; cursor: pointer; }
.srow-icon { width: 32px; height: 32px; border-radius: 9px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.srow-icon.navy { background: #E8EEF5; }
.srow-icon.navy ion-icon { color: var(--mc-navy); font-size: 16px; }
</style>
