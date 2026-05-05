<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AnalysisConsole from '../components/analysis/AnalysisConsole.vue'
import NewAnalysisPanel from '../components/analysis/NewAnalysisPanel.vue'
import AppShell from '../components/layout/AppShell.vue'
import WorkspaceSidebar from '../components/layout/WorkspaceSidebar.vue'
import { apiRequest } from '../lib/api'
import { useAuthStore } from '../stores/auth'

interface Node {
  id: string
  label: string
  type: string
}

interface Edge {
  source: string
  target: string
  type: string
}

interface CodeGraph {
  nodes: Node[]
  edges: Edge[]
}

interface HistoryItem {
  jobId: string
  repoUrl: string
  status: string
  progress: number
  createdAt: string
  completedAt?: string
}

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const isLoading = ref(false)
const errorMessage = ref('')
const currentGraph = ref<CodeGraph | null>(null)
const selectedWorkspace = ref('new-analysis')
const repositoryForm = reactive({
  githubUrl: '',
})

const history = ref<HistoryItem[]>([])
const progressUpdate = ref<{ percentage: number, status: string } | null>(null)
let activeEventSource: EventSource | null = null

const isNewAnalysis = computed(() => selectedWorkspace.value === 'new-analysis')

const workspaces = computed(() => {
  const historical = history.value.map(item => ({
    id: item.jobId,
    label: item.repoUrl.replace('https://github.com/', ''),
    hint: item.status
  }))
  const selectedMissing =
    selectedWorkspace.value !== 'new-analysis'
    && !history.value.some(item => item.jobId === selectedWorkspace.value)
      ? [{ id: selectedWorkspace.value, label: 'Analysis Result', hint: 'Unknown' }]
      : []
  return [...selectedMissing, ...historical]
})

const repositoryName = computed(() => {
  if (selectedWorkspace.value !== 'new-analysis') {
    const item = history.value.find(h => h.jobId === selectedWorkspace.value)
    return item ? item.repoUrl.replace('https://github.com/', '') : 'Analysis Result'
  }

  const rawUrl = repositoryForm.githubUrl.trim()
  if (!rawUrl) return 'New repository'

  try {
    const parsed = new URL(rawUrl)
    const parts = parsed.pathname.split('/').filter(Boolean)
    return parts.slice(0, 2).join('/') || parsed.host
  } catch {
    return rawUrl.replace(/^https?:\/\//, '')
  }
})

const repositoryUrl = computed(() => {
  if (selectedWorkspace.value !== 'new-analysis') {
    const item = history.value.find(h => h.jobId === selectedWorkspace.value)
    return item?.repoUrl ?? ''
  }
  return repositoryForm.githubUrl.trim()
})

const visualizationModes = [
  { label: 'Graph View', value: 'graph' },
]
const activeVisualizationMode = ref('graph')

onMounted(async () => {
  await authStore.initialize()
  await loadHistory()
  const initialWorkspace = typeof route.params.jobId === 'string'
    ? route.params.jobId
    : 'new-analysis'
  setWorkspace(initialWorkspace, { syncRoute: false })
})

async function loadHistory() {
  try {
    history.value = await apiRequest<HistoryItem[]>('/api/analysis/history', {
      headers: { Authorization: `Bearer ${authStore.token}` }
    })
  } catch (err) {
    console.error('Failed to load history', err)
  }
}

async function loadResult(jobId: string) {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const result = await apiRequest<CodeGraph>(`/api/analysis/result/${jobId}`, {
      headers: { Authorization: `Bearer ${authStore.token}` }
    })
    currentGraph.value = {
      nodes: Array.isArray(result.nodes) ? result.nodes : [],
      edges: Array.isArray(result.edges) ? result.edges : [],
    }
  } catch (err) {
    errorMessage.value = 'Failed to load analysis result.'
  } finally {
    isLoading.value = false
  }
}

async function startAnalysis() {
  if (!repositoryForm.githubUrl) return
  
  isLoading.value = true
  errorMessage.value = ''
  progressUpdate.value = { percentage: 0, status: 'Queuing...' }
  currentGraph.value = null

  try {
    const response = await apiRequest<{ jobId: string }>('/api/repo/analyze', {
      method: 'POST',
      body: JSON.stringify({ repoUrl: repositoryForm.githubUrl }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${authStore.token}`
      }
    })

    startProgressStream(response.jobId)

  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : 'Failed to start analysis.'
    isLoading.value = false
  }
}

function onWorkspaceSelect(id: string) {
  setWorkspace(id, { syncRoute: true })
}

function setWorkspace(id: string, options: { syncRoute: boolean }) {
  selectedWorkspace.value = id
  errorMessage.value = ''
  if (id === 'new-analysis') {
    stopProgressStream()
    currentGraph.value = null
    progressUpdate.value = null
    isLoading.value = false
    syncRoute(id, options)
    return
  } else {
    currentGraph.value = null
    const item = history.value.find(h => h.jobId === id)
    if (item && item.status.toLowerCase() !== 'completed') {
      progressUpdate.value = { percentage: item.progress, status: item.status }
      if (item.status.toLowerCase() === 'failed') {
        errorMessage.value = 'Analysis failed.'
        stopProgressStream()
        syncRoute(id, options)
      } else {
        startProgressStream(id)
        syncRoute(id, options)
      }
      return
    }

    progressUpdate.value = null
    stopProgressStream()
    loadResult(id)
    syncRoute(id, options)
  }
}

function syncRoute(id: string, options: { syncRoute: boolean }) {
  if (!options.syncRoute) return

  if (id === 'new-analysis') {
    if (route.name !== 'analysis-new') {
      router.replace({ name: 'analysis-new' })
    }
    return
  }

  if (route.params.jobId === id && route.name === 'analysis-job') {
    return
  }

  router.replace({ name: 'analysis-job', params: { jobId: id } })
}

function startProgressStream(jobId: string) {
  stopProgressStream()

  const eventSource = new EventSource(`${import.meta.env.VITE_API_BASE_URL}/api/repo/analyze/stream/${jobId}`)
  activeEventSource = eventSource

  const handleProgressEvent = (event: MessageEvent) => {
    const data = JSON.parse(event.data)
    progressUpdate.value = { percentage: data.progressPercentage, status: data.currentStatus }

    if (data.currentStatus === 'Completed') {
      stopProgressStream()
      loadResult(jobId)
      loadHistory()
    } else if (data.currentStatus === 'Failed') {
      stopProgressStream()
      errorMessage.value = 'Analysis failed.'
      isLoading.value = false
    }
  }

  eventSource.addEventListener('progress', handleProgressEvent)
  eventSource.onmessage = handleProgressEvent

  eventSource.onerror = () => {
    stopProgressStream()
    errorMessage.value = 'Lost connection to progress stream.'
    isLoading.value = false
  }
}

function stopProgressStream() {
  activeEventSource?.close()
  activeEventSource = null
}

async function logout() {
  authStore.clear()
  await router.push('/login')
}

onUnmounted(() => {
  stopProgressStream()
})
</script>

<template>
  <AppShell>
    <template #sidebar>
      <WorkspaceSidebar
        :workspaces="workspaces"
        :selectedId="selectedWorkspace"
        :userName="authStore.user?.displayName ?? 'User'"
        :userEmail="authStore.user?.email ?? ''"
        :userInitials="authStore.initials"
        @select="onWorkspaceSelect"
        @new="onWorkspaceSelect('new-analysis')"
        @logout="logout"
      />
    </template>

    <AnalysisConsole
      v-if="!isNewAnalysis"
      :repositoryName="repositoryName"
      :repositoryUrl="repositoryUrl"
      :isLoading="isLoading"
      :errorMessage="errorMessage"
      :visualizationModes="visualizationModes"
      :activeMode="activeVisualizationMode"
      :graphData="currentGraph"
      :progress="progressUpdate"
      @refresh="loadHistory"
      @mode-change="activeVisualizationMode = $event"
    />
    <NewAnalysisPanel
      v-if="isNewAnalysis"
      :repositoryForm="repositoryForm"
      :isLoading="isLoading"
      @submit="startAnalysis"
    />
  </AppShell>
</template>
