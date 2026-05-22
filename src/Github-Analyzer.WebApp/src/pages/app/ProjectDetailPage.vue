<script setup lang="ts">
import { useRoute } from 'vue-router';
import { onMounted, onUnmounted, ref } from 'vue';
import { useProjectApi } from '../../composables/useProjectApi';
import type { ProjectResponse, ProgressEvent } from '../../composables/useProjectApi';
import type { StatisticAnalysis } from '../../types/statistic-analysis';
import type { CodeGraph } from '../../types/code-graph';
import StatisticTab from '../../components/StatisticTab.vue';
import CodeGraphTab from '../../components/CodeGraphTab.vue';

const route = useRoute();
const {
  fetchProject: getProject,
  streamQueueProgress,
  getCodeGraphAnalysis,
  getStatisticAnalysis
} = useProjectApi();

// ─── Page state ───────────────────────────────────────────────────────────────
const project   = ref<ProjectResponse | null>(null);
const loading   = ref(true);
const activeTab = ref<'statistic' | 'codegraph'>('statistic');

// ─── Code Graph state ─────────────────────────────────────────────────────────
const codeGraphProgress = ref<ProgressEvent | null>(null);
const graphData         = ref<CodeGraph | null>(null);

// ─── Statistic state ──────────────────────────────────────────────────────────
const statisticProgress = ref<ProgressEvent | null>(null);
const statisticData     = ref<StatisticAnalysis | null>(null);

// ─── Unsubscribe handles ──────────────────────────────────────────────────────
let unsubCodeGraph: (() => void) | null = null;
let unsubStatistic: (() => void) | null = null;

// ─── Fetch ────────────────────────────────────────────────────────────────────
async function fetchProject() 
{
  loading.value = true;
  try 
  {
    project.value = await getProject(route.params.id as string);
    subscribeToCodeGraph();
    subscribeToStatistic();
    checkExistingCodeGraph();
    checkExistingStatistic();
  }
  catch (error) 
  {
    console.error('Failed to fetch project', error);
  }
  finally 
  {
    loading.value = false;
  }
}

// ─── Code Graph ───────────────────────────────────────────────────────────────
async function checkExistingCodeGraph() 
{
  try 
  {
    const analysis = await getCodeGraphAnalysis(route.params.id as string);
    if (analysis && (analysis as any).graphData) 
    {
      graphData.value = (analysis as any).graphData;
    }
  }
  catch { /* not completed yet */ }
}

function subscribeToCodeGraph() 
{
  if (!project.value) return;
  try 
  {
    const unsub = streamQueueProgress(
      project.value.id, 'CodeGraph',
      async (event) => 
      {
        codeGraphProgress.value = event;
        if (event.status === 'Completed') 
        {
          // Small delay to let the backend flush the DB write before we fetch
          await new Promise(r => setTimeout(r, 500));
          await checkExistingCodeGraph();
        }
      },
      () => 
      {
        if (!codeGraphProgress.value || codeGraphProgress.value.status !== 'Completed')
          checkExistingCodeGraph();
      }
    );
    if (typeof unsub === 'function') unsubCodeGraph = unsub;
  }
  catch 
  {
    checkExistingCodeGraph();
  }
}

// ─── Statistic ────────────────────────────────────────────────────────────────
async function checkExistingStatistic() 
{
  try 
  {
    const data = await getStatisticAnalysis(route.params.id as string);
    if (data) statisticData.value = data;
  }
  catch { /* not completed yet */ }
}

function subscribeToStatistic() 
{
  if (!project.value) return;
  try 
  {
    const unsub = streamQueueProgress(
      project.value.id, 'Statistic',
      async (event) => 
      {
        statisticProgress.value = event;
        if (event.status === 'Completed') 
        {
          await new Promise(r => setTimeout(r, 500));
          await checkExistingStatistic();
        }
      },
      () => 
      {
        if (!statisticProgress.value || statisticProgress.value.status !== 'Completed')
          checkExistingStatistic();
      }
    );
    if (typeof unsub === 'function') unsubStatistic = unsub;
  }
  catch 
  {
    checkExistingStatistic();
  }
}

// ─── Lifecycle ────────────────────────────────────────────────────────────────
onMounted(() => fetchProject());
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
  <div class="flex h-full min-h-0 w-full flex-col p-4 lg:p-6">

    <!-- Loading -->
    <div v-if="loading"
      class="flex flex-col items-center gap-4 py-20"
    >
      <NIcon name="i-lucide-loader-2"
        class="h-8 w-8 animate-spin text-gray-400"
      />
      <p class="text-gray-500">Loading analysis details...</p>
    </div>

    <div v-else-if="project"
      class="flex min-h-0 flex-1 flex-col gap-4"
    >

      <!-- ── Repo Info Card ───────────────────────────────────────────────── -->
      <NCard class="
        shrink-0 border-0 bg-white shadow-sm ring-1 ring-gray-200
        dark:bg-gray-900 dark:ring-gray-800
      "
        :ui="{ body: 'p-5' }"
      >
        <div class="flex items-start gap-3">
          <div class="
            mt-1 rounded-lg bg-gray-100 p-2
            dark:bg-gray-800
          "
          >
            <NIcon name="i-lucide-github"
              class="
                h-5 w-5 text-gray-700
                dark:text-gray-300
              "
            />
          </div>
          <div>
            <h1 class="
              font-mono text-base font-bold text-gray-900
              dark:text-white
            "
            >
              {{ project.repositoryUrl }}
            </h1>
            <div class="mt-1 flex flex-wrap items-center gap-2">
              <span v-if="project.branchName"
                class="
                  inline-flex items-center gap-1 rounded-full bg-blue-50 px-2
                  py-0.5 text-xs text-blue-700 ring-1 ring-blue-200
                  dark:bg-blue-900/30 dark:text-blue-300 dark:ring-blue-800
                "
              >
                <NIcon name="i-lucide-git-branch"
                  class="h-3 w-3"
                />
                {{ project.branchName }}
              </span>
              <span v-if="project.lastCommitHash"
                class="
                  inline-flex items-center gap-1 rounded-full bg-gray-100 px-2
                  py-0.5 font-mono text-xs text-gray-600
                  dark:bg-gray-800 dark:text-gray-400
                "
              >
                <NIcon name="i-lucide-git-commit-horizontal"
                  class="h-3 w-3"
                />
                {{ project.lastCommitHash.slice(0, 7) }}
              </span>
            </div>
          </div>
        </div>
      </NCard>

      <!-- ── Tab Navigation ───────────────────────────────────────────────── -->
      <div class="
        flex shrink-0 items-center gap-1 border-b border-gray-200
        dark:border-gray-800
      "
      >
        <button id="tab-statistic"
          @click="activeTab = 'statistic'"
          class="
            flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium
            transition-colors duration-150
          "
          :class="activeTab === 'statistic'
            ? `
              border-primary-500 text-primary-600
              dark:text-primary-400
            `
            : `
              border-transparent text-gray-500
              hover:text-gray-700
              dark:text-gray-400
              dark:hover:text-gray-200
            `"
        >
          <NIcon name="i-lucide-bar-chart-2"
            class="h-4 w-4"
          />
          Statistik
        </button>
        <button id="tab-codegraph"
          @click="activeTab = 'codegraph'"
          class="
            flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium
            transition-colors duration-150
          "
          :class="activeTab === 'codegraph'
            ? `
              border-primary-500 text-primary-600
              dark:text-primary-400
            `
            : `
              border-transparent text-gray-500
              hover:text-gray-700
              dark:text-gray-400
              dark:hover:text-gray-200
            `"
        >
          <NIcon name="i-lucide-network"
            class="h-4 w-4"
          />
          Code Graph
        </button>
      </div>

      <!-- ── Tab Content (fills remaining height, no scroll on page level) ── -->
      <div class="flex min-h-0 flex-1 flex-col">

        <!-- STATISTIC tab — scrolls internally, page does not scroll -->
        <StatisticTab
          v-if="activeTab === 'statistic'"
          :data="statisticData"
          :progress="statisticProgress"
          class="min-h-0 flex-1 overflow-y-auto"
        />

        <!-- CODE GRAPH tab — fills remaining height, D3 renders inside absolute container -->
        <div
          v-if="activeTab === 'codegraph'"
          class="
            relative min-h-0 flex-1 overflow-hidden rounded-xl border-2
            border-dashed border-gray-200
            dark:border-gray-800
          "
          style="min-height: 400px"
        >
          <CodeGraphTab
            :data="graphData"
            :progress="codeGraphProgress"
            class="absolute inset-0"
          />
        </div>

      </div>
    </div>
  </div>
</template>
