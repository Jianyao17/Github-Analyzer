<script setup lang="ts">
import { ref, nextTick, computed } from 'vue';
import { useRoute } from 'vue-router';
import { Splitpanes, Pane } from 'splitpanes';
import type { ProgressEvent } from '@/composables/useProjectApi';
import type { CodeGraph, GraphNode } from '@/types/analysis/code-graph';
import CodeGraphView from '@/components/code-graph/CodeGraphView.vue';
import CodeViewer from '@/components/code-viewer/CodeViewer.vue';
import 'splitpanes/dist/splitpanes.css';

import { useWindowSize } from '@vueuse/core';

// ─── Props ────────────────────────────────────────────────────────────────────
const props = defineProps<{
  data: CodeGraph | null
  progress: ProgressEvent | null
}>();

const route = useRoute();
const { width } = useWindowSize();
const isMobile = computed(() => width.value < 768);

// ─── Code Viewer Ref ──────────────────────────────────────────────────────────
const codeViewerRef = ref<InstanceType<typeof CodeViewer> | null>(null);
const codeGraphRef = ref<InstanceType<typeof CodeGraphView> | null>(null);
const isViewerOpen = ref(false);

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
  if (!codeGraphRef.value || !props.data) return;
  
  const normalizedPath = path.replace(/\\/g, '/');
  
  // Find node by id or pathId (some nodes use pathId, some use id)
  const node:any = props.data.nodes.find(n => 
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
    <!-- ── Waiting / in-progress overlay ──────────────────────────────────── -->
    <div
      v-if="!data"
      class="
        absolute inset-0 z-10 flex flex-col items-center justify-center gap-6
        bg-white/80 backdrop-blur-sm
        dark:bg-gray-900/80
      "
      style="background-image: radial-gradient(#e5e7eb 1px, transparent 1px); background-size: 20px 20px;"
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
            class="
              text-gray-200
              dark:text-gray-700
            "
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
            class="text-green-500"
          />
        </svg>

        <!-- Percentage label -->
        <span class="
          text-2xl font-bold text-gray-800
          dark:text-white
        "
        >
          {{ Math.round(progress?.progress || 0) }}<span class="
            text-sm text-gray-500
          "
          >%</span>
        </span>
      </div>

      <!-- Status text -->
      <div class="text-center">
        <p class="
          text-lg font-bold text-gray-700
          dark:text-gray-300
        "
        >
          Menyiapkan Graph Node Codebase...
        </p>
        <p class="
          mt-1 max-w-md animate-pulse text-sm text-gray-500
          dark:text-gray-400
        "
        >
          {{ progress?.message || 'Menunggu proses analisa selesai.' }}
        </p>
      </div>
    </div>

    <!-- ── Graph view & Code view ────────────────────────── -->
    <Splitpanes v-if="data"
      class="default-theme absolute inset-0"
      :horizontal="isMobile"
    >
      <Pane min-size="20"
        :size="isViewerOpen ? (isMobile ? 50 : 55) : 100"
      >
        <CodeGraphView
          ref="codeGraphRef"
          :data="data"
          class="h-full w-full"
          @show-source-code="handleShowSourceCode"
        />
      </Pane>
      <Pane v-if="isViewerOpen"
        min-size="20"
        size="45"
      >
        <CodeViewer
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
  background-color: #f3f4f6;
  border-left: 1px solid #e5e7eb;
}
.dark .splitpanes.default-theme .splitpanes__splitter {
  background-color: #1f2937;
  border-left: 1px solid #374151;
}
</style>
