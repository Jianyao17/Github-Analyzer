<script setup lang="ts">
import { useRoute } from 'vue-router';
import { useWindowSize } from '@vueuse/core';
import { Splitpanes, Pane } from 'splitpanes';
import { ref, nextTick, computed, watch } from 'vue';
import { useProjectApi } from '@/composables/useProjectApi';
import { useOnboardingStore } from '@/stores/onboarding.store';
import type { ProgressEvent } from '@/composables/useProjectApi';
import type { GraphNode } from '@/types/analysis/code-graph';

import CodeViewer from '@/components/code-viewer/CodeViewer.vue';
import CodeGraphView from '@/components/code-graph/CodeGraphView.vue';
import 'splitpanes/dist/splitpanes.css';


const { useCodeGraphQuery } = useProjectApi();
const store = useOnboardingStore();
const route = useRoute();

// ─── Props ────────────────────────────────────────────────────────────────────
defineProps<{
  progress: ProgressEvent | null
}>();

const { width } = useWindowSize();
const { data, isLoading } = useCodeGraphQuery(route.params.id as string);
const isMobile = computed(() => width.value < 768);

// ─── Code Viewer Ref ──────────────────────────────────────────────────────────
const codeViewerRef = ref<InstanceType<typeof CodeViewer> | null>(null);
const codeGraphRef = ref<InstanceType<typeof CodeGraphView> | null>(null);
const isViewerOpen = ref(false);

// ─── Onboarding ───────────────────────────────────────────────────────────────
watch(data, (newData) => 
{
  if (newData) 
  {
    store.triggerCodeGraphTour(() => codeGraphRef.value?.getEngine() ?? null);
  }
}, { immediate: true });

function handleShowSourceCode(node: GraphNode) 
{
  isViewerOpen.value = true;
  if (codeGraphRef.value) 
  {
    codeGraphRef.value.focusNode(node as any);
    codeGraphRef.value.highlightNode((node as any).id || (node as any).pathId);
  }
  
  const attemptOpenFile = (retries = 0) => 
  {
    if (codeViewerRef.value) 
    {
      const relativePath = node.pathId.split('::')[0];
      if (relativePath) 
      {
        codeViewerRef.value.openFile(relativePath, node.startLine, node.endLine);
      }
    }
    else if (retries < 10) 
    {
      setTimeout(() => attemptOpenFile(retries + 1), 50);
    }
  };
  
  nextTick(() => attemptOpenFile(0));
}

function handleFocusNode(path: string) 
{
  if (!codeGraphRef.value || !data.value) return;
  
  const normalizedPath = path.replace(/\\/g, '/');
  
  // Find node by id or pathId (some nodes use pathId, some use id)
  const node:any = data.value?.nodes.find((n: any) => 
  {
    const p = (n.pathId || '').replace(/\\/g, '/');
    return (n as any).id === path || 
           p === normalizedPath || 
           p.startsWith(normalizedPath + '::') || 
           p.startsWith(normalizedPath + '/');
  });

  if (node) 
  {
    const targetNode = { ...node, id: node.pathId || node.id };
    codeGraphRef.value.focusNode(targetNode as any);
    codeGraphRef.value.highlightNode(targetNode.id);

    // 0: Directory, 1: Namespace
    if (node.type === 0 || node.type === 1) 
    {
      if (codeViewerRef.value) 
      {
        codeViewerRef.value.clearHighlightLines();
      }
    }
  }
}
</script>

<template>
  <div class="relative h-full w-full">
    <!-- ── Loading state ────────────────────────────────────────────────────── -->
    <div v-if="isLoading"
      class="absolute inset-0 z-10 p-4"
    >
      <NSkeleton class="h-full w-full rounded-xl" />
    </div>

    <!-- ── Waiting / in-progress overlay ──────────────────────────────────── -->
    <div
      v-else-if="!data"
      class="
        absolute inset-0 z-10 flex flex-col items-center justify-center gap-6
        bg-[var(--ui-bg)]/80 backdrop-blur-sm
      "
      style="background-image: radial-gradient(var(--ui-border) 1px, transparent 1px); background-size: 20px 20px;"
    >
      <!-- Circular progress ring -->
      <div class="relative flex h-28 w-28 items-center justify-center">
        <!-- Track -->
        <svg class="absolute inset-0 h-full w-full"
          viewBox="0 0 100 100"
        >
          <circle cx="50"
            cy="50"
            r="42"
            stroke="currentColor"
            stroke-width="8"
            fill="transparent"
            class="text-[var(--ui-border)]"
          />
        </svg>

        <!-- Spinning arc -->
        <svg class="absolute inset-0 h-full w-full animate-spin"
          viewBox="0 0 100 100"
        >
          <circle cx="50"
            cy="50"
            r="42"
            stroke="currentColor"
            stroke-width="8"
            stroke-linecap="round"
            fill="transparent"
            stroke-dasharray="264"
            stroke-dashoffset="160"
            class="text-[var(--ui-primary)]"
          />
        </svg>

        <!-- Percentage label -->
        <span class="text-2xl font-bold text-[var(--ui-text-highlighted)]">
          {{ Math.round(progress?.progress || 0) }}<span class="
            text-sm text-[var(--ui-text-muted)]
          "
          >%</span>
        </span>
      </div>

      <!-- Status text -->
      <div class="text-center">
        <p class="text-lg font-bold text-[var(--ui-text)]">
          Menyiapkan Graph Node Codebase...
        </p>
        <p class="
          mt-1 max-w-md animate-pulse text-sm text-[var(--ui-text-muted)]
        "
        >
          {{ progress?.message || 'Menunggu proses analisa selesai.' }}
        </p>
      </div>
    </div>

    <!-- ── Graph view & Code view ────────────────────────── -->
    <Splitpanes v-if="data"
      id="code-graph-canvas"
      class="default-theme absolute inset-0"
      :horizontal="isMobile"
    >
      <Pane min-size="20"
        :size="isViewerOpen ? (isMobile ? 50 : 55) : 100"
        class="relative"
      >
        <CodeGraphView
          ref="codeGraphRef"
          :data="data"
          class="h-full w-full"
          @show-source-code="handleShowSourceCode"
        />
        <!-- Tooltip Canvas Navigation -->
        <div class="absolute top-4 right-4 z-20">
          <NTooltip text="Buka Panduan Singkat"
            :popper="{ placement: 'left' }"
          >
            <button class="
              flex h-8 w-8 items-center justify-center rounded-full
              bg-[var(--ui-bg)] text-[var(--ui-text-muted)] ring-1
              ring-[var(--ui-border)] transition-colors
              hover:bg-[var(--ui-bg-elevated)] hover:text-[var(--ui-text)]
            "
            >
              <NIcon name="i-lucide-info"
                class="h-4.5 w-4.5"
              />
            </button>
          </NTooltip>
        </div>
      </Pane>
      <Pane v-if="isViewerOpen"
        min-size="20"
        size="45"
      >
        <CodeViewer
          id="code-viewer"
          ref="codeViewerRef"
          :project-id="route.params.id as string"
          @close-viewer="isViewerOpen = false"
          @focus-node="handleFocusNode"
        />
      </Pane>
    </Splitpanes>
  </div>
</template>

<style>
/* Splitpanes styling for dark mode */
.splitpanes.default-theme .splitpanes__pane {
  background-color: transparent;
}
.splitpanes.default-theme .splitpanes__splitter {
  background-color: var(--ui-bg-elevated);
  border-left: 1px solid var(--ui-border);
  position: relative;
  transition: background-color 0.2s ease;
}
.splitpanes.default-theme .splitpanes__splitter:hover {
  background-color: var(--ui-border);
}

/* Modern Handle Indicator */
.splitpanes.default-theme .splitpanes__splitter:before {
  content: "";
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  background-color: var(--ui-text-muted);
  border-radius: 9999px;
  transition: background-color 0.2s ease;
  z-index: 10;
}
.splitpanes.default-theme .splitpanes__splitter:after {
  display: none !important; /* Hide the default splitpanes second line */
}

.splitpanes.default-theme .splitpanes__splitter:hover:before {
  background-color: var(--ui-primary);
}

/* Handle Dimensions based on orientation */
.splitpanes--vertical > .splitpanes__splitter:before {
  width: 3px;
  height: 30px;
}
.splitpanes--horizontal > .splitpanes__splitter:before {
  width: 30px;
  height: 3px;
}
</style>
