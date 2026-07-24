import { apiFetch } from './http'
import type { OperationsBoard, Reservation, ReservationAvailability, ReservationDetail } from '@/types/api'

export type ReservationView = 'active' | 'history'

export type ReservationFilters = {
  view: ReservationView
  dateFrom?: string
  dateTo?: string
  statusId?: number
}

export type ReservationCreatePayload = {
  clientId: number
  availableTurnId: number
  promotionId?: number | null
  reservationDate: string
  reservationStatusId: number
}

function queryString(values: Record<string, string | number | undefined>) {
  const params = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== '') params.set(key, String(value))
  })
  return params.toString()
}

export const reservationsApi = {
  list: (filters: ReservationFilters) =>
    apiFetch<Reservation[]>(`/api/reservations?${queryString(filters)}`),
  availability: (date: string) =>
    apiFetch<ReservationAvailability[]>(`/api/reservations/availability?date=${encodeURIComponent(date)}`),
  operationsBoard: () => apiFetch<OperationsBoard>('/api/reservations/operations-board'),
  detail: (id: number) => apiFetch<ReservationDetail>(`/api/reservations/${id}`),
  create: (payload: ReservationCreatePayload) =>
    apiFetch<ReservationDetail>('/api/reservations', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  changeStatus: (id: number, reservationStatusId: number) =>
    apiFetch<ReservationDetail>(`/api/reservations/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ reservationStatusId }),
    }),
}
