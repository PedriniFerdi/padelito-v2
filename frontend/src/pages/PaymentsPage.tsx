import { useMemo, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { CreditCard, Plus } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { paymentsApi, type PaymentFilters } from '@/api/payments.api'
import { reservationsApi } from '@/api/reservations.api'
import { PaymentDialog } from '@/components/payments/PaymentDialog'
import { todayInClub } from '@/lib/dates'

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const input =
  'w-full rounded-xl border border-[#CBD5E1] bg-white px-3 py-2.5 text-sm outline-none focus:border-[#0F766E] focus:ring-2 focus:ring-[#99F6E4]'

export function PaymentsPage() {
  const client = useQueryClient()
  const [searchParams] = useSearchParams()
  const initialReservation = Number(searchParams.get('reservationId')) || undefined
  const today = todayInClub()
  const [filters, setFilters] = useState<PaymentFilters>({ dateFrom: today, dateTo: today, reservationId: initialReservation })
  const [draft, setDraft] = useState<PaymentFilters>({ dateFrom: today, dateTo: today, reservationId: initialReservation })
  const [open, setOpen] = useState(Boolean(initialReservation))
  const payments = useQuery({ queryKey: ['payments', filters], queryFn: () => paymentsApi.list(filters) })
  const methods = useQuery({ queryKey: ['payment-methods'], queryFn: paymentsApi.methods })
  const active = useQuery({ queryKey: ['reservations', 'active', 'payments'], queryFn: () => reservationsApi.list({ view: 'active' }) })
  const history = useQuery({ queryKey: ['reservations', 'history', 'payments'], queryFn: () => reservationsApi.list({ view: 'history' }) })
  const reservations = useMemo(
    () => [...(active.data ?? []), ...(history.data ?? [])].filter((reservation) => reservation.status !== 'Canceled'),
    [active.data, history.data],
  )
  const total = payments.data?.reduce((sum, payment) => sum + payment.amount, 0) ?? 0

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 text-sm font-bold text-[#0F766E]">
            <CreditCard className="size-4" /> Checkout
          </div>
          <h2 className="mt-2 text-3xl font-black">Payments</h2>
          <p className="mt-1 text-sm text-[#64748B]">Partial collections and open reservation balances.</p>
        </div>
        <button className="inline-flex items-center gap-2 rounded-xl bg-[#0F766E] px-4 py-2.5 text-sm font-bold text-white" onClick={() => setOpen(true)}>
          <Plus className="size-4" /> Record payment
        </button>
      </header>

      <section className="grid gap-4 sm:grid-cols-2">
        <article className="rounded-2xl border border-[#E2E8F0] bg-white p-5">
          <p className="text-sm font-semibold text-[#64748B]">Filtered total</p>
          <p className="mt-1 text-2xl font-black">{money.format(total)}</p>
        </article>
        <article className="rounded-2xl border border-[#E2E8F0] bg-white p-5">
          <p className="text-sm font-semibold text-[#64748B]">Payments found</p>
          <p className="mt-1 text-2xl font-black">{payments.data?.length ?? 0}</p>
        </article>
      </section>

      <form
        className="grid gap-3 rounded-2xl border border-[#E2E8F0] bg-white p-4 md:grid-cols-4"
        onSubmit={(event) => {
          event.preventDefault()
          setFilters(draft)
        }}
      >
        <label className="text-sm font-bold">
          Payment from
          <input className={`${input} mt-1`} onChange={(event) => setDraft({ ...draft, dateFrom: event.target.value || undefined })} type="date" value={draft.dateFrom ?? ''} />
        </label>
        <label className="text-sm font-bold">
          Payment to
          <input className={`${input} mt-1`} onChange={(event) => setDraft({ ...draft, dateTo: event.target.value || undefined })} type="date" value={draft.dateTo ?? ''} />
        </label>
        <label className="text-sm font-bold">
          Method
          <select className={`${input} mt-1`} onChange={(event) => setDraft({ ...draft, methodId: Number(event.target.value) || undefined })} value={draft.methodId ?? ''}>
            <option value="">All methods</option>
            {methods.data?.map((method) => (
              <option key={method.id} value={method.id}>
                {method.description}
              </option>
            ))}
          </select>
        </label>
        <button className="self-end rounded-xl border border-[#99F6E4] px-4 py-2.5 font-bold text-[#0F766E]">Apply filters</button>
      </form>

      <section className="overflow-hidden rounded-2xl border border-[#E2E8F0] bg-white">
        {payments.isLoading ? <p className="p-8 text-center text-sm text-[#64748B]">Loading payments...</p> : null}
        {payments.isError ? <p className="p-5 text-sm font-semibold text-red-700">{payments.error.message}</p> : null}
        {payments.isSuccess && payments.data.length === 0 ? <p className="p-10 text-center text-sm text-[#64748B]">No payments match the selected filters.</p> : null}
        {payments.isSuccess && payments.data.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-[#F8FAFC] text-xs uppercase text-[#64748B]">
                <tr>
                  <th className="px-4 py-3">Date</th>
                  <th className="px-4 py-3">Reservation</th>
                  <th className="px-4 py-3">Method</th>
                  <th className="px-4 py-3 text-right">Amount</th>
                  <th className="px-4 py-3 text-right">Balance</th>
                </tr>
              </thead>
              <tbody>
                {payments.data.map((payment) => (
                  <tr className="border-t border-[#F1F5F9]" key={payment.id}>
                    <td className="px-4 py-3">{new Date(payment.paymentDate).toLocaleString('en-US')}</td>
                    <td className="px-4 py-3">
                      <b>#{payment.reservationId} - {payment.clientName}</b>
                      <div className="text-xs text-[#64748B]">{payment.courtName}</div>
                    </td>
                    <td className="px-4 py-3">{payment.paymentMethod}</td>
                    <td className="px-4 py-3 text-right font-bold">{money.format(payment.amount)}</td>
                    <td className="px-4 py-3 text-right">{money.format(payment.pendingBalance)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </section>

      {open ? (
        <PaymentDialog
          initialReservationId={initialReservation}
          methods={methods.data ?? []}
          onClose={() => setOpen(false)}
          onSaved={() => {
            setOpen(false)
            client.invalidateQueries({ queryKey: ['payments'] })
            client.invalidateQueries({ queryKey: ['reservations'] })
            client.invalidateQueries({ queryKey: ['dashboard'] })
          }}
          reservations={reservations}
        />
      ) : null}
    </div>
  )
}
