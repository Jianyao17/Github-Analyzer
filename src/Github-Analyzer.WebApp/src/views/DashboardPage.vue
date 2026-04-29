<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
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

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const snapshot = ref<AnalysisSnapshot | null>(null)
const isLoading = ref(false)
const errorMessage = ref('')

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

onMounted(async () => {
  authStore.hydrate()

  const token = route.query.token
  if (typeof token === 'string' && token.length > 0) {
    authStore.setTokenOnly(token)
    await router.replace('/')
  }

  if (!authStore.token) {
    await router.push('/login')
    return
  }

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

async function logout() {
  authStore.clear()
  await router.push('/login')
}
</script>

<template>
  <main class="shell">
    <section class="hero-card">
      <div class="hero-top">
        <div>
          <div class="eyebrow">Repository Intelligence</div>
          <h1>AST graph base workspace is online.</h1>
          <p>
            Ini masih base project, tapi jalur utamanya sudah siap: auth, API, Aspire orchestration,
            PostgreSQL, dan visual stub untuk graph analysis.
          </p>
        </div>
        <UButton color="neutral" variant="soft" size="lg" @click="logout">Logout</UButton>
      </div>
    </section>

    <section class="metrics-grid">
      <UCard v-for="metric in metrics" :key="metric.label" class="metric-card">
        <template #header>
          <span class="eyebrow">{{ metric.label }}</span>
        </template>
        <div class="metric-value">{{ metric.value }}</div>
      </UCard>
    </section>

    <section class="panel">
      <div class="panel-header">
        <div>
          <div class="eyebrow">Sample Analysis</div>
          <h2>{{ snapshot?.repository ?? 'Loading repository snapshot...' }}</h2>
        </div>
        <UButton :loading="isLoading" size="lg" color="primary" @click="loadSnapshot">
          Refresh
        </UButton>
      </div>

      <p v-if="errorMessage" class="error-banner">{{ errorMessage }}</p>

      <div v-if="snapshot" class="graph-grid">
        <UCard>
          <template #header>
            <h3>Nodes</h3>
          </template>
          <ul class="entity-list">
            <li v-for="node in snapshot.nodes" :key="node.id">
              <span>{{ node.label }}</span>
              <UBadge color="neutral" variant="subtle">{{ node.kind }}</UBadge>
            </li>
          </ul>
        </UCard>

        <UCard>
          <template #header>
            <h3>Edges</h3>
          </template>
          <ul class="entity-list">
            <li v-for="edge in snapshot.edges" :key="`${edge.source}-${edge.target}`">
              <span>{{ edge.source }} -> {{ edge.target }}</span>
              <UBadge color="secondary" variant="subtle">{{ edge.relationship }}</UBadge>
            </li>
          </ul>
        </UCard>
      </div>
    </section>
  </main>
</template>
