import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'

import { IonicVue } from '@ionic/vue'

import '@ionic/vue/css/core.css'
import '@ionic/vue/css/normalize.css'
import '@ionic/vue/css/structure.css'
import '@ionic/vue/css/typography.css'
import '@ionic/vue/css/padding.css'
import '@ionic/vue/css/flex-utils.css'
import '@ionic/vue/css/text-alignment.css'

import './theme/variables.css'
import './theme/app.css'

const pinia = createPinia()
const app   = createApp(App)
  .use(IonicVue, { mode: 'ios' })
  .use(pinia)
  .use(router)

// Carica il profilo dealer salvato prima di montare l'app
import { useOperatorStore } from './stores/operator'
router.isReady().then(() => {
  const opStore = useOperatorStore()
  opStore.load()
  app.mount('#app')
  // Aggiorna il profilo dal backend in background per avere sempre i settings più recenti
  opStore.refreshProfile()
})
