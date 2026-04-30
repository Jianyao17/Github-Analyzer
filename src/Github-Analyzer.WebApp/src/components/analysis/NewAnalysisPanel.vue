<script setup lang="ts">
const props = defineProps<{
  repositoryForm: { githubUrl: string }
  isLoading: boolean
}>()

const emit = defineEmits<{
  (event: 'submit'): void
}>()
</script>

<template>
  <UCard class="border-muted">
    <div class="flex min-h-80 flex-col gap-6">
      <div class="flex flex-1 flex-col items-center justify-center gap-3 text-center">
        <UBadge color="secondary" variant="soft">New repo analysis</UBadge>
        <p class="text-lg font-semibold text-highlighted">
          Analyze any GitHub Repository
        </p>
        <p class="max-w-md text-sm text-muted">
          Masukkan URL repository GitHub dan kirim untuk memulai proses analisis menggunakan Tree-sitter.
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
      </UForm>
    </div>
  </UCard>
</template>
