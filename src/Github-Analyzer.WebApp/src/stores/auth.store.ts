import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import apiClient from '../api/_axios';

export interface UserProfile 
{
  id: string
  displayName: string
  avatarUrl: string | null
  username: string
  email: string
}

export const useAuthStore = defineStore('auth', () => 
{
  const token = ref<string>(localStorage.getItem('auth_token') || '');
  const user = ref<UserProfile | null>(null);

  const initialized = ref(false);
  const loading = ref(false);

  // Initialize token in apiClient if it exists from localStorage
  if (token.value) {
    apiClient.setToken(token.value);
  }

  const isAuthenticated = computed(() => !!token.value && !!user.value);

  const initials = computed(() => 
  {
    // Ambil nama pengguna dari displayName atau username, lalu ambil inisialnya
    const source = user.value?.displayName || user.value?.username || 'GA';
    return source
      .split(' ')
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('');
  });

  function setToken(accessToken: string) 
  {
    token.value = accessToken;
    localStorage.setItem('auth_token', accessToken);
    apiClient.setToken(accessToken);
  }

  function setUser(userProfile: UserProfile | null) {
    user.value = userProfile;
  }

  function setInitialized(value: boolean) {
    initialized.value = value;
  }

  function setLoading(value: boolean) {
    loading.value = value;
  }

  function clearAuth() 
  {
    token.value = '';
    user.value = null;
    localStorage.removeItem('auth_token');
    apiClient.clearToken();
  }

  return {
    token,
    user,
    loading,
    initialized,
    isAuthenticated,
    initials,
    setToken,
    setUser,
    setInitialized,
    setLoading,
    clearAuth
  };
});
