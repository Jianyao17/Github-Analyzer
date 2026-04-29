import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

export interface UserProfile {
  id: string
  email: string
  displayName: string
}

export interface AuthResponse {
  accessToken: string
  expiresAtUtc: string
  user: UserProfile
}

const storageKey = 'github-analyzer.auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string>('')
  const user = ref<UserProfile | null>(null)

  const isAuthenticated = computed(() => token.value.length > 0 && user.value !== null)

  function hydrate() {
    const rawState = localStorage.getItem(storageKey)
    if (!rawState) {
      return
    }

    const parsedState = JSON.parse(rawState) as Partial<AuthResponse>
    token.value = parsedState.accessToken ?? ''
    user.value = parsedState.user ?? null
  }

  function setAuth(response: AuthResponse) {
    token.value = response.accessToken
    user.value = response.user
    localStorage.setItem(storageKey, JSON.stringify(response))
  }

  function setTokenOnly(accessToken: string) {
    token.value = accessToken
    localStorage.setItem(storageKey, JSON.stringify({
      accessToken,
      expiresAtUtc: '',
      user: user.value,
    }))
  }

  function clear() {
    token.value = ''
    user.value = null
    localStorage.removeItem(storageKey)
  }

  return {
    clear,
    hydrate,
    isAuthenticated,
    setAuth,
    setTokenOnly,
    token,
    user,
  }
})
