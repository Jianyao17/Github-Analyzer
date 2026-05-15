import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwind from '@tailwindcss/vite';
import ui from '@nuxt/ui/vite';

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    tailwind(),
    ui({
      prefix: 'N',
      theme: {
        defaultVariants: {
          size: 'lg',
        },
      },
    }),
  ],
  server: {
    port: Number(process.env.PORT ?? 3002),
    strictPort: true,
  },
});
