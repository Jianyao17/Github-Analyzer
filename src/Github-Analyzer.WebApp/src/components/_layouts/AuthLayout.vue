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
      bg-linear-to-br from-green-50 to-emerald-100 p-4
      dark:from-gray-900 dark:to-gray-800
    "
  >
    <!-- Decorative background blobs -->
    <div
      class="
        absolute top-[-10%] left-[-10%] h-96 w-96 rounded-full bg-green-400/20
        mix-blend-multiply blur-3xl
        dark:bg-green-500/10 dark:mix-blend-overlay
      "
    >
    </div>
    <div
      class="
        absolute right-[-10%] bottom-[-10%] h-96 w-96 rounded-full
        bg-emerald-400/20 mix-blend-multiply blur-3xl
        dark:bg-emerald-500/10 dark:mix-blend-overlay
      "
    >
    </div>

    <NCard
      class="
        relative z-10 w-full max-w-100 rounded-xl border border-white/20
        bg-white/80 shadow-2xl backdrop-blur-xl
        dark:border-gray-800 dark:bg-gray-900/80
      "
    >
      <template #header>
        <div class="flex flex-col items-center text-center">
          <NIcon name="i-lucide-github"
            class="
              mb-4 h-12 w-12 text-green-600
              dark:text-green-400
            "
          />
          <h1 class="
            text-2xl font-bold text-gray-900
            dark:text-white
          "
          >{{ title }}</h1>
          <p v-if="subtitle"
            class="
              mt-1 text-sm text-gray-500
              dark:text-gray-400
            "
          >
            {{ subtitle }}
          </p>
        </div>
      </template>

      <slot />
    </NCard>
  </div>
</template>
