import apiClient from '../api/axios';
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

  const login = async (payload: LoginPayload) => 
  {
    const response = await client.post<ApiResponse<LoginResponse>>('/auth/login', payload);
    return response.data.data;
  };

  const register = async (payload: RegisterPayload) => 
  {
    const response = await client.post<ApiResponse<RegisterResponse>>('/auth/register', payload);
    return response.data.data;
  };

  const googleLogin = async (payload: GoogleLoginPayload) => 
  {
    const response = await client.post<ApiResponse<string>>('/auth/google', payload);
    return response.data.data;
  };

  const getCurrentUser = async () => 
  {
    const response = await client.get<ApiResponse<UserProfile>>('/auth/me');
    return response.data.data;
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
    googleLogin,
    getCurrentUser,
    verifyEmail,
    forgotPassword,
    resetPassword,
  };
};
