<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useProjectApi } from '../composables/useProjectApi';
import type { ProjectResponse } from '../types/_api/project';

const props = defineProps<{
  repositoryUrl: string;
  modelValue: string;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', val: string): void;
}>();

const { fetchProjects } = useProjectApi();
const allProjects = ref<ProjectResponse[]>([]);
const loading = ref(false);

// Load all projects in the system to filter versions of the same repository
const loadProjects = async () => 
{
  loading.value = true;
  try 
  {
    allProjects.value = await fetchProjects();
  } 
  catch (err) 
  {
    console.error('Failed to load projects for selector', err);
  } 
  finally 
  {
    loading.value = false;
  }
};

onMounted(() => 
{
  loadProjects();
});

// Filter out projects belonging to the same repository (case-insensitive and trimmed)
const siblingProjects = computed(() => 
{
  if (!props.repositoryUrl) return [];
  const targetUrl = props.repositoryUrl.toLowerCase().trim();
  return allProjects.value.filter(
    p => p.repositoryUrl.toLowerCase().trim() === targetUrl
  );
});

// Get unique branches for this repository
const branches = computed(() => 
{
  const list = siblingProjects.value
    .map(p => p.branchName || 'main')
    .filter(Boolean);
  return [...new Set(list)];
});

// Track the active branch selected in dropdown
const selectedBranch = ref<string>('');

// Sync dropdown selection with the active active projectId (modelValue)
watch([siblingProjects, () => props.modelValue], () => 
{
  const activeProj = siblingProjects.value.find(p => p.id === props.modelValue);
  if (activeProj) 
  {
    selectedBranch.value = activeProj.branchName || 'main';
  } 
  else if (branches.value.length > 0 && !selectedBranch.value) 
  {
    selectedBranch.value = branches.value[0];
  }
}, { immediate: true });

// Filter commits for the currently selected branch
const commitsForSelectedBranch = computed(() => 
  siblingProjects.value.filter(
    p => (p.branchName || 'main') === selectedBranch.value
  ));

// Format commits items for NSelectMenu
const commitItems = computed(() => 
  commitsForSelectedBranch.value.map(p => 
  {
    const shortHash = p.lastCommitHash ? p.lastCommitHash.slice(0, 7) : 'Unknown';
    const dateStr = p.createdAtUtc 
      ? new Intl.DateTimeFormat('id-ID', 
        { day: 'numeric', month: 'short', year: 'numeric' })
        .format(new Date(p.createdAtUtc))
      : '';
    return {
      label: `${shortHash} (${dateStr})`,
      id: p.id
    };
  }));

// Computed v-model binding for active projectId
const selectedCommitId = computed({
  get() 
  {
    return props.modelValue;
  },
  set(val: string) 
  {
    if (val && val !== props.modelValue) 
    {
      emit('update:modelValue', val);
    }
  }
});
</script>

<template>
  <NCard
    class="
      shrink-0 border-0 bg-[var(--ui-bg)]/70 ring-1 ring-[var(--ui-border)]
      backdrop-blur-md
    "
    :ui="{ body: 'p-4 md:p-5 h-full flex flex-col justify-center' }"
  >
    <div class="
      grid h-full w-full grid-cols-1 items-end gap-4
      sm:grid-cols-2
    "
    >
      <!-- Branch Dropdown -->
      <div class="flex min-w-0 flex-col gap-1.5">
        <label class="
          text-xs font-semibold tracking-wider text-[var(--ui-text-muted)]
          uppercase
        "
        >
          Branch
        </label>
        <NSelectMenu
          v-model="selectedBranch"
          :items="branches"
          :loading="loading"
          placeholder="Pilih Branch"
          size="sm"
          color="neutral"
          variant="subtle"
          class="w-full"
        />
      </div>

      <!-- Commit Dropdown -->
      <div class="flex min-w-0 flex-col gap-1.5">
        <label class="
          text-xs font-semibold tracking-wider text-[var(--ui-text-muted)]
          uppercase
        "
        >
          Analisis Commit
        </label>
        <NSelectMenu
          v-model="selectedCommitId"
          value-key="id"
          :items="commitItems"
          :loading="loading"
          placeholder="Pilih Commit"
          size="sm"
          color="neutral"
          variant="subtle"
          class="w-full"
        />
      </div>
    </div>
  </NCard>
</template>
