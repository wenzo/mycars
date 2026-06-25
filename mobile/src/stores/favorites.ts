import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface FavoriteVehicle {
  id: string
  brandName: string
  model: string
  version?: string
  coverImageUrl?: string
}

const STORAGE_KEY = 'mycars_favorites'

export const useFavoritesStore = defineStore('favorites', () => {
  const items = ref<FavoriteVehicle[]>(load())

  function load(): FavoriteVehicle[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      return raw ? JSON.parse(raw) : []
    } catch {
      return []
    }
  }

  function persist() {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items.value))
  }

  function isFavorite(id: string) {
    return items.value.some(v => v.id === id)
  }

  function toggle(vehicle: FavoriteVehicle) {
    const idx = items.value.findIndex(v => v.id === vehicle.id)
    if (idx >= 0) items.value.splice(idx, 1)
    else          items.value.push(vehicle)
    persist()
  }

  function remove(id: string) {
    const idx = items.value.findIndex(v => v.id === id)
    if (idx >= 0) { items.value.splice(idx, 1); persist() }
  }

  const count = computed(() => items.value.length)

  return { items, isFavorite, toggle, remove, count }
})
