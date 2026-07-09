const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5211'

let getAuthToken: (() => string | null) | null = null
let handleUnauthorized: (() => void) | null = null

export class ApiRequestError extends Error {
  readonly statusCode: number

  constructor(message: string, statusCode: number) {
    super(message)
    this.statusCode = statusCode
  }
}

export function setAuthTokenProvider(provider: () => string | null) {
  getAuthToken = provider
}

export function setUnauthorizedHandler(handler: () => void) {
  handleUnauthorized = handler
}

export async function apiFetch<TResponse>(
  path: string,
  init?: RequestInit,
): Promise<TResponse> {
  const token = getAuthToken?.()
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  })

  if (!response.ok) {
    if (response.status === 401) {
      handleUnauthorized?.()
    }

    throw new ApiRequestError(await getErrorMessage(response), response.status)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  return (await response.json()) as TResponse
}

async function getErrorMessage(response: Response) {
  const fallback = `La API respondio con estado ${response.status}.`

  try {
    const data = (await response.json()) as { message?: string; title?: string }
    return data.message ?? data.title ?? fallback
  } catch {
    return fallback
  }
}
