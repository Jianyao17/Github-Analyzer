<script setup lang="ts">
import { useRoute } from 'vue-router';
import { computed, onMounted, ref, watch } from 'vue';
import type { ProjectResponse } from '../types/_api/project.ts';
import { useProjectApi } from '../composables/useProjectApi';
import ProjectItemButton from './ProjectItemButton.vue';
import UserProfileCard from './UserProfileCard.vue';

type SidebarProps = {
  isCollapsed: boolean
  isOpen: boolean
  isMobile: boolean
};

defineProps<SidebarProps>();

defineEmits<{
  (e: 'close'): void
  (e: 'toggle-collapse'): void
}>();

const route = useRoute();
const { fetchProjects } = useProjectApi();

const projects = ref<ProjectResponse[]>([]);
const isProjectsCollapsed = ref(false);

const activeProjectId = computed(() => 
  (Array.isArray(route.params.id)
    ? route.params.id[0]
    : route.params.id));

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

watch(() => route.params.id, (newId) => 
{
  if (newId) 
  {
    loadProjects();
  }
});

function toggleProjectsCollapsed() 
{
  isProjectsCollapsed.value = !isProjectsCollapsed.value;
}
</script>

<template>
  <!-- Sidebar Overlay (Mobile) -->
  <div 
    v-if="isMobile && isOpen" 
    class="
      fixed inset-0 z-40 bg-gray-900/50 backdrop-blur-sm transition-opacity
    " 
    @click="$emit('close')" 
  />

  <!-- Sidebar -->
  <Transition name="sidebar-slide">
    <aside
      v-show="!isMobile || isOpen"
      class="
        fixed inset-y-0 left-0 z-50 flex flex-col border-r border-gray-200
        bg-white transition-[width] duration-300 ease-in-out
        will-change-[width,transform]
        dark:border-gray-800 dark:bg-gray-900
      "
      :class="isMobile ? 'w-64' : (isCollapsed ? 'w-18' : 'w-64')"
    >
      <!-- Logo Section -->
      <div class="flex h-16 shrink-0 items-center gap-3 px-3 py-4"
        :class="isCollapsed && !isMobile ? `justify-center` : `justify-between`"
      >
        <!-- Collapsed: Logo is the toggle button -->
        <NButton
          v-if="isCollapsed && !isMobile"
          icon="i-lucide-github"
          color="gray"
          variant="ghost"
          class="
            mx-auto flex h-10 w-10 items-center justify-center text-primary-600
            dark:text-primary-400
          "
          @click="$emit('toggle-collapse')"
        />
      
        <!-- Expanded: Logo + Text + Toggle Button -->
        <div v-else
          class="flex w-full items-center justify-between"
        >
          <div class="flex min-w-0 items-center gap-3">
            <div class="
              flex h-8 w-8 shrink-0 items-center justify-center rounded-lg
              bg-primary-600
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
            @click="isMobile ? $emit('close') : $emit('toggle-collapse')"
          />
        </div>
      </div>

      <!-- New Analysis Button -->
      <div class="px-3 py-4">
        <!-- Expanded or Mobile: Show icon + text -->
        <NButton
          v-show="!isCollapsed || isMobile"
          to="/app/analysis/new"
          icon="i-lucide-plus"
          label="New Analysis"
          color="primary"
          size="lg"
          block
        />
        <!-- Collapsed Mobile: Show only icon -->
        <NButton
          v-show="isCollapsed && !isMobile"
          to="/app/analysis/new"
          icon="i-lucide-plus"
          color="primary"
          class="mx-auto"
          size="lg"
          block
        />
      </div>

      <!-- Navigation Links -->
      <div class="flex min-h-0 flex-1 flex-col p-3">
        <div class="group mb-2 flex items-center justify-between px-2"
          v-if="!isCollapsed || isMobile"
        >
          <div class="w-1/2">
            <span class="
              text-sm font-medium text-gray-600
              dark:text-gray-300
            "
            >
              Projects
            </span>
          </div>
          <div class="w-1/2">
            <NButton
              variant="ghost"
              color="gray"
              size="sm"
              class="
                group w-full justify-end gap-1.5 px-0 text-xs text-gray-500
                hover:text-gray-700
                dark:text-gray-400
                dark:hover:text-gray-200
              "
              @click="toggleProjectsCollapsed"
            >
              <span class="
                opacity-0 transition-opacity
                group-hover:opacity-100
              "
              >
                {{ isProjectsCollapsed ? 'Expand' : 'Collapse' }}
              </span>
              <NIcon
                :name="isProjectsCollapsed ? 'i-lucide-chevron-down' : 'i-lucide-chevron-up'"
                class="h-3.5 w-3.5"
              />
            </NButton>
          </div>
        </div>

        <div class="sidebar-scroll flex-1 space-y-1 overflow-y-auto pr-1"
          v-show="!isProjectsCollapsed || isCollapsed"
        >
          <div v-if="projects.length === 0"
            class="px-2 py-4 text-center text-xs text-gray-500"
          >
            <span v-if="!isCollapsed || isMobile">No projects yet</span>
            <NIcon v-else
              name="i-lucide-folder-open"
              class="mx-auto h-5 w-5"
            />
          </div>
          <ProjectItemButton
            v-for="project in projects"
            :key="project.id"
            :project="project"
            :is-active="activeProjectId === project.id"
            :is-collapsed="isCollapsed"
            :is-mobile="isMobile"
          />
        </div>
      </div>

      <UserProfileCard
        :is-collapsed="isCollapsed"
        :is-mobile="isMobile"
      />
    </aside>
  </Transition>
</template>

<style scoped>
.sidebar-slide-enter-active,
.sidebar-slide-leave-active {
  transition: transform 300ms ease-in-out;
}

.sidebar-slide-enter-from,
.sidebar-slide-leave-to {
  transform: translateX(-100%);
}

.sidebar-slide-enter-to,
.sidebar-slide-leave-from {
  transform: translateX(0);
}

.sidebar-scroll::-webkit-scrollbar {
  width: 8px;
}

.sidebar-scroll::-webkit-scrollbar-track {
  background: transparent;
}

.sidebar-scroll::-webkit-scrollbar-thumb {
  background-color: rgba(148, 163, 184, 0.6);
  border-radius: 999px;
  border: 2px solid transparent;
  background-clip: padding-box;
}

.dark .sidebar-scroll::-webkit-scrollbar-thumb {
  background-color: rgba(148, 163, 184, 0.35);
}
</style>
