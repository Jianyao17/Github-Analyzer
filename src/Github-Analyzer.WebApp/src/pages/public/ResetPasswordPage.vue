<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthApi } from '../../composables/useAuthApi';
import AuthLayout from '../../components/_layouts/AuthLayout.vue';

const route = useRoute();
const authApi = useAuthApi();

const state = reactive({
  email: '',
  token: '',
  password: '',
  confirmPassword: ''
});

const loading = ref(false);
const error = ref('');
const success = ref(false);
const showPassword = ref(false);

onMounted(() => 
{
  const email = route.query.email as string;
  const token = route.query.token as string;

  if (!email || !token) 
  {
    error.value = 'Invalid reset link. Missing email or token.';
  }
  else
  {
    state.email = email;
    state.token = token;
  }
});

async function onSubmit() 
{
  if (state.password !== state.confirmPassword)
  {
    error.value = 'Passwords do not match.';
    return;
  }

  loading.value = true;
  error.value = '';
  success.value = false;

  try 
  {
    await authApi.resetPassword({
      email: state.email,
      token: state.token,
      newPassword: state.password
    });
    success.value = true;
  }
  catch (err: any) 
  {
    error.value = err.response?.data?.message || 'Failed to reset password. The link might be invalid or expired.';
  }
  finally 
  {
    loading.value = false;
  }
}
</script>

<template>
  <AuthLayout title="Reset Password"
    subtitle="Buat password baru Anda"
  >
    <div v-if="success" class="text-center space-y-6">
      <NAlert
        color="success"
        variant="subtle"
        icon="i-lucide-check-circle"
        title="Password berhasil direset!"
        description="Sekarang Anda dapat masuk dengan password baru Anda."
      />
      <NButton to="/login"
        block
        color="primary"
      >
        Kembali ke Login
      </NButton>
    </div>

    <NForm v-else :state="state"
      @submit="onSubmit"
      class="space-y-6"
    >
      <NAlert v-if="error"
        color="error"
        variant="subtle"
        icon="i-lucide-alert-circle"
        :title="error"
      />

      <NFormField label="Password Baru"
        name="password"
      >
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

      <NFormField label="Konfirmasi Password Baru"
        name="confirmPassword"
      >
        <NInput v-model="state.confirmPassword"
          id="confirmPassword"
          name="confirmPassword"
          :type="showPassword ? 'text' : 'password'"
          placeholder="••••••••"
          icon="i-lucide-lock"
          class="w-full"
        />
      </NFormField>

      <NButton type="submit"
        block
        :loading="loading"
        color="primary"
        :disabled="!state.email || !state.token"
      >
        Reset Password
      </NButton>
    </NForm>
  </AuthLayout>
</template>
