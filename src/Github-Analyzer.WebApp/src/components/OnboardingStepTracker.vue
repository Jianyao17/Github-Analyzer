<script setup lang="ts">
import { watch } from 'vue';
import { useOnboardingStore } from '../stores/onboarding.store';

const props = defineProps<{ step: any }>();
const store = useOnboardingStore();

/**
  * Setup event listener for the current step
  * Triggered when the step changes
  * @param newStep - The new step to setup event listener for
  */
watch(() => props.step, (newStep) => 
{
  if (newStep) 
  {
    const index = store.currentSteps.findIndex(s => s.content?.title === newStep.content?.title);
    if (index !== -1) 
    {
      store.setupGraphEventListener(index);
    }
  }
}, { immediate: true });
</script>

<template>
  <div style="display: none"></div>
</template>
