<script setup lang="ts">
import { ref } from 'vue'
import CodeGraphView from './CodeGraphView.vue'

interface VisualizationMode {
  label: string
  value: string
}

interface MetricItem {
  label: string
  value: number
}

interface GraphNode {
  pathId: string
  label: string
  type: number | string
}

interface GraphEdge {
  from: string
  to: string
  type: number | string
}

const props = defineProps<{
  repositoryName: string
  repositoryUrl: string
  isLoading: boolean
  errorMessage: string
  visualizationModes: VisualizationMode[]
  activeMode: string
  metrics: MetricItem[]
  graphData: { nodes: GraphNode[], sourceRelEdges: GraphEdge[], useRelEdges: GraphEdge[] } | null
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

    <div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_240px]">
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
          <div class="space-y-4">
            <div>
              <p class="text-xs font-medium uppercase tracking-[0.18em] text-dimmed">
                Summary
              </p>
              <p class="text-sm text-muted">
                Ringkasan hasil analisis tersedia di tab Graph.
              </p>
            </div>
            <div class="grid gap-3">
              <div v-for="metric in metrics" :key="metric.label" class="space-y-1">
                <p class="text-xs font-medium uppercase tracking-[0.18em] text-dimmed">
                  {{ metric.label }}
                </p>
                <p class="text-2xl font-semibold text-highlighted">
                  {{ metric.value }}
                </p>
              </div>
            </div>
          </div>
        </UCard>

        <UCard v-if="activeTab === 'graph'" class="relative h-125 border-muted p-0 overflow-hidden">
          <CodeGraphView v-if="graphData" :nodes="graphData.nodes" :sourceRelEdges="graphData.sourceRelEdges" :useRelEdges="graphData.useRelEdges" />
          <div v-else class="flex h-full items-center justify-center text-muted">
            No graph data available. Run analysis first.
          </div>
        </UCard>
      </div>

      <div class="space-y-4">
        <UCard class="border-muted">
          <div class="space-y-3">
            <p class="text-xs font-medium uppercase tracking-[0.18em] text-dimmed">
              Metrics
            </p>
            <div class="grid gap-3">
              <div v-for="metric in metrics" :key="metric.label" class="space-y-1">
                <p class="text-xs font-medium uppercase tracking-[0.18em] text-dimmed">
                  {{ metric.label }}
                </p>
                <p class="text-2xl font-semibold text-highlighted">
                  {{ metric.value }}
                </p>
              </div>
            </div>
          </div>
        </UCard>
      </div>
    </div>
  </div>
</template>
