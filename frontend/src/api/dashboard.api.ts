import { apiFetch } from './http'
import type { DashboardSummary } from '@/types/api'

export const dashboardApi = { summary: () => apiFetch<DashboardSummary>('/api/dashboard/summary') }
