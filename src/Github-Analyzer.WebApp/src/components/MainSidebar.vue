<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router';
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
const router = useRouter();
const { fetchProjects, renameProject, deleteProject } = useProjectApi();

const projects = ref<ProjectResponse[]>([]);
const isProjectsCollapsed = ref(false);

const isDeleteModalOpen = ref(false);
const projectToDelete = ref<ProjectResponse | null>(null);

async function handleRename(id: string, newTitle: string) 
{
  try 
  {
    await renameProject(id, newTitle);
    await loadProjects();
  }
  catch (e) 
  {
    console.error('Failed to rename project', e);
  }
}

function promptDelete(id: string) 
{
  const p = projects.value.find(x => x.id === id);
  if (p) 
  {
    projectToDelete.value = p;
    isDeleteModalOpen.value = true;
  }
}

async function confirmDelete() 
{
  if (!projectToDelete.value) return;
  try 
  {
    await deleteProject(projectToDelete.value.id);
    if (activeProjectId.value === projectToDelete.value.id) 
    {
      router.push('/app/analysis/new');
    }
    await loadProjects();
  }
  catch (e) 
  {
    console.error('Failed to delete project', e);
  }
  finally 
  {
    isDeleteModalOpen.value = false;
    projectToDelete.value = null;
  }
}

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
      fixed inset-0 z-40 bg-[var(--ui-bg-elevated)]/50 backdrop-blur-sm
      transition-opacity
    " 
    @click="$emit('close')" 
  />

  <!-- Sidebar -->
  <Transition name="sidebar-slide">
    <aside
      v-show="!isMobile || isOpen"
      class="
        fixed inset-y-0 left-0 z-50 flex flex-col border-r
        border-[var(--ui-border)] bg-[var(--ui-bg)] transition-[width]
        duration-300 ease-in-out will-change-[width,transform]
      "
      :class="isMobile ? 'w-64' : (isCollapsed ? 'w-18' : 'w-64')"
    >
      <!-- Logo Section -->
      <div id="onboarding-sidebar-logo"
        class="flex h-16 shrink-0 items-center gap-3 px-3 py-4"
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
              truncate text-lg font-bold tracking-tight text-[var(--ui-text)]
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
      <div id="onboarding-new-analysis-btn"
        class="px-3 py-4"
      >
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
      <div id="onboarding-projects-list"
        class="flex min-h-0 flex-1 flex-col p-3"
      >
        <template v-if="!isCollapsed || isMobile">
          <div class="group mb-2 flex items-center justify-between px-2">
            <div class="w-1/2">
              <span class="text-sm font-medium text-[var(--ui-text-muted)]">
                Projects
              </span>
            </div>
            <div class="w-1/2">
              <NButton
                variant="ghost"
                color="gray"
                size="sm"
                class="
                  group w-full justify-end gap-1.5 px-0 text-xs
                  text-[var(--ui-text-muted)]
                  hover:text-[var(--ui-text)]
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
            v-show="!isProjectsCollapsed"
          >
            <div v-if="projects.length === 0"
              class="px-2 py-4 text-center text-xs text-[var(--ui-text-muted)]"
            >
              <span>No projects yet</span>
            </div>
            <ProjectItemButton
              v-for="project in projects"
              :key="project.id"
              :project="project"
              :is-active="activeProjectId === project.id"
              :is-collapsed="false"
              :is-mobile="isMobile"
              @rename="handleRename"
              @delete="promptDelete"
            />
          </div>
        </template>
        <template v-else>
          <!-- Collapsed: Dropdown Button -->
          <div class="flex flex-1 flex-col items-center pt-2">
            <NPopover
              :content="{ side: 'right', align: 'start', sideOffset: 0 }"
              :ui="{ content: 'z-[60]' }"
            >
              <template #default="{ open }">
                <NButton
                  icon="i-lucide-folder-open"
                  color="gray"
                  variant="ghost"
                  class="
                    mx-auto flex h-10 w-10 items-center justify-center
                    transition-all duration-200
                  "
                  :class="open 
                    ? `
                      bg-[var(--ui-bg-elevated)] text-primary-600
                      dark:text-primary-400
                    ` 
                    : `
                      hover:bg-[var(--ui-bg-elevated)]
                      hover:text-[var(--ui-text)]
                    `"
                />
              </template>
              <template #content="{ close }">
                <div class="flex w-64 flex-col p-3">
                  <div class="group mb-2 flex items-center justify-between px-2">
                    <span class="
                      text-sm font-medium text-[var(--ui-text-muted)]
                    "
                    >
                      Projects
                    </span>
                  </div>
                  <div class="
                    sidebar-scroll max-h-[60vh] space-y-1 overflow-y-auto pr-1
                  "
                  >
                    <div v-if="projects.length === 0"
                      class="
                        px-2 py-4 text-center text-xs
                        text-[var(--ui-text-muted)]
                      "
                    >
                      <span>No projects yet</span>
                    </div>
                    <ProjectItemButton
                      v-for="project in projects"
                      :key="project.id"
                      :project="project"
                      :is-active="activeProjectId === project.id"
                      :is-collapsed="false"
                      :is-mobile="isMobile"
                      @rename="handleRename"
                      @delete="promptDelete"
                      @click="close"
                    />
                  </div>
                </div>
              </template>
            </NPopover>
          </div>
        </template>
      </div>

      <UserProfileCard
        :is-collapsed="isCollapsed"
        :is-mobile="isMobile"
      />
    </aside>
  </Transition>

  <!-- Delete Confirmation Modal -->
  <NModal 
    v-model:open="isDeleteModalOpen" 
    :ui="{ 
      content: 'sm:max-w-sm', 
      overlay: 'bg-gray-900/25 dark:bg-gray-900/50 backdrop-blur-xs',
      footer: 'justify-end gap-1'
    }"
  >
    <template #header>
      <div class="
        flex items-center gap-1 text-red-600
        dark:text-red-400
      "
      >
        <NIcon name="i-lucide-alert-triangle"
          class="h-5 w-5"
        />
        <h3 class="text-base font-semibold">Delete Project</h3>
      </div>
    </template>
    
    <template #body>
      <p class="text-sm leading-relaxed text-[var(--ui-text-muted)]">
        Are you sure you want to delete <span class="
          font-semibold text-[var(--ui-text)]
        "
        >"{{ projectToDelete?.title }}"</span>? 
        <br/> This action cannot be undone and will <span class="
          font-medium underline
        "
        >remove all associated analysis data</span>.
      </p>
    </template>
    
    <template #footer>
      <NButton label="Cancel"
        color="default"
        @click="isDeleteModalOpen = false"
      />
      <NButton label="Delete"
        color="error"
        @click="confirmDelete"
      />
    </template>
  </NModal>
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
