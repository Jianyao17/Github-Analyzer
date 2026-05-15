<script setup lang="ts">
const emit = defineEmits<{
  (e: 'click'): void;
}>();
</script>

<template>
  <div class="mt-6 flex w-full flex-col gap-4">
    <!-- Divider -->
    <div class="relative">
      <div class="absolute inset-0 flex items-center">
        <div class="
          w-full border-t border-gray-200
          dark:border-gray-800
        "
        ></div>
      </div>
      <div class="relative flex justify-center text-sm">
        <span class="
          bg-white px-2 text-gray-500
          dark:bg-gray-900
        "
        >
          Atau lanjutkan dengan
        </span>
      </div>
    </div>

    <!-- Google Button -->
    <NButton color="neutral"
      icon="material-icon-theme:google"
      variant="outline"
      @click="emit('click')"
      block
    >
      Google
    </NButton>
  </div>
</template>
