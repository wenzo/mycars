import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { Capacitor } from '@capacitor/core'

export interface OperatorProfile {
  operatorId: string
  businessName: string
  publicCode: string
  slug: string
  phone: string | null
  email: string | null
  websiteUrl: string | null
  whatsappNumber: string | null
  address: string | null
  city: string | null
  province: string | null
  zipCode: string | null
  primaryColor: string | null
  secondaryColor: string | null
  accentColor: string | null
  logoUrl: string | null
  coverImageUrl: string | null
  tagline: string | null
  // Modulo noleggio
  rentalModuleEnabled:      boolean
  rentalPhotosEnabled:      boolean
  rentalContractPdfEnabled: boolean
  rentalShowPrices:         boolean
  rentalConditions:         Record<string, unknown> | null
  rentalServicesCatalog:    Record<string, unknown> | null
  // Privacy
  privacyPolicyHtml:        string | null
}

const STORAGE_KEY = 'mycars_operator'

export const useOperatorStore = defineStore('operator', () => {
  const profile = ref<OperatorProfile | null>(null)

  const isConnected = computed(() => profile.value !== null)
  const slug = computed(() => profile.value?.slug ?? '')
  const apiBase = computed(() => {
    const env = import.meta.env.VITE_API_BASE_URL as string | undefined
    if (env) return env
    // Su Capacitor nativo gli URL relativi puntano a capacitor://localhost, non al server.
    // In quel caso usiamo sempre l'URL di produzione come fallback.
    return Capacitor.isNativePlatform() ? 'https://www.mywebexp.com' : ''
  })

  function resolveUrl(url: string | null | undefined): string {
    if (!url) return ''
    if (url.startsWith('http')) return url
    return (import.meta.env.VITE_API_BASE_URL ?? '') + url
  }

  function load() {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) {
      try {
        profile.value = JSON.parse(raw)
        applyTheme()
      } catch {
        localStorage.removeItem(STORAGE_KEY)
      }
    }
  }

  function save(data: OperatorProfile) {
    profile.value = data
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
    applyTheme()
  }

  function disconnect() {
    profile.value = null
    localStorage.removeItem(STORAGE_KEY)
    applyTheme()
  }

  function applyTheme() {
    const root = document.documentElement
    const p  = profile.value

    // Fallback ai colori di default del mockup
    root.style.setProperty('--dealer-primary',   p?.primaryColor   ?? '#1E3A5F')
    root.style.setProperty('--dealer-secondary', p?.secondaryColor ?? '#D62828')
    root.style.setProperty('--dealer-accent',    p?.accentColor    ?? '#2E75B6')
  }

  async function connectByCode(code: string): Promise<void> {
    const base = apiBase.value
    const res = await fetch(`${base}/api/operator/connect?code=${encodeURIComponent(code)}`)
    if (!res.ok) throw new Error('Codice non valido o scaduto.')
    const data: OperatorProfile = await res.json()
    save(data)
  }

  async function refreshProfile(): Promise<void> {
    if (!profile.value?.slug) return
    const base = apiBase.value
    const res = await fetch(`${base}/api/operator/profile/${profile.value.slug}`)
    if (res.ok) {
      const data: OperatorProfile = await res.json()
      save(data)
    }
  }

  async function fetchPrivacyPolicy(): Promise<string | null> {
    if (!slug.value) return null
    try {
      const res = await fetch(`${apiBase.value}/api/public/privacy-policy?slug=${encodeURIComponent(slug.value)}`)
      if (!res.ok) return null
      const data = await res.json()
      return data.html || null
    } catch {
      return null
    }
  }

  return {
    profile,
    isConnected,
    slug,
    apiBase,
    resolveUrl,
    load,
    save,
    disconnect,
    applyTheme,
    connectByCode,
    refreshProfile,
    fetchPrivacyPolicy,
  }
})
