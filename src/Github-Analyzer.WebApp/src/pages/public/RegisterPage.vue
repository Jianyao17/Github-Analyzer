<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useAuthApi } from '../../composables/useAuthApi';
import AuthLayout from '../../components/_layouts/AuthLayout.vue';
import ProviderAuthSection from '../../components/ProviderAuthSection.vue';

const authApi = useAuthApi();

const state = reactive({
  username: '',
  email: '',
  password: ''
});

const loading = ref(false);
const error = ref('');
const success = ref(false);
const showPassword = ref(false);

async function onSubmit() 
{
  loading.value = true;
  error.value = '';

  try 
  {
    await authApi.register(state);
    success.value = true;
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
</script>

<template>
  <AuthLayout title="Create an account"
    subtitle="Get started with Github Analyzer"
  >
    <div v-if="success"
      class="space-y-6 text-center"
    >
      <NAlert
        color="success"
        variant="subtle"
        icon="i-lucide-mail"
        title="Pendaftaran Berhasil!"
        description="Silakan periksa kotak masuk email Anda untuk melakukan verifikasi akun. Setelah diverifikasi, Anda dapat login."
      />
      <NButton to="/login"
        block
        color="primary"
      >
        Pergi ke Halaman Login
      </NButton>
    </div>

    <div v-else
      class="space-y-6"
    >
      <NForm :state="state"
        @submit="onSubmit"
        class="space-y-4"
      >
        <NAlert v-if="error"
          color="error"
          variant="subtle"
          icon="i-lucide-alert-circle"
          :title="error"
        />

        <NFormField label="Username"
          name="username"
          required
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
          required
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
          required
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
          :loading="loading"
          color="primary"
          class="mt-2"
          block
        >
          Create account
        </NButton>
      </NForm>

      <ProviderAuthSection />

      <p class="mt-6 text-center text-sm text-[var(--ui-text-muted)]">
        Sudah punya akun?
        <RouterLink :to="{ name: 'public.login' }"
          class="
            ml-1 font-medium text-[var(--ui-primary)] transition-colors
            hover:underline hover:opacity-80
          "
        >
          Sign in
        </RouterLink>
      </p>
    </div>
  </AuthLayout>
</template>
