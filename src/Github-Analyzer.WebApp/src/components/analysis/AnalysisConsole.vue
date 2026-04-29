<script setup lang="ts">
import { ref } from 'vue'
interface VisualizationMode {
  label: string
  value: string
}

interface MetricItem {
  label: string
  value: number
}

defineProps<{
  repositoryName: string
  repositoryUrl: string
  isLoading: boolean
  errorMessage: string
  repositoryForm: { githubUrl: string }
  visualizationModes: VisualizationMode[]
  activeMode: string
  metrics: MetricItem[]
}>()

const tabs = [
  { id: 'overview', label: 'Overview' },
  { id: 'graph', label: 'Graph' },
  { id: 'files', label: 'Files' },
  { id: 'deps', label: 'Dependencies' },
]
const activeTab = ref('overview')

const emit = defineEmits<{
  (event: 'submit'): void
  (event: 'refresh'): void
  (event: 'mode-change', mode: string): void
}>()
</script>

<template>
  <div class="space-y-5">
    <UCard class="border-(--ui-border-muted)">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div class="space-y-1">
          <p class="text-xs font-medium uppercase tracking-[0.2em] text-(--ui-text-dimmed)">
            Repository
          </p>
          <h1 class="text-2xl font-semibold text-(--ui-text-highlighted)">
            {{ repositoryName }}
          </h1>
          <a
            v-if="repositoryUrl"
            :href="repositoryUrl"
            target="_blank"
            rel="noreferrer"
            class="text-sm text-(--ui-text-muted) underline"
          >
            {{ repositoryUrl }}
          </a>
          <p v-else class="text-sm text-(--ui-text-muted)">
            Paste a GitHub URL to start analysis.
          </p>
        </div>

        <UButton
          color="neutral"
          variant="soft"
          icon="i-lucide-refresh-cw"
          :loading="isLoading"
          @click="emit('refresh')"
        >
          Refresh analysis
        </UButton>
      </div>
    </UCard>

    <UAlert
      v-if="errorMessage"
      color="error"
      variant="subtle"
      title="Analysis error"
      :description="errorMessage"
    />

    <div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_240px]">
      <UCard class="border-(--ui-border-muted)">
        <div class="flex min-h-[320px] flex-col gap-6">
          <div class="flex flex-1 flex-col items-center justify-center gap-3 text-center">
            <UBadge color="secondary" variant="soft">New repo analysis</UBadge>
            <p class="text-lg font-semibold text-(--ui-text-highlighted)">
              Start a new analysis like a ChatGPT session
            </p>
            <p class="max-w-md text-sm text-(--ui-text-muted)">
              Masukkan URL repository GitHub dan kirim untuk memulai proses analisis.
            </p>
          </div>

          <UForm :state="repositoryForm" class="space-y-3" @submit.prevent="emit('submit')">
            <div class="flex flex-col gap-3 sm:flex-row">
              <UInput
                v-model="repositoryForm.githubUrl"
                icon="i-lucide-github"
                size="xl"
                class="w-full"
                placeholder="https://github.com/owner/repository"
              />
              <UButton type="submit" size="xl" icon="i-lucide-send" :loading="isLoading">
                Analyze
              </UButton>
            </div>
            <UButton color="neutral" variant="soft" size="lg" icon="i-lucide-history">
              Load previous result
            </UButton>
          </UForm>
        </div>
      </UCard>

      <div class="space-y-4">
        <UCard class="border-(--ui-border-muted)">
          <div class="space-y-3">
            <p class="text-xs font-medium uppercase tracking-[0.18em] text-(--ui-text-dimmed)">
              View mode
            </p>
            <UButton
              v-for="mode in visualizationModes"
              :key="mode.value"
              block
              color="neutral"
              :variant="activeMode === mode.value ? 'soft' : 'ghost'"
              class="justify-start"
              @click="emit('mode-change', mode.value)"
            >
              {{ mode.label }}
            </UButton>
          </div>
        </UCard>

        <UCard class="border-(--ui-border-muted)">
          <div class="space-y-3">
            <p class="text-xs font-medium uppercase tracking-[0.18em] text-(--ui-text-dimmed)">
              Metrics
            </p>
            <div class="grid gap-3">
              <div v-for="metric in metrics" :key="metric.label" class="space-y-1">
                <p class="text-xs font-medium uppercase tracking-[0.18em] text-(--ui-text-dimmed)">
                  {{ metric.label }}
                </p>
                <p class="text-2xl font-semibold text-(--ui-text-highlighted)">
                  {{ metric.value }}
                </p>
              </div>
            </div>
          </div>
        </UCard>
      </div>
    </div>

    <div class="flex flex-wrap items-center gap-2 rounded-[--ui-radius] border border-(--ui-border-muted) bg-(--ui-bg-elevated) p-2">
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
  </div>
</template>
