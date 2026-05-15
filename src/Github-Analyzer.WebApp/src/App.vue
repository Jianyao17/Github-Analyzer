<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import toastManager from './lib/toast';

const toast = useToast();

let unsubscribe: (() => void) | null = null;

onMounted(() => 
{
  unsubscribe = toastManager.subscribe((message, type) => 
  {
    toast.add({
      description: message,
      title: type === 'error' ? 'Error' : 'Success',
      color: type === 'error' ? 'error' : 'success',
      icon: type === 'error' ? 'i-lucide-alert-circle' : 'i-lucide-check-circle',
    });
  });
});

onUnmounted(() => 
{
  if (unsubscribe) unsubscribe();
});
</script>

<template>
  <NApp>
    <RouterView />
  </NApp>
</template>
