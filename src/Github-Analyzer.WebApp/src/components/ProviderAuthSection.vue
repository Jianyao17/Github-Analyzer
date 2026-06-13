<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useAuthApi } from '../composables/useAuthApi';
import ProviderAuthButton from './ProviderAuthButton.vue';

type AuthProviderItem = 
{
  key: string;
  icon: string;
  label: string;
  kind?: 'google' | 'custom';
  color?: 'primary' | 'neutral';
  variant?: 'outline' | 'solid';
  onClick?: () => void | Promise<void>;
};

const providerItems: AuthProviderItem[] = 
[
  {
    key: 'google',
    kind: 'google',
    icon: 'material-icon-theme:google',
    label: 'Lanjutkan dengan Google'
  },
];

const authApi = useAuthApi();
const isGoogleAuthEnabled = ref(false);
const isProviderAuthLoading = ref(true);

const visibleProviders = computed(() => 
  providerItems.filter(provider => provider.kind !== 'google' || isGoogleAuthEnabled.value));

const displayedProviders = computed(() => 
  visibleProviders.value.length ? visibleProviders.value : providerItems);

const isProviderContentVisible = computed(() => 
  !isProviderAuthLoading.value && visibleProviders.value.length > 0);

onMounted(() => 
{
  authApi.isGoogleAuthEnabled()
    .then(enabled => isGoogleAuthEnabled.value = enabled)
    .catch(() => isGoogleAuthEnabled.value = false)
    .finally(() => isProviderAuthLoading.value = false);
});

function handleProviderClick(provider: AuthProviderItem) 
{
  if (provider.kind === 'google') 
  {
    authApi.googleAuth();
    return;
  }

  provider.onClick?.();
}
</script>

<template>
  <div class="mt-4">
    <div class="relative">
      <div class="flex w-full flex-col gap-4"
        :class="isProviderContentVisible ? 'visible' : `
          pointer-events-none invisible
        `"
        aria-hidden="true"
      >
        <div class="relative">
          <div class="absolute inset-0 flex items-center">
            <div class="w-full border-t border-[var(--ui-border)]"></div>
          </div>

          <div class="relative flex justify-center text-sm">
            <span class="bg-[var(--ui-bg)] px-2 text-[var(--ui-text-muted)]">
              Atau
            </span>
          </div>
        </div>

        <div class="flex flex-col gap-3">
          <ProviderAuthButton
            v-for="provider in displayedProviders"
            :key="provider.key"
            :icon="provider.icon"
            :label="provider.label"
            :color="provider.color"
            :variant="provider.variant"
            @click="handleProviderClick(provider)"
          />
        </div>
      </div>

      <div v-if="isProviderAuthLoading"
        class="absolute inset-0 flex items-center justify-center"
      >
        <NIcon name="i-lucide-loader-2"
          class="h-5 w-5 animate-spin text-[var(--ui-text-muted)]"
        />
      </div>
    </div>
  </div>
</template>