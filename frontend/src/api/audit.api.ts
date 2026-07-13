import { apiFetch } from '@/api/http'
import type { ReservationAudit } from '@/types/api'

export type AuditFilters = { dateFrom?: string; dateTo?: string; reservationId?: number; action?: string; username?: string }

export const auditApi = {
  list: (filters: AuditFilters) => {
    const params = new URLSearchParams()
    Object.entries(filters).forEach(([key, value]) => { if (value) params.set(key, String(value)) })
    return apiFetch<ReservationAudit[]>(`/api/audit/reservations?${params}`)
  },
}
