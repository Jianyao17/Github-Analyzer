<script setup lang="ts">
import { ref } from 'vue';
import { onClickOutside } from '@vueuse/core';
import type { GraphD3Options } from '@/composables/useGraphD3';

defineProps<{
  supportsNamespace: boolean;
}>();

// Model for graph settings
const settings = defineModel<Required<GraphD3Options>>('settings', { required: true });

const isSettingsOpen = ref(false);
const settingsDropdownContainer = ref<HTMLElement | null>(null);

// Close dropdown when clicking outside
onClickOutside(settingsDropdownContainer, () => 
{
  isSettingsOpen.value = false;
});
</script>

<template>
  <div class="pointer-events-auto relative"
    ref="settingsDropdownContainer"
  >
    <!-- Trigger Button -->
    <button
      class="
        relative z-20 flex h-[42px] items-center justify-between gap-2
        rounded-lg border border-gray-200 bg-white px-3 py-2.5 text-sm
        font-semibold text-gray-700 transition-all duration-200
        hover:border-gray-300 hover:bg-gray-50 hover:shadow-sm
        active:bg-gray-100
        sm:h-auto
        dark:border-gray-700 dark:bg-gray-900 dark:text-gray-300
        dark:hover:border-gray-600 dark:hover:bg-gray-800
        dark:active:bg-gray-800
      "
      :class="isSettingsOpen ? `
        border-gray-300 bg-gray-50 shadow-sm
        dark:border-gray-600 dark:bg-gray-800
      ` : ''"
      @click="isSettingsOpen = !isSettingsOpen"
    >
      <span class="
        hidden
        sm:inline
      "
      >Graph Settings</span>
      <NIcon 
        name="i-lucide-settings" 
        class="h-4 w-4 text-gray-400 transition-transform duration-300"
        :class="isSettingsOpen ? `
          rotate-90 text-gray-600
          dark:text-gray-200
        ` : ''"
      />
    </button>

    <!-- Custom Dropdown Menu -->
    <transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="transform scale-95 opacity-0 translate-y-2 sm:translate-y-2 sm:scale-95"
      enter-to-class="transform scale-100 opacity-100 translate-y-0 sm:translate-y-0 sm:scale-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="transform scale-100 opacity-100 translate-y-0"
      leave-to-class="transform scale-95 opacity-0 translate-y-2"
    >
      <div
        v-if="isSettingsOpen"
        class="
          fixed bottom-20 left-1/2 z-30 w-[90vw] max-w-[320px] -translate-x-1/2
          rounded-xl border border-gray-200 bg-white p-3 shadow-2xl
          sm:absolute sm:right-0 sm:bottom-full sm:left-auto sm:mb-3 sm:w-64
          sm:translate-x-0 sm:shadow-md
          dark:border-gray-700 dark:bg-gray-900
        "
      >
        <!-- Graph Settings Header -->
        <div class="
          mb-3 px-1 text-xs font-bold tracking-wider text-gray-400 uppercase
          dark:text-gray-500
        "
        >
          Graph Settings
        </div>
        
        <div class="flex flex-col gap-3">
          <!-- Graph Mode -->
          <div>
            <button
              class="
                flex w-full items-center justify-between gap-2 rounded-lg border
                border-gray-200 bg-white px-3 py-2.5 text-sm font-semibold
                text-gray-700 transition-all duration-200
                hover:border-gray-300 hover:bg-gray-50
                active:bg-gray-100
                dark:border-gray-700 dark:bg-gray-900 dark:text-gray-300
                dark:hover:border-gray-600 dark:hover:bg-gray-800
                dark:active:bg-gray-800
              "
              :disabled="!supportsNamespace"
              :class="!supportsNamespace ? `
                cursor-not-allowed opacity-50
                hover:border-gray-200 hover:bg-white
              ` : ''"
              @click="settings.mode = settings.mode === 'directory' ? 'namespace' : 'directory'"
            >
              <NIcon 
                class="h-4 w-4 shrink-0 transition-colors duration-200" 
                :name="settings.mode === 'directory' ? 'i-lucide-folder-tree' : 'i-lucide-boxes'" 
              />
              
              <span class="ml-1 min-w-0 flex-1 truncate text-left select-none">
                {{ settings.mode === 'directory' ? 'Directory Based' : 'Namespace Based' }}
              </span>

              <NIcon 
                name="i-lucide-arrow-right-left" 
                class="h-3.5 w-3.5 shrink-0 text-gray-400 opacity-50" 
              />
            </button>

            <div v-if="!supportsNamespace"
              class="
                mt-2 px-1 text-left text-[10px] leading-tight text-gray-400
              "
            >
              This repository's language does not support Namespaces.
            </div>
          </div>

          <!-- Layout Algorithm -->
          <div class="
            rounded-lg border border-gray-200 bg-white transition-all
            duration-200
            hover:border-gray-300 hover:bg-gray-50
            dark:border-gray-700 dark:bg-gray-900
            dark:hover:border-gray-600 dark:hover:bg-gray-800
          "
          >
            <NSelect
              v-model="settings.layout"
              :items="[
                { label: 'Star Balloon', value: 'star-balloon', icon: 'i-lucide-network' },
                { label: 'Hierarchical', value: 'hierarchical', icon: 'i-lucide-git-merge' }
              ]"
              icon="i-lucide-layout-dashboard"
              size="lg"
              variant="none"
              class="relative z-50 w-full"
              :content="{ position: 'popper', side: 'top', align: 'center', sideOffset: 8 }"
              :ui="{
                content: 'z-[100]',
                base: 'font-semibold text-gray-700 dark:text-gray-300 cursor-pointer',
                leadingIcon: 'text-gray-400 shrink-0',
                trailingIcon: 'text-gray-400 opacity-50 shrink-0'
              }"
            />
          </div>
          <!-- Orientation Algorithm (Only visible if hierarchical) -->
          <div v-if="settings.layout === 'hierarchical'" class="
            rounded-lg border border-gray-200 bg-white transition-all
            duration-200
            hover:border-gray-300 hover:bg-gray-50
            dark:border-gray-700 dark:bg-gray-900
            dark:hover:border-gray-600 dark:hover:bg-gray-800
          "
          >
            <NSelect
              v-model="settings.orientation"
              :items="[
                { label: 'Left to Right', value: 'LR', icon: 'i-lucide-arrow-right' },
                { label: 'Right to Left', value: 'RL', icon: 'i-lucide-arrow-left' },
                { label: 'Top to Bottom', value: 'TB', icon: 'i-lucide-arrow-down' },
                { label: 'Bottom to Top', value: 'BT', icon: 'i-lucide-arrow-up' }
              ]"
              icon="i-lucide-monitor-smartphone"
              size="lg"
              variant="none"
              class="relative z-50 w-full"
              :content="{ position: 'popper', side: 'top', align: 'center', sideOffset: 8 }"
              :ui="{
                content: 'z-[100]',
                base: 'font-semibold text-gray-700 dark:text-gray-300 cursor-pointer',
                leadingIcon: 'text-gray-400 shrink-0',
                trailingIcon: 'text-gray-400 opacity-50 shrink-0'
              }"
            />
          </div>
        </div>
        
      </div>
    </transition>
  </div>
</template>
