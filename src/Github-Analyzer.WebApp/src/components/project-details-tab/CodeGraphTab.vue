<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted, nextTick } from 'vue';
import type { ProgressEvent } from '../../composables/useProjectApi';
import type { CodeGraph } from '../../types/analysis/code-graph';

import { GraphD3 } from '../../lib/graph/graph.main';
import { ZoomDragPlugin } from '../../lib/graph/plugins/zoom-drag.plugin';
import { SelectionPlugin } from '../../lib/graph/plugins/selection.plugin';
import { HoverPlugin } from '../../lib/graph/plugins/hover.plugin';
import { GraphDebugger } from '../../lib/graph/graph.debug';

// ─── Props ────────────────────────────────────────────────────────────────────
const props = defineProps<{
    data: CodeGraph | null
    progress: ProgressEvent | null
  }>();

// ─── Refs ─────────────────────────────────────────────────────────────────────
const graphContainer = ref<HTMLElement | null>(null);

// ─── Graph instance ───────────────────────────────────────────────────────────
let graph: GraphD3 | null = null;

function initGraph(data: CodeGraph): void 
{
  if (!graphContainer.value) return;

  if (!graph) 
  {
    // First render — create instance and register plugins once
    graph = new GraphD3({
      container: graphContainer.value,
      data,
    });

    graph
      .use(new ZoomDragPlugin())
      .use(new SelectionPlugin())
      .use(new HoverPlugin());

    // Attach sebelum render() — only active when DEV
    new GraphDebugger({
      enabled:      import.meta.env.DEV,
      logRenderTime: true,
      logNodeCount:  true,
      logSimulation: true,
      logFps:        true,
      logMemory:     true
    }).attachTo(graph);

    graph.render();
  }
  else 
  {
    // Subsequent renders — update data, plugins are preserved
    graph.update(data);
  }
}

// Re-render whenever the prop changes
watch(() => props.data, (newData) => 
{
  if (newData) initGraph(newData);
});

// When this component mounts (tab becomes active via v-if), render if data is
// already present — ensures D3 gets real container dimensions, not 0×0.
onMounted(async () => 
{
  if (props.data) 
  {
    await nextTick();
    initGraph(props.data);
  }
});

onUnmounted(() => 
{
  graph?.destroy();
  graph = null;
});
</script>

<template>
  <!--
    Outer wrapper fills whatever space the parent gives.
    `overflow-hidden` prevents this from growing the page height.
  -->
  <div class="relative h-full w-full"
    style="background-image: radial-gradient(#e5e7eb 1px, transparent 1px); background-size: 20px 20px;"
  >

    <!-- ── Waiting / in-progress state ──────────────────────────────────────── -->
    <div v-if="!data"
      class="
        absolute inset-0 z-10 flex flex-col items-center justify-center gap-6
        bg-white/80 backdrop-blur-sm
        dark:bg-gray-900/80
      "
    >
      <div class="relative flex h-28 w-28 items-center justify-center">
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
      <div class="text-center">
        <p class="
          text-lg font-bold text-gray-700
          dark:text-gray-300
        "
        >Menyiapkan Graph Node Codebase...</p>
        <p class="
          mt-1 max-w-md animate-pulse text-sm text-gray-500
          dark:text-gray-400
        "
        >
          {{ progress?.message || 'Menunggu proses analisa selesai.' }}
        </p>
      </div>
    </div>

    <!-- ── D3 Graph container ─────────────────────────────────────────────── -->
    <div v-if="data"
      ref="graphContainer"
      class="
        absolute inset-0 cursor-grab
        active:cursor-grabbing
      "
    />

    <!-- Empty graph placeholder -->
    <div v-if="data && data.nodes?.length === 0"
      class="
        pointer-events-none absolute inset-0 flex items-center justify-center
      "
    >
      <div class="text-center text-gray-400 italic">
        [ Tampilan Graph Node Codebase ]<br />
        Tidak ada node yang dapat dirender.
      </div>
    </div>

    <!-- ── Legend ─────────────────────────────────────────────────────────── -->
    <div v-if="data"
      class="
        pointer-events-none absolute right-6 bottom-6 flex flex-col gap-3
        rounded-xl border border-gray-200 bg-white/90 p-4 text-sm shadow-lg
        backdrop-blur-md
        dark:border-gray-700 dark:bg-gray-800/90
      "
    >
      <div
        class="
          mb-1 flex items-center justify-between gap-4 border-b border-gray-200
          pb-2 font-bold
          dark:border-gray-700
        "
      >
        <span>Nodes Legend</span>
        <span class="text-xs font-normal text-gray-500">{{ data.nodes?.length || 0 }} Nodes</span>
      </div>
      <div class="flex items-center gap-3">
        <div class="h-4 w-4 rounded-full bg-[#FBBF24]"></div> Directory
      </div>
      <div class="flex items-center gap-3">
        <div class="h-4 w-4 rounded-full bg-[#A78BFA]"></div> Namespace
      </div>
      <div class="flex items-center gap-3">
        <div class="h-4 w-4 rounded-full bg-[#60A5FA]"></div> File
      </div>
      <div class="flex items-center gap-3">
        <div class="h-4 w-4 rounded-full bg-[#34D399]"></div> Class
      </div>
      <div class="flex items-center gap-3">
        <div class="h-4 w-4 rounded-full bg-[#F87171]"></div> Function
      </div>
    </div>

  </div>
</template>
