<script setup lang="ts">
import { onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../../stores/auth.store';
import { useAuthApi } from '../../composables/useAuthApi';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const authApi = useAuthApi();

onMounted(async () => 
{
  const token = route.query.token as string;

  if (token) 
  {
    auth.setToken(token);
    await authApi.loadCurrentUser();

    // Redirect to the main app page after successful authentication
    router.push({ name: 'app.analysis.new' });
  }
  else 
  {
    //router.push({ name: 'public.login' });
  }
});
</script>

<template>
  <div class="
    relative flex min-h-screen items-center justify-center overflow-hidden
    bg-[var(--ui-bg-muted)] p-6
  "
  >
    <div class="
      absolute top-10 -left-24 h-64 w-64 rounded-full bg-emerald-400/20 blur-3xl
      dark:bg-emerald-500/10
    "
    />
    <div class="
      absolute -right-16 bottom-0 h-72 w-72 rounded-full bg-sky-400/20 blur-3xl
      dark:bg-sky-500/10
    "
    />

    <NCard class="
      relative z-10 w-full max-w-md border border-[var(--ui-border)]
      bg-[var(--ui-bg)]/80 shadow-2xl backdrop-blur-xl
    "
    >
      <template #header>
        <div class="flex flex-col items-center gap-2 text-center">
          <NIcon name="i-lucide-shield-check"
            class="
              h-10 w-10 text-emerald-600
              dark:text-emerald-400
            "
          />
          <h1 class="text-2xl font-bold text-[var(--ui-text-highlighted)]">
            Signing you in
          </h1>
          <p class="text-sm text-[var(--ui-text-muted)]">
            Completing authentication
          </p>
        </div>
      </template>

      <div class="flex flex-col items-center gap-4 text-center">
        <NIcon name="i-lucide-loader-2"
          class="
            h-10 w-10 animate-spin text-primary-600
            dark:text-primary-400
          "
        />
        <p class="text-sm text-[var(--ui-text-muted)]">
          Please wait, we are preparing your workspace.
        </p>
      </div>
    </NCard>
  </div>
</template>
