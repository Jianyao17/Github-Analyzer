<script setup lang="ts">
import { useSidebar } from '../../composables/useSidebar';
import Sidebar from '../MainSidebar.vue';

const { isCollapsed, isOpen, isMobile, 
  close, toggleCollapse } = useSidebar();
</script>

<template>
  <div class="
    flex h-screen overflow-hidden bg-gray-50 font-sans
    dark:bg-gray-950
  "
  >
    <Sidebar
      :is-open="isOpen"
      :is-collapsed="isCollapsed"
      :is-mobile="isMobile"
      @toggle-collapse="toggleCollapse"
      @close="close"
    />

    <!-- Main Content Area -->
    <main
      class="
        flex min-w-0 flex-1 flex-col transition-[margin-left] duration-300
        will-change-[margin-left]
      "
      :class="[!isMobile ? (isCollapsed ? 'ml-20' : 'ml-64') : 'ml-0']"
    >
      <!-- Floating Mobile Menu Button -->
      <NButton
        v-if="isMobile && !isOpen"
        variant="solid"
        color="primary"
        icon="i-lucide-menu"
        class="
          fixed top-4 left-4 z-40 flex h-12 w-12 items-center justify-center
          rounded-full shadow-lg
          lg:hidden
        "
        @click="isOpen = true"
      />

      <!-- Page Content -->
      <div class="min-h-0 flex-1 overflow-x-hidden overflow-y-auto">
        <RouterView :key="$route.fullPath"
          class="min-h-0 flex-1"
        />
      </div>
    </main>
  </div>
</template>
