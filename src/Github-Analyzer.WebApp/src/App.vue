<script setup lang="ts">
import ToastHost from './components/feedback/ToastHost.vue'
</script>

<template>
  <UApp>
    <RouterView />
    <ToastHost />
  </UApp>
</template>
