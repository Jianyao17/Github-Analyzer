import './styles/main.css'
import App from './App.vue'
import ui from '@nuxt/ui/vue-plugin'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createAppRouter } from './router'
import { useThemeStore } from './stores/theme'

const app = createApp(App)
const pinia = createPinia()
const router = createAppRouter(pinia)

app.use(pinia)
app.use(router)
app.use(ui)

const themeStore = useThemeStore(pinia)
themeStore.initialize()

app.mount('#app')
