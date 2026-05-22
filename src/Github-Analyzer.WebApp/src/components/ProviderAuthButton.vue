<script setup lang="ts">
withDefaults(
  defineProps<{
    icon: string;
    label: string;
    color?: 'primary' | 'neutral';
    variant?: 'outline' | 'solid';
  }>(),
  {
    color: 'neutral',
    variant: 'outline'
  }
);

defineEmits<{
  (e: 'click'): void;
}>();
</script>

<template>
  <NButton
    block
    :color="color"
    :icon="icon"
    :variant="variant"
    @click="$emit('click')"
  >
    {{ label }}
  </NButton>
</template>