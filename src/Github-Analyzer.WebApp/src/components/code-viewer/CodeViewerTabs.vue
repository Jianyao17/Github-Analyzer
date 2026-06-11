<script setup lang="ts">
import { ref } from 'vue';

defineProps<{
  tabs: { id: string; label: string }[];
  activeTabId: string | null;
}>();

const emit = defineEmits<{
  (e: 'select', id: string): void;
  (e: 'close', id: string): void;
}>();

const scrollContainer = ref<HTMLElement | null>(null);

const handleWheel = (e: WheelEvent) => 
{
  if (!scrollContainer.value) return;
  
  // If scrolling primarily vertically (like a standard mouse wheel), translate it to horizontal
  if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) 
  {
    e.preventDefault();
    scrollContainer.value.scrollBy({
      left: e.deltaY * 2,
      behavior: 'smooth'
    });
  }
};
</script>

<template>
  <div 
    ref="scrollContainer"
    @wheel="handleWheel"
    class="
      hide-scrollbar flex min-h-[42px] items-center gap-1 overflow-x-auto px-2
      pt-2
    "
  >

    <div
      v-for="tab in tabs"
      :key="tab.id"
      class="
        group relative z-10 flex shrink-0 cursor-pointer items-center gap-2
        rounded-t-lg border-t border-r border-b border-l px-3 py-1.5 text-sm
        transition-colors
      "
      :class="[
        activeTabId === tab.id
          ? `
            border-gray-200 border-b-transparent bg-white text-gray-900
            dark:border-gray-800 dark:border-b-transparent dark:bg-[#1f1f1f]
            dark:text-gray-100
          `
          : `
            border-transparent border-b-gray-200 text-gray-500
            hover:bg-gray-100
            dark:border-b-gray-800 dark:text-gray-400
            dark:hover:bg-gray-800
          `
      ]"
      @click="emit('select', tab.id)"
    >
      <span class="max-w-[150px] truncate font-medium">{{ tab.label }}</span>
      <button
        type="button"
        class="
          rounded p-0.5 opacity-0
          group-hover:opacity-100
          hover:bg-gray-200
          dark:hover:bg-gray-700
        "
        :class="{ 'opacity-100': activeTabId === tab.id }"
        @click.stop="emit('close', tab.id)"
      >
        <NIcon name="i-heroicons-x-mark-20-solid"
          class="h-3.5 w-3.5"
        />
      </button>
    </div>
  </div>
</template>
<style scoped>
.hide-scrollbar {
  -ms-overflow-style: none;  /* IE and Edge */
  scrollbar-width: none;  /* Firefox */
}
.hide-scrollbar::-webkit-scrollbar {
  display: none; /* Chrome, Safari and Opera */
}
</style>
