import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface CustomerProfile {
  fullName: string
  phone: string
  email: string
  city: string
}

export interface SelectedOption {
  key: string
  label: string
  pricePerDay: number | null
  priceFlat: number | null
}

export interface LocalRentalRequest {
  localId: string
  vehicleId: string | null
  vehicleName: string
  formula: string
  formulaLabel: string
  startDate: string
  endDate: string
  days: number | null
  estimatedCost: number | null
  options: SelectedOption[]
  notes: string
  submittedAt: string
  trackingCode: string | null
}

const PROFILE_KEY  = 'mc_customer_profile'
const REQUESTS_KEY = 'mc_rental_requests'

export const useRentalClientStore = defineStore('rentalClient', () => {
  const profile  = ref<CustomerProfile>(loadProfile())
  const requests = ref<LocalRentalRequest[]>(loadRequests())

  function loadProfile(): CustomerProfile {
    try {
      const raw = localStorage.getItem(PROFILE_KEY)
      return raw ? JSON.parse(raw) : { fullName: '', phone: '', email: '', city: '' }
    } catch { return { fullName: '', phone: '', email: '', city: '' } }
  }

  function loadRequests(): LocalRentalRequest[] {
    try {
      const raw = localStorage.getItem(REQUESTS_KEY)
      return raw ? JSON.parse(raw) : []
    } catch { return [] }
  }

  function saveProfile(data: CustomerProfile) {
    profile.value = { ...data }
    localStorage.setItem(PROFILE_KEY, JSON.stringify(profile.value))
  }

  function addRequest(req: Omit<LocalRentalRequest, 'localId' | 'submittedAt'>) {
    const entry: LocalRentalRequest = {
      ...req,
      localId:      crypto.randomUUID(),
      submittedAt:  new Date().toISOString(),
      trackingCode: req.trackingCode ?? null,
    }
    requests.value = [entry, ...requests.value]
    localStorage.setItem(REQUESTS_KEY, JSON.stringify(requests.value))
    return entry
  }

  function clearRequests() {
    requests.value = []
    localStorage.removeItem(REQUESTS_KEY)
  }

  return { profile, requests, saveProfile, addRequest, clearRequests }
})
