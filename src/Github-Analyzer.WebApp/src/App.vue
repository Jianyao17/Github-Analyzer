<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import toastManager from './lib/toast'

const toast = useToast()

let unsubscribe: (() => void) | null = null

onMounted(() => {
  unsubscribe = toastManager.subscribe((message, type) => {
    toast.add({
      title: type === 'error' ? 'Error' : 'Success',
      description: message,
      color: type === 'error' ? 'red' : 'green',
      icon: type === 'error' ? 'i-lucide-alert-circle' : 'i-lucide-check-circle',
    })
  })
})

onUnmounted(() => {
  if (unsubscribe) unsubscribe()
})
</script>

<template>
  <UApp>
    <RouterView />
  </UApp>
</template>
