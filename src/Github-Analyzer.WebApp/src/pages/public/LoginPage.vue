<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../../stores/auth.store';
import AuthLayout from '../../components/_Layouts/AuthLayout.vue';
import GoogleAuthButton from '../../components/GoogleAuthButton.vue';

const isGoogleAuthEnabled = ref(false);

const router = useRouter();
const route = useRoute();
const auth = useAuthStore();

const state = reactive({
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
    await auth.login(state);
    const redirect = route.query.redirect as string || '/app/analysis/new';

    
    router.push({ path: redirect });
  }
  catch (err: any) 
  {
    error.value = err.response?.data?.message || 'Login failed. Please check your credentials.';
  }
  finally 
  {
    loading.value = false;
  }
}

function loginWithGoogle() 
{
  console.log('Login with Google clicked');
}
</script>

<template>
  <AuthLayout title="Welcome back"
    subtitle="Sign in to your account"
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
        <template #hint>
          <RouterLink :to="{ name: 'public.forgot-password' }"
            class="
              text-primary-600
              hover:text-primary-500
              dark:text-primary-400
              text-sm font-medium
            "
          >
            Lupa password?
          </RouterLink>
        </template>

        <NInput v-model="state.password"
          id="password"
          name="password"
          :type="showPassword ? 'text' : 'password'"
          placeholder="••••••••"
          icon="i-lucide-lock"
          class="w-full"
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
        Sign in
      </NButton>
    </NForm>

    <GoogleAuthButton v-if="isGoogleAuthEnabled"
      @click="loginWithGoogle"
    />

    <p class="
      mt-6 text-center text-sm text-gray-500
      dark:text-gray-400
    "
    >
      Belum punya akun?
      <RouterLink :to="{ name: 'public.register' }"
        class="
          text-primary-600
          dark:text-primary-400
          hover:text-primary-500
          ml-1 font-medium transition-colors
          hover:underline
        "
      >
        Sign up
      </RouterLink>
    </p>
  </AuthLayout>
</template>
