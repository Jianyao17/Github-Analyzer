<script setup lang="ts">
import { setToastApi, toaster } from './lib/toast';

const toast = useToast();
setToastApi(toast);
</script>

<template>
  <NApp :toaster="toaster">
    <RouterView />
  </NApp>
</template>
