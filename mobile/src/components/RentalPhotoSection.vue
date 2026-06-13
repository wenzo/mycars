<template>
  <div class="photo-section">
    <div class="section-header">
      <span class="section-title">{{ title }}</span>
      <ion-button v-if="canUpload" fill="clear" size="small" @click="pickPhoto">
        <ion-icon :icon="cameraOutline" slot="start" />
        Aggiungi
      </ion-button>
    </div>

    <div class="photo-grid" v-if="photos.length > 0">
      <div class="photo-item" v-for="photo in photos" :key="photo.id">
        <img :src="resolveUrl(photo.url)" @click="openPhoto(photo.url)" />
        <ion-button
          v-if="canUpload"
          class="delete-btn"
          fill="clear"
          color="danger"
          size="small"
          @click.stop="emit('delete', photo.id)"
        >
          <ion-icon :icon="trashOutline" slot="icon-only" />
        </ion-button>
      </div>
    </div>
    <p v-else class="empty-photos">Nessuna foto</p>

    <input
      ref="fileInput"
      type="file"
      accept="image/*"
      capture="environment"
      style="display:none"
      @change="onFileChange"
    />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { IonButton, IonIcon } from '@ionic/vue'
import { cameraOutline, trashOutline } from 'ionicons/icons'
import { useOperatorStore } from '@/stores/operator'
import type { RentalPhoto } from '@/stores/rentals'

const props = defineProps<{
  title:    string
  phase:    'departure' | 'return'
  photos:   RentalPhoto[]
  rentalId: string
  canUpload: boolean
}>()

const emit = defineEmits<{
  upload: [{ file: File; phase: 'departure' | 'return' }]
  delete: [photoId: string]
}>()

const opStore   = useOperatorStore()
const fileInput = ref<HTMLInputElement>()

function resolveUrl(url: string) {
  if (!url) return ''
  if (url.startsWith('http')) return url
  return (import.meta.env.VITE_API_BASE_URL ?? '') + url
}

function pickPhoto() {
  fileInput.value?.click()
}

function onFileChange(ev: Event) {
  const file = (ev.target as HTMLInputElement).files?.[0]
  if (file) emit('upload', { file, phase: props.phase })
  if (fileInput.value) fileInput.value.value = ''
}

function openPhoto(url: string) {
  window.open(resolveUrl(url), '_blank')
}
</script>

<style scoped>
.photo-section { margin-bottom: 12px; }
.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
}
.section-title { font-size: 13px; font-weight: 600; color: var(--ion-color-dark); }
.photo-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.photo-item {
  position: relative;
  width: 90px;
  height: 70px;
}
.photo-item img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid var(--ion-border-color, #ddd);
  cursor: pointer;
}
.delete-btn {
  position: absolute;
  top: -6px;
  right: -6px;
  --padding-start: 2px;
  --padding-end: 2px;
  --padding-top: 2px;
  --padding-bottom: 2px;
  background: white;
  border-radius: 50%;
}
.empty-photos {
  font-size: 12px;
  color: var(--ion-color-medium);
  margin: 0;
}
</style>
