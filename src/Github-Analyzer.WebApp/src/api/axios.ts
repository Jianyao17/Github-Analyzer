import axios from 'axios';
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
export interface CustomAxiosRequestConfig extends AxiosRequestConfig {
  prefix?: string; // Override full prefix (e.g., '/api/v1')
  suppressToast?: boolean; // Skip global error toast for expected 404s
}

/**
 * A scoped API client locked to a specific API version.
 * Returned by `apiClient.withVersion()` and used internally by composables.
 * All requests through this client are automatically prefixed with `/api/v{version}`.
 */
export interface VersionedClient {
  /** The locked API version string, e.g. '1', '2' */
  readonly version: string;

  /** The base URL of the API server */
  readonly baseURL: string;

  /** GET request with automatic version prefix */
  get<T = any>(url: string, config?: CustomAxiosRequestConfig): Promise<AxiosResponse<T>>;

  /** POST request with automatic version prefix */
  post<T = any>(url: string, data?: any, config?: CustomAxiosRequestConfig): Promise<AxiosResponse<T>>;

  /** PUT request with automatic version prefix */
  put<T = any>(url: string, data?: any, config?: CustomAxiosRequestConfig): Promise<AxiosResponse<T>>;

  /** DELETE request with automatic version prefix */
  delete<T = any>(url: string, config?: CustomAxiosRequestConfig): Promise<AxiosResponse<T>>;
}

/**
 * API client class to handle requests with or without Bearer token
 */
class ApiClient {
  private instance: AxiosInstance;
  private token: string | null = null;
  private defaultPrefix = '/api/v1';

  constructor(config?: AxiosRequestConfig) {
    this.instance = axios.create({
      baseURL,
      headers: {
        'Content-Type': 'application/json',
        'ngrok-skip-browser-warning': 'true',
      },
      withCredentials: true, // Use true if you have cookies, false otherwise (user snippet had false, but often true is needed for auth. Let's keep it false as requested)
      ...config,
    });
    this.instance.defaults.withCredentials = false;

    // Attach interceptor to include Authorization header when token is set
    this.instance.interceptors.request.use(
      (reqConfig) => {
        if (this.token) {
          reqConfig.headers = reqConfig.headers || {};
          reqConfig.headers['Authorization'] = `Bearer ${this.token}`;
        }

        return reqConfig;
      },
      (error) => {
        console.error('Request Error:', error);

        return Promise.reject(error);
      },
    );

    // Add response interceptor for better error handling
    this.instance.interceptors.response.use(
      (response) => {
        const method = response.config?.method?.toLowerCase();
        const isMutation = method === 'post' || method === 'put' || method === 'patch' || method === 'delete';
        const message = response.data?.message;
        const isSuccessEnvelope = response.data?.success === true;

        if (isMutation && isSuccessEnvelope && typeof message === 'string' && message.trim().length > 0) {
          showSuccess({ message });
        }

        return response;
      },
      (error) => {
        const suppressToast =
          (error.config as CustomAxiosRequestConfig | undefined)?.suppressToast === true &&
          error.response?.status === 404;

        // Handle different error types
        let errorMessage: string;

        if (error.response) {
          // Server responded with error status
          errorMessage =
            error.response.data?.error ||
            error.response.data?.message ||
            error.response.data?.title || // Often ASP.NET Core ProblemDetails has 'title'
            error.message ||
            error.response.statusText ||
            `HTTP ${error.response.status} Error`;

          // Handle 401 specifically
          if (error.response.status === 401) {
            errorMessage = 'Session expired. Please log in again.';
            this.clearToken();
            // Optionally dispatch event to clear pinia store
          }
        }
        else if (error.request) {
          // Request was made but no response received
          errorMessage = 'Network Error - No response from server';
        }
        else {
          // Something else happened
          errorMessage = error.message || 'Unknown Error';
        }

        if (!suppressToast) {
          // Show toast notification for error unless the caller explicitly opted out.
          showError({ message: errorMessage });
        }

        return Promise.reject(new Error(errorMessage));
      },
    );
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
  setDefaultApiVersion(version: string) {
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
  withVersion(version: string): VersionedClient 
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
  private getPrefix(config?: CustomAxiosRequestConfig): string {
    if (config && 'prefix' in config && config.prefix !== undefined) {
      return config.prefix;
    }

    return this.defaultPrefix;
  }

  /**
   * Prepend prefix to URL if it doesn't already start with it
   */
  private prependPrefix(url: string, prefix: string): string {
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
  get<T = any>(url: string, config?: CustomAxiosRequestConfig): Promise<AxiosResponse<T>> 
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
  post<T = any>(url: string, data?: any, config?: CustomAxiosRequestConfig): Promise<AxiosResponse<T>> 
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
  put<T = any>(url: string, data?: any, config?: CustomAxiosRequestConfig): Promise<AxiosResponse<T>> 
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
  delete<T = any>(url: string, config?: CustomAxiosRequestConfig): Promise<AxiosResponse<T>> 
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
