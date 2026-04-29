<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { apiBaseUrl, apiRequest } from '../lib/api'
import { useAuthStore, type AuthResponse } from '../stores/auth'

const authStore = useAuthStore()
const router = useRouter()
const isLoading = ref(false)
const errorMessage = ref('')
const form = reactive({
  email: 'admin@github-analyzer.local',
  password: 'Password123!',
  displayName: 'Github Analyzer Admin',
})

authStore.hydrate()

async function register() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const response = await apiRequest<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(form),
    })

    authStore.setAuth(response)
    await router.push('/')
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Register failed.'
  } finally {
    isLoading.value = false
  }
}

async function login() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const response = await apiRequest<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({
        email: form.email,
        password: form.password,
      }),
    })

    authStore.setAuth(response)
    await router.push('/')
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Login failed.'
  } finally {
    isLoading.value = false
  }
}

function loginWithGoogle() {
  window.location.href = `${apiBaseUrl}/api/auth/google/login`
}
</script>

<template>
  <main class="shell">
    <section class="hero-card">
      <div class="eyebrow">Github-Analyzer</div>
      <h1>Base platform for repository AST analysis.</h1>
      <p>
        Backend memakai ASP.NET Core 10 + Aspire + PostgreSQL. Frontend memakai Vue 3 + Vite +
        Nuxt UI.
      </p>
    </section>

    <section class="panel">
      <div class="panel-header">
        <div>
          <div class="eyebrow">Authentication</div>
          <h2>JWT + Identity + Google Login</h2>
        </div>
        <UBadge color="neutral" variant="soft">Scaffold</UBadge>
      </div>

      <div class="form-grid">
        <UInput v-model="form.displayName" placeholder="Display name" size="xl" />
        <UInput v-model="form.email" placeholder="Email" size="xl" />
        <UInput v-model="form.password" type="password" placeholder="Password" size="xl" />
      </div>

      <p v-if="errorMessage" class="error-banner">{{ errorMessage }}</p>

      <div class="actions">
        <UButton :loading="isLoading" size="xl" color="primary" @click="login">
          Login
        </UButton>
        <UButton :loading="isLoading" size="xl" color="neutral" variant="soft" @click="register">
          Register
        </UButton>
        <UButton size="xl" color="secondary" variant="outline" @click="loginWithGoogle">
          Continue with Google
        </UButton>
      </div>
    </section>
  </main>
</template>
