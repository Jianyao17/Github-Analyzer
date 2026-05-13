<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useProjectApi } from '../../composables/useProjectApi'
import type { ProjectResponse, ProgressEvent } from '../../composables/useProjectApi'
import type { CodeGraph } from '../../types/code-graph'
import { useCodeGraphD3 } from '../../composables/useCodeGraphD3'

const route = useRoute()
const { fetchProject: getProject, streamQueueProgress, getCodeGraphAnalysis } = useProjectApi()

const project = ref<ProjectResponse | null>(null)
const codeGraphProgress = ref<ProgressEvent | null>(null)
const loading = ref(true)

const graphData = ref<CodeGraph | null>(null)
const graphContainer = ref<HTMLElement | null>(null)

// Initialize D3 graph
useCodeGraphD3(graphContainer, graphData)

let unsubCodeGraph: (() => void) | null = null

async function fetchProject() 
{
  loading.value = true
  try 
  {
    project.value = await getProject(route.params.id as string)
    subscribeToProgress()
    checkExistingGraph()
  } 
  catch (error) 
  {
    console.error('Failed to fetch project', error)
  } 
  finally 
  {
    loading.value = false
  }
}

async function checkExistingGraph() {
  try {
    const analysis = await getCodeGraphAnalysis(route.params.id as string)
    if (analysis && (analysis as any).graphData) {
      graphData.value = (analysis as any).graphData
    }
  } catch(e) {
    // It's normal if not completed yet
  }
}

async function fetchAnalysisFallback() {
  try {
    const analysis = await getCodeGraphAnalysis(route.params.id as string)
    if (analysis && (analysis as any).graphData) {
      graphData.value = (analysis as any).graphData
      codeGraphProgress.value = {
        jobType: 'CodeGraph',
        status: 'Completed',
        progress: 100,
        message: 'Loaded from previous analysis.'
      }
    }
  } catch(e) {
    console.error('Fallback GET analysis failed', e)
  }
}

function subscribeToProgress() 
{
  if (!project.value) return


  unsubCodeGraph = streamQueueProgress(project.value.id, 'CodeGraph', (event) => {
    codeGraphProgress.value = event
    if (event.status === 'Completed') {
      checkExistingGraph()
    }
  }, () => {
    // Fallback if SSE stream closes or error before completion
    if (!codeGraphProgress.value || codeGraphProgress.value.status !== 'Completed') {
      fetchAnalysisFallback()
    }
  })
}

onMounted(() => 
{
  fetchProject()
})

onUnmounted(() => 
{
  if (unsubCodeGraph) unsubCodeGraph()
})
</script>

<template>
  <div class="w-full h-full min-h-[calc(100vh-2rem)] flex flex-col space-y-4">
    <div v-if="loading" class="flex flex-col items-center py-20 gap-4 flex-1">
      <UIcon name="i-lucide-loader-2" class="w-8 h-8 animate-spin text-gray-400" />
      <p class="text-gray-500">Loading analysis details...</p>
    </div>

    <div v-else-if="project" class="flex flex-col flex-1 space-y-4">

      <!-- Repo Info Card -->
      <UCard class="shadow-sm border-0 ring-1 ring-gray-200 dark:ring-gray-800 bg-white dark:bg-gray-900" :ui="{ body: 'p-6' }">
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 class="text-xl font-bold font-mono text-gray-900 dark:text-white flex items-center gap-2">
              {{ project.repositoryUrl }}
            </h1>
            <p class="text-gray-500 dark:text-gray-400 mt-1">
              Repositori ini digunakan untuk menganalisis struktur kode backend menggunakan sistem node graph.
            </p>
          </div>
        </div>
      </UCard>

      <!-- Analysis Tabs Header -->
      <div class="flex items-center gap-4 border-b border-gray-200 dark:border-gray-800 mt-2 shrink-0">
        <div class="px-4 py-2 border-b-2 border-gray-800 dark:border-white font-bold text-gray-900 dark:text-white">
          Tab Analisa
        </div>
      </div>

      <!-- Graph Visualization Area -->
      <UCard class="relative w-full flex-1 border-2 border-dashed border-gray-200 dark:border-gray-800 overflow-hidden" :ui="{ base: 'flex-1 flex flex-col', body: 'p-0 h-full w-full flex-1' }">
        <div class="w-full h-full relative" style="background-image: radial-gradient(#e5e7eb 1px, transparent 1px); background-size: 20px 20px;">
          <!-- Wait state -->
          <div v-if="!graphData" class="absolute inset-0 flex flex-col items-center justify-center bg-white/80 dark:bg-gray-900/80 backdrop-blur-sm z-10 gap-4">
            <UIcon name="i-lucide-loader-2" class="w-10 h-10 animate-spin text-gray-400" />
            <div class="text-center">
               <p class="font-bold text-gray-700 dark:text-gray-300">Menyiapkan Graph Node Codebase...</p>
               <p class="text-sm text-gray-500 dark:text-gray-400 max-w-md mt-1">{{ codeGraphProgress?.message || 'Menunggu proses analisa selesai.' }}</p>
            </div>
            <UProgress v-if="codeGraphProgress" :value="codeGraphProgress.progress" color="primary" class="max-w-md w-full mt-2" />
          </div>

          <!-- Graph Container -->
          <div v-if="graphData" ref="graphContainer" class="w-full h-full cursor-grab active:cursor-grabbing"></div>
          
          <div v-if="graphData && graphData.nodes?.length === 0" class="absolute inset-0 flex items-center justify-center pointer-events-none">
            <div class="text-center text-gray-400 italic">
              [ Tampilan Graph Node Codebase ]<br/>
              Gunakan library seperti D3.js atau Cytoscape untuk merender graph di sini.
            </div>
          </div>

          <!-- Legend -->
          <div v-if="graphData" class="absolute bottom-6 right-6 bg-white/90 dark:bg-gray-800/90 backdrop-blur-md p-4 rounded-xl shadow-lg border border-gray-200 dark:border-gray-700 text-sm flex flex-col gap-3 pointer-events-none">
            <div class="font-bold border-b border-gray-200 dark:border-gray-700 pb-2 mb-1 flex items-center justify-between gap-4">
              <span>Nodes Legend</span>
              <span class="text-xs font-normal text-gray-500">{{ graphData.nodes?.length || 0 }} Nodes</span>
            </div>
            <div class="flex items-center gap-3"><div class="w-4 h-4 rounded-full bg-[#FBBF24]"></div> Directory</div>
            <div class="flex items-center gap-3"><div class="w-4 h-4 rounded-full bg-[#A78BFA]"></div> Namespace</div>
            <div class="flex items-center gap-3"><div class="w-4 h-4 rounded-full bg-[#60A5FA]"></div> File</div>
            <div class="flex items-center gap-3"><div class="w-4 h-4 rounded-full bg-[#34D399]"></div> Class</div>
            <div class="flex items-center gap-3"><div class="w-4 h-4 rounded-full bg-[#F87171]"></div> Function</div>
          </div>
        </div>
      </UCard>

    </div>
  </div>
</template>
