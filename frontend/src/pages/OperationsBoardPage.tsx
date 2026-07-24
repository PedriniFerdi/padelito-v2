import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Banknote, CalendarClock, Check, CircleX, CreditCard, Flag, RefreshCw, TimerReset } from 'lucide-react'
import { ApiRequestError } from '@/api/http'
import { paymentsApi } from '@/api/payments.api'
import { reservationsApi } from '@/api/reservations.api'
import { PaymentDialog } from '@/components/payments/PaymentDialog'
import type { OperationsReservation, Reservation, ReservationStatus } from '@/types/api'

const statusIds = {
  Pending: 1,
  Confirmed: 2,
  Canceled: 3,
  Completed: 4,
} as const

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

function errorMessage(error: unknown) {
  return error instanceof ApiRequestError ? error.message : 'The operation could not be completed.'
}

export function OperationsBoardPage() {
  const queryClient = useQueryClient()
  const [paymentReservationId, setPaymentReservationId] = useState<number>()
  const [actionError, setActionError] = useState<string>()
  const board = useQuery({ queryKey: ['operations-board'], queryFn: reservationsApi.operationsBoard, refetchInterval: 60000 })
  const methods = useQuery({ queryKey: ['payment-methods'], queryFn: paymentsApi.methods })

  const paymentReservations = useMemo(
    () => (board.data?.timelineByCourt.flatMap((court) => court.reservations).filter((reservation) => reservation.status !== 'Canceled').map(toReservation) ?? []),
    [board.data],
  )

  const changeStatus = useMutation({
    mutationFn: ({ id, nextStatusId }: { id: number; nextStatusId: number }) => reservationsApi.changeStatus(id, nextStatusId),
    onSuccess: async () => {
      setActionError(undefined)
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['operations-board'] }),
        queryClient.invalidateQueries({ queryKey: ['reservations'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ])
    },
    onError: (error) => setActionError(errorMessage(error)),
  })

  if (board.isLoading) {
    return <OperationsSkeleton />
  }

  if (board.isError) {
    return <ErrorBanner message={errorMessage(board.error)} />
  }

  const data = board.data!
  return (
    <section className="space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-4 pt-1">
        <div>
          <p className="flex items-center gap-2 text-sm font-bold text-[#6fe0b2]">
            <CalendarClock className="size-4" />
            Daily operations
          </p>
          <h2 className="font-display mt-1 text-[26px] font-black leading-8 text-white">Today's operations</h2>
          <p className="mt-1 text-sm font-medium text-[#9fa6b1]">
            {formatDate(data.operationalDate)} - updated {formatTime(data.generatedAt)}
          </p>
        </div>
        <button
          className="inline-flex h-10 items-center gap-2 rounded-lg border border-[#3e4943] bg-[#171f33] px-3 text-sm font-bold text-[#dae2fd] transition hover:border-[#6fe0b2] hover:text-white disabled:opacity-60"
          disabled={board.isFetching}
          onClick={() => board.refetch()}
          type="button"
        >
          <RefreshCw className={`size-4 ${board.isFetching ? 'animate-spin' : ''}`} />
          Refresh
        </button>
      </header>

      {actionError ? <ErrorBanner message={actionError} /> : null}

      <section className="grid gap-3 md:grid-cols-4">
        <MetricCard icon={CalendarClock} label="Today's reservations" value={data.reservationsToday} />
        <MetricCard icon={TimerReset} label="Starting soon" value={data.startingSoonCount} />
        <MetricCard icon={Banknote} label="Awaiting payment" value={data.upcomingUnpaidCount} />
        <MetricCard icon={Flag} label="Completed" value={data.completedCount} />
      </section>

      <section className="grid gap-5 xl:grid-cols-[minmax(0,1.4fr)_minmax(330px,.6fr)]">
        <Timeline
          disabled={changeStatus.isPending}
          items={data.timelineByCourt}
          onChangeStatus={(id, nextStatusId) => changeStatus.mutate({ id, nextStatusId })}
          onPay={setPaymentReservationId}
        />
        <div className="space-y-5">
          <ActionList
            disabled={changeStatus.isPending}
            empty="No reservations are starting in the next hour."
            items={data.startingSoonReservations}
            onChangeStatus={(id, nextStatusId) => changeStatus.mutate({ id, nextStatusId })}
            onPay={setPaymentReservationId}
            title="Starting soon"
          />
          <ActionList
            disabled={changeStatus.isPending}
            empty="No outstanding balances for today's reservations."
            items={data.upcomingUnpaidReservations}
            onChangeStatus={(id, nextStatusId) => changeStatus.mutate({ id, nextStatusId })}
            onPay={setPaymentReservationId}
            title="Awaiting payment"
          />
        </div>
      </section>

      {paymentReservationId ? (
        <PaymentDialog
          initialReservationId={paymentReservationId}
          methods={methods.data ?? []}
          onClose={() => setPaymentReservationId(undefined)}
          onSaved={() => {
            setPaymentReservationId(undefined)
            queryClient.invalidateQueries({ queryKey: ['operations-board'] })
            queryClient.invalidateQueries({ queryKey: ['payments'] })
            queryClient.invalidateQueries({ queryKey: ['reservations'] })
            queryClient.invalidateQueries({ queryKey: ['dashboard'] })
          }}
          reservations={paymentReservations}
        />
      ) : null}
    </section>
  )
}

function Timeline({
  items,
  disabled,
  onChangeStatus,
  onPay,
}: {
  items: { courtId: number; courtName: string; reservations: OperationsReservation[] }[]
  disabled: boolean
  onChangeStatus: (id: number, statusId: number) => void
  onPay: (id: number) => void
}) {
  return (
    <section className="overflow-hidden rounded-lg border border-[#3e4943] bg-[#171f33]/90">
      <div className="border-b border-white/[0.08] px-4 py-3">
        <h3 className="text-sm font-bold text-[#f1f4f5]">Timeline by court</h3>
      </div>
      {items.length === 0 ? (
        <EmptyState text="No reservations yet today." />
      ) : (
        <div className="divide-y divide-white/[0.08]">
          {items.map((court) => (
            <div className="grid gap-3 p-4 lg:grid-cols-[150px_minmax(0,1fr)]" key={court.courtId}>
              <div>
                <p className="text-sm font-black text-white">{court.courtName}</p>
                <p className="mt-1 text-xs font-medium text-[#9fa6b1]">
                  {court.reservations.length} {court.reservations.length === 1 ? 'reservation' : 'reservations'}
                </p>
              </div>
              <div className="grid gap-2">
                {court.reservations.map((reservation) => (
                  <ReservationRow disabled={disabled} key={reservation.id} onChangeStatus={onChangeStatus} onPay={onPay} reservation={reservation} />
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}

function ActionList({
  title,
  items,
  empty,
  disabled,
  onChangeStatus,
  onPay,
}: {
  title: string
  items: OperationsReservation[]
  empty: string
  disabled: boolean
  onChangeStatus: (id: number, statusId: number) => void
  onPay: (id: number) => void
}) {
  return (
    <section className="rounded-lg border border-[#3e4943] bg-[#171f33]/90">
      <h3 className="border-b border-white/[0.08] px-4 py-3 text-sm font-bold text-[#f1f4f5]">{title}</h3>
      {items.length === 0 ? (
        <EmptyState text={empty} />
      ) : (
        <div className="space-y-2 p-3">
          {items.map((reservation) => (
            <ReservationCard disabled={disabled} key={reservation.id} onChangeStatus={onChangeStatus} onPay={onPay} reservation={reservation} />
          ))}
        </div>
      )}
    </section>
  )
}

function ReservationRow({
  reservation,
  disabled,
  onChangeStatus,
  onPay,
}: {
  reservation: OperationsReservation
  disabled: boolean
  onChangeStatus: (id: number, statusId: number) => void
  onPay: (id: number) => void
}) {
  return (
    <article className="grid gap-3 rounded-lg border border-white/[0.07] bg-[#10192d] p-3 lg:grid-cols-[96px_minmax(0,1fr)_auto] lg:items-center">
      <div>
        <p className="font-display text-lg font-black text-white">{timeRange(reservation)}</p>
        <StatusBadge status={reservation.status} />
      </div>
      <div className="min-w-0">
        <p className="truncate text-sm font-bold text-[#edf0f2]">{reservation.clientName}</p>
        <p className="mt-1 text-xs font-semibold text-[#9fa6b1]">
          {money.format(reservation.finalPrice)} - balance {money.format(reservation.pendingBalance)}
        </p>
      </div>
      <QuickActions disabled={disabled} onChangeStatus={onChangeStatus} onPay={onPay} reservation={reservation} />
    </article>
  )
}

function ReservationCard({
  reservation,
  disabled,
  onChangeStatus,
  onPay,
}: {
  reservation: OperationsReservation
  disabled: boolean
  onChangeStatus: (id: number, statusId: number) => void
  onPay: (id: number) => void
}) {
  return (
    <article className="rounded-lg bg-white/[0.045] p-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate text-sm font-black text-white">{reservation.clientName}</p>
          <p className="mt-1 text-xs font-semibold text-[#9fa6b1]">
            {reservation.courtName} - {timeRange(reservation)}
          </p>
        </div>
        <StatusBadge status={reservation.status} />
      </div>
      <div className="mt-3 flex items-center justify-between gap-3 text-xs font-semibold text-[#dae2fd]">
        <span>{reservation.paymentStatus}</span>
        <span className={reservation.pendingBalance > 0 ? 'text-[#ffd166]' : 'text-[#6fe0b2]'}>
          {money.format(reservation.pendingBalance)}
        </span>
      </div>
      <div className="mt-3">
        <QuickActions disabled={disabled} onChangeStatus={onChangeStatus} onPay={onPay} reservation={reservation} />
      </div>
    </article>
  )
}

function QuickActions({
  reservation,
  disabled,
  onChangeStatus,
  onPay,
}: {
  reservation: OperationsReservation
  disabled: boolean
  onChangeStatus: (id: number, statusId: number) => void
  onPay: (id: number) => void
}) {
  return (
    <div className="flex flex-wrap justify-end gap-2">
      {reservation.pendingBalance > 0 && reservation.status !== 'Canceled' ? (
        <ActionButton icon={CreditCard} label="Collect" onClick={() => onPay(reservation.id)} />
      ) : null}
      {reservation.status === 'Pending' ? (
        <ActionButton disabled={disabled} icon={Check} label="Check-in" onClick={() => onChangeStatus(reservation.id, statusIds.Confirmed)} />
      ) : null}
      {reservation.status === 'Confirmed' ? (
        <ActionButton disabled={disabled} icon={Flag} label="Complete" onClick={() => onChangeStatus(reservation.id, statusIds.Completed)} />
      ) : null}
      {reservation.status === 'Pending' || reservation.status === 'Confirmed' ? (
        <ActionButton destructive disabled={disabled} icon={CircleX} label="Cancel" onClick={() => onChangeStatus(reservation.id, statusIds.Canceled)} />
      ) : null}
    </div>
  )
}

function ActionButton({
  icon: Icon,
  label,
  onClick,
  disabled = false,
  destructive = false,
}: {
  icon: typeof CreditCard
  label: string
  onClick: () => void
  disabled?: boolean
  destructive?: boolean
}) {
  return (
    <button
      className={`inline-flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-bold transition active:translate-y-px disabled:cursor-not-tolowed disabled:opacity-50 ${
        destructive ? 'border-[#ffb4ab]/40 text-[#ffb4ab] hover:bg-[#93000a]/25' : 'border-[#6fe0b2]/35 text-[#6fe0b2] hover:bg-[#003824]/60'
      }`}
      disabled={disabled}
      onClick={onClick}
      title={label}
      type="button"
    >
      <Icon className="size-3.5" />
      {label}
    </button>
  )
}

function MetricCard({ icon: Icon, label, value }: { icon: typeof CalendarClock; label: string; value: number }) {
  return (
    <article className="flex h-[76px] items-center justify-between rounded-lg border border-[#3e4943] bg-[#171f33]/90 px-4">
      <div>
        <p className="text-sm font-medium text-[#aab2bd]">{label}</p>
        <p className="font-display mt-1 text-2xl font-black text-white">{value}</p>
      </div>
      <span className="grid size-10 place-items-center rounded-lg bg-[#003824] text-[#6fe0b2]">
        <Icon className="size-5" />
      </span>
    </article>
  )
}

function StatusBadge({ status }: { status: ReservationStatus }) {
  const styles: Record<ReservationStatus, string> = {
    Pending: 'bg-[#3c2f12] text-[#ffd166] ring-[#ffd166]/25',
    Confirmed: 'bg-[#003824] text-[#6fe0b2] ring-[#6fe0b2]/25',
    Canceled: 'bg-[#4a1d20] text-[#ffb4ab] ring-[#ffb4ab]/25',
    Completed: 'bg-white/[0.07] text-[#c7ced8] ring-white/[0.12]',
  }
  return <span className={`mt-1 inline-flex rounded-md px-2 py-0.5 text-xs font-bold ring-1 ring-inset ${styles[status]}`}>{status}</span>
}

function ErrorBanner({ message }: { message: string }) {
  return <div className="rounded-lg border border-[#ffb4ab]/30 bg-[#93000a]/25 p-4 text-sm font-semibold text-[#ffdad6]">{message}</div>
}

function EmptyState({ text }: { text: string }) {
  return <p className="px-4 py-8 text-center text-sm font-semibold text-[#9fa6b1]">{text}</p>
}

function OperationsSkeleton() {
  return (
    <div className="space-y-5">
      <div className="h-16 animate-pulse rounded-lg bg-white/[0.06]" />
      <div className="grid gap-3 md:grid-cols-4">
        {[0, 1, 2, 3].map((item) => (
          <div className="h-[76px] animate-pulse rounded-lg bg-white/[0.06]" key={item} />
        ))}
      </div>
      <div className="h-96 animate-pulse rounded-lg bg-white/[0.06]" />
    </div>
  )
}

function timeRange(reservation: OperationsReservation) {
  return `${reservation.startTime.slice(0, 5)}-${reservation.endTime.slice(0, 5)}`
}

function formatDate(value: string) {
  return new Date(`${value}T12:00:00`).toLocaleDateString('en-US')
}

function formatTime(value: string) {
  return new Date(value).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
}

function toReservation(reservation: OperationsReservation): Reservation {
  return {
    id: reservation.id,
    reservationDate: reservation.reservationDate,
    clientId: reservation.clientId,
    clientName: reservation.clientName,
    availableTurnId: reservation.availableTurnId,
    courtName: reservation.courtName,
    startTime: reservation.startTime,
    endTime: reservation.endTime,
    reservationStatusId: reservation.reservationStatusId,
    status: reservation.status,
    promotionName: null,
    basePrice: reservation.finalPrice,
    finalPrice: reservation.finalPrice,
    createdAt: `${reservation.reservationDate}T00:00:00`,
  }
}
