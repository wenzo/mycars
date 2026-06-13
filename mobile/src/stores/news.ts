import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useOperatorStore } from './operator'

export interface NewsItem {
  id: string
  newsType: string
  code: string | null
  title: string
  slug: string
  excerpt: string | null
  body: string | null
  coverImageUrl: string | null
  linkUrl: string | null
  startsAt: string | null
  expiresAt: string | null
  publishedAt: string | null
}

export const useNewsStore = defineStore('news', () => {
  const op = useOperatorStore()

  const items      = ref<NewsItem[]>([])
  const totalCount = ref(0)
  const loading    = ref(false)
  const error      = ref<string | null>(null)
  const detail     = ref<NewsItem | null>(null)
  const activeType = ref<string | null>(null)
  const page       = ref(0)
  const pageSize   = 20

  async function fetchNews(reset = false) {
    if (!op.slug) return
    if (reset) page.value = 0
    loading.value = true
    error.value   = null
    try {
      const q = new URLSearchParams({ page: String(page.value), pageSize: String(pageSize) })
      if (activeType.value) q.set('newsType', activeType.value)
      const res = await fetch(`${op.apiBase}/api/public/${op.slug}/news?${q}`)
      const text = await res.text()
      const data = text ? JSON.parse(text) : {}
      if (!res.ok) throw new Error(data?.error ?? data?.message ?? `Errore ${res.status}`)
      if (reset) items.value = data.items ?? []
      else items.value.push(...(data.items ?? []))
      totalCount.value = data.totalCount ?? 0
    } catch (e: any) {
      error.value = e?.message ?? 'Errore caricamento news'
    } finally {
      loading.value = false
    }
  }

  async function fetchNextPage() {
    if (loading.value || items.value.length >= totalCount.value) return
    page.value++
    await fetchNews(false)
  }

  async function fetchDetail(id: string) {
    if (!op.slug) return
    loading.value = true
    detail.value  = null
    try {
      const res = await fetch(`${op.apiBase}/api/public/${op.slug}/news/${id}`)
      if (!res.ok) throw new Error('News non trovata')
      detail.value = await res.json()
    } finally {
      loading.value = false
    }
  }

  function setType(type: string | null) {
    activeType.value = type
    fetchNews(true)
  }

  return {
    items, totalCount, loading, error, detail, activeType, page,
    fetchNews, fetchNextPage, fetchDetail, setType,
  }
})
