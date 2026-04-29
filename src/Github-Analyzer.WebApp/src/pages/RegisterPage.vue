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

const registerForm = reactive({
  displayName: 'Github Analyzer Admin',
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
    await authStore.register(registerForm)
    toast.add({
      title: 'Registrasi berhasil',
      description: 'Akun berhasil dibuat.',
      tone: 'success',
    })
    await router.push('/')
  } catch (error) {
    const errors = []
    let fallback = 'Registrasi gagal.'

    if (error instanceof ApiError) {
      const payloadErrors = error.payload && 'errors' in error.payload ? error.payload.errors : undefined

      if (error.status === 409) {
        errors.push({ name: 'email', message: error.message })
        fallback = error.message
      } else if (payloadErrors) {
        for (const [key, messages] of Object.entries(payloadErrors)) {
          const joined = typeof messages === 'string' ? messages : (messages as string[]).join(' ')
          if (/password/i.test(key)) {
            errors.push({ name: 'password', message: joined })
          } else if (/email|username/i.test(key)) {
            errors.push({ name: 'email', message: joined })
          } else if (/displayname|name/i.test(key)) {
            errors.push({ name: 'displayName', message: joined })
          }
          fallback = joined
        }
      } else {
        errors.push({ name: 'password', message: error.message })
        fallback = error.message
      }
    } else {
      errors.push({ name: 'password', message: 'Authentication failed.' })
      fallback = 'Authentication failed.'
    }

    form.value?.setErrors(errors)

    toast.add({
      title: 'Registrasi gagal',
      description: fallback,
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
              <h1 class="text-2xl font-semibold text-(--ui-text-highlighted)">Buat akun</h1>
              <p class="text-sm text-(--ui-text-muted)">
                Daftar untuk mulai menganalisis repository GitHub Anda.
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
                Register with Google
              </UButton>
              <div class="flex items-center gap-3 text-xs text-(--ui-text-dimmed)">
                <USeparator class="flex-1" />
                <span>atau</span>
                <USeparator class="flex-1" />
              </div>
            </div>

            <UForm ref="form" :state="registerForm" class="space-y-4" @submit.prevent="submit">
              <UFormField label="Display name" name="displayName">
                <UInput v-model="registerForm.displayName" size="xl" class="w-full" />
              </UFormField>

              <UFormField label="Email" name="email">
                <UInput v-model="registerForm.email" type="email" size="xl" class="w-full" />
              </UFormField>

              <UFormField label="Password" name="password">
                <UInput v-model="registerForm.password" type="password" size="xl" class="w-full" />
              </UFormField>

              <UButton type="submit" block :loading="isSubmitting" size="xl">
                Create Account
              </UButton>
            </UForm>

            <p class="text-center text-sm text-(--ui-text-muted)">
              Sudah punya akun?
              <RouterLink class="font-semibold text-(--ui-text-highlighted)" to="/login">
                Sign in
              </RouterLink>
            </p>
          </div>
        </UCard>
      </UContainer>
    </div>
  </UApp>
</template>
