import axios from 'axios';
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import toastManager from '../lib/toast';

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
  prefix?: string; // Default is '/api', set to '' to disable
}

/**
 * API client class to handle requests with or without Bearer token
 */
class ApiClient 
{
  private instance: AxiosInstance;
  private token: string | null = null;
  private defaultPrefix = '/api';

  constructor(config?: AxiosRequestConfig) 
  {
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
      (reqConfig) => 
      {
        if (this.token) 
        {
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
      (response) => 
        response,
      (error) => 
      {
        // Handle different error types
        let errorMessage: string;

        if (error.response) 
        {
          // Server responded with error status
          errorMessage =
            error.response.data?.error ||
            error.response.data?.message ||
            error.response.data?.title || // Often ASP.NET Core ProblemDetails has 'title'
            error.message ||
            error.response.statusText ||
            `HTTP ${error.response.status} Error`;
            
          // Handle 401 specifically
          if (error.response.status === 401) 
          {
            errorMessage = 'Session expired. Please log in again.';
            this.clearToken();
            // Optionally dispatch event to clear pinia store
          }
        }
        else if (error.request) 
        {
          // Request was made but no response received
          errorMessage = 'Network Error - No response from server';
        }
        else 
        {
          // Something else happened
          errorMessage = error.message || 'Unknown Error';
        }

        // Show toast notification for error
        toastManager.showError(errorMessage);

        return Promise.reject(new Error(errorMessage));
      },
    );
  }

  /**
   * Set Bearer token for future requests
   * @param token - JWT or access token string
   */
  setToken(token: string) 
  {
    this.token = token;
  }

  /**
   * Clear the stored token (unauthenticated mode)
   */
  clearToken() 
  {
    this.token = null;
  }

  /**
   * Set default prefix (default is '/api')
   * @param prefix - Prefix to prepend to all URLs (e.g., '/api', '/v1', '')
   */
  setDefaultPrefix(prefix: string) 
  {
    this.defaultPrefix = prefix;
  }

  /**
   * Get the prefix for a request (use provided prefix or default)
   */
  private getPrefix(config?: CustomAxiosRequestConfig): string 
  {
    if (config && 'prefix' in config) 
    {
      return config.prefix ?? '';
    }

    return this.defaultPrefix;
  }

  /**
   * Prepend prefix to URL if it doesn't already start with it
   */
  private prependPrefix(url: string, prefix: string): string 
  {
    if (!prefix || url.startsWith(prefix)) 
    {
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
