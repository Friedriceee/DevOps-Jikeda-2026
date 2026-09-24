import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'

import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth'

const pinia = createPinia()
useAuthStore(pinia).restoreSession()

createApp(App)
  .use(pinia)
  .use(router)
  .use(ElementPlus)
  .mount('#app')
