import { apiFetch } from './http'
import type { DashboardRevenueIntelligence, DashboardSummary } from '@/types/api'

export type RevenueIntelligenceFilters = {
  dateFrom?: string
  dateTo?: string
}

function query(filters: RevenueIntelligenceFilters) {
  const params = new URLSearchParams()
  if (filters.dateFrom) params.set('dateFrom', filters.dateFrom)
  if (filters.dateTo) params.set('dateTo', filters.dateTo)
  const value = params.toString()
  return value ? `?${value}` : ''
}

export const dashboardApi = {
  summary: () => apiFetch<DashboardSummary>('/api/dashboard/summary'),
  revenueIntelligence: (filters: RevenueIntelligenceFilters = {}) =>
    apiFetch<DashboardRevenueIntelligence>(`/api/dashboard/revenue-intelligence${query(filters)}`),
}
