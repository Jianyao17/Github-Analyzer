import './styles/main.css';
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import { useThemeStore } from './stores/theme.store';
import { createGraphEngine } from './plugins/graph';
import ui from '@nuxt/ui/vue-plugin';
import router from './router';
import App from './App.vue';

const app = createApp(App);
const pinia = createPinia();
const graphEngine = createGraphEngine(true);

app.use(pinia);
app.use(router);
app.use(graphEngine);
app.use(ui);

const themeStore = useThemeStore();
themeStore.initTheme();

app.mount('#app');
