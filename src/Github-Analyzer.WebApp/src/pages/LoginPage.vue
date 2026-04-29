<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '../lib/api'
import { useToastSystem } from '../composables/useToastSystem'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const toast = useToastSystem()
const form = ref()
const isSubmitting = ref(false)

const loginForm = reactive({
  email: 'admin@github-analyzer.local',
  password: 'Password123!',
})

onMounted(async () => {
  await authStore.initialize()
})

async function submit() {
  isSubmitting.value = true
  form.value?.clear()

  try {
    await authStore.login(loginForm)

    toast.add({
      title: 'Login berhasil',
      description: 'Selamat datang kembali.',
      tone: 'success',
    })
    await router.push('/')
  } catch (error) {
    const errors = []
    let fallbackError = 'Authentication failed.'
    
    if (error instanceof ApiError) {
      if (error.status === 401) {
        errors.push({ name: 'email', message: 'Email atau password salah.' })
        errors.push({ name: 'password', message: 'Email atau password salah.' })
        fallbackError = 'Email atau password salah.'
      } else {
        errors.push({ name: 'password', message: error.message })
        fallbackError = error.message
      }
    } else {
      errors.push({ name: 'password', message: 'Authentication failed.' })
    }

    form.value?.setErrors(errors)

    toast.add({
      title: 'Login gagal',
      description: fallbackError,
      tone: 'error',
    })
  } finally {
    isSubmitting.value = false
  }
}

function continueWithGoogle() {
  authStore.startGoogleLogin()
}
</script>

<template>
  <UApp>
    <div class="min-h-screen bg-(--ui-bg) text-(--ui-text)">
      <UContainer class="flex min-h-screen items-center justify-center py-10">
        <UCard class="w-full max-w-md border-(--ui-border-muted)">
          <template #header>
            <div class="space-y-2 text-center">
              <UBadge color="neutral" variant="soft">Github-Analyzer</UBadge>
              <h1 class="text-2xl font-semibold text-(--ui-text-highlighted)">Masuk ke akun</h1>
              <p class="text-sm text-(--ui-text-muted)">
                Selamat datang kembali. Gunakan kredensial Anda untuk lanjut.
              </p>
            </div>
          </template>

          <div class="space-y-5">
            <div v-if="authStore.authOptions.googleEnabled" class="space-y-4">
              <UButton
                block
                color="neutral"
                variant="soft"
                icon="i-lucide-shield"
                size="xl"
                @click="continueWithGoogle"
              >
                Continue with Google
              </UButton>
              <div class="flex items-center gap-3 text-xs text-(--ui-text-dimmed)">
                <USeparator class="flex-1" />
                <span>atau</span>
                <USeparator class="flex-1" />
              </div>
            </div>

            <UForm ref="form" :state="loginForm" class="space-y-4" @submit.prevent="submit">
              <UFormField label="Email" name="email">
                <UInput v-model="loginForm.email" type="email" size="xl" class="w-full" />
              </UFormField>

              <UFormField label="Password" name="password">
                <UInput v-model="loginForm.password" type="password" size="xl" class="w-full" />
              </UFormField>

              <UButton type="submit" block :loading="isSubmitting" size="xl">
                Sign In
              </UButton>
            </UForm>

            <p class="text-center text-sm text-(--ui-text-muted)">
              Belum punya akun?
              <RouterLink class="font-semibold text-(--ui-text-highlighted)" to="/register">
                Buat akun
              </RouterLink>
            </p>
          </div>
        </UCard>
      </UContainer>
    </div>
  </UApp>
</template>
