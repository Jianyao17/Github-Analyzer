<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import AuthLayout from '../../components/_Layouts/AuthLayout.vue'
import { useAuthStore } from '../../stores/auth.store'

const router = useRouter()
const auth = useAuthStore()

const state = reactive({
  username: '',
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
    await auth.register(state)
    router.push('/analysis/new')
  } 
  catch (err: any) 
  {
    error.value = err.response?.data?.message || 'Registration failed. Please try again.'
  } 
  finally 
  {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout
    title="Create an account"
    subtitle="Get started with Github Analyzer"
  >
    <UForm :state="state" @submit="onSubmit" class="space-y-5">
      <UAlert
        v-if="error"
        color="red"
        variant="subtle"
        icon="i-lucide-alert-circle"
        :title="error"
      />

      <UFormField label="Username" name="username">
        <UInput
          v-model="state.username"
          placeholder="johndoe"
          icon="i-lucide-user"
          size="lg"
          class="w-full"
        />
      </UFormField>

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
        Create account
      </UButton>
    </UForm>

    <p class="mt-8 text-center text-sm text-gray-500 dark:text-gray-400">
      Already have an account?
      <RouterLink :to="{ name: 'public.login' }" class="font-bold text-primary-600 dark:text-primary-400 hover:underline ml-1">
        Sign in
      </RouterLink>
    </p>
  </AuthLayout>
</template>
