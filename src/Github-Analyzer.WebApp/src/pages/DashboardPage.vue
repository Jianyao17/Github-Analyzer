<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AnalysisConsole from '../components/analysis/AnalysisConsole.vue'
import AppShell from '../components/layout/AppShell.vue'
import WorkspaceSidebar from '../components/layout/WorkspaceSidebar.vue'
import { apiRequest } from '../lib/api'
import { useAuthStore } from '../stores/auth'

interface AnalysisNode {
  id: string
  label: string
  kind: string
}

interface AnalysisEdge {
  source: string
  target: string
  relationship: string
}

interface AnalysisSnapshot {
  repository: string
  fileCount: number
  nodeCount: number
  edgeCount: number
  nodes: AnalysisNode[]
  edges: AnalysisEdge[]
}

const router = useRouter()
const authStore = useAuthStore()
const isLoading = ref(false)
const errorMessage = ref('')
const snapshot = ref<AnalysisSnapshot | null>(null)
const selectedWorkspace = ref('new-analysis')
const repositoryForm = reactive({
  githubUrl: 'https://github.com/octocat/Hello-World',
})

const workspaces = ref([
  { id: 'new-analysis', label: 'New analysis', hint: 'Workspace baru' },
  { id: 'octocat', label: 'octocat/Hello-World', hint: 'Sample AST graph' },
  { id: 'aspire', label: 'dotnet/aspire', hint: 'Orchestration reference' },
  { id: 'vue', label: 'vuejs/core', hint: 'Compiler and runtime' },
])

const repositoryName = computed(() => {
  const rawUrl = repositoryForm.githubUrl.trim()
  if (!rawUrl) {
    return 'New repository'
  }

  try {
    const parsed = new URL(rawUrl)
    const parts = parsed.pathname.split('/').filter(Boolean)
    return parts.slice(0, 2).join('/') || parsed.host
  } catch {
    return rawUrl.replace(/^https?:\/\//, '')
  }
})

const repositoryUrl = computed(() => repositoryForm.githubUrl.trim())

const metrics = computed(() => {
  if (!snapshot.value) {
    return []
  }

  return [
    { label: 'Files', value: snapshot.value.fileCount },
    { label: 'Nodes', value: snapshot.value.nodeCount },
    { label: 'Edges', value: snapshot.value.edgeCount },
  ]
})

const visualizationModes = [
  { label: 'Graph View', value: 'graph' },
  { label: 'Files', value: 'files' },
  { label: 'Dependencies', value: 'deps' },
]
const activeVisualizationMode = ref('graph')

onMounted(async () => {
  await authStore.initialize()
  await loadSnapshot()
})

async function loadSnapshot() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    snapshot.value = await apiRequest<AnalysisSnapshot>('/api/analysis/sample', {
      headers: {
        Authorization: `Bearer ${authStore.token}`,
      },
    })
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to load analysis data.'
  } finally {
    isLoading.value = false
  }
}

async function analyzeRepository() {
  selectedWorkspace.value = 'new-analysis'
  await loadSnapshot()
}

async function logout() {
  authStore.clear()
  await router.push('/login')
}
</script>

<template>
  <AppShell>
    <template #sidebar>
      <WorkspaceSidebar
        :workspaces="workspaces"
        :selectedId="selectedWorkspace"
        :userName="authStore.user?.displayName ?? 'Authenticated user'"
        :userEmail="authStore.user?.email ?? 'No email loaded'"
        :userInitials="authStore.initials"
        @select="selectedWorkspace = $event"
        @new="selectedWorkspace = 'new-analysis'"
        @logout="logout"
      />
    </template>

    <AnalysisConsole
      :repositoryName="repositoryName"
      :repositoryUrl="repositoryUrl"
      :isLoading="isLoading"
      :errorMessage="errorMessage"
      :repositoryForm="repositoryForm"
      :metrics="metrics"
      :visualizationModes="visualizationModes"
      :activeMode="activeVisualizationMode"
      @refresh="loadSnapshot"
      @submit="analyzeRepository"
      @mode-change="activeVisualizationMode = $event"
    />
  </AppShell>
</template>
