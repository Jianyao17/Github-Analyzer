<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthApi } from '../../composables/useAuthApi';
import AuthLayout from '../../components/_layouts/AuthLayout.vue';

const route = useRoute();
const authApi = useAuthApi();

const loading = ref(true);
const success = ref(false);
const error = ref('');

onMounted(async () => 
{
  const userId = route.query.userId as string;
  const token = route.query.token as string;

  if (!userId || !token) 
  {
    error.value = 'Invalid verification link. Missing user ID or token.';
    loading.value = false;
    return;
  }

  try 
  {
    await authApi.verifyEmail({ userId, token });
    success.value = true;
  }
  catch (err: any) 
  {
    error.value = err.response?.data?.message || 'Email verification failed. The link might be invalid or expired.';
  }
  finally 
  {
    loading.value = false;
  }
});
</script>

<template>
  <AuthLayout title="Verifikasi Email"
    subtitle="Memverifikasi email Anda..."
  >
    <div class="text-center space-y-6">
      <NAlert v-if="loading"
        color="primary"
        variant="subtle"
        icon="i-lucide-loader-2"
        title="Sedang memproses..."
        description="Mohon tunggu sebentar sementara kami memverifikasi email Anda."
      />

      <NAlert v-else-if="error"
        color="error"
        variant="subtle"
        icon="i-lucide-alert-circle"
        :title="error"
      />

      <NAlert v-else-if="success"
        color="success"
        variant="subtle"
        icon="i-lucide-check-circle"
        title="Email berhasil diverifikasi!"
        description="Sekarang Anda dapat masuk ke akun Anda."
      />

      <NButton v-if="!loading"
        to="/login"
        block
        color="primary"
      >
        Kembali ke Login
      </NButton>
    </div>
  </AuthLayout>
</template>
