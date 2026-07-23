import { apiFetch } from './http'
import type { AuthResponse, CurrentUser, LoginRequest } from '@/types/api'

export function login(request: LoginRequest) {
  return apiFetch<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export function fetchCurrentUser() {
  return apiFetch<CurrentUser>('/api/auth/me')
}

export function logout() {
  return apiFetch<void>('/api/auth/logout', { method: 'POST' })
}
