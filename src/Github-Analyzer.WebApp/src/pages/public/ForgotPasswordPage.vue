<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useAuthApi } from '../../composables/useAuthApi';
import AuthLayout from '../../components/_Layouts/AuthLayout.vue';

const authApi = useAuthApi();

const state = reactive({
  email: ''
});

const loading = ref(false);
const error = ref('');
const success = ref(false);

async function onSubmit() 
{
  loading.value = true;
  error.value = '';
  success.value = false;

  try 
  {
    await authApi.forgotPassword(state);
    success.value = true;
  }
  catch (err: any) 
  {
    error.value = err.response?.data?.message || 'Failed to request password reset. Please try again later.';
  }
  finally 
  {
    loading.value = false;
  }
}
</script>

<template>
  <AuthLayout title="Lupa Password"
    subtitle="Masukkan email Anda untuk mereset password"
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

      <NAlert v-else-if="success"
        color="success"
        variant="subtle"
        icon="i-lucide-check-circle"
        title="Email Terkirim"
        description="Jika email Anda terdaftar, kami telah mengirimkan email instruksi untuk mereset password."
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
          :disabled="success || loading"
        />
      </NFormField>

      <NButton type="submit"
        block
        :loading="loading"
        color="primary"
        :disabled="success"
      >
        Kirim Link Reset Password
      </NButton>
    </NForm>

    <p class="
      mt-6 text-center text-sm text-gray-500
      dark:text-gray-400
    "
    >
      Ingat password Anda?
      <RouterLink :to="{ name: 'public.login' }"
        class="
          text-primary-600
          dark:text-primary-400
          hover:text-primary-500
          ml-1 font-medium transition-colors
          hover:underline
        "
      >
        Kembali ke Login
      </RouterLink>
    </p>
  </AuthLayout>
</template>
