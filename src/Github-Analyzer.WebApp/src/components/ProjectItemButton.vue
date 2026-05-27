<script setup lang="ts">
import type { ProjectResponse } from '../types/_api/project';

type ProjectItemButtonProps = {
  project: ProjectResponse
  isActive: boolean
  isCollapsed: boolean
  isMobile: boolean
};

defineProps<ProjectItemButtonProps>();
</script>

<template>
  <NButton
    class="w-full py-2 transition-colors"
    :to="{ name: 'app.project-detail', params: { id: project.id } }"
    :color="isActive ? 'primary' : 'gray'"
    :variant="isActive ? 'soft' : 'ghost'"
    :class="[
      (isCollapsed && !isMobile) 
        ? 'justify-center px-2' 
        : 'justify-start',
        
      isActive ? `
        bg-primary-50 font-bold text-primary-600
        dark:bg-primary-950 dark:text-primary-400
      ` : ''
    ]"
  >
    <div class="flex w-full items-center overflow-hidden"
      :class="(isCollapsed && !isMobile) ? 'justify-center' : 'gap-2'"
    >
      <NIcon name="i-lucide-github"
        class="h-5 w-5 shrink-0 justify-center"
      />
      <div v-if="!isCollapsed || isMobile"
        class="flex min-w-0 flex-col items-start text-left"
      >
        <span class="w-full truncate"
          :class="isActive ? `font-bold` : `font-medium`"
        >
          {{ project.repositoryName }}
        </span>
        <span class="mt-0.5 max-w-full truncate text-[10px] font-normal"
          :class="isActive ? `
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
</template>
