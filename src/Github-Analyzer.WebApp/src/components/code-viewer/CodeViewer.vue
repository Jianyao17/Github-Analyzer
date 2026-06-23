<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useCodeViewer } from '@/composables/useCodeViewer';
import CodeViewerSettings from './CodeViewerSettings.vue';
import CodeViewerSearch from './CodeViewerSearch.vue';
import CodeViewerTabs from './CodeViewerTabs.vue';

const props = defineProps<{
  projectId: string;
}>();

defineEmits<{
  (e: 'close-viewer'): void;
  (e: 'focus-node', path: string): void;
}>();

const editorContainer = ref<HTMLElement | null>(null);

const {
  viewerTheme,
  tabs,
  activeTabId,
  editorView,
  isLoading,
  isSearchOpen,
  openFile,
  closeTab,
  initEditor,
  highlightLines,
  clearHighlightLines,
} = useCodeViewer(props.projectId);

function handleGlobalKeyDown(e: KeyboardEvent) 
{
  // Prevent browser search and open code viewer search instead
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'f') 
  {
    e.preventDefault();
    e.stopPropagation();
    if (tabs.value.length > 0) 
    {
      isSearchOpen.value = true;
    }
  }
}

onMounted(() => 
{
  if (editorContainer.value) 
  {
    initEditor(editorContainer.value);
  }
  window.addEventListener('keydown', handleGlobalKeyDown, { capture: true });
});

onUnmounted(() => 
{
  window.removeEventListener('keydown', handleGlobalKeyDown, { capture: true });
});

defineExpose({
  openFile,
  highlightLines,
  clearHighlightLines
});

const activeTabPathParts = computed(() => 
{
  if (!activeTabId.value) return [];
  return activeTabId.value.split('/');
});
</script>

<template>
  <div class="flex h-full w-full flex-col bg-[var(--ui-bg)]">
    <div class="
      relative z-40 flex items-end justify-between bg-[var(--ui-bg-muted)] pr-2
    "
    >
      <!-- Seamless Bottom Border -->
      <div class="
        pointer-events-none absolute right-0 bottom-0 left-0 z-0 h-[1px] w-full
        bg-[var(--ui-border)]
      "
      ></div>

      <CodeViewerTabs
        :tabs="tabs"
        :active-tab-id="activeTabId"
        @select="(id) => openFile(id)"
        @close="(id) => closeTab(id)"
        class="relative z-10 flex-1"
      />
      <div class="relative z-10 flex items-center gap-2 pb-1.5 pl-2">
        <button
          class="
            flex items-center justify-center rounded p-1.5
            text-[var(--ui-text-muted)] transition-colors
            hover:bg-[var(--ui-bg-elevated)]
            hover:text-[var(--ui-text-highlighted)]
          "
          title="Search in File"
          @click="isSearchOpen = !isSearchOpen"
        >
          <NIcon name="i-lucide-search"
            class="h-4 w-4"
          />
        </button>
        <CodeViewerSettings v-model:theme="viewerTheme" />
        <button
          class="
            flex items-center justify-center rounded p-1.5
            text-[var(--ui-text-muted)] transition-colors
            hover:bg-[var(--ui-bg-elevated)]
            hover:text-[var(--ui-text-highlighted)]
          "
          title="Close Viewer"
          @click="$emit('close-viewer')"
        >
          <NIcon name="i-heroicons-x-mark-20-solid"
            class="h-5 w-5"
          />
        </button>
      </div>
    </div>
    
    <!-- Breadcrumbs -->
    <div v-if="activeTabPathParts.length > 0"
      class="
        flex items-center border-b border-[var(--ui-border)] bg-[var(--ui-bg)]
        px-4 py-1.5 text-xs text-[var(--ui-text-muted)]
      "
    >
      <template v-for="(part, index) in activeTabPathParts"
        :key="index"
      >
        <span class="
          cursor-pointer transition-colors
          hover:text-[var(--ui-text)]
        "
          @click="$emit('focus-node', activeTabPathParts.slice(0, index + 1).join('/'))"
        >{{ part }}</span>
        <NIcon v-if="index < activeTabPathParts.length - 1"
          name="i-lucide-chevron-right"
          class="mx-1 h-3 w-3 opacity-50"
        />
      </template>
    </div>

    <div class="relative flex-1 overflow-hidden">
      <!-- Search Component (Top-Left to avoid settings menu) -->
      <CodeViewerSearch 
        v-model="isSearchOpen"
        :view="editorView"
      />

      <!-- Loading Overlay -->
      <div
        v-if="isLoading"
        class="
          absolute inset-0 z-10 flex items-center justify-center
          bg-[var(--ui-bg)]/50 backdrop-blur-sm
        "
      >
        <span class="
          loading-dots text-sm font-medium text-[var(--ui-text-muted)]
        "
        >Loading code</span>
      </div>

      <!-- Empty State -->
      <div
        v-if="tabs.length === 0"
        class="
          absolute inset-0 flex items-center justify-center text-sm
          text-[var(--ui-text-muted)]
        "
      >
        Select a node to view source code
      </div>

      <!-- Editor Container -->
      <div
        ref="editorContainer"
        class="h-full w-full outline-none"
        :class="{ 'opacity-0': tabs.length === 0 }"
      ></div>
    </div>
  </div>
</template>

<style scoped>
.loading-dots::after {
  content: '';
  animation: ellipsis 1.5s infinite;
  display: inline-block;
  text-align: left;
  width: 1em;
}
@keyframes ellipsis {
  0% { content: ''; }
  25% { content: '.'; }
  50% { content: '..'; }
  75% { content: '...'; }
}
</style>
