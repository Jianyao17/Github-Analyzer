<script setup lang="ts">
import type { ProgressEvent } from '@/composables/useProjectApi';
import type { CodeGraph } from '@/types/analysis/code-graph';
import CodeGraphView from '@/components/code-graph/CodeGraphView.vue';

// ─── Props ────────────────────────────────────────────────────────────────────
defineProps<{
  data: CodeGraph | null
  progress: ProgressEvent | null
}>();
</script>

<template>
  <!--
    CodeGraphTab — manages the loading/ready state for the graph tab.
    When data is available it delegates all rendering to CodeGraphView.
  -->
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
          <circle
            cx="50"
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
          <circle
            cx="50"
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

    <!-- ── Graph view (rendered once data is ready) ────────────────────────── -->
    <CodeGraphView
      v-if="data"
      :data="data"
      class="absolute inset-0"
    />

  </div>
</template>
