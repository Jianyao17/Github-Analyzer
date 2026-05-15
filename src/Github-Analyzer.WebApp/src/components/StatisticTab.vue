<script setup lang="ts">
  import type { StatisticAnalysis } from '../types/statistic-analysis'
  import type { ProgressEvent } from '../composables/useProjectApi'
  import { computed } from 'vue'

  // ─── Props ────────────────────────────────────────────────────────────────────
  const props = defineProps<{
    data: StatisticAnalysis | null
    progress: ProgressEvent | null
  }>()

  // ─── Derived ──────────────────────────────────────────────────────────────────
  const formattedSize = computed(() => {
    const bytes = props.data?.sizeInBytes
    if (bytes == null) return '—'
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`
  })

  const commentRatio = computed(() => {
    const total = props.data?.totalLinesOfCode
    const comments = props.data?.commentLines
    if (!total || !comments) return 0
    return Math.round((comments / total) * 100)
  })
</script>

<template>
  <!-- ── Waiting / in-progress state ──────────────────────────────────────── -->
  <div v-if="!data" class="flex flex-col items-center justify-center flex-1 gap-6 py-16">
    <div class="relative flex items-center justify-center w-24 h-24">
      <svg class="absolute inset-0 w-full h-full" viewBox="0 0 100 100">
        <circle cx="50" cy="50" r="42" stroke="currentColor" stroke-width="8" fill="transparent"
          class="text-gray-200 dark:text-gray-700" />
      </svg>
      <svg class="absolute inset-0 w-full h-full animate-spin" viewBox="0 0 100 100">
        <circle cx="50" cy="50" r="42" stroke="currentColor" stroke-width="8" stroke-linecap="round" fill="transparent"
          stroke-dasharray="264" :stroke-dashoffset="264 - (264 * (progress?.progress || 0)) / 100"
          class="text-primary-500" />
      </svg>
      <span class="text-xl font-bold text-gray-800 dark:text-white">
        {{ Math.round(progress?.progress || 0) }}<span class="text-xs text-gray-400">%</span>
      </span>
    </div>
    <div class="text-center">
      <p class="font-semibold text-gray-700 dark:text-gray-300">Menyiapkan Analisis Statistik...</p>
      <p class="text-sm text-gray-500 dark:text-gray-400 mt-1 animate-pulse max-w-sm">
        {{ progress?.message || 'Menunggu proses analisa selesai.' }}
      </p>
    </div>
  </div>

  <!-- ── Data cards ─────────────────────────────────────────────────────────── -->
  <div v-else class="grid grid-cols-1 gap-4 overflow-y-auto pb-4">

    <!-- Row 1 — Git Statistics -->
    <div>
      <h2
        class="text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-gray-500 mb-3 flex items-center gap-2">
        <UIcon name="i-lucide-git-branch" class="w-3.5 h-3.5" /> Git Statistics
      </h2>
      <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">

        <UCard class="ring-1 ring-gray-200 dark:ring-gray-800 border-0" :ui="{ body: 'p-4' }">
          <div class="flex items-center gap-3">
            <div class="p-2 rounded-lg bg-amber-50 dark:bg-amber-900/20">
              <UIcon name="i-lucide-git-commit-horizontal" class="w-5 h-5 text-amber-600 dark:text-amber-400" />
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
                {{ data.totalCommits?.toLocaleString() ?? '—' }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400">Total Commits</p>
            </div>
          </div>
        </UCard>

        <UCard class="ring-1 ring-gray-200 dark:ring-gray-800 border-0" :ui="{ body: 'p-4' }">
          <div class="flex items-center gap-3">
            <div class="p-2 rounded-lg bg-violet-50 dark:bg-violet-900/20">
              <UIcon name="i-lucide-users" class="w-5 h-5 text-violet-600 dark:text-violet-400" />
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
                {{ data.totalContributors?.toLocaleString() ?? '—' }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400">Contributors</p>
            </div>
          </div>
        </UCard>

        <UCard class="ring-1 ring-gray-200 dark:ring-gray-800 border-0" :ui="{ body: 'p-4' }">
          <div class="flex items-center gap-3">
            <div class="p-2 rounded-lg bg-sky-50 dark:bg-sky-900/20">
              <UIcon name="i-lucide-git-branch-plus" class="w-5 h-5 text-sky-600 dark:text-sky-400" />
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
                {{ data.totalBranches?.toLocaleString() ?? '—' }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400">Branches</p>
            </div>
          </div>
        </UCard>

      </div>
    </div>

    <!-- Row 2 — Structural Statistics -->
    <div>
      <h2
        class="text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-gray-500 mb-3 flex items-center gap-2">
        <UIcon name="i-lucide-folder-open" class="w-3.5 h-3.5" /> Struktur Repository
      </h2>
      <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">

        <UCard class="ring-1 ring-gray-200 dark:ring-gray-800 border-0" :ui="{ body: 'p-4' }">
          <div class="flex items-center gap-3">
            <div class="p-2 rounded-lg bg-yellow-50 dark:bg-yellow-900/20">
              <UIcon name="i-lucide-folder" class="w-5 h-5 text-yellow-600 dark:text-yellow-400" />
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
                {{ data.totalFolders?.toLocaleString() ?? '—' }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400">Folders</p>
            </div>
          </div>
        </UCard>

        <UCard class="ring-1 ring-gray-200 dark:ring-gray-800 border-0" :ui="{ body: 'p-4' }">
          <div class="flex items-center gap-3">
            <div class="p-2 rounded-lg bg-blue-50 dark:bg-blue-900/20">
              <UIcon name="i-lucide-file-code-2" class="w-5 h-5 text-blue-600 dark:text-blue-400" />
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
                {{ data.totalFiles?.toLocaleString() ?? '—' }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400">Files</p>
            </div>
          </div>
        </UCard>

        <UCard class="ring-1 ring-gray-200 dark:ring-gray-800 border-0" :ui="{ body: 'p-4' }">
          <div class="flex items-center gap-3">
            <div class="p-2 rounded-lg bg-emerald-50 dark:bg-emerald-900/20">
              <UIcon name="i-lucide-database" class="w-5 h-5 text-emerald-600 dark:text-emerald-400" />
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
                {{ formattedSize }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400">Total Size</p>
            </div>
          </div>
        </UCard>

      </div>
    </div>

    <!-- Row 3 — Code Line Statistics -->
    <div>
      <h2
        class="text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-gray-500 mb-3 flex items-center gap-2">
        <UIcon name="i-lucide-code-2" class="w-3.5 h-3.5" /> Analisis Baris Kode
      </h2>
      <UCard class="ring-1 ring-gray-200 dark:ring-gray-800 border-0" :ui="{ body: 'p-5' }">
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-6">
          <div class="flex flex-col gap-1">
            <span class="text-3xl font-bold text-gray-900 dark:text-white tabular-nums">
              {{ data.totalLinesOfCode?.toLocaleString() ?? '—' }}
            </span>
            <span class="text-xs text-gray-500 dark:text-gray-400">Total Lines</span>
          </div>
          <div class="flex flex-col gap-1">
            <span class="text-3xl font-bold text-emerald-600 dark:text-emerald-400 tabular-nums">
              {{ data.codeLines?.toLocaleString() ?? '—' }}
            </span>
            <span class="text-xs text-gray-500 dark:text-gray-400">Code Lines</span>
          </div>
          <div class="flex flex-col gap-1">
            <span class="text-3xl font-bold text-sky-600 dark:text-sky-400 tabular-nums">
              {{ data.commentLines?.toLocaleString() ?? '—' }}
            </span>
            <span class="text-xs text-gray-500 dark:text-gray-400">Comments</span>
          </div>
          <div class="flex flex-col gap-1">
            <span class="text-3xl font-bold text-gray-500 dark:text-gray-400 tabular-nums">
              {{ data.blankLines?.toLocaleString() ?? '—' }}
            </span>
            <span class="text-xs text-gray-500 dark:text-gray-400">Blank Lines</span>
          </div>
        </div>

        <!-- Proportional breakdown bar -->
        <div v-if="data.totalLinesOfCode" class="mt-5 space-y-2">
          <div class="flex h-3 w-full rounded-full overflow-hidden gap-px">
            <div class="bg-emerald-500 dark:bg-emerald-400 transition-all duration-700"
              :style="{ width: ((data.codeLines || 0) / data.totalLinesOfCode * 100) + '%' }" />
            <div class="bg-sky-400 transition-all duration-700"
              :style="{ width: ((data.commentLines || 0) / data.totalLinesOfCode * 100) + '%' }" />
            <div class="bg-gray-200 dark:bg-gray-700 flex-1 transition-all duration-700" />
          </div>
          <div class="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400">
            <span class="flex items-center gap-1.5">
              <span class="w-2.5 h-2.5 rounded-sm bg-emerald-500 dark:bg-emerald-400 inline-block"></span>
              Code
            </span>
            <span class="flex items-center gap-1.5">
              <span class="w-2.5 h-2.5 rounded-sm bg-sky-400 inline-block"></span>
              Comments ({{ commentRatio }}%)
            </span>
            <span class="flex items-center gap-1.5">
              <span class="w-2.5 h-2.5 rounded-sm bg-gray-200 dark:bg-gray-700 inline-block"></span>
              Blank
            </span>
          </div>
        </div>
      </UCard>
    </div>

    <!-- Footer meta -->
    <p v-if="data.generatedAtUtc" class="text-xs text-gray-400 dark:text-gray-600 text-right pb-1">
      Generated {{ new Date(data.generatedAtUtc).toLocaleString() }}
    </p>

  </div>
</template>
