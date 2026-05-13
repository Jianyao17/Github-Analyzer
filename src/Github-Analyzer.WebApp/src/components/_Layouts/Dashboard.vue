<script setup lang="ts">
import { useSidebar } from '../../composables/useSidebar';
import { useAuthStore } from '../../stores/auth.store';
import { useRouter } from 'vue-router';
import { useThemeStore } from '../../stores/theme.store';

const { isCollapsed, isOpen, isMobile, close, toggleCollapse } = useSidebar();
const auth = useAuthStore();
const theme = useThemeStore();
const router = useRouter();

import { onMounted, ref } from 'vue';
import { useProjectApi } from '../../composables/useProjectApi';
import type { ProjectResponse } from '../../composables/useProjectApi';

const { fetchProjects } = useProjectApi();

const projects = ref<ProjectResponse[]>([]);

onMounted(async () => {
  try {
    projects.value = await fetchProjects();
  } catch (e) {
    console.error('Failed to fetch projects for sidebar', e);
  }
});

function handleLogout() {
  auth.logout();
  router.push('/login');
}
</script>

<template>
  <div class="flex h-screen bg-gray-50 dark:bg-gray-950 overflow-hidden font-sans">
    <!-- Sidebar Overlay (Mobile) -->
    <div 
      v-if="isMobile && isOpen" 
      class="fixed inset-0 bg-gray-900/50 backdrop-blur-sm z-40 transition-opacity" 
      @click="close" 
    />

    <!-- Sidebar -->
    <aside
      class="fixed inset-y-0 left-0 z-50 flex flex-col bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 transition-all duration-300 ease-in-out shadow-lg"
      :class="[
        isMobile ? (isOpen ? 'translate-x-0 w-64' : '-translate-x-full w-64') : (isCollapsed ? 'w-20' : 'w-64')
      ]"
    >
      <!-- Logo Section -->
      <div class="h-16 flex items-center px-4 gap-3 border-b border-gray-100 dark:border-gray-800 shrink-0" :class="isCollapsed && !isMobile ? 'justify-center' : 'justify-between'">
        <!-- Collapsed: Logo is the toggle button -->
        <UButton
          v-if="isCollapsed && !isMobile"
          icon="i-lucide-github"
          color="gray"
          variant="ghost"
          class="w-10 h-10 flex items-center justify-center text-primary-600 dark:text-primary-400"
          @click="toggleCollapse"
        />
        
        <!-- Expanded: Logo + Text + Toggle Button -->
        <div v-else class="flex items-center w-full justify-between">
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-8 h-8 bg-primary-600 rounded-lg flex items-center justify-center shrink-0">
              <UIcon name="i-lucide-github" class="w-5 h-5 text-white" />
            </div>
            <span class="font-bold text-lg tracking-tight text-gray-900 dark:text-white truncate">
              GitAnalyzer
            </span>
          </div>
          <UButton
            variant="ghost"
            color="gray"
            icon="i-lucide-panel-left-close"
            class="shrink-0"
            @click="isMobile ? close() : toggleCollapse()"
          />
        </div>
      </div>

      <!-- New Analysis Button -->
      <div class="p-4 border-b border-gray-100 dark:border-gray-800">
        <UButton
          to="/app/analysis/new"
          icon="i-lucide-plus"
          label="New Analysis"
          color="primary"
          block
          size="md"
          v-show="!isCollapsed || isMobile"
        />
        <UButton
          v-show="isCollapsed && !isMobile"
          to="/app/analysis/new"
          icon="i-lucide-plus"
          color="primary"
          block
          size="md"
        />
      </div>

      <!-- Navigation Links -->
      <div class="flex-1 overflow-y-auto py-4 px-3 space-y-1">
        <div v-if="projects.length === 0" class="px-2 py-4 text-xs text-center text-gray-500">
          <span v-if="!isCollapsed || isMobile">No projects yet</span>
          <UIcon v-else name="i-lucide-folder-open" class="w-5 h-5 mx-auto" />
        </div>
        <UButton
          v-for="project in projects"
          :key="project.id"
          :to="{ name: 'app.project-detail', params: { id: project.id } }"
          color="gray"
          variant="ghost"
          class="w-full justify-start py-2"
          active-class="bg-primary-50 dark:bg-primary-950 text-primary-600 dark:text-primary-400"
        >
          <div class="flex items-center gap-2 w-full overflow-hidden">
            <UIcon name="i-lucide-github" class="w-5 h-5 shrink-0" />
            <div v-if="!isCollapsed || isMobile" class="flex flex-col items-start min-w-0 text-left">
              <span class="truncate w-full font-medium">{{ project.repositoryName }}</span>
              <span class="text-[10px] text-gray-400 dark:text-gray-500 font-normal mt-0.5">{{ new Date(project.createdAtUtc).toLocaleDateString() }}</span>
            </div>
          </div>
        </UButton>
      </div>

      <!-- User Profile & Settings -->
      <div class="p-4 border-t border-gray-100 dark:border-gray-800 bg-gray-50/50 dark:bg-gray-900/50">
        <div class="flex items-center justify-between mb-4" v-if="!isCollapsed || isMobile">
          <UButton
            :icon="theme.theme === 'dark' ? 'i-lucide-moon' : 'i-lucide-sun'"
            color="gray"
            variant="ghost"
            size="sm"
            @click="theme.toggleTheme"
          />
          <UButton
            icon="i-lucide-log-out"
            color="red"
            variant="ghost"
            size="sm"
            @click="handleLogout"
          />
        </div>

        <div class="flex items-center gap-3 p-2 rounded-xl bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 shadow-sm">
          <UAvatar
            :alt="auth.user?.username || 'GA'"
            size="sm"
            class="bg-primary-600 text-white font-bold shrink-0"
          />
          <div v-if="!isCollapsed || isMobile" class="min-w-0 flex-1">
            <p class="text-sm font-bold text-gray-900 dark:text-white truncate">
              {{ auth.user?.username || 'Guest' }}
            </p>
            <p class="text-[10px] text-gray-500 truncate uppercase font-bold tracking-widest">
              {{ auth.user?.email || 'Not logged in' }}
            </p>
          </div>
        </div>
      </div>
    </aside>

    <!-- Main Content Area -->
    <main
      class="flex-1 flex flex-col min-w-0 transition-all duration-300"
      :class="[!isMobile ? (isCollapsed ? 'ml-20' : 'ml-64') : 'ml-0']"
    >
      <!-- Floating Mobile Menu Button -->
      <UButton
        v-if="isMobile && !isOpen"
        variant="solid"
        color="primary"
        icon="i-lucide-menu"
        class="fixed top-4 left-4 z-40 lg:hidden shadow-lg rounded-full w-12 h-12 flex items-center justify-center"
        @click="isOpen = true"
      />

      <!-- Page Content -->
      <div class="flex-1 h-screen overflow-y-auto">
        <div class="w-full h-full p-4 lg:p-6">
          <RouterView :key="$route.fullPath" />
        </div>
      </div>
    </main>
  </div>
</template>
