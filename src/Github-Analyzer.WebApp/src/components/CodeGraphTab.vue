<script setup lang="ts">
  import { ref, watch, onMounted, nextTick } from 'vue'
  import { useCodeGraphD3 } from '../composables/useCodeGraphD3'
  import type { ProgressEvent } from '../composables/useProjectApi'
  import type { CodeGraph } from '../types/code-graph'

  // ─── Props ────────────────────────────────────────────────────────────────────
  const props = defineProps<{
    data: CodeGraph | null
    progress: ProgressEvent | null
  }>()

  // ─── Refs ─────────────────────────────────────────────────────────────────────
  const graphContainer = ref<HTMLElement | null>(null)
  const graphData = ref<CodeGraph | null>(props.data)

  // ─── D3 ───────────────────────────────────────────────────────────────────────
  const { renderGraph } = useCodeGraphD3(graphContainer, graphData)

  // Keep graphData ref in sync when the prop changes
  // (useCodeGraphD3 will re-render via its own flush:'post' watch)
  watch(() => props.data, (newData) => {
    graphData.value = newData
  })

  // When this component mounts (tab becomes active via v-if), re-render if data
  // is already present — ensures D3 gets real container dimensions, not 0×0.
  onMounted(async () => {
    if (graphData.value) {
      await nextTick()
      renderGraph()
    }
  })

</script>

<template>
  <!--
    Outer wrapper fills whatever space the parent gives.
    `overflow-hidden` prevents this from growing the page height.
  -->
  <div class="relative w-full h-full"
    style="background-image: radial-gradient(#e5e7eb 1px, transparent 1px); background-size: 20px 20px;">

    <!-- ── Waiting / in-progress state ──────────────────────────────────────── -->
    <div v-if="!data"
      class="absolute inset-0 flex flex-col items-center justify-center bg-white/80 dark:bg-gray-900/80 backdrop-blur-sm z-10 gap-6">
      <div class="relative flex items-center justify-center w-28 h-28">
        <svg class="absolute inset-0 w-full h-full" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="42" stroke="currentColor" stroke-width="8" fill="transparent"
            class="text-gray-200 dark:text-gray-700" />
        </svg>
        <svg class="absolute inset-0 w-full h-full animate-spin" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="42" stroke="currentColor" stroke-width="8" stroke-linecap="round"
            fill="transparent" stroke-dasharray="264" stroke-dashoffset="160" class="text-green-500" />
        </svg>
        <span class="text-2xl font-bold text-gray-800 dark:text-white">
          {{ Math.round(progress?.progress || 0) }}<span class="text-sm text-gray-500">%</span>
        </span>
      </div>
      <div class="text-center">
        <p class="font-bold text-lg text-gray-700 dark:text-gray-300">Menyiapkan Graph Node Codebase...</p>
        <p class="text-sm text-gray-500 dark:text-gray-400 max-w-md mt-1 animate-pulse">
          {{ progress?.message || 'Menunggu proses analisa selesai.' }}
        </p>
      </div>
    </div>

    <!-- ── D3 Graph container ─────────────────────────────────────────────── -->
    <div v-if="data" ref="graphContainer" class="absolute inset-0 cursor-grab active:cursor-grabbing" />

    <!-- Empty graph placeholder -->
    <div v-if="data && data.nodes?.length === 0"
      class="absolute inset-0 flex items-center justify-center pointer-events-none">
      <div class="text-center text-gray-400 italic">
        [ Tampilan Graph Node Codebase ]<br />
        Tidak ada node yang dapat dirender.
      </div>
    </div>

    <!-- ── Legend ─────────────────────────────────────────────────────────── -->
    <div v-if="data"
      class="absolute bottom-6 right-6 bg-white/90 dark:bg-gray-800/90 backdrop-blur-md p-4 rounded-xl shadow-lg border border-gray-200 dark:border-gray-700 text-sm flex flex-col gap-3 pointer-events-none">
      <div
        class="font-bold border-b border-gray-200 dark:border-gray-700 pb-2 mb-1 flex items-center justify-between gap-4">
        <span>Nodes Legend</span>
        <span class="text-xs font-normal text-gray-500">{{ data.nodes?.length || 0 }} Nodes</span>
      </div>
      <div class="flex items-center gap-3">
        <div class="w-4 h-4 rounded-full bg-[#FBBF24]"></div> Directory
      </div>
      <div class="flex items-center gap-3">
        <div class="w-4 h-4 rounded-full bg-[#A78BFA]"></div> Namespace
      </div>
      <div class="flex items-center gap-3">
        <div class="w-4 h-4 rounded-full bg-[#60A5FA]"></div> File
      </div>
      <div class="flex items-center gap-3">
        <div class="w-4 h-4 rounded-full bg-[#34D399]"></div> Class
      </div>
      <div class="flex items-center gap-3">
        <div class="w-4 h-4 rounded-full bg-[#F87171]"></div> Function
      </div>
    </div>

  </div>
</template>
