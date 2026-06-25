import { createRouter, createWebHistory } from '@ionic/vue-router'
import { RouteRecordRaw } from 'vue-router'

const routes: Array<RouteRecordRaw> = [
  { path: '/', redirect: '/splash' },
  { path: '/splash', component: () => import('@/views/SplashPage.vue') },
  { path: '/home',   component: () => import('@/views/HomePage.vue') },

  {
    path: '/tabs',
    component: () => import('@/views/TabsPage.vue'),
    children: [
      { path: '', redirect: '/tabs/vetrina' },
      { path: 'vetrina',        component: () => import('@/views/VetrinaPage.vue') },
      { path: 'news',           component: () => import('@/views/NewsPage.vue') },
      { path: 'contatti',       component: () => import('@/views/ContattiPage.vue') },
      { path: 'impostazioni',   component: () => import('@/views/ImpostazioniPage.vue') },
      { path: 'noleggio',       component: () => import('@/views/NoleggioHubPage.vue') },
      { path: 'profilo',         component: () => import('@/views/ProfiloPage.vue') },
      { path: 'veicolo/:id',    component: () => import('@/views/VehicleDetailPage.vue') },
      { path: 'news/:id',       component: () => import('@/views/NewsDetailPage.vue') },
      { path: 'ricerca',        component: () => import('@/views/SearchPage.vue') },
    ],
  },

  { path: '/noleggio/:id',  component: () => import('@/views/NoleggioDetailPage.vue') },
]

export default createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})
