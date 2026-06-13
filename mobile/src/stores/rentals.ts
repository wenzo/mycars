import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useOperatorStore } from './operator'

export interface Rental {
  id: string
  operatorId: string
  vehicleId: string
  customerName: string
  customerPhone: string | null
  customerLicense: string | null
  customerFiscalCode: string | null
  startDate: string        // ISO date yyyy-MM-dd
  plannedEndDate: string
  actualEndDate: string | null
  kmDeparture: number | null
  kmReturn: number | null
  fuelDeparture: FuelLevel | null
  fuelReturn: FuelLevel | null
  agreedPrice: number | null
  depositAmount: number | null
  depositReturned: boolean
  paymentMethod: PaymentMethod | null
  isPaid: boolean
  status: RentalStatus
  notes: string | null
  createdAt: string
  updatedAt: string
  // enriched
  vehicleBrand: string | null
  vehicleModel: string | null
  vehicleTarga: string | null
}

export interface RentalPhoto {
  id: string
  rentalId: string
  phase: 'departure' | 'return'
  url: string
  createdAt: string
}

export interface RentalDashboard {
  activeCount: number
  bookedCount: number
  returningTodayCount: number
  returningToday: Rental[]
}

export type RentalStatus = 'booked' | 'active' | 'closed' | 'cancelled'
export type FuelLevel = 'full' | 'three_quarters' | 'half' | 'quarter' | 'empty'
export type PaymentMethod = 'cash' | 'pos' | 'transfer'

export interface PagedResult<T> {
  items: T[]
  totalCount: number
}

export interface CreateRentalPayload {
  vehicleId: string
  customerName: string
  customerPhone?: string
  customerLicense?: string
  customerFiscalCode?: string
  startDate: string
  plannedEndDate: string
  agreedPrice?: number
  depositAmount?: number
  paymentMethod?: PaymentMethod
  notes?: string
}

export const FUEL_LABELS: Record<FuelLevel, string> = {
  full:           'Pieno',
  three_quarters: '3/4',
  half:           'Metà',
  quarter:        '1/4',
  empty:          'Vuoto',
}

export const STATUS_LABELS: Record<RentalStatus, string> = {
  booked:    'Prenotato',
  active:    'In corso',
  closed:    'Concluso',
  cancelled: 'Annullato',
}

export const STATUS_COLORS: Record<RentalStatus, string> = {
  booked:    'warning',
  active:    'success',
  closed:    'medium',
  cancelled: 'danger',
}

export const useRentalsStore = defineStore('rentals', () => {
  const operator = useOperatorStore()

  const items       = ref<Rental[]>([])
  const totalCount  = ref(0)
  const loading     = ref(false)
  const detail      = ref<Rental | null>(null)
  const dashboard   = ref<RentalDashboard | null>(null)
  const photos      = ref<RentalPhoto[]>([])
  const currentPage = ref(0)
  const hasMore     = ref(true)

  function apiBase() {
    return operator.apiBase
  }

  // ── Dashboard ───────────────────────────────────────────────────────────────

  async function fetchDashboard() {
    const res = await fetch(`${apiBase()}/api/admin/rentals/dashboard`, {
      credentials: 'include',
    })
    if (!res.ok) throw new Error('Errore caricamento dashboard noleggi')
    const data = await res.json()
    dashboard.value = {
      activeCount:          data.active_count,
      bookedCount:          data.booked_count,
      returningTodayCount:  data.returning_today_count,
      returningToday:       data.returning_today ?? [],
    }
  }

  // ── Lista ────────────────────────────────────────────────────────────────────

  async function fetchRentals(status?: RentalStatus | '') {
    loading.value    = true
    currentPage.value = 0
    try {
      const params = new URLSearchParams({ page: '0', pageSize: '20' })
      if (status) params.set('status', status)
      const res = await fetch(`${apiBase()}/api/admin/rentals?${params}`, {
        credentials: 'include',
      })
      if (!res.ok) throw new Error('Errore caricamento noleggi')
      const data: PagedResult<Rental> = await res.json()
      items.value      = data.items
      totalCount.value = data.totalCount
      hasMore.value    = data.items.length < data.totalCount
    } finally {
      loading.value = false
    }
  }

  async function fetchNextPage(status?: RentalStatus | '') {
    if (!hasMore.value || loading.value) return
    loading.value = true
    currentPage.value++
    try {
      const params = new URLSearchParams({
        page:     String(currentPage.value),
        pageSize: '20',
      })
      if (status) params.set('status', status)
      const res = await fetch(`${apiBase()}/api/admin/rentals?${params}`, {
        credentials: 'include',
      })
      if (!res.ok) throw new Error()
      const data: PagedResult<Rental> = await res.json()
      items.value.push(...data.items)
      hasMore.value = items.value.length < data.totalCount
    } finally {
      loading.value = false
    }
  }

  // ── Dettaglio ────────────────────────────────────────────────────────────────

  async function fetchDetail(id: string) {
    loading.value = true
    try {
      const res = await fetch(`${apiBase()}/api/admin/rentals/${id}`, {
        credentials: 'include',
      })
      if (!res.ok) throw new Error('Noleggio non trovato')
      detail.value = await res.json()
    } finally {
      loading.value = false
    }
  }

  // ── Disponibilità ─────────────────────────────────────────────────────────────

  async function checkAvailability(
    vehicleId: string,
    startDate: string,
    endDate: string,
    excludeRentalId?: string
  ): Promise<boolean> {
    const params = new URLSearchParams({ vehicleId, startDate, endDate })
    if (excludeRentalId) params.set('excludeRentalId', excludeRentalId)
    const res = await fetch(`${apiBase()}/api/admin/rentals/availability?${params}`, {
      credentials: 'include',
    })
    if (!res.ok) return false
    const data = await res.json()
    return data.available
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────────

  async function createRental(payload: CreateRentalPayload): Promise<Rental> {
    const res = await fetch(`${apiBase()}/api/admin/rentals`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(payload),
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({}))
      throw new Error(err.message ?? 'Errore creazione noleggio')
    }
    return res.json()
  }

  async function updateRental(id: string, payload: Partial<Rental>): Promise<Rental> {
    const res = await fetch(`${apiBase()}/api/admin/rentals/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(payload),
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({}))
      throw new Error(err.message ?? 'Errore aggiornamento noleggio')
    }
    const updated = await res.json()
    if (detail.value?.id === id) detail.value = updated
    return updated
  }

  // ── Transizioni stato ─────────────────────────────────────────────────────────

  async function activateRental(
    id: string,
    kmDeparture?: number,
    fuelDeparture?: FuelLevel
  ) {
    const res = await fetch(`${apiBase()}/api/admin/rentals/${id}/activate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ kmDeparture, fuelDeparture }),
    })
    if (!res.ok) throw new Error('Errore attivazione noleggio')
    if (detail.value?.id === id) await fetchDetail(id)
  }

  async function closeRental(
    id: string,
    actualEndDate?: string,
    kmReturn?: number,
    fuelReturn?: FuelLevel
  ) {
    const res = await fetch(`${apiBase()}/api/admin/rentals/${id}/close`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ actualEndDate, kmReturn, fuelReturn }),
    })
    if (!res.ok) throw new Error('Errore chiusura noleggio')
    if (detail.value?.id === id) await fetchDetail(id)
  }

  async function cancelRental(id: string) {
    const res = await fetch(`${apiBase()}/api/admin/rentals/${id}/cancel`, {
      method: 'POST',
      credentials: 'include',
    })
    if (!res.ok) throw new Error('Errore annullamento noleggio')
    if (detail.value?.id === id) await fetchDetail(id)
  }

  // ── Foto ──────────────────────────────────────────────────────────────────────

  async function fetchPhotos(rentalId: string) {
    const res = await fetch(`${apiBase()}/api/admin/rentals/${rentalId}/photos`, {
      credentials: 'include',
    })
    if (!res.ok) return
    photos.value = await res.json()
  }

  async function uploadPhoto(rentalId: string, file: File, phase: 'departure' | 'return') {
    const form = new FormData()
    form.append('file', file)
    const res = await fetch(
      `${apiBase()}/api/admin/rentals/${rentalId}/photos?phase=${phase}`,
      { method: 'POST', credentials: 'include', body: form }
    )
    if (!res.ok) throw new Error('Errore upload foto')
    const photo: RentalPhoto = await res.json()
    photos.value.push(photo)
    return photo
  }

  async function deletePhoto(rentalId: string, photoId: string) {
    await fetch(`${apiBase()}/api/admin/rentals/${rentalId}/photos/${photoId}`, {
      method: 'DELETE',
      credentials: 'include',
    })
    photos.value = photos.value.filter(p => p.id !== photoId)
  }

  // ── Contratto ─────────────────────────────────────────────────────────────────

  function openContract(rentalId: string) {
    window.open(`${apiBase()}/api/admin/rentals/${rentalId}/contract`, '_blank')
  }

  // ── Push promemoria ───────────────────────────────────────────────────────────

  async function sendReturnReminder(rentalId: string) {
    const res = await fetch(`${apiBase()}/api/admin/rentals/${rentalId}/remind`, {
      method: 'POST',
      credentials: 'include',
    })
    if (!res.ok) throw new Error('Errore invio notifica')
  }

  return {
    items, totalCount, loading, detail, dashboard, photos, hasMore,
    fetchDashboard, fetchRentals, fetchNextPage, fetchDetail,
    checkAvailability,
    createRental, updateRental,
    activateRental, closeRental, cancelRental,
    fetchPhotos, uploadPhoto, deletePhoto,
    openContract, sendReturnReminder,
  }
})
