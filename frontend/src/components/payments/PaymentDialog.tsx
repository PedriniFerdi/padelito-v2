import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { X } from 'lucide-react'
import { paymentsApi } from '@/api/payments.api'
import { reservationsApi } from '@/api/reservations.api'
import { paymentSchema, toFieldErrors, type FieldErrors } from '@/lib/validation'
import type { PaymentMethod, Reservation } from '@/types/api'

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const input =
  'w-full rounded-xl border border-[#CBD5E1] bg-white px-3 py-2.5 text-sm outline-none focus:border-[#0F766E] focus:ring-2 focus:ring-[#99F6E4]'

type PaymentDialogProps = {
  initialReservationId?: number
  methods: PaymentMethod[]
  reservations: Reservation[]
  onClose: () => void
  onSaved: () => void
}

export function PaymentDialog({ reservations, methods, initialReservationId, onClose, onSaved }: PaymentDialogProps) {
  const [reservationId, setReservationId] = useState(initialReservationId ?? 0)
  const [methodId, setMethodId] = useState(0)
  const [amount, setAmount] = useState('')
  const [note, setNote] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const detail = useQuery({
    queryKey: ['reservation-detail', reservationId],
    queryFn: () => reservationsApi.detail(reservationId),
    enabled: reservationId > 0,
  })
  const mutation = useMutation({ mutationFn: paymentsApi.create, onSuccess: onSaved })

  function submit(event: FormEvent) {
    event.preventDefault()
    const parsed = paymentSchema.safeParse({
      reservationId,
      paymentMethodId: methodId,
      amount: amount === '' ? Number.NaN : Number(amount),
      pendingBalance: detail.data?.pendingBalance ?? 0,
      note,
    })
    if (!parsed.success) {
      setFieldErrors(toFieldErrors(parsed.error))
      return
    }
    setFieldErrors({})
    mutation.mutate({
      reservationId: parsed.data.reservationId,
      paymentMethodId: parsed.data.paymentMethodId,
      amount: parsed.data.amount,
      paymentDate: new Date().toISOString(),
      note: parsed.data.note || null,
    })
  }

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-slate-950/45 p-4">
      <form className="w-full max-w-lg rounded-2xl bg-white p-5 shadow-2xl" onSubmit={submit}>
        <div className="flex items-center justify-between">
          <h3 className="text-xl font-black text-[#0F172A]">Record payment</h3>
          <button
            aria-label="Close"
            className="rounded-lg p-2 text-[#475569] transition hover:bg-[#F1F5F9]"
            onClick={onClose}
            type="button"
          >
            <X className="size-5" />
          </button>
        </div>
        <div className="mt-5 space-y-4">
          <label className="block text-sm font-bold text-[#334155]">
            Reservation
            <select
              className={`${input} mt-1`}
              onChange={(event) => setReservationId(Number(event.target.value))}
              required
              value={reservationId || ''}
            >
              <option value="">Select</option>
              {reservations.map((reservation) => (
                <option key={reservation.id} value={reservation.id}>
                  #{reservation.id} - {reservation.clientName} - {reservation.courtName} - {money.format(reservation.finalPrice)}
                </option>
              ))}
            </select>
            {fieldErrors.reservationId ? <span className="mt-1 block text-xs text-red-600">{fieldErrors.reservationId}</span> : null}
          </label>
          {detail.data ? (
            <div className="grid grid-cols-3 gap-2 rounded-xl bg-[#F8FAFC] p-3 text-center text-xs text-[#475569]">
              <div>
                Price<b className="block text-sm text-[#0F172A]">{money.format(detail.data.finalPrice)}</b>
              </div>
              <div>
                Paid<b className="block text-sm text-[#0F172A]">{money.format(detail.data.totalPaid)}</b>
              </div>
              <div>
                Balance<b className="block text-sm text-[#0F766E]">{money.format(detail.data.pendingBalance)}</b>
              </div>
            </div>
          ) : null}
          <label className="block text-sm font-bold text-[#334155]">
            Method
            <select
              className={`${input} mt-1`}
              onChange={(event) => setMethodId(Number(event.target.value))}
              required
              value={methodId || ''}
            >
              <option value="">Select</option>
              {methods.map((method) => (
                <option key={method.id} value={method.id}>
                  {method.description}
                </option>
              ))}
            </select>
            {fieldErrors.paymentMethodId ? <span className="mt-1 block text-xs text-red-600">{fieldErrors.paymentMethodId}</span> : null}
          </label>
          <label className="block text-sm font-bold text-[#334155]">
            Amount
            <input
              className={`${input} mt-1`}
              max={detail.data?.pendingBalance}
              min="0.01"
              onChange={(event) => setAmount(event.target.value)}
              required
              step="0.01"
              type="number"
              value={amount}
            />
            {fieldErrors.amount ? <span className="mt-1 block text-xs text-red-600">{fieldErrors.amount}</span> : null}
          </label>
          <label className="block text-sm font-bold text-[#334155]">
            Note
            <textarea className={`${input} mt-1`} maxLength={255} onChange={(event) => setNote(event.target.value)} rows={2} value={note} />
            {fieldErrors.note ? <span className="mt-1 block text-xs text-red-600">{fieldErrors.note}</span> : null}
          </label>
          {mutation.isError ? <p className="rounded-lg bg-red-50 p-3 text-sm font-semibold text-red-700">{mutation.error.message}</p> : null}
        </div>
        <div className="mt-5 flex justify-end gap-3">
          <button className="rounded-xl border px-4 py-2 text-sm font-bold text-[#334155]" onClick={onClose} type="button">
            Cancel
          </button>
          <button
            className="rounded-xl bg-[#0F766E] px-4 py-2 text-sm font-bold text-white disabled:opacity-50"
            disabled={mutation.isPending || !reservationId || !methodId || !amount || !detail.data}
            type="submit"
          >
            Save payment
          </button>
        </div>
      </form>
    </div>
  )
}
