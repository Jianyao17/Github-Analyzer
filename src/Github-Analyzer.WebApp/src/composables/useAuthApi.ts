import apiClient, { baseURL } from '../api/axios';
import { useAuthStore } from '../stores/auth.store';
import type { UserProfile } from '../stores/auth.store';
import type { ApiResponse } from '../types/api-response';

export interface LoginPayload {
  email?: string;
  password?: string;
}

export interface RegisterPayload {
  email?: string;
  password?: string;
  username?: string;
}

export interface GoogleLoginPayload {
  idToken: string;
}

export interface LoginResponse {
  accessToken: string;
}

export interface RegisterResponse {
  id: string;
  email: string;
  username: string;
}

export interface VerifyEmailPayload {
  userId: string;
  token: string;
}

export interface ForgotPasswordPayload {
  email: string;
}

export interface ResetPasswordPayload {
  email: string;
  token: string;
  newPassword: string;
}

/**
 * Composable untuk Auth API.
 * @param version - Versi API yang digunakan, e.g. '1', '2'. Default: '1'.
 *
 * @example
 * const { login, register } = useAuthApi()      // menggunakan v1
 * const { login }           = useAuthApi('2')    // menggunakan v2
 */
export const useAuthApi = (version = '1') => 
{
  const client = apiClient.withVersion(version);
  const authStore = useAuthStore();

  const getCurrentUser = async () => 
  {
    const response = await client.get<ApiResponse<UserProfile>>('/auth/me');
    return response.data.data;
  };

  const logout = () => 
  {
    authStore.clearAuth();
  };

  const loadCurrentUser = async () => 
  {
    authStore.setLoading(true);
    try 
    {
      const data = await getCurrentUser();
      authStore.setUser(data);
      return data;
    } 
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    catch (error) 
    {
      logout();
      return null;
    } 
    finally 
    {
      authStore.setLoading(false);
    }
  };

  const initialize = async () => 
  {
    if (authStore.initialized) return;
    
    if (authStore.token) 
    {
      await loadCurrentUser();
    }
    
    authStore.setInitialized(true);
  };

  const login = async (payload: LoginPayload) => 
  {
    const response = await client.post<ApiResponse<LoginResponse>>('/auth/login', payload);
    const data = response.data.data;
    if (!data?.accessToken)
    {
      throw new Error('Missing access token from login response.');
    }
    
    authStore.setToken(data.accessToken);

    await loadCurrentUser();
    return data.accessToken;
  };

  const register = async (payload: RegisterPayload) => 
  {
    const response = await client.post<ApiResponse<RegisterResponse>>('/auth/register', payload);
    return response.data.data;
  };

  const isGoogleAuthEnabled = async () => 
  {
    const response = await client.get<ApiResponse<{ IsEnabled: boolean }>>('/auth/google/isEnabled');
    const data = response.data.data as unknown;

    if (typeof data === 'boolean') return data;
    if (data && typeof data === 'object')
    {
      const payload = data as { IsEnabled?: boolean; isEnabled?: boolean };
      return payload.IsEnabled ?? payload.isEnabled ?? false;
    }

    return false;
  }

  const googleAuth = async (returnPath?: string) => 
  {
    // Redirect the user to the backend endpoint that initiates 
    // the Google OAuth flow with the optional returnPath as a query parameter.
    window.location.href = baseURL + '/api/v1/auth/google' + 
      (returnPath ? `?returnPath=${encodeURIComponent(returnPath)}` : ''); 
  };

  const verifyEmail = async (payload: VerifyEmailPayload) => 
  {
    const response = await client.post<ApiResponse<string>>('/auth/verify-email', payload);
    return response.data.data;
  };

  const forgotPassword = async (payload: ForgotPasswordPayload) => 
  {
    const response = await client.post<ApiResponse<string>>('/auth/forgot-password', payload);
    return response.data.data;
  };

  const resetPassword = async (payload: ResetPasswordPayload) => 
  {
    const response = await client.post<ApiResponse<string>>('/auth/reset-password', payload);
    return response.data.data;
  };

  return {
    login,
    register,
    logout,
    initialize,
    loadCurrentUser,
    googleAuth,
    isGoogleAuthEnabled,
    getCurrentUser,
    verifyEmail,
    forgotPassword,
    resetPassword,
  };
};
