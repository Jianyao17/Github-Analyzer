<script setup lang="ts">
import { computed, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useProjectApi } from '@/composables/useProjectApi';
import { useOnboardingStore } from '@/stores/onboarding.store';
import type { ProgressEvent } from '@/composables/useProjectApi';

const route = useRoute();
const store = useOnboardingStore();
const { useStatisticQuery } = useProjectApi();

// ─── Props ────────────────────────────────────────────────────────────────────
defineProps<{
  progress: ProgressEvent | null
}>();

const { data, isLoading } = useStatisticQuery(route.params.id as string);

// ─── Derived ──────────────────────────────────────────────────────────────────
const formattedSize = computed(() => 
{
  const bytes = data.value?.sizeInBytes;
  if (bytes == null) return '—';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
});

const commentRatio = computed(() => 
{
  const total = data.value?.totalLinesOfCode;
  const comments = data.value?.commentLines;
  if (!total || !comments) return 0;
  return Math.round((comments / total) * 100);
});

// ─── Onboarding ───────────────────────────────────────────────────────────────
watch(data, (newData) => 
{
  if (newData) 
  {
    store.triggerOverviewTour();
  }
}, { immediate: true });
</script>

<template>
  <div class="flex h-full w-full flex-col">
    <!-- ── Loading state ────────────────────────────────────────────────────── -->
    <div v-if="isLoading"
      class="
        flex h-full flex-col overflow-hidden rounded-xl border-0
        bg-[var(--ui-bg)]/70 ring-1 ring-[var(--ui-border)] backdrop-blur-md
      "
    >
      <div class="
        flex-1 overflow-y-auto p-4
        lg:p-5
      "
      >
        <div class="flex flex-col gap-6">
          
          <!-- Git Statistics Skeleton -->
          <div class="flex flex-col gap-3">
            <NSkeleton class="h-4 w-28 rounded-md" />
            <div class="flex flex-col gap-2">
              <div v-for="i in 3"
                :key="'git-'+i"
                class="flex items-center justify-between py-0.5"
              >
                <div class="flex items-center gap-2">
                  <NSkeleton class="h-4 w-4 rounded-full" />
                  <NSkeleton class="h-4 w-20 rounded-md" />
                </div>
                <NSkeleton class="h-4 w-12 rounded-md" />
              </div>
            </div>
          </div>

          <hr class="border-[var(--ui-border)]" />

          <!-- Repository Structure Skeleton -->
          <div class="flex flex-col gap-3">
            <NSkeleton class="h-4 w-36 rounded-md" />
            <div class="flex flex-col gap-2">
              <div v-for="i in 3"
                :key="'struct-'+i"
                class="flex items-center justify-between py-0.5"
              >
                <div class="flex items-center gap-2">
                  <NSkeleton class="h-4 w-4 rounded-full" />
                  <NSkeleton class="h-4 w-16 rounded-md" />
                </div>
                <NSkeleton class="h-4 w-14 rounded-md" />
              </div>
            </div>
          </div>

          <hr class="border-[var(--ui-border)]" />

          <!-- Code Lines Skeleton -->
          <div class="flex flex-col gap-3">
            <NSkeleton class="h-4 w-32 rounded-md" />
            <div class="mt-1 flex flex-col gap-3">
              <NSkeleton class="h-2 w-full rounded-full" />
              <div class="mt-1 flex flex-col gap-2">
                <div v-for="i in 3"
                  :key="'lines-'+i"
                  class="flex items-center justify-between py-0.5"
                >
                  <div class="flex items-center gap-2">
                    <NSkeleton class="h-2.5 w-2.5 rounded-full" />
                    <NSkeleton class="h-4 w-14 rounded-md" />
                    <NSkeleton class="h-4 w-8 rounded-md" />
                  </div>
                  <NSkeleton class="h-4 w-16 rounded-md" />
                </div>
              </div>
            </div>
          </div>

        </div>
      </div>
    </div>

    <!-- ── Waiting / in-progress state ──────────────────────────────────────── -->
    <div v-else-if="!data"
      class="
        flex h-full flex-col items-center justify-center gap-6 rounded-xl
        border-1 border-[var(--ui-border)] bg-[var(--ui-bg)]/70 py-12
        backdrop-blur-md
      "
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
      <div class="px-4 text-center">
        <p class="font-semibold text-[var(--ui-text)]">Menyiapkan Analisis...</p>
        <p class="
          mt-1 max-w-[200px] animate-pulse text-sm text-[var(--ui-text-muted)]
        "
        >
          {{ progress?.message || 'Menunggu proses selesai.' }}
        </p>
      </div>
    </div>

    <!-- ── Data cards ─────────────────────────────────────────────────────────── -->
    <div v-else
      class="
        flex h-full flex-col overflow-hidden rounded-xl border-0
        bg-[var(--ui-bg)]/70 ring-1 ring-[var(--ui-border)] backdrop-blur-md
      "
    >
      <div class="
        flex-1 overflow-y-auto p-4
        lg:p-5
      "
      >
        <div class="flex flex-col gap-6">
          
          <!-- Git Statistics -->
          <div class="flex flex-col gap-3">
            <h2 class="text-sm font-semibold text-[var(--ui-text-highlighted)]">Git Statistics</h2>
            <div class="flex flex-col gap-2 text-sm text-[var(--ui-text)]"
              id="git-stats"
            >
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2 text-[var(--ui-text-muted)]">
                  <NIcon name="i-lucide-git-commit-horizontal"
                    class="h-4 w-4"
                  />
                  <span>Commits</span>
                </div>
                <span class="font-semibold text-[var(--ui-text-highlighted)]">{{ data.totalCommits?.toLocaleString() ?? '—' }}</span>
              </div>
              
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2 text-[var(--ui-text-muted)]">
                  <NIcon name="i-lucide-users"
                    class="h-4 w-4"
                  />
                  <span>Contributors</span>
                </div>
                <span class="font-semibold text-[var(--ui-text-highlighted)]">{{ data.totalContributors?.toLocaleString() ?? '—' }}</span>
              </div>
              
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2 text-[var(--ui-text-muted)]">
                  <NIcon name="i-lucide-git-branch-plus"
                    class="h-4 w-4"
                  />
                  <span>Branches</span>
                </div>
                <span class="font-semibold text-[var(--ui-text-highlighted)]">{{ data.totalBranches?.toLocaleString() ?? '—' }}</span>
              </div>
            </div>
          </div>

          <hr class="border-[var(--ui-border)]" />

          <!-- Structural Statistics -->
          <div class="flex flex-col gap-3">
            <h2 class="text-sm font-semibold text-[var(--ui-text-highlighted)]">Repository Structure</h2>
            <div class="flex flex-col gap-2 text-sm text-[var(--ui-text)]">
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2 text-[var(--ui-text-muted)]">
                  <NIcon name="i-lucide-folder"
                    class="h-4 w-4"
                  />
                  <span>Folders</span>
                </div>
                <span class="font-semibold text-[var(--ui-text-highlighted)]">{{ data.totalFolders?.toLocaleString() ?? '—' }}</span>
              </div>
              
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2 text-[var(--ui-text-muted)]">
                  <NIcon name="i-lucide-file-code-2"
                    class="h-4 w-4"
                  />
                  <span>Files</span>
                </div>
                <span class="font-semibold text-[var(--ui-text-highlighted)]">{{ data.totalFiles?.toLocaleString() ?? '—' }}</span>
              </div>
              
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2 text-[var(--ui-text-muted)]">
                  <NIcon name="i-lucide-database"
                    class="h-4 w-4"
                  />
                  <span>Total Size</span>
                </div>
                <span class="font-semibold text-[var(--ui-text-highlighted)]">{{ formattedSize }}</span>
              </div>
            </div>
          </div>

          <hr class="border-[var(--ui-border)]" />

          <!-- Code Line Statistics -->
          <div class="flex flex-col gap-3"
            id="code-lines"
          >
            <h2 class="text-sm font-semibold text-[var(--ui-text-highlighted)]">Languages & Lines</h2>
            
            <div v-if="data.totalLinesOfCode"
              class="mt-1 flex flex-col gap-3"
            >
              <!-- Proportional breakdown bar -->
              <div class="flex h-2 w-full gap-px overflow-hidden rounded-full">
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
                    h-full flex-1 bg-[var(--ui-border)] transition-all
                    duration-700
                  "
                  />
                </NTooltip>
              </div>
              
              <!-- Legend list -->
              <div class="
                mt-1 flex flex-col gap-2 text-sm text-[var(--ui-text)]
              "
              >
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="
                      inline-block h-2.5 w-2.5 rounded-full bg-emerald-500
                      dark:bg-emerald-400
                    "
                    ></span>
                    <span class="
                      font-semibold text-[var(--ui-text-highlighted)]
                    "
                    >Code</span>
                    <span class="text-[var(--ui-text-muted)]">{{ ((data.codeLines || 0) / data.totalLinesOfCode * 100).toFixed(1) }}%</span>
                  </div>
                  <span class="text-[var(--ui-text-muted)]">{{ data.codeLines?.toLocaleString() ?? '—' }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="
                      inline-block h-2.5 w-2.5 rounded-full bg-sky-400
                    "
                    ></span>
                    <span class="
                      font-semibold text-[var(--ui-text-highlighted)]
                    "
                    >Comments</span>
                    <span class="text-[var(--ui-text-muted)]">{{ commentRatio }}%</span>
                  </div>
                  <span class="text-[var(--ui-text-muted)]">{{ data.commentLines?.toLocaleString() ?? '—' }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="
                      inline-block h-2.5 w-2.5 rounded-full
                      bg-[var(--ui-border)]
                    "
                    ></span>
                    <span class="
                      font-semibold text-[var(--ui-text-highlighted)]
                    "
                    >Blank</span>
                  </div>
                  <span class="text-[var(--ui-text-muted)]">{{ data.blankLines?.toLocaleString() ?? '—' }}</span>
                </div>
              </div>
            </div>
            <div v-else
              class="text-sm text-[var(--ui-text-muted)]"
            >No line data available</div>
          </div>
        </div>
      </div>
      
      <!-- Footer meta -->
      <div v-if="data.generatedAtUtc"
        class="
          border-t border-[var(--ui-border)] bg-[var(--ui-bg-elevated)]/30 p-4
        "
      >
        <p class="text-xs text-[var(--ui-text-muted)]">
          Generated {{ new Date(data.generatedAtUtc).toLocaleString() }}
        </p>
      </div>
    </div>
  </div>
</template>
