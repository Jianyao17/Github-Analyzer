export interface ApiResponse<T> {
  success: boolean;
  message: string | null;
  data: T | null;
}

/**
 * Shared API version type.
 * Keeps autocomplete for common versions while remaining open for future versions.
 */
export type ApiVersion = '1' | '2' | (string & {});
