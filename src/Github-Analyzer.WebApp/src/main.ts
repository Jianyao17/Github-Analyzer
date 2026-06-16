import './styles/main.css';
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import { PiniaColada } from '@pinia/colada';
import { useThemeStore } from './stores/theme.store';
import ui from '@nuxt/ui/vue-plugin';
import router from './router';
import App from './App.vue';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(PiniaColada);
app.use(router);
app.use(ui);

const themeStore = useThemeStore();
themeStore.initTheme();

app.mount('#app');
