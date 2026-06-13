<script setup lang="ts">
defineProps<{
    title: string
    subtitle?: string
  }>();
</script>

<template>
  <div
    class="
      relative flex min-h-screen items-center justify-center overflow-hidden
      bg-[var(--ui-bg-muted)] p-4
    "
  >
    <!-- Decorative background blobs -->
    <div
      class="
        absolute top-[-10%] left-[-10%] h-96 w-96 rounded-full
        bg-[var(--ui-primary)]/20 mix-blend-multiply blur-3xl
        dark:bg-[var(--ui-primary)]/10 dark:mix-blend-overlay
      "
    >
    </div>
    <div
      class="
        absolute right-[-10%] bottom-[-10%] h-96 w-96 rounded-full
        bg-[var(--ui-primary)]/20 mix-blend-multiply blur-3xl
        dark:bg-[var(--ui-primary)]/10 dark:mix-blend-overlay
      "
    >
    </div>

    <NCard
      class="
        relative z-10 w-full max-w-100 rounded-xl border
        border-[var(--ui-border)] bg-[var(--ui-bg)]/80 shadow-2xl
        backdrop-blur-xl
      "
    >
      <template #header>
        <div class="flex flex-col items-center text-center">
          <NIcon name="i-lucide-github"
            class="mb-4 h-12 w-12 text-[var(--ui-primary)]"
          />
          <h1 class="text-2xl font-bold text-[var(--ui-text)]">{{ title }}</h1>
          <p v-if="subtitle"
            class="mt-1 text-sm text-[var(--ui-text-muted)]"
          >
            {{ subtitle }}
          </p>
        </div>
      </template>

      <slot />
    </NCard>
  </div>
</template>
