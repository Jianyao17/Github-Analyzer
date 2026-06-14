<script setup lang="ts">
import type { StatisticAnalysis } from '../../types/analysis/statistic-analysis';
import type { ProgressEvent } from '../../composables/useProjectApi';
import { useOnboardingStore } from '../../stores/onboarding.store';
import { computed, watch } from 'vue';

const store = useOnboardingStore();

// ─── Props ────────────────────────────────────────────────────────────────────
const props = defineProps<{
    data: StatisticAnalysis | null
    progress: ProgressEvent | null
  }>();

// ─── Derived ──────────────────────────────────────────────────────────────────
const formattedSize = computed(() => 
{
  const bytes = props.data?.sizeInBytes;
  if (bytes == null) return '—';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
});

const commentRatio = computed(() => 
{
  const total = props.data?.totalLinesOfCode;
  const comments = props.data?.commentLines;
  if (!total || !comments) return 0;
  return Math.round((comments / total) * 100);
});

// ─── Onboarding ───────────────────────────────────────────────────────────────
watch(() => props.data, (newData) => 
{
  if (newData) 
  {
    store.triggerOverviewTour();
  }
}, { immediate: true });
</script>

<template>
  <div class="flex h-full min-h-0 w-full flex-col">
    <!-- ── Waiting / in-progress state ──────────────────────────────────────── -->
    <div v-if="!data"
      class="flex flex-1 flex-col items-center justify-center gap-6 py-16"
    >
      <div class="relative flex h-24 w-24 items-center justify-center">
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
            :stroke-dashoffset="264 - (264 * (progress?.progress || 0)) / 100"
            class="text-primary-500"
          />
        </svg>
        <span class="text-xl font-bold text-[var(--ui-text-highlighted)]">
          {{ Math.round(progress?.progress || 0) }}<span class="
            text-xs text-[var(--ui-text-muted)]
          "
          >%</span>
        </span>
      </div>
      <div class="text-center">
        <p class="font-semibold text-[var(--ui-text)]">Menyiapkan Analisis Statistik...</p>
        <p class="
          mt-1 max-w-sm animate-pulse text-sm text-[var(--ui-text-muted)]
        "
        >
          {{ progress?.message || 'Menunggu proses analisa selesai.' }}
        </p>
      </div>
    </div>

    <!-- ── Data cards ─────────────────────────────────────────────────────────── -->
    <div v-else
      class="grid grid-cols-1 gap-4 overflow-y-auto pb-4"
    >

      <!-- Row 1 — Git Statistics -->
      <div>
        <h2
          class="
            mb-3 flex items-center gap-2 text-xs font-semibold tracking-wider
            text-[var(--ui-text-muted)] uppercase
          "
        >
          <NIcon name="i-lucide-git-branch"
            class="h-3.5 w-3.5"
          /> Git Statistics
        </h2>
        <div id="onboarding-git-stats"
          class="
            grid grid-cols-2 gap-3
            sm:grid-cols-3
          "
        >

          <NCard class="border-0 ring-1 ring-[var(--ui-border)]"
            :ui="{ body: 'p-4' }"
          >
            <div class="flex items-center gap-3">
              <div class="
                rounded-lg bg-amber-50 p-2
                dark:bg-amber-900/20
              "
              >
                <NIcon name="i-lucide-git-commit-horizontal"
                  class="
                    h-5 w-5 text-amber-600
                    dark:text-amber-400
                  "
                />
              </div>
              <div>
                <p class="
                  text-2xl font-bold text-[var(--ui-text-highlighted)]
                  tabular-nums
                "
                >
                  {{ data.totalCommits?.toLocaleString() ?? '—' }}
                </p>
                <p class="text-xs text-[var(--ui-text-muted)]">Total Commits</p>
              </div>
            </div>
          </NCard>

          <NCard class="border-0 ring-1 ring-[var(--ui-border)]"
            :ui="{ body: 'p-4' }"
          >
            <div class="flex items-center gap-3">
              <div class="
                rounded-lg bg-violet-50 p-2
                dark:bg-violet-900/20
              "
              >
                <NIcon name="i-lucide-users"
                  class="
                    h-5 w-5 text-violet-600
                    dark:text-violet-400
                  "
                />
              </div>
              <div>
                <p class="
                  text-2xl font-bold text-[var(--ui-text-highlighted)]
                  tabular-nums
                "
                >
                  {{ data.totalContributors?.toLocaleString() ?? '—' }}
                </p>
                <p class="text-xs text-[var(--ui-text-muted)]">Contributors</p>
              </div>
            </div>
          </NCard>

          <NCard class="border-0 ring-1 ring-[var(--ui-border)]"
            :ui="{ body: 'p-4' }"
          >
            <div class="flex items-center gap-3">
              <div class="
                rounded-lg bg-sky-50 p-2
                dark:bg-sky-900/20
              "
              >
                <NIcon name="i-lucide-git-branch-plus"
                  class="
                    h-5 w-5 text-sky-600
                    dark:text-sky-400
                  "
                />
              </div>
              <div>
                <p class="
                  text-2xl font-bold text-[var(--ui-text-highlighted)]
                  tabular-nums
                "
                >
                  {{ data.totalBranches?.toLocaleString() ?? '—' }}
                </p>
                <p class="text-xs text-[var(--ui-text-muted)]">Branches</p>
              </div>
            </div>
          </NCard>

        </div>
      </div>

      <!-- Row 2 — Structural Statistics -->
      <div>
        <h2
          class="
            mb-3 flex items-center gap-2 text-xs font-semibold tracking-wider
            text-[var(--ui-text-muted)] uppercase
          "
        >
          <NIcon name="i-lucide-folder-open"
            class="h-3.5 w-3.5"
          /> Struktur Repository
        </h2>
        <div class="
          grid grid-cols-2 gap-3
          sm:grid-cols-3
        "
        >

          <NCard class="border-0 ring-1 ring-[var(--ui-border)]"
            :ui="{ body: 'p-4' }"
          >
            <div class="flex items-center gap-3">
              <div class="
                rounded-lg bg-yellow-50 p-2
                dark:bg-yellow-900/20
              "
              >
                <NIcon name="i-lucide-folder"
                  class="
                    h-5 w-5 text-yellow-600
                    dark:text-yellow-400
                  "
                />
              </div>
              <div>
                <p class="
                  text-2xl font-bold text-[var(--ui-text-highlighted)]
                  tabular-nums
                "
                >
                  {{ data.totalFolders?.toLocaleString() ?? '—' }}
                </p>
                <p class="text-xs text-[var(--ui-text-muted)]">Folders</p>
              </div>
            </div>
          </NCard>

          <NCard class="border-0 ring-1 ring-[var(--ui-border)]"
            :ui="{ body: 'p-4' }"
          >
            <div class="flex items-center gap-3">
              <div class="
                rounded-lg bg-blue-50 p-2
                dark:bg-blue-900/20
              "
              >
                <NIcon name="i-lucide-file-code-2"
                  class="
                    h-5 w-5 text-blue-600
                    dark:text-blue-400
                  "
                />
              </div>
              <div>
                <p class="
                  text-2xl font-bold text-[var(--ui-text-highlighted)]
                  tabular-nums
                "
                >
                  {{ data.totalFiles?.toLocaleString() ?? '—' }}
                </p>
                <p class="text-xs text-[var(--ui-text-muted)]">Files</p>
              </div>
            </div>
          </NCard>

          <NCard class="border-0 ring-1 ring-[var(--ui-border)]"
            :ui="{ body: 'p-4' }"
          >
            <div class="flex items-center gap-3">
              <div class="
                rounded-lg bg-emerald-50 p-2
                dark:bg-emerald-900/20
              "
              >
                <NIcon name="i-lucide-database"
                  class="
                    h-5 w-5 text-emerald-600
                    dark:text-emerald-400
                  "
                />
              </div>
              <div>
                <p class="
                  text-2xl font-bold text-[var(--ui-text-highlighted)]
                  tabular-nums
                "
                >
                  {{ formattedSize }}
                </p>
                <p class="text-xs text-[var(--ui-text-muted)]">Total Size</p>
              </div>
            </div>
          </NCard>

        </div>
      </div>

      <!-- Row 3 — Code Line Statistics -->
      <div>
        <h2
          class="
            mb-3 flex items-center gap-2 text-xs font-semibold tracking-wider
            text-[var(--ui-text-muted)] uppercase
          "
        >
          <NIcon name="i-lucide-code-2"
            class="h-3.5 w-3.5"
          /> Analisis Baris Kode
        </h2>
        <NCard id="onboarding-code-lines"
          class="
            border-0 ring-1 ring-gray-200
            dark:ring-gray-800
          "
          :ui="{ body: 'p-5' }"
        >
          <div class="
            grid grid-cols-2 gap-6
            sm:grid-cols-4
          "
          >
            <div class="flex flex-col gap-1">
              <span class="
                text-3xl font-bold text-[var(--ui-text-highlighted)]
                tabular-nums
              "
              >
                {{ data.totalLinesOfCode?.toLocaleString() ?? '—' }}
              </span>
              <span class="
                text-xs text-gray-500
                dark:text-gray-400
              "
              >Total Lines</span>
            </div>
            <div class="flex flex-col gap-1">
              <span class="
                text-3xl font-bold text-emerald-600 tabular-nums
                dark:text-emerald-400
              "
              >
                {{ data.codeLines?.toLocaleString() ?? '—' }}
              </span>
              <span class="
                flex items-center gap-1 text-xs text-gray-500
                dark:text-gray-400
              "
              >
                Code Lines
                <NTooltip text="Baris kode murni (tanpa komentar/kosong)." :popper="{ placement: 'top' }">
                  <NIcon name="i-lucide-info" class="h-3 w-3 cursor-help text-[var(--ui-text-muted)]" />
                </NTooltip>
              </span>
            </div>
            <div class="flex flex-col gap-1">
              <span class="
                text-3xl font-bold text-sky-600 tabular-nums
                dark:text-sky-400
              "
              >
                {{ data.commentLines?.toLocaleString() ?? '—' }}
              </span>
              <span class="
                flex items-center gap-1 text-xs text-gray-500
                dark:text-gray-400
              "
              >
                Comments
                <NTooltip text="Baris dokumentasi atau komentar." :popper="{ placement: 'top' }">
                  <NIcon name="i-lucide-info" class="h-3 w-3 cursor-help text-[var(--ui-text-muted)]" />
                </NTooltip>
              </span>
            </div>
            <div class="flex flex-col gap-1">
              <span class="
                text-3xl font-bold text-[var(--ui-text-muted)] tabular-nums
              "
              >
                {{ data.blankLines?.toLocaleString() ?? '—' }}
              </span>
              <span class="
                flex items-center gap-1 text-xs text-gray-500
                dark:text-gray-400
              "
              >
                Blank Lines
                <NTooltip text="Baris kosong untuk spasi pemformatan." :popper="{ placement: 'top' }">
                  <NIcon name="i-lucide-info" class="h-3 w-3 cursor-help text-[var(--ui-text-muted)]" />
                </NTooltip>
              </span>
            </div>
          </div>

          <!-- Proportional breakdown bar -->
          <div v-if="data.totalLinesOfCode"
            class="mt-5 space-y-2"
          >
            <div class="flex h-3 w-full gap-px overflow-hidden rounded-full">
              <NTooltip :text="`${((data.codeLines || 0) / data.totalLinesOfCode * 100).toFixed(1)}% (${data.codeLines?.toLocaleString() ?? 0} lines)`">
                <div class="
                  h-full bg-emerald-500 transition-all duration-700
                  dark:bg-emerald-400
                "
                  :style="{ width: ((data.codeLines || 0) / data.totalLinesOfCode * 100) + '%' }"
                />
              </NTooltip>
              <NTooltip :text="`${((data.commentLines || 0) / data.totalLinesOfCode * 100).toFixed(1)}% (${data.commentLines?.toLocaleString() ?? 0} lines)`">
                <div class="h-full bg-sky-400 transition-all duration-700"
                  :style="{ width: ((data.commentLines || 0) / data.totalLinesOfCode * 100) + '%' }"
                />
              </NTooltip>
              <NTooltip :text="`${((data.blankLines || 0) / data.totalLinesOfCode * 100).toFixed(1)}% (${data.blankLines?.toLocaleString() ?? 0} lines)`">
                <div class="
                  h-full flex-1 bg-[var(--ui-border)] transition-all duration-700
                "
                />
              </NTooltip>
            </div>
            <div class="
              flex items-center gap-4 text-xs text-[var(--ui-text-muted)]
            "
            >
              <span class="flex items-center gap-1.5">
                <span class="
                  inline-block h-2.5 w-2.5 rounded-sm bg-emerald-500
                  dark:bg-emerald-400
                "
                ></span>
                Code
              </span>
              <span class="flex items-center gap-1.5">
                <span class="inline-block h-2.5 w-2.5 rounded-sm bg-sky-400"></span>
                Comments ({{ commentRatio }}%)
              </span>
              <span class="flex items-center gap-1.5">
                <span class="
                  inline-block h-2.5 w-2.5 rounded-sm bg-[var(--ui-border)]
                "
                ></span>
                Blank
              </span>
            </div>
          </div>
        </NCard>
      </div>

      <!-- Footer meta -->
      <p v-if="data.generatedAtUtc"
        class="pb-1 text-right text-xs text-[var(--ui-text-muted)]"
      >
        Generated {{ new Date(data.generatedAtUtc).toLocaleString() }}
      </p>
    </div>
  </div>
</template>
