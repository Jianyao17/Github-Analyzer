<script setup lang="ts">
import { ref } from 'vue';
import { onClickOutside } from '@vueuse/core';

defineProps<{
    theme: 'light' | 'dark';
  }>();

defineEmits<{
    (e: 'update:theme', val: 'light' | 'dark'): void;
  }>();

const isOpen = ref(false);
const dropdownRef = ref<HTMLElement | null>(null);

onClickOutside(dropdownRef, () => 
{
  isOpen.value = false;
});
</script>

<template>
  <div class="pointer-events-auto relative flex items-center"
    ref="dropdownRef"
  >
    <button class="
      flex items-center justify-center rounded p-1.5 text-[var(--ui-text-muted)]
      transition-colors
      hover:bg-[var(--ui-bg-elevated)] hover:text-[var(--ui-text)]
    "
      :class="{ 'bg-[var(--ui-bg-elevated)] text-[var(--ui-text)]': isOpen }"
      title="Editor Settings"
      @click="isOpen = !isOpen"
    >
      <NIcon name="i-lucide-settings"
        class="h-4 w-4 transition-transform duration-300"
        :class="isOpen ? 'rotate-90' : ''"
      />
    </button>

    <transition enter-active-class="transition duration-200 ease-out"
      enter-from-class="transform scale-95 opacity-0 -translate-y-2"
      enter-to-class="transform scale-100 opacity-100 translate-y-0"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="transform scale-100 opacity-100 translate-y-0"
      leave-to-class="transform scale-95 opacity-0 -translate-y-2"
    >
      <div v-if="isOpen"
        class="
          absolute top-full right-0 z-50 mt-2 w-64 rounded-xl border
          border-[var(--ui-border)] bg-[var(--ui-bg)] p-3 shadow-xl
        "
      >
        <div class="
          mb-3 px-1 text-xs font-bold tracking-wider text-[var(--ui-text-muted)]
          uppercase
        "
        >
          Editor Settings
        </div>

        <div class="flex flex-col gap-3">
          <div class="flex gap-2">
            <button class="
              flex flex-1 items-center justify-center gap-2 rounded-lg border
              py-2 text-xs font-semibold transition-all duration-200
            "
              :class="[
                theme === 'light'
                  ? `
                    border-primary/30 bg-primary/5 text-primary
                    dark:border-primary/30 dark:bg-primary/10
                    dark:text-primary-400
                  `
                  : `
                    border-[var(--ui-border)] bg-[var(--ui-bg)]
                    text-[var(--ui-text-highlighted)]
                    hover:bg-[var(--ui-bg-elevated)]
                  `
              ]"
              title="Light Mode"
              @click="$emit('update:theme', 'light')"
            >
              <NIcon name="i-lucide-sun"
                class="h-3.5 w-3.5"
              />
              Light
            </button>
            <button class="
              flex flex-1 items-center justify-center gap-2 rounded-lg border
              py-2 text-xs font-semibold transition-all duration-200
            "
              :class="[
                theme === 'dark'
                  ? `
                    border-primary/30 bg-primary/5 text-primary
                    dark:border-primary/30 dark:bg-primary/10
                    dark:text-primary-400
                  `
                  : `
                    border-[var(--ui-border)] bg-[var(--ui-bg)]
                    text-[var(--ui-text-highlighted)]
                    hover:bg-[var(--ui-bg-elevated)]
                  `
              ]"
              title="Dark Mode"
              @click="$emit('update:theme', 'dark')"
            >
              <NIcon name="i-lucide-moon"
                class="h-3.5 w-3.5"
              />
              Dark
            </button>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>
