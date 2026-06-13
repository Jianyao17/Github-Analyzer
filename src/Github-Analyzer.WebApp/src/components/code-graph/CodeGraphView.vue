<script setup lang="ts">
import { ref, computed } from 'vue';
import { useGraphD3 } from '@/composables/useGraphD3';
import type { CodeGraph } from '@/types/analysis/code-graph';
import GraphSearchModal from './GraphSearchModal.vue';
import GraphSettingsMenu from './GraphSettingsMenu.vue';
import GraphContextMenu from './GraphContextMenu.vue';
import GraphLegend from './GraphLegend.vue';

// ─── Props & Emits ────────────────────────────────────────────────────────────
const props = defineProps<{
  data: CodeGraph;
}>();

import type { GraphNode } from '@/types/analysis/code-graph';
import type { D3Node } from '@graph.types';

const emit = defineEmits<{
  (e: 'show-source-code', payload: GraphNode): void;
}>();

// ─── Graph ────────────────────────────────────────────────────────────────────
const graphContainer = ref<HTMLElement | null>(null);
const graphData = computed(() => props.data);

// ─── Search modal ref ─────────────────────────────────────────────────────────
const searchModal = ref<{ open: () => void; close: () => void } | null>(null);

// ─── Context Menu state ───────────────────────────────────────────────────────
const contextMenu = ref({ show: false, x: 0, y: 0, node: null as D3Node | null });

// ─── D3 Graph ─────────────────────────────────────────────────────────────────
const { 
  isGraphLoading,
  settings, maxCollapseDepth,
  search, focusNode, focusHover,
  focusResults, clearSearch, expandAll, 
  collapseAll, highlightNode, getEngine
} = useGraphD3(graphContainer, graphData, 
  {
  // Layout
    layout: 'star-balloon',
  
    // Collapse depth
    collapseDepth: 2,

    // Show context menu when node is right clicked
    onContextMenu: (x, y, node) => 
    {
      contextMenu.value = {
        show: true,
        x,
        y,
        node
      };
    }
  }
);

function handleShowSourceCode(node: D3Node) 
{
  emit('show-source-code', 
    {
      pathId: node.id,
      label: node.label,
      type: node.type,
      startLine: node.startLine,
      endLine: node.endLine
    });
}

// ─── Settings ─────────────────────────────────────────────────────────────────
const supportsNamespace = computed(() => 
  props.data.nodes.some(n => n.type === 1)
);

defineExpose({
  focusNode,
  highlightNode,
  getEngine
});
</script>

<template>
  <!--
    CodeGraphView — self-contained graph visualization.
    Must be `position: relative` (class `relative`) so the absolute-positioned
    GraphSearchModal overlay is contained within this element.
  -->
  <div
    class="relative h-full w-full overflow-hidden bg-[var(--ui-bg)]"
    style="background-image: radial-gradient(var(--ui-border) 1px, transparent 1px); background-size: 20px 20px;"
  >
    <!-- ── D3 graph container (behind everything) ─────────────────────────────── -->
    <div
      ref="graphContainer"
      class="
        absolute inset-0 cursor-grab
        active:cursor-grabbing
      "
    />

    <!-- ── Search trigger (top-left, z-20) ──────────────────────────────────── -->
    <div class="absolute top-4 left-4 z-20">
      <button
        class="
          flex items-center gap-2.5 rounded-lg border border-[var(--ui-border)]
          bg-[var(--ui-bg)] px-3.5 py-2.5 text-sm transition-colors
          hover:bg-[var(--ui-bg-elevated)]
        "
        @click="searchModal?.open()"
      >
        <NIcon name="i-lucide-search"
          class="h-5 w-5 shrink-0 text-[var(--ui-text-muted)]"
        />
        <span class="
          hidden text-[var(--ui-text-muted)]
          sm:block
        "
        >Search</span>
        <div class="
          hidden h-4 w-px shrink-0 bg-[var(--ui-border)]
          sm:block
        "
        />
        <NKbd size="md"
          class="
            hidden
            sm:flex
          "
        >Ctrl K</NKbd>
      </button>
    </div>

    <!-- ── Search modal (absolute inset-0 z-30, contained within this element) ── -->
    <GraphSearchModal
      ref="searchModal"
      :search="search"
      :focus-node="focusNode"
      :focus-hover="focusHover"
      :focus-results="focusResults"
      :clear-search="clearSearch"
      :total-nodes="data.nodes?.length"
    />

    <!-- ── Empty graph placeholder ──────────────────────────────────────────── -->
    <div
      v-if="data.nodes?.length === 0"
      class="
        pointer-events-none absolute inset-0 flex items-center justify-center
      "
    >
      <div class="text-center text-[var(--ui-text-muted)] italic">
        [ Tampilan Graph Node Codebase ]<br />
        Tidak ada node yang dapat dirender.
      </div>
    </div>

    <!-- ── Loading Overlay ──────────────────────────────────────────────────── -->
    <div
      v-if="isGraphLoading"
      class="
        absolute inset-0 z-10 flex items-center justify-center
        bg-[var(--ui-bg)]/50 backdrop-blur-sm
      "
    >
      <div class="
        flex items-center gap-3 rounded-full bg-[var(--ui-bg)] px-5 py-3
        shadow-sm ring-1 ring-[var(--ui-border)]
      "
      >
        <NIcon name="i-lucide-loader-2"
          class="h-5 w-5 animate-spin text-[var(--ui-primary)]"
        />
        <span class="text-sm font-medium text-[var(--ui-text-highlighted)]">Preparing Graph...</span>
      </div>
    </div>

    <!-- ── Settings & Legend (bottom-right, z-20) ────────────────────────────── -->
    <div class="
      absolute right-4 bottom-4 z-20 flex items-end gap-3
      sm:right-6 sm:bottom-6
    "
    >
      <!-- Graph Settings Menu -->
      <GraphSettingsMenu 
        :supports-namespace="supportsNamespace"
        :max-collapse-depth="maxCollapseDepth"
        v-model:settings="settings"
        @expand-all="expandAll"
        @collapse-all="collapseAll"
      />

      <!-- Legend -->
      <div class="pointer-events-none">
        <GraphLegend :data="data" />
      </div>
    </div>

    <!-- ── Context Menu (absolute top-0 left-0, z-50) ───────────────────────── -->
    <GraphContextMenu
      v-model:show="contextMenu.show"
      :x="contextMenu.x"
      :y="contextMenu.y"
      :node="contextMenu.node"
      @show-source-code="handleShowSourceCode"
    />
  </div>
</template>
