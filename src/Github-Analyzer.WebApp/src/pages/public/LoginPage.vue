<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import AuthLayout from '../../components/_Layouts/AuthLayout.vue'
import { useAuthStore } from '../../stores/auth.store'
import { baseURL } from '../../api/axios'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const state = reactive({
  email: '',
  password: ''
})

const loading = ref(false)
const error = ref('')

async function onSubmit() 
{
  loading.value = true
  error.value = ''
  
  try 
  {
    await auth.login(state)
    const redirect = route.query.redirect as string || '/app/dashboard'
    router.push(redirect)
  } 
  catch (err: any) 
  {
    error.value = err.response?.data?.message || 'Login failed. Please check your credentials.'
  } 
  finally 
  {
    loading.value = false
  }
}

function loginWithGoogle() 
{
  window.location.href = `${baseURL}/api/auth/google/login`
}
</script>

<template>
  <AuthLayout
    title="Welcome back"
    subtitle="Sign in to your account"
  >
    <UForm :state="state" @submit="onSubmit" class="space-y-5">
      <UAlert
        v-if="error"
        color="red"
        variant="subtle"
        icon="i-lucide-alert-circle"
        :title="error"
      />

      <UFormField label="Email Address" name="email">
        <UInput
          v-model="state.email"
          type="email"
          placeholder="you@example.com"
          icon="i-lucide-mail"
          size="lg"
          class="w-full"
        />
      </UFormField>

      <UFormField label="Password" name="password">
        <UInput
          v-model="state.password"
          type="password"
          placeholder="••••••••"
          icon="i-lucide-lock"
          size="lg"
          class="w-full"
        />
      </UFormField>

      <UButton
        type="submit"
        block
        size="lg"
        :loading="loading"
        color="primary"
      >
        Sign in
      </UButton>
    </UForm>

    <div class="relative my-8">
      <div class="absolute inset-0 flex items-center">
        <span class="w-full border-t border-gray-200 dark:border-gray-800" />
      </div>
      <div class="relative flex justify-center text-xs uppercase tracking-widest font-bold text-gray-400">
        <span class="bg-white dark:bg-gray-900 px-4">Or continue with</span>
      </div>
    </div>

    <UButton
      color="gray"
      variant="outline"
      block
      size="lg"
      icon="i-logos-google-icon"
      @click="loginWithGoogle"
    >
      Sign in with Google
    </UButton>

    <p class="mt-8 text-center text-sm text-gray-500 dark:text-gray-400">
      Don’t have an account?
      <RouterLink :to="{ name: 'public.register' }" class="font-bold text-primary-600 dark:text-primary-400 hover:underline ml-1">
        Create one
      </RouterLink>
    </p>
  </AuthLayout>
</template>
