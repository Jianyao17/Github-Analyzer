import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { useAuthApi } from '../composables/useAuthApi'
import type { LoginPayload, RegisterPayload } from '../composables/useAuthApi'
import apiClient from '../api/axios'

export interface UserProfile {
  id: string
  email: string
  username: string
}

export const useAuthStore = defineStore('auth', () => 
{
  const token = ref<string>(localStorage.getItem('auth_token') || '')
  const user = ref<UserProfile | null>(null)
  const initialized = ref(false)
  const loading = ref(false)
  
  const authApi = useAuthApi()

  // Initialize token in apiClient if it exists from localStorage
  if (token.value) {
    apiClient.setToken(token.value)
  }

  const isAuthenticated = computed(() => !!token.value && !!user.value)
  
  const initials = computed(() => 
  {
    if (!user.value?.username) return 'GA'
    return user.value.username
      .split(' ')
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('')
  })

  function setAuth(accessToken: string) 
  {
    token.value = accessToken
    localStorage.setItem('auth_token', accessToken)
    apiClient.setToken(accessToken)
  }

  async function initialize() 
  {
    if (initialized.value) return
    
    if (token.value) 
    {
      await loadCurrentUser()
    }
    
    initialized.value = true
  }

  async function loadCurrentUser() 
  {
    loading.value = true
    try 
    {
      const data = await authApi.getCurrentUser()
      user.value = data
      return data
    } 
    catch (error) 
    {
      logout()
      return null
    } 
    finally 
    {
      loading.value = false
    }
  }

  async function login(payload: LoginPayload) 
  {
    const accessToken = await authApi.login(payload)
    setAuth(accessToken)
    await loadCurrentUser()
    return accessToken
  }

  async function register(payload: RegisterPayload) 
  {
    const accessToken = await authApi.register(payload)
    setAuth(accessToken)
    await loadCurrentUser()
    return accessToken
  }

  function logout() 
  {
    token.value = ''
    user.value = null
    localStorage.removeItem('auth_token')
    apiClient.clearToken()
  }

  return {
    token,
    user,
    loading,
    initialized,
    isAuthenticated,
    initials,
    initialize,
    login,
    register,
    logout,
    setAuth,
    loadCurrentUser
  }
})
