import { useAuthStore } from '../stores/auth.store';
import type { ApiVersion } from '../types/_api/api';
import type {
  LoginPayload,
  RegisterPayload,
  LoginResponse,
  RegisterResponse,
  VerifyEmailPayload,
  ForgotPasswordPayload,
  ResetPasswordPayload,
} from '../types/_api/auth';

import {
  loginApi,
  registerApi,
  verifyEmailApi,
  getCurrentUserApi,
  isGoogleAuthEnabledApi,
  getGoogleAuthRedirectUrl,
  forgotPasswordApi,
  resetPasswordApi,
} from '../api/auth.api';

/**
 * Composable untuk Auth API.
 * @param version - Versi API yang digunakan, e.g. '1', '2'. Default: '1'.
 *
 * @example
 * const { login, register } = useAuthApi()      // menggunakan v1
 * const { login }           = useAuthApi('2')    // menggunakan v2
 */
export const useAuthApi = (version: ApiVersion = '1') => 
{
  // Version is forwarded to API helpers below.
  const authStore = useAuthStore();

  const getCurrentUser = async () => 
  {
    const response = await getCurrentUserApi(version);
    return response.data;
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
    const response = await loginApi(payload, version);
    const data = response.data as LoginResponse | undefined;
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
    const response = await registerApi(payload, version);
    return response.data as RegisterResponse;
  };

  const isGoogleAuthEnabled = async () => 
  {
    const response = await isGoogleAuthEnabledApi(version);
    const data = response.data as unknown;

    if (typeof data === 'boolean') return data;
    if (data && typeof data === 'object') 
    {
      const payload = data as { IsEnabled?: boolean; isEnabled?: boolean };
      return payload.IsEnabled ?? payload.isEnabled ?? false;
    }

    return false;
  };

  const googleAuth = async (returnPath?: string) => 
  {
    // Redirect the user to the backend endpoint that initiates 
    // the Google OAuth flow with the optional returnPath as a query parameter.
    window.location.href = getGoogleAuthRedirectUrl(returnPath, version);
  };

  const verifyEmail = async (payload: VerifyEmailPayload) => 
  {
    const response = await verifyEmailApi(payload, version);
    return response.data;
  };

  const forgotPassword = async (payload: ForgotPasswordPayload) => 
  {
    const response = await forgotPasswordApi(payload, version);
    return response.data;
  };

  const resetPassword = async (payload: ResetPasswordPayload) => 
  {
    const response = await resetPasswordApi(payload, version);
    return response.data;
  };

  return {
    login,
    register,
    logout,
    googleAuth,
    initialize,
    loadCurrentUser,
    isGoogleAuthEnabled,
    getCurrentUser,
    verifyEmail,
    forgotPassword,
    resetPassword,
  };
};
