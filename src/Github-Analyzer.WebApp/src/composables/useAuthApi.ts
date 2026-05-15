import apiClient from '../api/axios';
import type { UserProfile } from '../stores/auth.store';

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

export const useAuthApi = () => 
{
  const login = async (payload: LoginPayload) => 
  {
    const response = await apiClient.post<string>('/auth/login', payload);
    return response.data;
  };

  const register = async (payload: RegisterPayload) => 
  {
    const response = await apiClient.post<string>('/auth/register', payload);
    return response.data;
  };

  const googleLogin = async (payload: GoogleLoginPayload) => 
  {
    const response = await apiClient.post<string>('/auth/google', payload);
    return response.data;
  };

  const getCurrentUser = async () => 
  {
    const response = await apiClient.get<UserProfile>('/auth/me');
    return response.data;
  };

  return {
    login,
    register,
    googleLogin,
    getCurrentUser,
  };
};
