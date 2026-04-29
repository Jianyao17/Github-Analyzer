import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { apiBaseUrl, apiRequest } from '../lib/api'

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

export interface AuthOptions {
  googleEnabled: boolean
}

interface PersistedAuthState {
  accessToken: string
  expiresAtUtc: string
}

const storageKey = 'github-analyzer.auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref('')
  const expiresAtUtc = ref('')
  const user = ref<UserProfile | null>(null)
  const authOptions = ref<AuthOptions>({ googleEnabled: false })
  const initialized = ref(false)
  const loadingProfile = ref(false)

  const isAuthenticated = computed(() => token.value.length > 0 && user.value !== null)
  const initials = computed(() => {
    if (!user.value?.displayName) {
      return 'GA'
    }

    return user.value.displayName
      .split(' ')
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('')
  })

  function hydrate() {
    const rawState = localStorage.getItem(storageKey)
    if (!rawState) {
      return
    }

    const parsedState = JSON.parse(rawState) as Partial<PersistedAuthState>
    token.value = parsedState.accessToken ?? ''
    expiresAtUtc.value = parsedState.expiresAtUtc ?? ''
  }

  function persist() {
    if (!token.value) {
      localStorage.removeItem(storageKey)
      return
    }

    localStorage.setItem(storageKey, JSON.stringify({
      accessToken: token.value,
      expiresAtUtc: expiresAtUtc.value,
    } satisfies PersistedAuthState))
  }

  async function loadAuthOptions() {
    try {
      authOptions.value = await apiRequest<AuthOptions>('/api/auth/options')
    } catch {
      authOptions.value = { googleEnabled: false }
    }
  }

  async function loadCurrentUser() {
    if (!token.value) {
      user.value = null
      return null
    }

    loadingProfile.value = true

    try {
      const profile = await apiRequest<UserProfile>('/api/auth/me', {
        headers: {
          Authorization: `Bearer ${token.value}`,
        },
      })

      user.value = profile
      return profile
    } catch {
      clear()
      return null
    } finally {
      loadingProfile.value = false
    }
  }

  async function initialize() {
    if (initialized.value) {
      return
    }

    hydrate()
    await Promise.all([
      loadAuthOptions(),
      token.value ? loadCurrentUser() : Promise.resolve(null),
    ])

    initialized.value = true
  }

  function setAuth(response: AuthResponse) {
    token.value = response.accessToken
    expiresAtUtc.value = response.expiresAtUtc
    user.value = response.user
    persist()
  }

  async function register(payload: {
    email: string
    password: string
    displayName: string
  }) {
    const response = await apiRequest<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    })

    setAuth(response)
    return response
  }

  async function login(payload: {
    email: string
    password: string
  }) {
    const response = await apiRequest<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    })

    setAuth(response)
    return response
  }

  async function completeGoogleLogin(accessToken: string) {
    token.value = accessToken
    expiresAtUtc.value = ''
    persist()
    await loadCurrentUser()
  }

  function startGoogleLogin() {
    window.location.href = `${apiBaseUrl}/api/auth/google/login`
  }

  function clear() {
    token.value = ''
    expiresAtUtc.value = ''
    user.value = null
    persist()
  }

  return {
    authOptions,
    clear,
    completeGoogleLogin,
    expiresAtUtc,
    initialize,
    initials,
    isAuthenticated,
    loadAuthOptions,
    loadCurrentUser,
    loadingProfile,
    login,
    register,
    startGoogleLogin,
    token,
    user,
  }
})
