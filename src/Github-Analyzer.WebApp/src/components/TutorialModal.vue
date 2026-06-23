<script setup lang="ts">
import { ref, computed, watch, onUnmounted } from 'vue';
import type { TutorialStep } from '@/stores/tutorial.store';

const props = defineProps<{
  open: boolean;
  title?: string;
  steps: TutorialStep[];
}>();

const emit = defineEmits<{
  (e: 'update:open', value: boolean): void;
}>();

const currentPage = ref(0);

const currentStep = computed(() => props.steps[currentPage.value]);
const isLastPage = computed(() => currentPage.value === props.steps.length - 1);
const isFirstPage = computed(() => currentPage.value === 0);

const isLoadingMedia = ref(true);

watch(currentPage, () => 
{
  isLoadingMedia.value = true;
});

function handleNext() 
{
  if (!isLastPage.value) 
  {
    currentPage.value++;
  }
  else 
  {
    closeModal();
  }
}

function handlePrev() 
{
  if (!isFirstPage.value) 
  {
    currentPage.value--;
  }
}

function closeModal() 
{
  emit('update:open', false);
  // Reset to first page after a short delay to allow transition to finish
  setTimeout(() => 
  {
    currentPage.value = 0;
  }, 300);
}

// Handle keyboard navigation
function handleKeyDown(e: KeyboardEvent) 
{
  if (!props.open) return;
  
  if (e.key === 'ArrowRight') 
  {
    e.preventDefault();
    e.stopPropagation();
    handleNext();
  }
  else if (e.key === 'ArrowLeft') 
  {
    e.preventDefault();
    e.stopPropagation();
    handlePrev();
  }
}

watch(() => props.open, (isOpen) => 
{
  if (isOpen) 
  {
    window.addEventListener('keydown', handleKeyDown, { capture: true });
  }
  else 
  {
    window.removeEventListener('keydown', handleKeyDown, { capture: true });
  }
});

onUnmounted(() => 
{
  window.removeEventListener('keydown', handleKeyDown, { capture: true });
});
</script>

<template>
  <NModal
    :open="open"
    class="z-[9999] rounded-lg"
    @update:open="emit('update:open', $event)"
    :ui="{
      content: 'w-[90vw] sm:w-full sm:max-w-[700px] flex flex-col overflow-hidden',
      header: 'p-4 sm:px-6 shrink-0',
      body: 'p-0 flex flex-col min-h-0',
      footer: 'p-4 sm:px-6 shrink-0'
    }"
  >
    <!-- Header -->
    <template #header>
      <div class="flex items-center">
        <h3 class="pr-8 text-lg font-semibold text-[var(--ui-text)]">
          {{ title || 'Tutorial' }}
        </h3>
        <NButton
          icon="i-lucide-x"
          variant="ghost"
          color="neutral"
          size="sm"
          class="
            absolute top-4 right-4 text-[var(--ui-text-muted)]
            hover:bg-[var(--ui-bg-elevated)] hover:text-[var(--ui-text)]
          "
          @click="closeModal"
        />
      </div>
    </template>

    <!-- Body -->
    <template #body>
      <div class="
        flex h-full min-h-0 w-full flex-col
        sm:flex-row
      "
      >
        <!-- Media -->
        <div class="
          relative flex aspect-[4/3] min-h-0 w-full shrink-0 items-center
          justify-center overflow-hidden border-b border-[var(--ui-border)]
          bg-[var(--ui-bg-elevated)]
          sm:aspect-[4/3] sm:w-[55%] sm:border-r sm:border-b-0
        "
        >
          
          <!-- Loading overlay -->
          <div v-if="isLoadingMedia && currentStep?.mediaSrc"
            class="
              absolute inset-0 z-10 flex items-center justify-center
              bg-[var(--ui-bg-elevated)]
            "
          >
            <NIcon name="i-lucide-loader-2"
              class="h-8 w-8 animate-spin text-[var(--ui-primary)]"
            />
          </div>

          <!-- Placeholder for media (gif/video) -->
          <img 
            v-if="currentStep?.mediaSrc && currentStep.mediaType !== 'video'" 
            :src="currentStep.mediaSrc" 
            :alt="currentStep.title"
            class="
              h-full w-full rounded-lg object-cover transition-opacity
              duration-300
            "
            :class="{ 'opacity-0': isLoadingMedia, 'opacity-100': !isLoadingMedia }"
            @load="isLoadingMedia = false"
            @error="isLoadingMedia = false"
          />
          <video
            v-else-if="currentStep?.mediaSrc && currentStep.mediaType === 'video'"
            :src="currentStep.mediaSrc"
            autoplay
            loop
            muted
            playsinline
            class="
              h-full w-full rounded-lg object-cover transition-opacity
              duration-300
            "
            :class="{ 'opacity-0': isLoadingMedia, 'opacity-100': !isLoadingMedia }"
            @loadeddata="isLoadingMedia = false"
            @error="isLoadingMedia = false"
          />
          <div v-else
            class="flex flex-col items-center gap-2 text-[var(--ui-text-muted)]"
          >
            <NIcon name="i-lucide-image"
              class="h-8 w-8 opacity-50"
            />
            <span class="text-sm">Media belum tersedia</span>
          </div>
        </div>

        <!-- Content -->
        <div class="
          flex w-full shrink-0 flex-col justify-start gap-2 overflow-y-auto p-4
          sm:w-[45%] sm:p-6
        "
        >
          <h4 class="text-base font-bold text-[var(--ui-text)]">
            {{ currentStep?.title }}
          </h4>
          <p class="text-sm leading-relaxed text-[var(--ui-text-muted)]">
            {{ currentStep?.description }}
          </p>
        </div>
      </div>
    </template>

    <!-- Footer -->
    <template #footer>
      <div class="relative flex w-full items-center justify-between">
        <!-- Back Button -->
        <div class="w-24">
          <NButton
            v-if="!isFirstPage"
            variant="outline"
            color="neutral"
            size="md"
            class="
              border-[var(--ui-border)] text-[var(--ui-text)]
              hover:bg-[var(--ui-bg-elevated)]
            "
            @click="handlePrev"
          >
            Kembali
          </NButton>
        </div>

        <!-- Pagination Indicators -->
        <div class="
          absolute left-1/2 flex -translate-x-1/2 items-center gap-1.5
        "
        >
          <div 
            v-for="(_, index) in steps" 
            :key="index"
            class="h-2 rounded-full transition-all duration-300"
            :class="index === currentPage ? 'w-4 bg-[var(--ui-primary)]' : `
              w-2 bg-[var(--ui-border)]
            `"
          />
        </div>

        <!-- Actions -->
        <div class="flex w-24 justify-end">
          <NButton
            variant="solid"
            color="primary"
            size="md"
            @click="handleNext"
          >
            {{ isLastPage ? 'Selesai' : 'Lanjut' }}
          </NButton>
        </div>
      </div>
    </template>
  </NModal>
</template>
