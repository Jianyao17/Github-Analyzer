<script setup lang="ts">
import { useRouter } from 'vue-router';
import { computed, shallowRef, watch } from 'vue';
import type { DropdownMenuItem } from '@nuxt/ui';
import { useAuthStore } from '../stores/auth.store';
import { useAuthApi } from '../composables/useAuthApi';
import { useThemeStore } from '../stores/theme.store';

type UserProfileCardProps = {
  isCollapsed: boolean
  isMobile: boolean
};

const { 
  isCollapsed, isMobile 
} = defineProps<UserProfileCardProps>();

const auth = useAuthStore();
const authApi = useAuthApi();
const theme = useThemeStore();
const router = useRouter();

const displayName = computed(() => auth.user?.displayName || auth.user?.username || 'Guest');
const username = computed(() => auth.user?.username || 'Guest');
const email = computed(() => auth.user?.email || 'Not logged in');
const isDark = computed(() => theme.theme === 'dark');

function handleLogout() 
{
  authApi.logout();
  router.push('/login');
}

function handleToggleTheme() 
{
  theme.toggleTheme();
}

const menuItems = shallowRef<DropdownMenuItem[][]>([]);
const menuContentClass = computed(() => 
  (isCollapsed && !isMobile
    ? 'w-64 z-[60]'
    : 'w-(--reka-dropdown-menu-trigger-width) z-[60]'));

watch([displayName, username, email, isDark], () => 
{
  menuItems.value = [
    [
      {
        label: displayName.value,
        description: email.value,
        avatar: {
          src: auth.user?.avatarUrl || undefined,
          icon: auth.user?.avatarUrl ? undefined : 'i-lucide-user',
          text: displayName.value,
          alt: displayName.value,
          size: 'lg'
        },
        type: 'label',
      }
    ],
    [
      {
        label: isDark.value ? 'Switch to Light' : 'Switch to Dark',
        icon: isDark.value ? 'i-lucide-sun' : 'i-lucide-moon',
        onSelect: (e) => 
        {
          e.preventDefault();
          handleToggleTheme();
        }
      },
      {
        label: 'Settings',
        icon: 'i-lucide-settings',
        disabled: true
      }
    ],
    [
      {
        label: 'Logout',
        icon: 'i-lucide-log-out',
        color: 'error',
        onSelect: handleLogout
      }
    ]
  ];
}, { immediate: true });
</script>

<template>
  <!-- User Profile & Settings -->
  <div class="border-t border-[var(--ui-border)] bg-[var(--ui-bg-muted)]/50 p-2">
    <NDropdownMenu
      :items="menuItems"
      :content="{ side: 'top', align: 'start', sideOffset: 8 }"
      :ui="{ content: menuContentClass }"
      :modal="false"
    >
      <NButton
        block
        color="neutral"
        variant="soft"
        class="w-full"
        :class="(isCollapsed && !isMobile) 
          ? 'justify-center px-2' 
          : 'justify-start'"
      >
        <div class="flex w-full items-center"
          :class="(isCollapsed && !isMobile) ? 'justify-center' : 'gap-3'"
        >
          <NAvatar
            :class="{
              'mx-auto': isCollapsed && !isMobile,
              'ml-0': !isCollapsed || isMobile
            }"
            :src="auth.user?.avatarUrl || undefined"
            :icon="auth.user?.avatarUrl ? undefined : 'i-lucide-user'"
            :alt="auth.user?.displayName || 
              auth.user?.username || 
              'GitHub Analyzer'"
            size="lg"
          />
          <div v-if="!isCollapsed || isMobile"
            class="min-w-0 flex-1"
          >
            <p class="
              truncate text-start text-sm font-semibold
              text-[var(--ui-text-highlighted)]
            "
            >
              {{ auth.user?.displayName || auth.user?.username || 'Guest' }}
            </p>
            <p class="
              truncate text-start text-[11px] text-[var(--ui-text-muted)]
            "
            >
              {{ auth.user?.email || 'Not logged in' }}
            </p>
          </div>
          <NIcon
            v-if="!isCollapsed || isMobile"
            name="i-lucide-chevrons-up-down"
            class="ml-auto h-4 w-4 text-[var(--ui-text-muted)]"
          />
        </div>
      </NButton>
    </NDropdownMenu>
  </div>
</template>
