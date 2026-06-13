<template>
  <ion-page>
    <ion-tabs>
      <ion-router-outlet />

      <ion-tab-bar slot="bottom">
        <ion-tab-button tab="vetrina" href="/tabs/vetrina">
          <ion-icon :icon="carOutline" />
          <ion-label>Vetrina</ion-label>
        </ion-tab-button>

        <ion-tab-button tab="news" href="/tabs/news">
          <ion-icon :icon="notificationsOutline" />
          <ion-label>News</ion-label>
        </ion-tab-button>

        <ion-tab-button tab="contatti" href="/tabs/contatti">
          <ion-icon :icon="locationOutline" />
          <ion-label>Contatti</ion-label>
        </ion-tab-button>

        <ion-tab-button tab="noleggio" href="/tabs/noleggio">
          <ion-icon :icon="keyOutline" />
          <ion-label>Noleggio</ion-label>
        </ion-tab-button>

        <ion-tab-button tab="impostazioni" href="/tabs/impostazioni">
          <ion-icon :icon="settingsOutline" />
          <ion-label>Impostazioni</ion-label>
        </ion-tab-button>
      </ion-tab-bar>
    </ion-tabs>
  </ion-page>
</template>

<script setup lang="ts">
import {
  IonPage, IonTabs, IonTabBar, IonTabButton,
  IonIcon, IonLabel, IonRouterOutlet,
} from '@ionic/vue'
import {
  carOutline,
  notificationsOutline,
  locationOutline,
  settingsOutline,
  keyOutline,
} from 'ionicons/icons'
</script>
