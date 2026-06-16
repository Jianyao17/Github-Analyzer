import { fileURLToPath, URL } from 'node:url';
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
      ui: {
        colors: {
          neutral: 'neutral'
        }
      },
    }),
    visualizer({
      filename: 'bundle-analysis.html',
      template: 'treemap',
      gzipSize: true,
      brotliSize: true,
      open: true,
    })
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@graph': fileURLToPath(new URL('./src/lib/graph', import.meta.url)),
      '@graph.types': fileURLToPath(new URL('./src/lib/graph/types/_index.ts', import.meta.url)),
      '@graph.plugins': fileURLToPath(new URL('./src/lib/graph/plugins/_index.ts', import.meta.url)),
    },
  },
  server: {
    port: Number(process.env.PORT ?? 5017),
    strictPort: true,
  },
  preview: {
    port: Number(process.env.PORT ?? 5017),
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

          // CodeViewer libraries
          if (id.includes('/codemirror') || 
              id.includes('/@codemirror') || 
              id.includes('/crelt'))
            return 'vendor-codemirror';
          
          // SplitPanes
          if (id.includes('/splitpanes'))
            return 'vendor-splitpanes';

          // Data + state
          if (id.includes('/axios') || id.includes('/pinia') || id.includes('@pinia/colada')) 
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
