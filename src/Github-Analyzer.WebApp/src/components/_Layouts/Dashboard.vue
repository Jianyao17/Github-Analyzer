<script setup lang="ts">
import type { ProjectResponse } from '../../composables/useProjectApi';
import { useProjectApi } from '../../composables/useProjectApi';
import { useSidebar } from '../../composables/useSidebar';
import { useThemeStore } from '../../stores/theme.store';
import { useAuthStore } from '../../stores/auth.store';
import { useRoute, useRouter } from 'vue-router';
import { onMounted, ref, watch } from 'vue';

const { isCollapsed, isOpen, isMobile, 
  close, toggleCollapse } = useSidebar();

const auth = useAuthStore();
const theme = useThemeStore();
const route = useRoute();
const router = useRouter();

const { fetchProjects } = useProjectApi();

const projects = ref<ProjectResponse[]>([]);

async function loadProjects() 
{
  try 
  {
    projects.value = await fetchProjects();
  }
  catch (e) 
  {
    console.error('Failed to fetch projects for sidebar', e);
  }
}

onMounted(() => 
{
  loadProjects();
});

// Refresh list tiap kali masuk ke route project detail baru (dari halaman "New Analysis")
watch(() => route.params.id, (newId) => 
{
  if (newId) 
  {
    // Refresh untuk memastikan project baru juga ditarik ke dalam daftar sidebar
    loadProjects();
  }
});

function handleLogout() 
{
  auth.logout();
  router.push('/login');
}
</script>

<template>
  <div class="
    flex h-screen overflow-hidden bg-gray-50 font-sans
    dark:bg-gray-950
  "
  >
    <!-- Sidebar Overlay (Mobile) -->
    <div 
      v-if="isMobile && isOpen" 
      class="
        fixed inset-0 z-40 bg-gray-900/50 backdrop-blur-sm transition-opacity
      " 
      @click="close" 
    />

    <!-- Sidebar -->
    <aside
      class="
        fixed inset-y-0 left-0 z-50 flex flex-col border-r border-gray-200
        bg-white shadow-lg transition-all duration-300 ease-in-out
        dark:border-gray-800 dark:bg-gray-900
      "
      :class="[
        isMobile ? (isOpen ? 'w-64 translate-x-0' : 'w-64 -translate-x-full') : (isCollapsed ? `
          w-20
        ` : `w-64`)
      ]"
    >
      <!-- Logo Section -->
      <div class="
        flex h-16 shrink-0 items-center gap-3 border-b border-gray-100 px-4
        dark:border-gray-800
      "
        :class="isCollapsed && !isMobile ? `justify-center` : `justify-between`"
      >
        <!-- Collapsed: Logo is the toggle button -->
        <NButton
          v-if="isCollapsed && !isMobile"
          icon="i-lucide-github"
          color="gray"
          variant="ghost"
          class="
            text-primary-600
            dark:text-primary-400
            flex h-10 w-10 items-center justify-center
          "
          @click="toggleCollapse"
        />
        
        <!-- Expanded: Logo + Text + Toggle Button -->
        <div v-else
          class="flex w-full items-center justify-between"
        >
          <div class="flex min-w-0 items-center gap-3">
            <div class="
              bg-primary-600 flex h-8 w-8 shrink-0 items-center justify-center
              rounded-lg
            "
            >
              <NIcon name="i-lucide-github"
                class="h-5 w-5 text-white"
              />
            </div>
            <span class="
              truncate text-lg font-bold tracking-tight text-gray-900
              dark:text-white
            "
            >
              GitHub Analyzer
            </span>
          </div>
          <NButton
            variant="ghost"
            color="gray"
            icon="i-lucide-panel-left-close"
            class="shrink-0"
            @click="isMobile ? close() : toggleCollapse()"
          />
        </div>
      </div>

      <!-- New Analysis Button -->
      <div class="
        border-b border-gray-100 p-4
        dark:border-gray-800
      "
      >
        <NButton
          to="/app/analysis/new"
          icon="i-lucide-plus"
          label="New Analysis"
          color="primary"
          block
          size="md"
          v-show="!isCollapsed || isMobile"
        />
        <NButton
          v-show="isCollapsed && !isMobile"
          to="/app/analysis/new"
          icon="i-lucide-plus"
          color="primary"
          block
          size="md"
        />
      </div>

      <!-- Navigation Links -->
      <div class="flex-1 space-y-1 overflow-y-auto px-3 py-4">
        <div v-if="projects.length === 0"
          class="px-2 py-4 text-center text-xs text-gray-500"
        >
          <span v-if="!isCollapsed || isMobile">No projects yet</span>
          <NIcon v-else
            name="i-lucide-folder-open"
            class="mx-auto h-5 w-5"
          />
        </div>
        <NButton
          v-for="project in projects"
          :key="project.id"
          :to="{ name: 'app.project-detail', params: { id: project.id } }"
          :color="route.params.id === project.id ? 'primary' : 'gray'"
          :variant="route.params.id === project.id ? 'soft' : 'ghost'"
          class="w-full justify-start py-2 transition-colors"
          :class="route.params.id === project.id ? `
            bg-primary-50
            dark:bg-primary-950
            text-primary-600
            dark:text-primary-400
            font-bold
          ` : ''"
        >
          <div class="flex w-full items-center gap-2 overflow-hidden">
            <NIcon name="i-lucide-github"
              class="h-5 w-5 shrink-0"
            />
            <div v-if="!isCollapsed || isMobile"
              class="flex min-w-0 flex-col items-start text-left"
            >
              <span class="w-full truncate"
                :class="route.params.id === project.id ? `font-bold` : `
                  font-medium
                `"
              >{{ project.repositoryName }}</span>
              <span class="mt-0.5 max-w-full truncate text-[10px] font-normal"
                :class="route.params.id === project.id ? `
                  text-primary-500/80
                  dark:text-primary-400/80
                ` : `
                  text-gray-400
                  dark:text-gray-500
                `"
              >{{ new Date(project.createdAtUtc).toLocaleDateString() }}</span>
            </div>
          </div>
        </NButton>
      </div>

      <!-- User Profile & Settings -->
      <div class="
        border-t border-gray-100 bg-gray-50/50 p-4
        dark:border-gray-800 dark:bg-gray-900/50
      "
      >
        <div class="mb-4 flex items-center justify-between"
          v-if="!isCollapsed || isMobile"
        >
          <NButton
            :icon="theme.theme === 'dark' ? 'i-lucide-moon' : 'i-lucide-sun'"
            color="gray"
            variant="ghost"
            size="sm"
            @click="theme.toggleTheme"
          />
          <NButton
            icon="i-lucide-log-out"
            color="red"
            variant="ghost"
            size="sm"
            @click="handleLogout"
          />
        </div>

        <div class="
          flex items-center gap-3 rounded-xl border border-gray-100 bg-white p-2
          shadow-sm
          dark:border-gray-700 dark:bg-gray-800
        "
        >
          <NAvatar
            :alt="auth.user?.username || 'GA'"
            size="sm"
            class="bg-primary-600 shrink-0 font-bold text-white"
          />
          <div v-if="!isCollapsed || isMobile"
            class="min-w-0 flex-1"
          >
            <p class="
              truncate text-sm font-bold text-gray-900
              dark:text-white
            "
            >
              {{ auth.user?.username || 'Guest' }}
            </p>
            <p class="
              truncate text-[10px] font-bold tracking-widest text-gray-500
              uppercase
            "
            >
              {{ auth.user?.email || 'Not logged in' }}
            </p>
          </div>
        </div>
      </div>
    </aside>

    <!-- Main Content Area -->
    <main
      class="flex min-w-0 flex-1 flex-col transition-all duration-300"
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
        <div class="
          flex h-full w-full flex-col p-4
          lg:p-6
        "
        >
          <RouterView :key="$route.fullPath"
            class="min-h-0 flex-1"
          />
        </div>
      </div>
    </main>
  </div>
</template>
