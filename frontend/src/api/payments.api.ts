import { apiFetch } from './http'
import type { Payment, PaymentMethod } from '@/types/api'

export type PaymentFilters = { dateFrom?: string; dateTo?: string; methodId?: number; reservationId?: number }
export type PaymentPayload = { reservationId: number; paymentMethodId: number; amount: number; paymentDate: string; note?: string | null }

function queryString(values: PaymentFilters) {
  const params = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => value !== undefined && value !== '' && params.set(key, String(value)))
  return params.toString()
}

export const paymentsApi = {
  list: (filters: PaymentFilters) => apiFetch<Payment[]>(`/api/payments?${queryString(filters)}`),
  create: (payload: PaymentPayload) => apiFetch<Payment>('/api/payments', { method: 'POST', body: JSON.stringify(payload) }),
  methods: () => apiFetch<PaymentMethod[]>('/api/catalogs/payment-methods'),
}
