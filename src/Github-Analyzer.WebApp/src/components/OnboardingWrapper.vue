<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { VOnboardingWrapper, VOnboardingStep, type StepEntity } from 'v-onboarding';
import { useOnboardingStore, type VOnboardingInstance } from '../stores/onboarding.store';
import 'v-onboarding/dist/style.css';

const store = useOnboardingStore();
const wrapperRef = ref<VOnboardingInstance | null>(null);
const isDev = import.meta.env.DEV;

// Bind the ref to the store so the store can trigger it
onMounted(() => 
{
  store.wrapperRef = wrapperRef.value;
});

// Watch for ref changes if the component re-renders
watch(wrapperRef, (val) => 
{
  store.wrapperRef = val;
});

function handleReset() 
{
  store.resetTours();
  window.location.reload();
}
</script>

<template>
  <Teleport to="body">
    <VOnboardingWrapper 
      ref="wrapperRef" 
      :steps="(store.currentSteps as StepEntity[])"
      :options="{ overlay: { borderRadius: 12, padding: 4 } }"
      @exit="() => wrapperRef?.finish()"
    >
      <template #default="{ previous, next, step, exit, isFirst, isLast }">
        <VOnboardingStep>
          <NCard 
            :key="step.content?.title"
            class="
              onboarding-card-animate pointer-events-auto z-[9999] w-[340px]
              border-[var(--ui-border)] shadow-2xl ring-1
              ring-[var(--ui-border)]
              sm:w-[380px]
            " 
            :ui="{ body: 'p-5 sm:p-6 flex flex-col gap-3' }"
          >
            <!-- Media (Image/Video) -->
            <div v-if="step.content?.media"
              class="
                mb-1 w-full overflow-hidden rounded-lg ring-1
                ring-[var(--ui-border)]
              "
            >
              <video 
                v-if="step.content.media.type === 'video'" 
                :src="step.content.media.url" 
                autoplay 
                loop 
                muted 
                class="h-auto w-full object-cover" 
              />
              <img 
                v-else 
                :src="step.content.media.url" 
                class="h-auto w-full object-cover" 
              />
            </div>

            <!-- Header / Title -->
            <div class="flex items-start justify-between gap-4">
              <div class="
                flex items-center gap-2.5 text-lg font-bold
                text-[var(--ui-text-highlighted)]
              "
              >
                <div class="
                  flex items-center justify-center rounded-lg
                  bg-[var(--ui-primary)]/10 p-1.5 text-[var(--ui-primary)]
                "
                >
                  <NIcon 
                    :name="step.content?.icon || 'i-lucide-info'" 
                    class="h-5 w-5 shrink-0" 
                  />
                </div>
                <span>{{ step.content?.title }}</span>
              </div>

              <!-- Step Counter -->
              <div class="
                mt-0.5 flex shrink-0 items-center justify-center rounded-full
                bg-[var(--ui-bg-elevated)] px-2.5 py-1 text-xs font-semibold
                text-[var(--ui-text-muted)] ring-1 ring-[var(--ui-border)]
                ring-inset
              "
              >
                {{ store.currentSteps.findIndex(s => s.content?.title === step.content?.title) + 1 }} / {{ store.currentSteps.length }}
              </div>
            </div>
            
            <!-- Description -->
            <p class="text-sm leading-relaxed text-[var(--ui-text-toned)]">
              {{ step.content?.description }}
            </p>

            <!-- Footer Actions -->
            <div class="
              mt-1 flex items-center justify-between border-t
              border-[var(--ui-border)] pt-3
            "
            >
              <NButton 
                variant="ghost" 
                color="gray" 
                size="md" 
                @click="exit" 
                class="
                  text-[var(--ui-text-muted)]
                  hover:text-[var(--ui-text)]
                "
              >
                Lewati
              </NButton>
              <div class="flex gap-2">
                <NButton 
                  v-if="!isFirst" 
                  variant="soft" 
                  color="gray" 
                  size="md" 
                  @click="previous"
                >
                  Kembali
                </NButton>
                <NButton 
                  color="primary" 
                  size="md" 
                  @click="next"
                >
                  {{ isLast ? 'Selesai' : 'Lanjut' }}
                </NButton>
              </div>
            </div>
          </NCard>
        </VOnboardingStep>
      </template>
    </VOnboardingWrapper>

    <!-- Dev Tool: Reset Tours -->
    <div v-if="isDev"
      class="fixed right-4 bottom-4 z-[100000]"
    >
      <NButton 
        size="xs" 
        color="rose" 
        variant="soft" 
        @click="handleReset"
        class="shadow-lg"
      >
        <template #leading>
          <NIcon name="i-lucide-rotate-ccw"
            class="h-3.5 w-3.5"
          />
        </template>
        Reset Tours
      </NButton>
    </div>
  </Teleport>
</template>

<style>
/* Override default v-onboarding z-index to be higher than MainSidebar (z-50) */
:root {
  --v-onboarding-overlay-z: 99998;
  --v-onboarding-step-z: 99999;
  --v-onboarding-step-arrow-background: var(--ui-bg);
  --v-onboarding-step-arrow-size: 16px;
}

/* Ensure the popper arrow container is correctly sized and sits ON TOP of the NCard */
[data-v-onboarding-wrapper] [data-popper-arrow] {
  z-index: 10000 !important;
  width: var(--v-onboarding-step-arrow-size) !important;
  height: var(--v-onboarding-step-arrow-size) !important;
}

/* Style the arrow itself (rotated square) */
[data-v-onboarding-wrapper] [data-popper-arrow]::before {
  background-color: var(--v-onboarding-step-arrow-background) !important;
  border: 1px solid var(--ui-border) !important;
  width: var(--v-onboarding-step-arrow-size) !important;
  height: var(--v-onboarding-step-arrow-size) !important;
  border-radius: 3px !important;
}

/* Fix arrow positions and hide inner borders so it seamlessly blends with the card */

/* Arrow pointing DOWN (Popper placed TOP) */
[data-v-onboarding-wrapper] [data-popper-placement^="top"] > [data-popper-arrow] {
  bottom: -8px !important;
}
[data-v-onboarding-wrapper] [data-popper-placement^="top"] > [data-popper-arrow]::before {
  border-width: 0 1px 1px 0 !important; /* Keep right and bottom borders */
}

/* Arrow pointing UP (Popper placed BOTTOM) */
[data-v-onboarding-wrapper] [data-popper-placement^="bottom"] > [data-popper-arrow] {
  top: -8px !important;
}
[data-v-onboarding-wrapper] [data-popper-placement^="bottom"] > [data-popper-arrow]::before {
  border-width: 1px 0 0 1px !important; /* Keep top and left borders */
}

/* Arrow pointing RIGHT (Popper placed LEFT) */
[data-v-onboarding-wrapper] [data-popper-placement^="left"] > [data-popper-arrow] {
  right: -8px !important;
}
[data-v-onboarding-wrapper] [data-popper-placement^="left"] > [data-popper-arrow]::before {
  border-width: 1px 1px 0 0 !important; /* Keep top and right borders */
}

/* Arrow pointing LEFT (Popper placed RIGHT) */
[data-v-onboarding-wrapper] [data-popper-placement^="right"] > [data-popper-arrow] {
  left: -8px !important;
}
[data-v-onboarding-wrapper] [data-popper-placement^="right"] > [data-popper-arrow]::before {
  border-width: 0 0 1px 1px !important; /* Keep bottom and left borders */
}

/* Smooth Animation for Overlay Cutout and Emission Effect */
[data-v-onboarding-wrapper] svg path 
{
  stroke-width: 2px !important;
  stroke: var(--ui-primary) !important;
  transition: d 0.4s cubic-bezier(0.16, 1, 0.3, 1) !important;
}

/* CSS Animation for the NCard Content (Fixes Popper height calculation) */
.onboarding-card-animate {
  animation: onboardingFadeIn 0.3s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

@keyframes onboardingFadeIn {
  from {
    opacity: 0;
    transform: translateY(12px) scale(0.98);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}
</style>
