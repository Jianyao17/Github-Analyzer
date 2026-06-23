<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue';
import { useProjectApi } from '@/composables/useProjectApi';
import ZeroMD from './ZeroMD.vue';

const props = defineProps<{
  projectId: string
}>();

const { getProjectSourceContent } = useProjectApi();

interface RepoFile {
  id: string;
  name: string;
  rawContent: string;
  icon: string;
  isMarkdown: boolean;
}

const isLoading = ref(true);
const availableFiles = ref<RepoFile[]>([]);
const activeFileId = ref<string | null>(null);
const activeFile = computed(() => availableFiles.value.find(f => f.id === activeFileId.value));

const guessLicenseType = (content: string): string => 
{
  if (!content) return 'License';
  const lowerContent = content.slice(0, 1000).toLowerCase();

  if (lowerContent.includes('apache license') && lowerContent.includes('version 2.0')) return 'Apache-2.0 license';
  if (lowerContent.includes('gnu general public license') && lowerContent.includes('version 3')) return 'GPL-3.0 license';
  if (lowerContent.includes('gnu general public license') && lowerContent.includes('version 2')) return 'GPL-2.0 license';
  if (lowerContent.includes('gnu affero general public license') && lowerContent.includes('version 3')) return 'AGPL-3.0 license';
  if (lowerContent.includes('the unlicense') || lowerContent.includes('this is free and unencumbered software released into the public domain')) return 'The Unlicense';
  if (lowerContent.includes('mit license') || lowerContent.includes('permission is hereby granted, free of charge, to any person obtaining a copy')) return 'MIT license';
  if (lowerContent.includes('bsd 3-clause') || lowerContent.includes('neither the name of the copyright holder nor the names of its contributors may be used to endorse')) return 'BSD 3-Clause license';
  if (lowerContent.includes('bsd 2-clause')) return 'BSD 2-Clause license';
  if (lowerContent.includes('mozilla public license')) return 'MPL-2.0 license';

  return 'License';
};

const fetchFiles = async () => 
{
  isLoading.value = true;
  availableFiles.value = [];
  activeFileId.value = null;

  const probes = [
    { id: 'readme', icon: 'i-lucide-book-open', name: 'README.md' },
    { id: 'contributing', icon: 'i-lucide-users', name: 'CONTRIBUTING.md' },
    { id: 'license', icon: 'i-lucide-scale', name: 'LICENSE' }
  ];

  await Promise.allSettled(probes.map(async (probe) => 
  {
    const response = await getProjectSourceContent(props.projectId, probe.name);
    if (response && response.content) 
    {
      const isMarkdown = probe.name.toLowerCase().endsWith('.md');
      
      let displayName = probe.name;
      if (probe.id === 'license') displayName = guessLicenseType(response.content);
      else if (probe.id === 'readme') displayName = 'README';
      else if (probe.id === 'contributing') displayName = 'Contributing';
      
      availableFiles.value.push({
        id: probe.id,
        name: displayName,
        rawContent: response.content,
        icon: probe.icon,
        isMarkdown
      });
    }
  }));

  // Sort them based on the original probe order
  const order = probes.map(p => p.id);
  availableFiles.value.sort((a, b) => order.indexOf(a.id) - order.indexOf(b.id));

  if (availableFiles.value.length > 0) 
  {
    activeFileId.value = availableFiles.value[0].id;
  }
  
  isLoading.value = false;
};

onMounted(() => 
{
  fetchFiles();
});

watch(() => props.projectId, () => 
{
  fetchFiles();
});
</script>

<template>
  <div v-if="isLoading"
    class="
      flex h-full min-h-[16rem] items-center justify-center rounded-xl
      bg-[var(--ui-bg)]/70 ring-1 ring-[var(--ui-border)] backdrop-blur-md
    "
  >
    <div class="flex flex-col items-center gap-4 text-[var(--ui-text-muted)]">
      <NIcon name="i-lucide-loader-2"
        class="h-8 w-8 animate-spin"
      />
      <p>Loading files...</p>
    </div>
  </div>

  <div v-else-if="availableFiles.length > 0"
    class="
      flex h-full flex-col overflow-hidden rounded-xl border-1
      border-[var(--ui-border)] bg-[var(--ui-bg)]/70 backdrop-blur-md
    "
  >
    <!-- Navigation Tabs -->
    <div class="
      scrollbar-hide flex items-center overflow-x-auto border-b
      border-[var(--ui-border)] bg-[var(--ui-bg-elevated)]/50 px-2 pt-2
    "
    >
      <button v-for="file in availableFiles"
        :key="file.id"
        @click="activeFileId = file.id"
        class="
          flex shrink-0 items-center gap-2 border-b-2 px-4 py-2.5 text-sm
          font-medium transition-colors
        "
        :class="activeFileId === file.id 
          ? 'border-[var(--ui-primary)] text-[var(--ui-text-highlighted)]' 
          : `
            border-transparent text-[var(--ui-text-muted)]
            hover:border-[var(--ui-border)] hover:text-[var(--ui-text)]
          `"
      >
        <NIcon :name="file.icon"
          class="h-4 w-4"
        />
        {{ file.name }}
      </button>
    </div>

    <!-- Content Area -->
    <div class="
      flex-1 overflow-y-auto bg-[var(--ui-bg)] p-6
      lg:p-8
    "
    >
      <div v-if="activeFile?.isMarkdown"
        class="w-full"
      >
        <ZeroMD :content="activeFile.rawContent" />
      </div>
      <div v-else
        class="text-[var(--ui-text)]"
      >
        <pre class="overflow-auto p-4 text-sm whitespace-pre-wrap">{{ activeFile?.rawContent }}</pre>
      </div>
    </div>
  </div>
  
  <div v-else
    class="
      flex h-full min-h-[16rem] items-center justify-center rounded-xl border-1
      border-[var(--ui-border)] bg-[var(--ui-bg)]/70 backdrop-blur-md
    "
  >
    <div class="
      flex flex-col items-center gap-3 px-4 text-center
      text-[var(--ui-text-muted)]
    "
    >
      <NIcon name="i-lucide-file-question"
        class="h-10 w-10 opacity-50"
      />
      <div>
        <p class="font-medium text-[var(--ui-text)]">No documentation files found</p>
        <p class="mt-1 text-sm">This repository does not have a standard README.md, CONTRIBUTING.md, or LICENSE file.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Hide scrollbar for tabs but keep functionality */
.scrollbar-hide {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
.scrollbar-hide::-webkit-scrollbar {
  display: none;
}
</style>
