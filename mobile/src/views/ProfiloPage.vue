<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-button @click="$router.back()">
            <ion-icon :icon="arrowBackOutline" slot="icon-only" />
          </ion-button>
        </ion-buttons>
        <ion-title>Profilo</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content style="--padding-bottom: calc(var(--ion-tab-bar-height, 56px) + var(--ion-safe-area-bottom, 0px))">
      <div class="profilo-wrap">

        <!-- Avatar -->
        <div class="profilo-avatar-card">
          <div class="avatar-circle">{{ avatarInitials }}</div>
          <div class="avatar-info">
            <div class="avatar-name">{{ clientStore.profile.fullName || 'Ospite' }}</div>
            <div class="avatar-sub">{{ clientStore.profile.email || 'Nessuna email' }}</div>
          </div>
        </div>

        <!-- Veicoli preferiti -->
        <div class="profilo-section-label">Veicoli preferiti</div>
        <div class="profilo-card" style="padding:14px 16px">
          <div v-if="favs.items.length === 0" class="empty-mini">
            Nessun veicolo nei preferiti.
          </div>
          <div v-else>
            <div
              v-for="fav in favs.items"
              :key="fav.id"
              class="fav-row"
              @click="$router.push(`/tabs/veicolo/${fav.id}`)"
            >
              <div class="fav-thumb">
                <img v-if="fav.coverImageUrl" :src="op.resolveUrl(fav.coverImageUrl)" alt="" />
                <ion-icon v-else :icon="carOutline" style="font-size:18px;color:var(--dealer-primary)" />
              </div>
              <div class="fav-info">
                <div class="fav-name">{{ fav.brandName }} {{ fav.model }}</div>
                <div v-if="fav.version" class="fav-version">{{ fav.version }}</div>
              </div>
              <button class="fav-remove" @click.stop="favs.remove(fav.id)" aria-label="Rimuovi preferito">
                <ion-icon :icon="closeOutline" />
              </button>
            </div>
          </div>
        </div>

        <!-- Dati personali -->
        <div class="profilo-section-label">Dati personali</div>
        <div class="profilo-card">
          <div class="field-wrap">
            <label class="field-label">Nome e cognome</label>
            <input v-model="profileForm.fullName" class="field-input" placeholder="Mario Rossi" />
          </div>
          <div class="field-wrap field-row">
            <div style="flex:1">
              <label class="field-label">Telefono</label>
              <input v-model="profileForm.phone" class="field-input" type="tel" placeholder="+39 333 1234567" />
            </div>
            <div style="flex:1">
              <label class="field-label">Email</label>
              <input v-model="profileForm.email" class="field-input" type="email" placeholder="email@esempio.it" />
            </div>
          </div>
          <div class="field-wrap">
            <label class="field-label">Città</label>
            <input v-model="profileForm.city" class="field-input" placeholder="Cerignola" />
          </div>
          <button class="save-btn" @click="saveProfile">Salva dati</button>
          <div v-if="profileSaved" class="save-ok">✓ Dati salvati</div>
        </div>

        <!-- Contatti dealer -->
        <div class="profilo-section-label">Concessionario</div>
        <div class="profilo-card dealer-info-card">
          <div v-if="op.profile?.businessName" class="di-row">
            <ion-icon :icon="businessOutline" />
            <span>{{ op.profile.businessName }}</span>
          </div>
          <div v-if="op.profile?.phone" class="di-row">
            <ion-icon :icon="callOutline" />
            <a :href="'tel:' + op.profile.phone">{{ op.profile.phone }}</a>
          </div>
          <div v-if="op.profile?.email" class="di-row">
            <ion-icon :icon="mailOutline" />
            <a :href="'mailto:' + op.profile.email">{{ op.profile.email }}</a>
          </div>
          <div v-if="op.profile?.city" class="di-row">
            <ion-icon :icon="locationOutline" />
            <span>{{ op.profile.city }}{{ op.profile?.province ? ` (${op.profile.province})` : '' }}</span>
          </div>
        </div>

        <!-- Noleggi recenti -->
        <div class="profilo-section-label">Noleggi recenti</div>
        <div class="profilo-card" style="padding:14px 16px">
          <div v-if="clientStore.requests.length === 0" class="empty-mini">
            Nessuna richiesta inviata ancora.
          </div>
          <div v-else>
            <div v-for="r in clientStore.requests.slice(0,3)" :key="r.localId" class="mini-req">
              <span class="mini-req-name">{{ r.vehicleName }}</span>
              <span class="mini-req-date">{{ fmtDate(r.startDate) }}</span>
            </div>
            <button
              v-if="clientStore.requests.length > 3"
              class="link-btn"
              @click="$router.push('/tabs/noleggio?tab=noleggi')"
            >
              Vedi tutti ({{ clientStore.requests.length }}) →
            </button>
          </div>
        </div>

      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import {
  IonPage, IonContent, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonIcon,
} from '@ionic/vue'
import {
  arrowBackOutline, carOutline, closeOutline,
  callOutline, mailOutline, locationOutline, businessOutline,
} from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'
import { useRentalClientStore } from '@/stores/rentalClient'
import { useFavoritesStore } from '@/stores/favorites'

const op          = useOperatorStore()
const clientStore = useRentalClientStore()
const favs        = useFavoritesStore()

const profileForm  = ref({ ...clientStore.profile })
const profileSaved = ref(false)

const avatarInitials = computed(() => {
  const name = clientStore.profile.fullName.trim()
  if (!name) return '?'
  const parts = name.split(' ')
  if (parts.length === 1) return parts[0][0].toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
})

function saveProfile() {
  clientStore.saveProfile({ ...profileForm.value })
  profileSaved.value = true
  setTimeout(() => { profileSaved.value = false }, 2000)
}

function fmtDate(d: string | undefined) {
  if (!d) return '—'
  const [y, m, dd] = d.split('-')
  if (!y || !m || !dd) return d
  return `${dd}/${m}/${y}`
}
</script>

<style scoped>
.profilo-wrap { padding: 16px; display: flex; flex-direction: column; gap: 4px; }

.profilo-avatar-card {
  background: linear-gradient(135deg, var(--dealer-primary), color-mix(in srgb, var(--dealer-primary) 70%, #000));
  border-radius: var(--mc-r); padding: 18px;
  display: flex; align-items: center; gap: 14px; margin-bottom: 12px;
}
.avatar-circle {
  width: 52px; height: 52px; border-radius: 50%;
  background: rgba(255,255,255,.22); color: #fff;
  font-family: var(--mc-font-heading); font-size: 18px; font-weight: 700;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.avatar-name { font-family: var(--mc-font-heading); font-size: 15px; font-weight: 700; color: #fff; }
.avatar-sub  { font-size: 11.5px; color: rgba(255,255,255,.7); margin-top: 2px; }

.profilo-section-label {
  font-size: 10.5px; font-weight: 700; color: var(--mc-text-light);
  text-transform: uppercase; letter-spacing: .07em; margin: 10px 0 6px;
}
.profilo-card {
  background: #fff; border-radius: var(--mc-r); border: 1px solid var(--mc-border);
  padding: 14px 16px; display: flex; flex-direction: column; gap: 10px;
}

.field-wrap  { display: flex; flex-direction: column; gap: 4px; }
.field-row   { flex-direction: row; gap: 10px; }
.field-label { font-size: 11px; font-weight: 600; color: var(--mc-text-mid); }
.field-input {
  height: 44px; border: 1.5px solid var(--mc-border);
  border-radius: var(--mc-r-sm); padding: 0 12px;
  font-size: 13.5px; color: var(--mc-text); background: #fff;
  outline: none; box-sizing: border-box; width: 100%;
}
.field-input:focus { border-color: var(--dealer-primary); }
.save-btn {
  height: 46px; background: var(--dealer-primary); color: #fff; border: none;
  border-radius: var(--mc-r-sm); font-family: var(--mc-font-heading); font-size: 14px; font-weight: 700;
  cursor: pointer; margin-top: 4px;
}
.save-ok { text-align: center; font-size: 12px; color: var(--ion-color-success); }

.dealer-info-card { gap: 8px; }
.di-row {
  display: flex; align-items: center; gap: 10px;
  font-size: 13px; color: var(--mc-text-mid);
}
.di-row ion-icon { font-size: 16px; color: var(--dealer-primary); flex-shrink: 0; }
.di-row a { color: var(--dealer-primary); text-decoration: none; }

.empty-mini { font-size: 12.5px; color: var(--mc-text-light); text-align: center; padding: 8px 0; }
.mini-req {
  display: flex; justify-content: space-between; align-items: center;
  padding: 6px 0; border-bottom: 1px solid var(--mc-surface);
  font-size: 12.5px;
}
.mini-req:last-child { border-bottom: none; }
.mini-req-name { color: var(--mc-text); font-weight: 600; }
.mini-req-date { color: var(--mc-text-light); }
.link-btn {
  background: none; border: none; cursor: pointer;
  color: var(--dealer-primary); font-size: 12.5px; font-weight: 600;
  padding: 4px 0; text-align: left;
}

.fav-row {
  display: flex; align-items: center; gap: 10px;
  padding: 7px 0; border-bottom: 1px solid var(--mc-surface); cursor: pointer;
}
.fav-row:last-child { border-bottom: none; }
.fav-thumb {
  width: 40px; height: 40px; border-radius: 8px; overflow: hidden; flex-shrink: 0;
  background: var(--mc-surface2); display: flex; align-items: center; justify-content: center;
}
.fav-thumb img { width: 100%; height: 100%; object-fit: cover; }
.fav-info { flex: 1; min-width: 0; }
.fav-name    { font-size: 13px; font-weight: 700; color: var(--mc-text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.fav-version { font-size: 11px; color: var(--mc-text-light); }
.fav-remove {
  width: 28px; height: 28px; border-radius: 50%; border: none; cursor: pointer;
  background: var(--mc-surface); display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.fav-remove ion-icon { font-size: 14px; color: var(--mc-text-light); }
</style>
