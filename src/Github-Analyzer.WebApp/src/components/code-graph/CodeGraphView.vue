<script setup lang="ts">
import { ref, computed } from 'vue';
import { useGraphD3 } from '@/composables/useGraphD3';
import type { CodeGraph } from '@/types/analysis/code-graph';
import GraphSearchModal from './GraphSearchModal.vue';
import GraphSettingsMenu from './GraphSettingsMenu.vue';
import GraphLegend from './GraphLegend.vue';

// ─── Props & Emits ────────────────────────────────────────────────────────────
const props = defineProps<{
  data: CodeGraph;
}>();

import type { GraphNode } from '@/types/analysis/code-graph';

const emit = defineEmits<{
  (e: 'show-source-code', payload: GraphNode): void;
}>();

// ─── Graph ────────────────────────────────────────────────────────────────────
const graphContainer = ref<HTMLElement | null>(null);
const graphData = computed(() => props.data);

const { 
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

    // Show source code when node is clicked
    onShowSourceCode: (node) => 
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
  }
);

// ─── Search modal ref ─────────────────────────────────────────────────────────
const searchModal = ref<{ open: () => void; close: () => void } | null>(null);

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
    class="relative h-full w-full overflow-hidden"
    style="background-image: radial-gradient(#e5e7eb 1px, transparent 1px); background-size: 20px 20px;"
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
          flex items-center gap-2.5 rounded-lg border border-gray-200 bg-white
          px-3.5 py-2.5 text-sm transition-colors
          hover:border-gray-300
          dark:border-gray-700 dark:bg-gray-900
          dark:hover:border-gray-600
        "
        @click="searchModal?.open()"
      >
        <NIcon name="i-lucide-search"
          class="
            h-5 w-5 shrink-0 text-gray-400
            dark:text-gray-500
          "
        />
        <span class="
          hidden text-gray-400
          sm:block
          dark:text-gray-500
        "
        >Search</span>
        <div class="
          hidden h-4 w-px shrink-0 bg-gray-200
          sm:block
          dark:bg-gray-700
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
      <div class="
        text-center text-gray-400 italic
        dark:text-gray-600
      "
      >
        [ Tampilan Graph Node Codebase ]<br />
        Tidak ada node yang dapat dirender.
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
  </div>
</template>
