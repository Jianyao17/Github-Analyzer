<script setup lang="ts">
import { onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../../stores/auth.store';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();

onMounted(async () => 
{
  const token = route.query.token as string;

  if (token) 
  {
    auth.setAuth(token);
    await auth.loadCurrentUser();
    router.push({ name: 'app.analysis.new' });
  }
  else 
  {
    router.push({ name: 'public.login' });
  }
});
</script>

<template>
  <div class="callback-loading">
    <div class="spinner"></div>
    <p>Completing authentication...</p>
  </div>
</template>

<style scoped>
  .callback-loading {
    height: 100vh;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 16px;
    color: #374151;
  }

  .spinner {
    width: 40px;
    height: 40px;
    border: 4px solid #f3f4f6;
    border-top: 4px solid #111827;
    border-radius: 50%;
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }

    100% {
      transform: rotate(360deg);
    }
  }

  .dark .callback-loading {
    color: #f3f4f6;
  }

  .dark .spinner {
    border-color: #1f2937;
    border-top-color: #f3f4f6;
  }
</style>
