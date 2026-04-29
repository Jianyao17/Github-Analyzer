import { computed, ref, watch } from 'vue'
import { defineStore } from 'pinia'

export type ThemeMode = 'light' | 'dark'

const storageKey = 'github-analyzer.theme'

function getSystemPreference(): ThemeMode {
  if (typeof window === 'undefined' || !window.matchMedia) {
    return 'light'
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function applyTheme(mode: ThemeMode) {
  if (typeof document === 'undefined') {
    return
  }

  document.documentElement.classList.toggle('dark', mode === 'dark')
}

export const useThemeStore = defineStore('theme', () => {
  const mode = ref<ThemeMode>('light')
  const isDark = computed(() => mode.value === 'dark')

  function hydrate() {
    if (typeof localStorage === 'undefined') {
      mode.value = getSystemPreference()
      return
    }

    const persisted = localStorage.getItem(storageKey) as ThemeMode | null
    mode.value = persisted ?? getSystemPreference()
  }

  function persist() {
    if (typeof localStorage === 'undefined') {
      return
    }

    localStorage.setItem(storageKey, mode.value)
  }

  function setMode(nextMode: ThemeMode) {
    mode.value = nextMode
  }

  function toggle() {
    mode.value = mode.value === 'dark' ? 'light' : 'dark'
  }

  function initialize() {
    hydrate()
    applyTheme(mode.value)
  }

  watch(mode, nextMode => {
    applyTheme(nextMode)
    persist()
  })

  return {
    initialize,
    isDark,
    mode,
    setMode,
    toggle,
  }
})
