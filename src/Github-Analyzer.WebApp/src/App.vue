<script setup lang="ts">
import { useToast } from '@nuxt/ui/composables';
import { setToastApi, toaster } from './lib/toast';
import OnboardingWrapper from './components/OnboardingWrapper.vue';

const toast = useToast();
setToastApi(toast);
</script>

<template>
  <NApp :toaster="toaster"
    :tooltip="{ delayDuration: 100 }"
  >
    <RouterView />
    <OnboardingWrapper />
  </NApp>
</template>
