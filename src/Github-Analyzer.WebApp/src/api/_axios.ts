import axios from 'axios';
import type { ApiVersion } from '../types/api';
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { showError, showSuccess } from '../lib/toast';

/**
 * Base URL configuration depending on environment
 */
export const baseURL =
  import.meta.env.MODE === 'development'
    ? import.meta.env.VITE_API_BASE_URL || 'http://localhost:5242' // Adjust default port if needed
    : import.meta.env.VITE_API_BASE_URL || '/api';

/**
 * Extended AxiosRequestConfig with custom prefix option
 */
export interface ApiRequestConfig extends AxiosRequestConfig 
{
  prefix?: string; // Override full prefix (e.g., '/api/v1')
  suppressToast?: boolean; // Skip global error toast for expected 404s
}

/**
 * A scoped API client locked to a specific API version.
 * Returned by `apiClient.withVersion()` and used internally by composables.
 * All requests through this client are automatically prefixed with `/api/v{version}`.
 */
export interface VersionedClient 
{
  /** The locked API version string, e.g. '1', '2' */
  readonly version: ApiVersion;

  /** The base URL of the API server */
  readonly baseURL: string;

  /** GET request with automatic version prefix */
  get<T = any>(url: string, config?: ApiRequestConfig): Promise<AxiosResponse<T>>;

  /** POST request with automatic version prefix */
  post<T = any>(url: string, data?: any, config?: ApiRequestConfig): Promise<AxiosResponse<T>>;

  /** PUT request with automatic version prefix */
  put<T = any>(url: string, data?: any, config?: ApiRequestConfig): Promise<AxiosResponse<T>>;

  /** DELETE request with automatic version prefix */
  delete<T = any>(url: string, config?: ApiRequestConfig): Promise<AxiosResponse<T>>;
}

/**
 * API client class to handle requests with or without Bearer token
 */
class ApiClient 
{
  private instance: AxiosInstance;
  private token: string | null = null;
  private defaultPrefix = '/api/v1';

  constructor(config?: AxiosRequestConfig) 
  {
    this.instance = axios.create({
      baseURL,
      headers: {
        'Content-Type': 'application/json',
        'ngrok-skip-browser-warning': 'true',
      },
      // By default, do not include credentials (cookies) in cross-origin requests.
      withCredentials: false,
      ...config,
    });

    // Attach interceptor to include Authorization header when token is set
    this.instance.interceptors.request.use(
      (reqConfig) => 
      {
        if (this.token) {
          reqConfig.headers = reqConfig.headers || {};
          reqConfig.headers['Authorization'] = `Bearer ${this.token}`;
        }
        return reqConfig;
      },
      (error) => 
      {
        console.error('Request Error:', error);
        return Promise.reject(error);
      },
    );

    // Add response interceptor for better error handling
    this.instance.interceptors.response.use(
      (response) => this.handleSuccessResponse(response),
      (error)    => this.handleResponseError(error),
    );
  }
  
  /**
   * Handle successful responses and show a toast for mutation endpoints when the API returns a success envelope.
   */
  private handleSuccessResponse<T>(response: AxiosResponse<T>): AxiosResponse<T>
  {
    // Determine if the request method is a mutation (POST, PUT, PATCH, DELETE)
    const method = response.config?.method?.toLowerCase();
    const isMutation = method === 'post'  || method === 'put'  || 
                       method === 'patch' || method === 'delete';

    const payload = response.data as 
      { success?: boolean; message?: unknown } | undefined;

    const message = payload?.message;

    // Show success toast if it's a mutation and the API indicates success with a message
    if (isMutation && payload?.success === true && 
        typeof message === 'string' && message.trim().length > 0) 
    {
      showSuccess({ message });
    }

    return response;
  }

  /**
   * Normalize Axios errors into a single message and optionally show a toast.
   */
  private handleResponseError(error: any): Promise<never>
  {
    const suppressToast = this.shouldSuppressErrorToast(error);
    const errorMessage = this.getErrorMessage(error);

    // Only show error toast if it's not a suppressed 404
    if (!suppressToast) 
    {
      showError({ message: errorMessage });
    }

    return Promise.reject(new Error(errorMessage));
  }

  /**
   * Determine if an error is a 404 that should suppress the error toast
   */
  private shouldSuppressErrorToast(error: any): boolean
  {
    const is404 = error.response?.status === 404;
    const isSuppressToast = (error.config as ApiRequestConfig | undefined)?.suppressToast === true;
    
    return is404 && isSuppressToast;
  }

  /**
   * Extract a user-friendly error message from an Axios error object
   */
  private getErrorMessage(error: any): string
  {
    if (error.response) 
    {
      // Handle 401 Unauthorized by clearing token and prompting re-login
      if (error.response.status === 401) 
      {
        this.clearToken();
        return 'Session expired. Please log in again.';
      }

      const responseData = error.response.data;

      // Try to extract a meaningful error message from the response data
      return (
        error.message ||
        responseData?.error   ||
        responseData?.message ||
        responseData?.title   ||
        error.response.statusText ||
        `HTTP ${error.response.status} Error`
      );
    }

    if (error.request) 
    {
      return 'Network Error - No response from server';
    }

    return error.message || 'Unknown Error';
  }


  /**
   * Set Bearer token for future requests
   * @param token - JWT or access token string
   */
  setToken(token: string) {
    this.token = token;
  }

  /**
   * Clear the stored token (unauthenticated mode)
   */
  clearToken() {
    this.token = null;
  }

  /**
   * Set default prefix (default is '/api/v1')
   * @param prefix - Prefix to prepend to all URLs (e.g., '/api/v1', '/api/v2', '')
   */
  setDefaultPrefix(prefix: string) {
    this.defaultPrefix = prefix;
  }

  /**
   * Set the default API version (default is '1')
   * @param version - Version string, e.g., '1', '2'
   */
  setDefaultApiVersion(version: ApiVersion) {
    this.defaultPrefix = `/api/v${version}`;
  }

  /**
   * Create a versioned client scoped to a specific API version.
   * All requests through the returned client are automatically prefixed with `/api/v{version}`.
   * This is intended to be used as an internal implementation detail inside composables.
   *
   * @param version - API version string, e.g. '1', '2'
   * @returns A VersionedClient with get/post/put/delete bound to the versioned prefix
   *
   * @example
   * // Inside a composable:
   * const client = apiClient.withVersion(version)
   * const response = await client.get('/projects') // → GET /api/v1/projects
   */
  withVersion(version: ApiVersion): VersionedClient 
  {
    const prefix = `/api/v${version}`;
    return {
      version,
      baseURL,
      get:    <T>(url: string, config?: AxiosRequestConfig)             => this.get<T>(url, { ...config, prefix }),
      post:   <T>(url: string, data?: any, config?: AxiosRequestConfig) => this.post<T>(url, data, { ...config, prefix }),
      put:    <T>(url: string, data?: any, config?: AxiosRequestConfig) => this.put<T>(url, data, { ...config, prefix }),
      delete: <T>(url: string, config?: AxiosRequestConfig)             => this.delete<T>(url, { ...config, prefix }),
    };
  }

  /**
   * Get the prefix for a request (use provided prefix or default)
   */
  private getPrefix(config?: ApiRequestConfig): string 
  {
    if (config && 'prefix' in config && config.prefix !== undefined) 
    { return config.prefix; }

    return this.defaultPrefix;
  }

  /**
   * Prepend prefix to URL if it doesn't already start with it
   */
  private prependPrefix(url: string, prefix: string): string 
  {
    if (!prefix || url.startsWith(prefix)) {
      return url;
    }

    // Ensure there's a slash between prefix and url if needed
    const normalizedPrefix = prefix.endsWith('/') ? prefix.slice(0, -1) : prefix;
    const normalizedUrl = url.startsWith('/') ? url : `/${url}`;

    return normalizedPrefix + normalizedUrl;
  }

  /**
   * Generic GET request
   * @param url - API endpoint URL
   * @param config - Axios config with optional 'prefix' property (default: '/api')
   */
  get<T = any>(url: string, config?: ApiRequestConfig): Promise<AxiosResponse<T>> 
  {
    const prefix = this.getPrefix(config);
    const prefixedUrl = this.prependPrefix(url, prefix);
    const { prefix: _, ...cleanConfig } = config || {};

    return this.instance.get<T>(prefixedUrl, cleanConfig);
  }

  /**
   * Generic POST request
   * @param url - API endpoint URL
   * @param data - Request payload
   * @param config - Axios config with optional 'prefix' property (default: '/api')
   */
  post<T = any>(url: string, data?: any, config?: ApiRequestConfig): Promise<AxiosResponse<T>> 
  {
    const prefix = this.getPrefix(config);
    const prefixedUrl = this.prependPrefix(url, prefix);
    const { prefix: _, ...cleanConfig } = config || {};

    return this.instance.post<T>(prefixedUrl, data, cleanConfig);
  }

  /**
   * Generic PUT request
   * @param url - API endpoint URL
   * @param data - Request payload
   * @param config - Axios config with optional 'prefix' property (default: '/api')
   */
  put<T = any>(url: string, data?: any, config?: ApiRequestConfig): Promise<AxiosResponse<T>> 
  {
    const prefix = this.getPrefix(config);
    const prefixedUrl = this.prependPrefix(url, prefix);
    const { prefix: _, ...cleanConfig } = config || {};

    return this.instance.put<T>(prefixedUrl, data, cleanConfig);
  }

  /**
   * Generic DELETE request
   * @param url - API endpoint URL
   * @param config - Axios config with optional 'prefix' property (default: '/api')
   */
  delete<T = any>(url: string, config?: ApiRequestConfig): Promise<AxiosResponse<T>> 
  {
    const prefix = this.getPrefix(config);
    const prefixedUrl = this.prependPrefix(url, prefix);
    const { prefix: _, ...cleanConfig } = config || {};

    return this.instance.delete<T>(prefixedUrl, cleanConfig);
  }
}

// Export a singleton instance
const apiClient = new ApiClient();

export default apiClient;
