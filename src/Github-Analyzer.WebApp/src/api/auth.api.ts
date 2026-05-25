import apiClient, { baseURL } from './_axios';
import type { ApiResponse, ApiVersion } from '../types/api';
import type { UserProfile } from '../stores/auth.store';
import type {
  LoginPayload,
  RegisterPayload,
  LoginResponse,
  RegisterResponse,
  VerifyEmailPayload,
  ForgotPasswordPayload,
  ResetPasswordPayload,
} from '../types/auth';

/**
 * Auth API helpers: lightweight wrappers around `apiClient`.
 * Each function returns the API envelope (`ApiResponse<T>`) so callers
 * can decide whether to access `.data`, `.message`, etc.
 */
export async function getCurrentUserApi(version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .get<ApiResponse<UserProfile>>('/auth/me')
    .then(res => res.data);
}

export async function loginApi(payload: LoginPayload, version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .post<ApiResponse<LoginResponse>>('/auth/login', payload)
    .then(res => res.data);
}

export async function registerApi(payload: RegisterPayload, version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .post<ApiResponse<RegisterResponse>>('/auth/register', payload)
    .then(res => res.data);
}

export async function isGoogleAuthEnabledApi(version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .get<ApiResponse<{ IsEnabled: boolean }>>('/auth/google/isEnabled')
    .then(res => res.data);
}

export function getGoogleAuthRedirectUrl(returnPath?: string, version: ApiVersion = '1') 
{
  const base = baseURL.replace(/\/$/, '');

  return `${base}/api/v${version}/auth/google` + 
    (returnPath ? `?returnPath=${encodeURIComponent(returnPath)}` : '');
}

export async function verifyEmailApi(payload: VerifyEmailPayload, version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .post<ApiResponse<string>>('/auth/verify-email', payload)
    .then(res => res.data);
}

export async function forgotPasswordApi(payload: ForgotPasswordPayload, version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .post<ApiResponse<string>>('/auth/forgot-password', payload)
    .then(res => res.data);
}

export async function resetPasswordApi(payload: ResetPasswordPayload, version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .post<ApiResponse<string>>('/auth/reset-password', payload)
    .then(res => res.data);
}
