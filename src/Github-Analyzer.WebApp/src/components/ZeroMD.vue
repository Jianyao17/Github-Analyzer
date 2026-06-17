<script setup lang="ts">
import { ref, watch, onUnmounted, computed } from 'vue';
import { useThemeStore } from '@/stores/theme.store';
import ZeroMd, { STYLES } from 'zero-md';

// Buat custom element terpisah yang mewarisi ZeroMd agar bisa menggunakan STYLES preset bawaan
if (typeof window !== 'undefined' && !customElements.get('zero-md-custom')) 
{
  customElements.define('zero-md-custom', class extends ZeroMd 
  {
    async load() 
    {
      await super.load();
      const themeAttr = this.getAttribute('theme');
      if (themeAttr === 'dark') 
      {
        this.template = STYLES.preset('dark') + `
          <style>
            .markdown-body {
              background-color: transparent !important;
            }
          </style>
        `;
      }
      else 
      {
        this.template = STYLES.preset('light') + `
          <style>
            .markdown-body {
              background-color: transparent !important;
            }
          </style>
        `;
      }
    }
  });
}

const props = defineProps<{
  content: string;
}>();

const themeStore = useThemeStore();
const componentKey = computed(() => themeStore.theme);

const mdUrl = ref('');

watch(() => props.content, (newContent) => 
{
  if (mdUrl.value) 
  {
    URL.revokeObjectURL(mdUrl.value);
    mdUrl.value = '';
  }
  if (newContent) 
  {
    // Create blob for markdown file so zero-md can read it
    const blob = new Blob([newContent], { type: 'text/markdown' });
    mdUrl.value = URL.createObjectURL(blob);
  }
}, { immediate: true });

onUnmounted(() => 
{
  if (mdUrl.value) 
  {
    URL.revokeObjectURL(mdUrl.value);
  }
});
</script>

<template>
  <div class="zero-md-wrapper w-full">
    <zero-md-custom :src="mdUrl"
      :theme="themeStore.theme"
      :key="componentKey"
    ></zero-md-custom>
  </div>
</template>

<style scoped>
.zero-md-wrapper {
  display: block;
}
</style>
