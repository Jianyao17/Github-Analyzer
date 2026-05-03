<script setup lang="ts">
import { ref, computed } from 'vue'
import CodeGraphView from './CodeGraphView.vue'

interface VisualizationMode {
  label: string
  value: string
}

interface MetricItem {
  label: string
  value: number
}

interface Node {
  id: string
  label: string
  type: number
}

interface Edge {
  from: string
  to: string
  type: number
}

const props = defineProps<{
  repositoryName: string
  repositoryUrl: string
  isLoading: boolean
  errorMessage: string
  visualizationModes: VisualizationMode[]
  activeMode: string
  metrics: MetricItem[]
  graphData: { nodes: Node[], sourceRelEdges: Edge[], useRelEdges: Edge[] } | null
  progress: { percentage: number, status: string } | null
}>()

const tabs = [
  { id: 'overview', label: 'Overview' },
  { id: 'graph', label: 'Graph' },
]
const activeTab = ref('graph')

const emit = defineEmits<{
  (event: 'refresh'): void
  (event: 'mode-change', mode: string): void
}>()

const nodeDistribution = computed(() => {
  if (!props.graphData) return []
  const types = [
    { label: 'Namespaces / Folders', type: 0, color: '#6366f1' },
    { label: 'Files', type: 1, color: '#10b981' },
    { label: 'Classes / Interfaces', type: 2, color: '#3b82f6' },
    { label: 'Functions / Methods', type: 3, color: '#a855f7' }
  ]
  
  return types.map(t => ({
    ...t,
    count: props.graphData!.nodes.filter(n => Number(n.type) === t.type).length
  })).filter(t => t.count > 0)
})

const complexityDensity = computed(() => {
  if (!props.graphData) return '0.0'
  const containers = props.graphData.nodes.filter(n => Number(n.type) === 1 || Number(n.type) === 2).length
  const functions = props.graphData.nodes.filter(n => Number(n.type) === 3).length
  return containers > 0 ? (functions / containers).toFixed(2) : '0.0'
})

const connectivityRatio = computed(() => {
  if (!props.graphData) return '0.0'
  const nodes = props.graphData.nodes.length
  const useEdges = props.graphData.useRelEdges.length
  return nodes > 0 ? (useEdges / nodes).toFixed(2) : '0.0'
})
</script>

<template>
  <div class="space-y-5">
    <UCard class="border-muted">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div class="space-y-1">
          <p class="text-xs font-medium uppercase tracking-[0.2em] text-dimmed">
            Repository
          </p>
          <h1 class="text-2xl font-semibold text-highlighted">
            {{ repositoryName }}
          </h1>
          <a
            v-if="repositoryUrl"
            :href="repositoryUrl"
            target="_blank"
            rel="noreferrer"
            class="text-sm text-muted underline"
          >
            {{ repositoryUrl }}
          </a>
          <p v-else class="text-sm text-muted">
            Paste a GitHub URL to start analysis.
          </p>
        </div>

        <div class="flex gap-2">
          <UButton
            color="neutral"
            variant="soft"
            icon="i-lucide-refresh-cw"
            :loading="isLoading"
            @click="emit('refresh')"
          >
            Refresh
          </UButton>
        </div>
      </div>
    </UCard>

    <UAlert
      v-if="errorMessage"
      color="error"
      variant="subtle"
      title="Analysis error"
      :description="errorMessage"
    />

    <UCard v-if="progress && progress.percentage < 100" class="border-muted">
      <div class="space-y-3">
        <div class="flex justify-between text-sm">
          <span class="font-medium">{{ progress.status }}</span>
          <span class="text-muted">{{ progress.percentage }}%</span>
        </div>
        <UProgress :value="progress.percentage" color="primary" />
      </div>
    </UCard>

    <div class="space-y-4">
      <div class="flex flex-wrap items-center gap-2 rounded-[--ui-radius] border border-muted bg-elevated p-2">
        <UButton
          v-for="tab in tabs"
          :key="tab.id"
          color="neutral"
          size="sm"
          :variant="activeTab === tab.id ? 'solid' : 'ghost'"
          @click="activeTab = tab.id"
        >
          {{ tab.label }}
        </UButton>
      </div>

      <UCard v-if="activeTab === 'overview'" class="border-muted">
        <div class="space-y-6">
          <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            <div v-for="metric in metrics" :key="metric.label" class="p-4 rounded-lg bg-muted/30 border border-muted/50">
              <p class="text-xs font-medium uppercase tracking-[0.18em] text-dimmed mb-1">
                {{ metric.label }}
              </p>
              <p class="text-3xl font-bold text-highlighted">
                {{ metric.value }}
              </p>
            </div>
          </div>

          <div v-if="graphData" class="grid gap-6 md:grid-cols-2">
            <div class="space-y-4">
              <h3 class="text-sm font-semibold uppercase tracking-wider text-dimmed border-b border-muted pb-2">
                Node Distribution
              </h3>
              <div class="space-y-3">
                <div v-for="type in nodeDistribution" :key="type.label" class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <div class="h-2 w-2 rounded-full" :style="{ backgroundColor: type.color }"></div>
                    <span class="text-sm text-muted">{{ type.label }}</span>
                  </div>
                  <span class="text-sm font-mono font-medium">{{ type.count }}</span>
                </div>
              </div>
            </div>

            <div class="space-y-4">
              <h3 class="text-sm font-semibold uppercase tracking-wider text-dimmed border-b border-muted pb-2">
                Project Insights
              </h3>
              <div class="grid gap-3">
                <div class="flex flex-col p-3 rounded bg-muted/20 border border-muted/30">
                  <span class="text-[10px] uppercase tracking-widest text-dimmed">Complexity Density</span>
                  <span class="text-lg font-semibold">{{ complexityDensity }}</span>
                  <span class="text-[10px] text-muted">Avg. functions per class/file</span>
                </div>
                <div class="flex flex-col p-3 rounded bg-muted/20 border border-muted/30">
                  <span class="text-[10px] uppercase tracking-widest text-dimmed">Connectivity</span>
                  <span class="text-lg font-semibold">{{ connectivityRatio }}</span>
                  <span class="text-[10px] text-muted">Use relations per node</span>
                </div>
              </div>
            </div>
          </div>

          <div v-else class="flex h-40 items-center justify-center text-muted italic">
            Analyze the repository to see project insights.
          </div>
        </div>
      </UCard>

      <UCard v-if="activeTab === 'graph'" class="relative h-160 border-muted p-0 overflow-hidden">
        <CodeGraphView 
          v-if="graphData" 
          :nodes="graphData.nodes" 
          :source-edges="graphData.sourceRelEdges" 
          :use-edges="graphData.useRelEdges" 
        />
        <div v-else class="flex h-full items-center justify-center text-muted">
          No graph data available. Run analysis first.
        </div>
      </UCard>
    </div>
  </div>
</template>
