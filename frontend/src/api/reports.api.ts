import { apiDownload, apiFetch } from '@/api/http'
import type { ReservationReport } from '@/types/api'

export type ReportFilters = { dateFrom?: string; dateTo?: string; statusId?: number }

function query(filters: ReportFilters) {
  const params = new URLSearchParams()
  if (filters.dateFrom) params.set('dateFrom', filters.dateFrom)
  if (filters.dateTo) params.set('dateTo', filters.dateTo)
  if (filters.statusId) params.set('statusId', String(filters.statusId))
  return params.toString()
}

export const reportsApi = {
  reservations: (filters: ReportFilters) => apiFetch<ReservationReport>(`/api/reports/reservations?${query(filters)}`),
  exportReservations: (filters: ReportFilters) => apiDownload(`/api/reports/reservations/export?${query(filters)}`),
}
