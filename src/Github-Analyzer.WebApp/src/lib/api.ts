const fallbackApiUrl = 'http://localhost:8080'

export interface ValidationProblemDetails {
  title?: string
  detail?: string
  message?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  status: number
  payload?: ValidationProblemDetails | Record<string, unknown>

  constructor(message: string, status: number, payload?: ValidationProblemDetails | Record<string, unknown>) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.payload = payload
  }
}

export const apiBaseUrl =
  (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ??
  fallbackApiUrl

function normalizeHeaders(headers: HeadersInit | undefined) {
  if (!headers) {
    return {} as Record<string, string>
  }

  if (headers instanceof Headers) {
    return Object.fromEntries(headers.entries())
  }

  if (Array.isArray(headers)) {
    return Object.fromEntries(headers)
  }

  return headers
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      ...(init.body ? { 'Content-Type': 'application/json' } : {}),
      ...normalizeHeaders(init.headers),
    },
  })

  if (!response.ok) {
    const contentType = response.headers.get('content-type') ?? ''
    if (contentType.includes('application/json')) {
      const payload = await response.json() as ValidationProblemDetails
      const message = payload.detail ?? payload.message ?? payload.title ?? `Request failed with status ${response.status}`

      throw new ApiError(message, response.status, payload)
    }

    const message = await response.text()
    throw new ApiError(message || `Request failed with status ${response.status}`, response.status)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}
