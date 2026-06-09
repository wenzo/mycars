import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useOperatorStore } from './operator'

export interface VehicleCard {
  id: string
  brandName: string
  brandSlug: string
  model: string
  version: string | null
  vehicleType: string
  condition: string
  fuel: string | null
  transmission: string | null
  registrationYear: number | null
  mileageKm: number
  price: number | null
  previousPrice: number | null
  currency: string
  isSold: boolean
  showAsSold: boolean
  prontaConsegna: boolean
  isNuovoArrivo: boolean
  coverImageUrl: string | null
  branchName: string | null
  city: string | null
  province: string | null
}

export interface VehicleDetail extends VehicleCard {
  internalCode: string
  horsepowerCv: number | null
  engineCapacityCc: number | null
  doors: number | null
  seats: number | null
  color: string | null
  emissionClass: string | null
  vatDeductible: boolean
  handicapAccessible: boolean
  negotiable: boolean
  description: string | null
  equipment: string
  branchId: string
}

export interface VehicleImage {
  id: string
  url: string
  sortOrder: number
}

export interface VehicleFilters {
  vehicleType?: string
  condition?: string
  fuel?: string
  prontaConsegna?: boolean
  isNuovoArrivo?: boolean
  minPrice?: number
  maxPrice?: number
  maxMileageKm?: number
  minYear?: number
  maxYear?: number
  branchId?: string
}

export const useVehicleStore = defineStore('vehicles', () => {
  const op = useOperatorStore()

  const items      = ref<VehicleCard[]>([])
  const totalCount = ref(0)
  const loading    = ref(false)
  const detail     = ref<VehicleDetail | null>(null)
  const images     = ref<VehicleImage[]>([])
  const filters    = ref<VehicleFilters>({})
  const page       = ref(0)
  const pageSize   = 20

  function buildQuery(f: VehicleFilters, p: number): string {
    const q = new URLSearchParams()
    q.set('page', String(p))
    q.set('pageSize', String(pageSize))
    if (f.vehicleType)      q.set('vehicleType',  f.vehicleType)
    if (f.condition)        q.set('condition',    f.condition)
    if (f.fuel)             q.set('fuel',         f.fuel)
    if (f.prontaConsegna)   q.set('prontaConsegna', 'true')
    if (f.isNuovoArrivo)    q.set('isNuovoArrivo',  'true')
    if (f.minPrice != null) q.set('minPrice',  String(f.minPrice))
    if (f.maxPrice != null) q.set('maxPrice',  String(f.maxPrice))
    if (f.maxMileageKm)     q.set('maxMileageKm', String(f.maxMileageKm))
    if (f.minYear)          q.set('minYear',   String(f.minYear))
    if (f.maxYear)          q.set('maxYear',   String(f.maxYear))
    if (f.branchId)         q.set('branchId',  f.branchId)
    return q.toString()
  }

  async function fetchVehicles(reset = false) {
    if (!op.slug) return
    if (reset) page.value = 0
    loading.value = true
    try {
      const qs  = buildQuery(filters.value, page.value)
      const res = await fetch(`${op.apiBase}/api/public/${op.slug}/vehicles?${qs}`)
      if (!res.ok) throw new Error('Errore caricamento veicoli')
      const data = await res.json()
      if (reset) items.value = data.items
      else items.value.push(...data.items)
      totalCount.value = data.totalCount
    } finally {
      loading.value = false
    }
  }

  async function fetchNextPage() {
    if (loading.value) return
    if (items.value.length >= totalCount.value) return
    page.value++
    await fetchVehicles(false)
  }

  async function fetchDetail(id: string) {
    if (!op.slug) return
    loading.value = true
    detail.value  = null
    images.value  = []
    try {
      const res = await fetch(`${op.apiBase}/api/public/${op.slug}/vehicles/${id}`)
      if (!res.ok) throw new Error('Veicolo non trovato')
      const data = await res.json()
      detail.value = data.vehicle
      images.value = data.images ?? []
    } finally {
      loading.value = false
    }
  }

  function applyFilters(f: VehicleFilters) {
    filters.value = f
    fetchVehicles(true)
  }

  function resetFilters() {
    filters.value = {}
    fetchVehicles(true)
  }

  return {
    items, totalCount, loading, detail, images, filters, page,
    fetchVehicles, fetchNextPage, fetchDetail, applyFilters, resetFilters,
  }
})
