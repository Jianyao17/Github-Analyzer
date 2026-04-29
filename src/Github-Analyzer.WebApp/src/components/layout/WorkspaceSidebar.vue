<script setup lang="ts">
import { computed } from 'vue'
import { useThemeStore } from '../../stores/theme'
interface WorkspaceItem {
  id: string
  label: string
  hint?: string
}

defineProps<{
  workspaces: WorkspaceItem[]
  selectedId: string
  userName: string
  userEmail?: string
  userInitials?: string
}>()

const themeStore = useThemeStore()
const themeLabel = computed(() => themeStore.isDark ? 'Dark' : 'Light')
const themeIcon = computed(() => themeStore.isDark ? 'i-lucide-moon' : 'i-lucide-sun')


const emit = defineEmits<{
  (event: 'select', id: string): void
  (event: 'new'): void
  (event: 'logout'): void
}>()
</script>

<template>
  <div class="flex h-full flex-col gap-5 p-5">
    <div class="flex items-center justify-between gap-3">
      <div class="space-y-1">
        <UBadge color="neutral" variant="soft">Github-Analyzer</UBadge>
        <p class="text-sm text-(--ui-text-muted)">Workspace</p>
      </div>
      <UAvatar :alt="userName" :text="userInitials" size="lg" />
    </div>

    <UButton block icon="i-lucide-plus" size="lg" class="justify-start" @click="emit('new')">
      New analysis
    </UButton>

    <USeparator />

    <div class="space-y-2">
      <p class="text-xs font-medium uppercase tracking-[0.18em] text-(--ui-text-dimmed)">
        Recent tabs
      </p>
      <UButton
        v-for="workspace in workspaces"
        :key="workspace.id"
        block
        color="neutral"
        :variant="selectedId === workspace.id ? 'soft' : 'ghost'"
        class="justify-start"
        @click="emit('select', workspace.id)"
      >
        <div class="flex min-w-0 flex-col items-start text-left">
          <span class="truncate">{{ workspace.label }}</span>
          <span v-if="workspace.hint" class="text-xs text-(--ui-text-muted)">{{ workspace.hint }}</span>
        </div>
      </UButton>
    </div>

    <div class="mt-auto space-y-4">
      <USeparator />

      <UPopover>
        <UButton block color="neutral" variant="soft" size="lg" class="justify-between">
          <div class="flex items-center gap-3">
            <UAvatar :alt="userName" :text="userInitials" size="sm" />
            <div class="text-left">
              <p class="text-sm font-semibold text-(--ui-text-highlighted)">{{ userName }}</p>
              <p class="text-xs text-(--ui-text-muted)">{{ userEmail ?? 'No email loaded' }}</p>
            </div>
          </div>
          <UIcon name="i-lucide-chevron-down" class="h-4 w-4" />
        </UButton>

        <template #content>
          <div class="w-56 space-y-1 p-2">
            <UButton block color="neutral" variant="ghost" icon="i-lucide-settings" class="justify-start">
              Settings
            </UButton>
            <UButton block color="neutral" variant="ghost" icon="i-lucide-user" class="justify-start">
              Account
            </UButton>
            <UButton
              block
              color="neutral"
              variant="ghost"
              :icon="themeIcon"
              class="justify-start"
              @click="themeStore.toggle"
            >
              Theme: {{ themeLabel }}
            </UButton>
            <USeparator class="my-1" />
            <UButton
              block
              color="neutral"
              variant="soft"
              icon="i-lucide-log-out"
              class="justify-start"
              @click="emit('logout')"
            >
              Logout
            </UButton>
          </div>
        </template>
      </UPopover>

    </div>
  </div>
</template>
