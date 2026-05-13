<script setup lang="ts">
defineProps<{
  title: string
  subtitle?: string
}>()
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-green-50 to-emerald-100 dark:from-gray-900 dark:to-gray-800 p-4 relative overflow-hidden">
    <!-- Decorative background blobs -->
    <div class="absolute top-[-10%] left-[-10%] w-96 h-96 bg-green-400/20 dark:bg-green-500/10 rounded-full blur-3xl mix-blend-multiply dark:mix-blend-overlay"></div>
    <div class="absolute bottom-[-10%] right-[-10%] w-96 h-96 bg-emerald-400/20 dark:bg-emerald-500/10 rounded-full blur-3xl mix-blend-multiply dark:mix-blend-overlay"></div>

    <UCard class="w-full max-w-md relative z-10 backdrop-blur-xl bg-white/80 dark:bg-gray-900/80 shadow-2xl border border-white/20 dark:border-gray-800">
      <template #header>
        <div class="text-center flex flex-col items-center">
          <UIcon name="i-lucide-github" class="w-12 h-12 text-green-600 dark:text-green-400 mb-4" />
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">{{ title }}</h1>
          <p v-if="subtitle" class="text-sm text-gray-500 dark:text-gray-400 mt-1">
            {{ subtitle }}
          </p>
        </div>
      </template>

      <slot />
    </UCard>
  </div>
</template>
