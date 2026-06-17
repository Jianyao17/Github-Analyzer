<script setup lang="ts">
import { useRoute } from 'vue-router';
import type { ProgressEvent } from '@/composables/useProjectApi';
import RepositoryStatistics from '../RepositoryStatistics.vue';
import RepositoryReadme from '../RepositoryReadme.vue';

const route = useRoute();

defineProps<{
  progress: ProgressEvent | null
}>();
</script>

<template>
  <div class="
    flex h-full min-h-0 w-full flex-col gap-6
    lg:flex-row
  "
  >
    <!-- Main content area for README -->
    <div class="h-full min-h-0 min-w-0 flex-1">
      <RepositoryReadme :project-id="route.params.id as string" />
    </div>

    <!-- Sidebar for Statistics -->
    <div class="
      h-full min-h-0 w-full shrink-0
      lg:w-[360px]
    "
    >
      <RepositoryStatistics :progress="progress" />
    </div>
  </div>
</template>
