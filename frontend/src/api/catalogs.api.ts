import { apiFetch } from './http'
import type {
  AvailableTurn,
  Client,
  Court,
  CourtType,
  Employee,
  Promotion,
  RoleCatalog,
  UserCatalog,
} from '@/types/api'

export type PersonPayload = {
  firstName: string
  lastName: string
  dni: string
  phone: string
  email: string
}

export type UserCreatePayload = {
  username: string
  password: string
  employeeId: number
  roleId: number
}

export type UserUpdatePayload = {
  username: string
  roleId: number
}

export type CourtPayload = {
  name: string
  courtTypeId: number
  hourPrice: number
}

export type TurnPayload = {
  courtId: number
  startTime: string
  endTime: string
}

export type PromotionPayload = {
  name: string
  description?: string | null
  discountPercentage: number
  dateFrom: string
  dateTo: string
}

function send<TResponse>(path: string, method: string, body?: unknown) {
  return apiFetch<TResponse>(path, {
    method,
    body: body ? JSON.stringify(body) : undefined,
  })
}

export const clientsApi = {
  list: () => apiFetch<Client[]>('/api/clients'),
  create: (payload: PersonPayload) => send<Client>('/api/clients', 'POST', payload),
  update: (id: number, payload: PersonPayload) => send<Client>(`/api/clients/${id}`, 'PUT', payload),
  activate: (id: number) => send<void>(`/api/clients/${id}/activate`, 'PATCH'),
  deactivate: (id: number) => send<void>(`/api/clients/${id}/deactivate`, 'PATCH'),
}

export const employeesApi = {
  list: () => apiFetch<Employee[]>('/api/employees', { cache: 'no-store' }),
  create: (payload: PersonPayload) => send<Employee>('/api/employees', 'POST', payload),
  update: (id: number, payload: PersonPayload) => send<Employee>(`/api/employees/${id}`, 'PUT', payload),
  activate: (id: number) => send<void>(`/api/employees/${id}/activate`, 'PATCH'),
  deactivate: (id: number) => send<void>(`/api/employees/${id}/deactivate`, 'PATCH'),
}

export const usersApi = {
  list: () => apiFetch<UserCatalog[]>('/api/users'),
  create: (payload: UserCreatePayload) => send<UserCatalog>('/api/users', 'POST', payload),
  update: (id: number, payload: UserUpdatePayload) => send<UserCatalog>(`/api/users/${id}`, 'PUT', payload),
  changePassword: (id: number, password: string) => send<void>(`/api/users/${id}/change-password`, 'PATCH', { password }),
  activate: (id: number) => send<void>(`/api/users/${id}/activate`, 'PATCH'),
  deactivate: (id: number) => send<void>(`/api/users/${id}/deactivate`, 'PATCH'),
}

export const rolesApi = {
  list: () => apiFetch<RoleCatalog[]>('/api/catalogs/roles'),
}

export const courtTypesApi = {
  list: () => apiFetch<CourtType[]>('/api/court-types'),
  create: (description: string) => send<CourtType>('/api/court-types', 'POST', { description }),
  update: (id: number, description: string) => send<CourtType>(`/api/court-types/${id}`, 'PUT', { description }),
}

export const courtsApi = {
  list: () => apiFetch<Court[]>('/api/courts'),
  create: (payload: CourtPayload) => send<Court>('/api/courts', 'POST', payload),
  update: (id: number, payload: CourtPayload) => send<Court>(`/api/courts/${id}`, 'PUT', payload),
  activate: (id: number) => send<void>(`/api/courts/${id}/activate`, 'PATCH'),
  deactivate: (id: number) => send<void>(`/api/courts/${id}/deactivate`, 'PATCH'),
}

export const turnsApi = {
  list: () => apiFetch<AvailableTurn[]>('/api/available-turns'),
  create: (payload: TurnPayload) => send<AvailableTurn>('/api/available-turns', 'POST', payload),
  update: (id: number, payload: TurnPayload) => send<AvailableTurn>(`/api/available-turns/${id}`, 'PUT', payload),
  activate: (id: number) => send<void>(`/api/available-turns/${id}/activate`, 'PATCH'),
  deactivate: (id: number) => send<void>(`/api/available-turns/${id}/deactivate`, 'PATCH'),
}

export const promotionsApi = {
  list: () => apiFetch<Promotion[]>('/api/promotions'),
  create: (payload: PromotionPayload) => send<Promotion>('/api/promotions', 'POST', payload),
  update: (id: number, payload: PromotionPayload) => send<Promotion>(`/api/promotions/${id}`, 'PUT', payload),
  activate: (id: number) => send<void>(`/api/promotions/${id}/activate`, 'PATCH'),
  deactivate: (id: number) => send<void>(`/api/promotions/${id}/deactivate`, 'PATCH'),
}
