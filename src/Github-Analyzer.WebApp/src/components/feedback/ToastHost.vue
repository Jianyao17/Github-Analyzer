<script setup lang="ts">
import { computed } from 'vue'
import { useToastStore } from '../../stores/toast'

const toastStore = useToastStore()

const toneBadge = computed(() =>
  (tone: 'success' | 'error' | 'info') => {
    if (tone === 'success') return { color: 'primary', label: 'Success' }
    if (tone === 'error') return { color: 'error', label: 'Error' }
    return { color: 'neutral', label: 'Info' }
  })
</script>

<template>
  <div class="pointer-events-none fixed inset-x-0 top-4 z-50 mx-auto flex w-full max-w-xs flex-col gap-2 px-3">
    <UCard
      v-for="toast in toastStore.items"
      :key="toast.id"
      class="pointer-events-auto border-(--ui-border-muted)"
    >
      <div class="flex items-start gap-2">
        <UBadge :color="toneBadge(toast.tone).color" variant="soft">
          {{ toneBadge(toast.tone).label }}
        </UBadge>
        <div class="min-w-0 flex-1">
          <p class="text-xs font-semibold text-(--ui-text-highlighted)">{{ toast.title }}</p>
          <p v-if="toast.description" class="mt-0.5 text-xs text-(--ui-text-muted)">
            {{ toast.description }}
          </p>
        </div>
        <UButton
          color="neutral"
          variant="ghost"
          icon="i-lucide-x"
          size="2xs"
          @click="toastStore.remove(toast.id)"
        />
      </div>
    </UCard>
  </div>
</template>
