import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import visualizer from 'rollup-plugin-visualizer';
import tailwind from '@tailwindcss/vite';
import ui from '@nuxt/ui/vite';

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    tailwind({
      optimize: true,
    }),
    ui({
      prefix: 'N',
      colorMode: false,
      theme: {
        transitions: true,
        defaultVariants: {
          size: 'lg',
        },
      },
    }),
    visualizer({
      filename: 'bundle-analysis.html',
      template: 'treemap',
      gzipSize: true,
      open: true,
    })
  ],
  server: {
    port: Number(process.env.PORT ?? 3002),
    strictPort: true,
  },
  preview: {
    port: Number(process.env.PORT ?? 3002),
    strictPort: true,
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: (id) =>
        {
          if (!id.includes('node_modules')) return;

          // Heavy graph/rendering libs
          if (id.includes('/d3')) 
            return 'vendor-d3';

          // Data + state
          if (id.includes('/axios') || id.includes('/pinia')) 
            return 'vendor-axios-pinia';

          // UI kit + icons + composables
          if (id.includes('/@nuxt/ui') || id.includes('/@iconify') || id.includes('/@vueuse/core')) 
            return 'vendor-ui-iconify-vueuse';

          // Core runtime + router
          if (id.includes('/vue') || id.includes('/vue-router')) 
            return 'vendor-vuecore-router';

          // Let Rollup decide the rest to avoid circular vendor chunks
          return;
        }
      }
    }
  }
});
