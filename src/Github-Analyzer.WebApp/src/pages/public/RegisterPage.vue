<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../../stores/auth.store';
import AuthLayout from '../../components/_layouts/AuthLayout.vue';
import GoogleAuthButton from '../../components/GoogleAuthButton.vue';

const isGoogleAuthEnabled = ref(false);

const router = useRouter();
const auth = useAuthStore();

const state = reactive({
  username: '',
  email: '',
  password: ''
});

const loading = ref(false);
const error = ref('');
const showPassword = ref(false);

async function onSubmit() 
{
  loading.value = true;
  error.value = '';

  try 
  {
    await auth.register(state);
    router.push('/analysis/new');
  }
  catch (err: any) 
  {
    error.value = err.response?.data?.message || 'Registration failed. Please try again.';
  }
  finally 
  {
    loading.value = false;
  }
}

function registerWithGoogle() 
{
  console.log('Register with Google clicked');
}
</script>

<template>
  <AuthLayout title="Create an account"
    subtitle="Get started with Github Analyzer"
  >
    <NForm :state="state"
      @submit="onSubmit"
      class="space-y-6"
    >
      <NAlert v-if="error"
        color="error"
        variant="subtle"
        icon="i-lucide-alert-circle"
        :title="error"
      />

      <NFormField label="Username"
        name="username"
      >
        <NInput v-model="state.username"
          id="username"
          name="username"
          autocomplete="username"
          placeholder="johndoe"
          icon="i-lucide-user"
          class="w-full"
        />
      </NFormField>

      <NFormField label="Email Address"
        name="email"
      >
        <NInput v-model="state.email"
          id="email"
          name="email"
          type="email"
          autocomplete="email"
          placeholder="you@example.com"
          icon="i-lucide-mail"
          class="w-full"
        />
      </NFormField>

      <NFormField label="Password"
        name="password"
      >
        <NInput v-model="state.password"
          id="password"
          name="password"
          placeholder="••••••••"
          icon="i-lucide-lock"
          class="w-full"
          :type="showPassword ? 'text' : 'password'"
          :ui="{ trailing: 'pe-3' }"
        >
          <template #trailing>
            <NButton color="neutral"
              variant="link"
              :padded="false"
              :icon="showPassword ? 'i-lucide-eye-off' : 'i-lucide-eye'"
              @click="showPassword = !showPassword"
            />
          </template>
        </NInput>
      </NFormField>

      <NButton type="submit"
        block
        :loading="loading"
        color="primary"
      >
        Create account
      </NButton>
    </NForm>

    <GoogleAuthButton v-if="isGoogleAuthEnabled"
      @click="registerWithGoogle"
    />

    <p class="
      mt-6 text-center text-sm text-gray-500
      dark:text-gray-400
    "
    >
      Sudah punya akun?
      <RouterLink :to="{ name: 'public.login' }"
        class="
          text-primary-600
          dark:text-primary-400
          hover:text-primary-500
          ml-1 font-medium transition-colors
          hover:underline
        "
      >
        Sign in
      </RouterLink>
    </p>
  </AuthLayout>
</template>
