<script setup lang="ts">
import { useQueryCache } from '@pinia/colada';
import { useRoute, useRouter } from 'vue-router';
import { computed, onUnmounted, ref, watch } from 'vue';
import { useProjectApi } from '@/composables/useProjectApi';
import type { ProgressEvent } from '@/composables/useProjectApi';
import OverviewTab from '@/components/project-details-tab/OverviewTab.vue';
import CodeGraphTab from '@/components/project-details-tab/CodeGraphTab.vue';

const route = useRoute();
const router = useRouter();
const queryCache = useQueryCache();
const {
  useProjectQuery,
  streamQueueProgress,
  issueStreamToken
} = useProjectApi();

// ─── Page state ───────────────────────────────────────────────────────────────

const projectId = computed(() => route.params.id as string);

const { data: project, isLoading: loading } = useProjectQuery(projectId);
const activeTab = computed({
  get() 
  {
    const tab = route.query.tab as string;
    return ['overview', 'codegraph'].includes(tab) 
      ? (tab as 'overview' | 'codegraph') 
      : 'overview';
  },
  set(newTab: 'overview' | 'codegraph') 
  {
    router.replace({ query: { ...route.query, tab: newTab } });
  }
});

const transitionName = ref('slide-left');
watch(activeTab, (newTab, oldTab) => 
{
  if (newTab === 'codegraph' && oldTab === 'overview') 
  {
    transitionName.value = 'slide-left';
  }
  else if (newTab === 'overview' && oldTab === 'codegraph') 
  {
    transitionName.value = 'slide-right';
  }
});

// ─── Code Graph state ─────────────────────────────────────────────────────────
const codeGraphProgress = ref<ProgressEvent | null>(null);

// ─── Statistic state ──────────────────────────────────────────────────────────
const statisticProgress = ref<ProgressEvent | null>(null);

// ─── Repo display helpers ──────────────────────────────────────────────────────
const githubRepoInfo = computed(() =>
{
  if (!project.value) return null;

  const parsed = parseGithubRepositoryUrl(project.value.repositoryUrl);
  if (!parsed) return null;

  return {
    owner: parsed.owner,
    repository: project.value.repositoryName || parsed.repository,
    ownerUrl: `https://github.com/${parsed.owner}`,
    repositoryUrl: `https://github.com/${parsed.owner}/${project.value.repositoryName || parsed.repository}`,
  };
});

const formattedCreatedAt = computed(() =>
{
  if (!project.value?.createdAtUtc) return '';
  try
  {
    const date = new Date(project.value.createdAtUtc);
    return new Intl.DateTimeFormat('id-ID', {
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    }).format(date);
  }
  catch
  {
    return '';
  }
});

// ─── Unsubscribe handles ──────────────────────────────────────────────────────
let unsubCodeGraph: (() => void) | null = null;
let unsubStatistic: (() => void) | null = null;

function parseGithubRepositoryUrl(repositoryUrl: string)
{
  try
  {
    const parsedUrl = new URL(repositoryUrl);
    const segments = parsedUrl.pathname.replace(/\.git$/, '').split('/').filter(Boolean);

    if (segments.length < 2) return null;

    const [owner, repository] = segments.slice(-2);
    return { owner, repository };
  }
  catch
  {
    return null;
  }
}

// ─── Fetch / Watch ────────────────────────────────────────────────────────────
watch(project, async (newProject) => 
{
  if (!newProject) return;

  let sharedStreamToken: string | undefined;
  if (!newProject.hasStatistic || !newProject.hasCodeGraph)
  {
    const { token } = await issueStreamToken(newProject.id);
    sharedStreamToken = token;
  }

  if (!newProject.hasStatistic && !unsubStatistic) 
  {
    subscribeToStatistic(sharedStreamToken);
  }

  if (!newProject.hasCodeGraph && !unsubCodeGraph) 
  {
    subscribeToCodeGraph(sharedStreamToken);
  }
}, { immediate: true });

// ─── Code Graph ───────────────────────────────────────────────────────────────
async function subscribeToCodeGraph(token?: string) 
{
  if (!project.value) return;
  try 
  {
    const unsub = await streamQueueProgress(
      project.value.id, 'CodeGraph',
      async (event) => 
      {
        codeGraphProgress.value = event;
        if (event.status === 'Completed') 
        {
          // Small delay to let the backend flush the DB write before we fetch
          await new Promise(r => setTimeout(r, 500));
          queryCache.invalidateQueries({ key: ['analysis', 'codegraph', project.value?.id as string] });
          queryCache.invalidateQueries({ key: ['project', project.value?.id as string] });
        }
      },
      {
        onComplete: () => 
        {
          if (!codeGraphProgress.value || codeGraphProgress.value.status !== 'Completed')
            queryCache.invalidateQueries({ key: ['analysis', 'codegraph', project.value?.id as string] });
        },
        token
      }
    );
    if (typeof unsub === 'function') unsubCodeGraph = unsub;
  }
  catch 
  {
    queryCache.invalidateQueries({ key: ['analysis', 'codegraph', project.value?.id] });
  }
}

// ─── Statistic ────────────────────────────────────────────────────────────────
async function subscribeToStatistic(token?: string) 
{
  if (!project.value) return;
  try 
  {
    const unsub = await streamQueueProgress(
      project.value.id, 'Statistic',
      async (event) => 
      {
        statisticProgress.value = event;
        if (event.status === 'Completed') 
        {
          await new Promise(r => setTimeout(r, 500));
          queryCache.invalidateQueries({ key: ['analysis', 'statistic', project.value?.id as string] });
          queryCache.invalidateQueries({ key: ['project', project.value?.id as string] });
        }
      },
      {
        onComplete: () => 
        {
          if (!statisticProgress.value || statisticProgress.value.status !== 'Completed')
            queryCache.invalidateQueries({ key: ['analysis', 'statistic', project.value?.id as string] });
        },
        token
      }
    );
    if (typeof unsub === 'function') unsubStatistic = unsub;
  }
  catch 
  {
    queryCache.invalidateQueries({ key: ['analysis', 'statistic', project.value?.id] });
  }
}

// ─── Lifecycle ────────────────────────────────────────────────────────────────
onUnmounted(() => 
{
  if (unsubCodeGraph) unsubCodeGraph();
  if (unsubStatistic) unsubStatistic();
});
</script>

<template>
  <!--
    `min-h-0` is critical: it prevents the flex child from overflowing its parent
    by allowing it to shrink below its intrinsic min-content height.
  -->
  <div class="
    flex h-full min-h-0 w-full flex-col p-4
    lg:p-6
  "
  >

    <!-- Loading -->
    <div v-if="loading"
      class="flex flex-1 flex-col items-center justify-center gap-4"
    >
      <NIcon name="i-lucide-loader-2"
        class="h-8 w-8 animate-spin text-[var(--ui-text-muted)]"
      />
      <p class="text-[var(--ui-text-muted)]">Loading analysis details...</p>
    </div>

    <div v-else-if="project"
      class="flex min-h-0 flex-1 flex-col gap-4"
    >

      <!-- ── Header Info & Version Selector ───────────────────────────────── -->
      <div class="
        flex shrink-0 flex-col items-stretch gap-4
        lg:flex-row
      "
      >
        <NCard
          id="repo-header"
          class="
            min-w-0 flex-1 border-0 bg-[var(--ui-bg)]/70 ring-1
            ring-[var(--ui-border)] backdrop-blur-md
          "
          :ui="{ body: 'p-4 md:p-5 h-full flex flex-col justify-center' }"
        >
          <div class="flex w-full min-w-0 items-center gap-4">
            <!-- Left: GitHub Icon Container (No Shadow) -->
            <div class="
              flex shrink-0 items-center justify-center rounded-xl
              bg-[var(--ui-bg-highlighted)]/10 p-2.5
              text-[var(--ui-text-highlighted)] ring-1 ring-[var(--ui-border)]
            "
            >
              <NIcon name="i-lucide-github"
                class="h-6 w-6"
              />
            </div>
            
            <!-- Middle: Repository Name & Meta Info -->
            <div class="flex min-w-0 flex-col justify-center gap-2">
              <h1 class="
                flex flex-wrap items-center gap-2 text-lg leading-none
                font-semibold text-[var(--ui-text-highlighted)]
              "
              >
                <template v-if="githubRepoInfo">
                  <a
                    :href="githubRepoInfo.ownerUrl"
                    target="_blank"
                    rel="noreferrer noopener"
                    class="
                      truncate text-base font-semibold transition-colors
                      hover:text-[var(--ui-primary)] hover:underline
                    "
                  >
                    {{ githubRepoInfo.owner }}
                  </a>
                  <span class="text-base font-semibold text-[var(--ui-border)]">/</span>
                  <a
                    :href="githubRepoInfo.repositoryUrl"
                    target="_blank"
                    rel="noreferrer noopener"
                    class="
                      truncate text-base font-semibold transition-colors
                      hover:text-[var(--ui-primary)] hover:underline
                    "
                  >
                    {{ githubRepoInfo.repository }}
                  </a>
                </template>
                <template v-else>
                  <span class="truncate text-base font-semibold">
                    {{ project.repositoryName }}
                  </span>
                </template>
              </h1>
              
              <!-- Badges for branch & commit hash (Restored previous design with smaller text-xs size) -->
              <div class="flex flex-wrap items-center gap-2.5">
                <span v-if="project.branchName"
                  class="
                    inline-flex items-center gap-1 rounded-full bg-blue-50 px-2
                    py-0.5 text-xs font-medium text-blue-700
                    dark:bg-blue-900/30 dark:text-blue-300
                  "
                >
                  <NIcon name="i-lucide-git-branch"
                    class="h-3 w-3"
                  />
                  {{ project.branchName }}
                </span>
                <NTooltip text="Hash commit target analisa."
                  :popper="{ placement: 'top' }"
                >
                  <span v-if="project.lastCommitHash"
                    class="
                      inline-flex cursor-help items-center gap-1 rounded-full
                      bg-[var(--ui-bg-elevated)] px-2 py-0.5 text-xs font-medium
                      text-[var(--ui-text-muted)]
                    "
                  >
                    <NIcon name="i-lucide-git-commit-horizontal"
                      class="h-3 w-3"
                    />
                    {{ project.lastCommitHash.slice(0, 7) }}
                  </span>
                </NTooltip>
                <NTooltip text="Waktu analisa tersimpan di database."
                  :popper="{ placement: 'top' }"
                >
                  <span v-if="formattedCreatedAt"
                    class="
                      inline-flex cursor-help items-center gap-1 rounded-full
                      bg-[var(--ui-bg-elevated)] px-2 py-0.5 text-xs font-medium
                      text-[var(--ui-text-muted)]
                    "
                  >
                    <NIcon name="i-lucide-calendar"
                      class="h-3 w-3"
                    />
                    {{ formattedCreatedAt }}
                  </span>
                </NTooltip>
              </div>
            </div>
          </div>
        </NCard>
      </div>

      <!-- ── Tab Navigation ───────────────────────────────────────────────── -->
      <div class="
        flex shrink-0 items-center gap-1 border-b border-[var(--ui-border)]
      "
      >
        <NTooltip text="Ringkasan metrik kode dan analisis statis."
          :popper="{ placement: 'top' }"
        >
          <button id="tab-overview"
            @click="activeTab = 'overview'"
            class="
              flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium
              transition-colors duration-150
            "
            :class="activeTab === 'overview'
              ? `border-[var(--ui-primary)] text-[var(--ui-primary)]`
              : `
                border-transparent text-[var(--ui-text-muted)]
                hover:text-[var(--ui-text)]
              `"
          >
            <NIcon name="i-lucide-info"
              class="h-4 w-4"
            />
            Overview
          </button>
        </NTooltip>
        <NTooltip text="Visualisasi struktur dan dependensi codebase."
          :popper="{ placement: 'top' }"
        >
          <button id="tab-codegraph"
            @click="activeTab = 'codegraph'"
            class="
              flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium
              transition-colors duration-150
            "
            :class="activeTab === 'codegraph'
              ? `border-[var(--ui-primary)] text-[var(--ui-primary)]`
              : `
                border-transparent text-[var(--ui-text-muted)]
                hover:text-[var(--ui-text)]
              `"
          >
            <NIcon name="i-lucide-network"
              class="h-4 w-4"
            />
            Code Graph
          </button>
        </NTooltip>
      </div>

      <!-- ── Tab Content (fills remaining height, no scroll on page level) ── -->
      <div class="flex min-h-0 flex-1 flex-col">
        <Transition :name="transitionName"
          mode="out-in"
        >
          <KeepAlive>
            <OverviewTab
              v-if="activeTab === 'overview'"
              key="overview"
              :progress="statisticProgress"
              class="min-h-0 flex-1 overflow-y-auto"
            />
            <CodeGraphTab
              v-else-if="activeTab === 'codegraph'"
              key="codegraph"
              :progress="codeGraphProgress"
              class="
                relative min-h-0 flex-1 overflow-hidden rounded-xl border-1
                border-[var(--ui-border)]
              "
              style="min-height: 400px"
            />
          </KeepAlive>
        </Transition>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Move forward (overview -> codegraph) */
.slide-left-enter-active,
.slide-left-leave-active {
  transition: opacity 0.10s ease-out, transform 0.10s ease-out;
}
.slide-left-enter-from {
  opacity: 0;
  transform: translateX(15px);
}
.slide-left-leave-to {
  opacity: 0;
  transform: translateX(-15px);
}

/* Move backward (codegraph -> overview) */
.slide-right-enter-active,
.slide-right-leave-active {
  transition: opacity 0.10s ease-out, transform 0.10s ease-out;
}
.slide-right-enter-from {
  opacity: 0;
  transform: translateX(-15px);
}
.slide-right-leave-to {
  opacity: 0;
  transform: translateX(15px);
}
</style>
