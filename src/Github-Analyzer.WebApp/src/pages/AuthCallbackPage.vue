<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const errorMessage = ref('')

onMounted(async () => {
  const token = route.query.token

  if (typeof token !== 'string' || token.length === 0) {
    errorMessage.value = 'Google callback tidak mengandung access token.'
    return
  }

  try {
    await authStore.completeGoogleLogin(token)
    await router.replace('/')
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Google login gagal diproses.'
  }
})
</script>

<template>
  <UApp>
    <div class="flex min-h-screen items-center justify-center bg-(--ui-bg)">
      <UCard class="w-full max-w-lg border-(--ui-border-muted)">
        <template #header>
          <div class="space-y-2">
            <UBadge color="secondary" variant="soft">Authentication</UBadge>
            <h1 class="text-2xl font-semibold text-(--ui-text-highlighted)">
              Menyelesaikan login Google
            </h1>
          </div>
        </template>

        <div class="space-y-4">
          <UAlert
            v-if="errorMessage"
            color="error"
            variant="subtle"
            title="Callback error"
            :description="errorMessage"
          />
          <template v-else>
            <USkeleton class="h-4 w-48" />
            <USkeleton class="h-3 w-full" />
            <USkeleton class="h-3 w-4/5" />
          </template>
        </div>
      </UCard>
    </div>
  </UApp>
</template>
