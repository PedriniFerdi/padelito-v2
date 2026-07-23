import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CalendarCheck, Check, CircleX, CreditCard, Flag, Plus, X } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ApiRequestError } from '@/api/http'
import {
  reservationsApi,
  type ReservationCreatePayload,
  type ReservationView,
} from '@/api/reservations.api'
import { clientsApi, promotionsApi } from '@/api/catalogs.api'
import type { Promotion, Reservation, ReservationAvailability, ReservationStatus } from '@/types/api'
import { reservationSchema, toFieldErrors, type FieldErrors } from '@/lib/validation'
import { todayInClub } from '@/lib/dates'

const statusIds = {
  Pendiente: 1,
  Confirmada: 2,
  Cancelada: 3,
  Finalizada: 4,
} as const

const money = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })
const dateFormatter = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'short', year: 'numeric' })

function errorMessage(error: unknown) {
  return error instanceof ApiRequestError ? error.message : 'No se pudo completar la operación.'
}

export function ReservationsPage() {
  const queryClient = useQueryClient()
  const [view, setView] = useState<ReservationView>('active')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [statusId, setStatusId] = useState<number | undefined>()
  const [showCreate, setShowCreate] = useState(false)
  const [actionError, setActionError] = useState<string>()

  useEffect(() => setStatusId(undefined), [view])

  const filters = { view, dateFrom, dateTo, statusId }
  const reservations = useQuery({
    queryKey: ['reservations', filters],
    queryFn: () => reservationsApi.list(filters),
  })

  const changeStatus = useMutation({
    mutationFn: ({ id, nextStatusId }: { id: number; nextStatusId: number }) =>
      reservationsApi.changeStatus(id, nextStatusId),
    onSuccess: async () => {
      setActionError(undefined)
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['reservations'] }),
        queryClient.invalidateQueries({ queryKey: ['reservation-availability'] }),
      ])
    },
    onError: (error) => setActionError(errorMessage(error)),
  })

  return (
    <section className="space-y-5">
      <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <p className="text-sm font-bold text-[#0F766E]">Operación diaria</p>
          <h3 className="mt-1 text-3xl font-bold tracking-tight text-[#0F172A]">Reservas</h3>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-[#475569]">
            Consultá la agenda, asigná turnos disponibles y seguí cada reserva hasta su cierre.
          </p>
        </div>
        <button
          className="inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-xl bg-[#0F766E] px-4 py-2.5 text-sm font-bold text-white shadow-[0_10px_24px_rgba(15,118,110,0.2)] transition hover:bg-[#115E59] active:translate-y-px"
          onClick={() => { setShowCreate(true); setActionError(undefined) }}
          type="button"
        >
          <Plus className="size-4" strokeWidth={2} />
          Nueva reserva
        </button>
      </header>

      {showCreate ? (
        <CreateReservationPanel
          onClose={() => setShowCreate(false)}
          onCreated={async () => {
            setShowCreate(false)
            setView('active')
            await Promise.all([
              queryClient.invalidateQueries({ queryKey: ['reservations'] }),
              queryClient.invalidateQueries({ queryKey: ['reservation-availability'] }),
            ])
          }}
        />
      ) : null}

      <div className="overflow-hidden rounded-2xl border border-[#E2E8F0] bg-white shadow-[0_18px_45px_rgba(15,23,42,0.06)]">
        <div className="border-b border-[#E2E8F0] px-4 pt-4">
          <div className="flex gap-1" role="tablist" aria-label="Vista de reservas">
            <Tab active={view === 'active'} label="Vigentes" onClick={() => setView('active')} />
            <Tab active={view === 'history'} label="Historial" onClick={() => setView('history')} />
          </div>
        </div>

        <div className="grid gap-3 border-b border-[#E2E8F0] bg-[#F8FAFC] p-4 sm:grid-cols-2 lg:grid-cols-3">
          <Field label="Desde">
            <input className={inputClass} onChange={(event) => setDateFrom(event.target.value)} type="date" value={dateFrom} />
          </Field>
          <Field label="Hasta">
            <input className={inputClass} onChange={(event) => setDateTo(event.target.value)} type="date" value={dateTo} />
          </Field>
          <Field label="Estado">
            <select className={inputClass} onChange={(event) => setStatusId(event.target.value ? Number(event.target.value) : undefined)} value={statusId ?? ''}>
              <option value="">Todos</option>
              {(view === 'active' ? ['Pendiente', 'Confirmada'] : ['Cancelada', 'Finalizada']).map((status) => (
                <option key={status} value={statusIds[status as ReservationStatus]}>{status}</option>
              ))}
            </select>
          </Field>
        </div>

        {actionError ? <ErrorBanner message={actionError} /> : null}
        {reservations.isLoading ? <ReservationSkeleton /> : null}
        {reservations.isError ? <ErrorBanner message={errorMessage(reservations.error)} /> : null}
        {reservations.isSuccess && reservations.data.length === 0 ? <EmptyState view={view} onCreate={() => setShowCreate(true)} /> : null}
        {reservations.isSuccess && reservations.data.length > 0 ? (
          <ReservationsTable
            isChanging={changeStatus.isPending}
            items={reservations.data}
            onChangeStatus={(id, nextStatusId) => changeStatus.mutate({ id, nextStatusId })}
          />
        ) : null}
      </div>
    </section>
  )
}

function CreateReservationPanel({ onClose, onCreated }: { onClose: () => void; onCreated: () => void | Promise<void> }) {
  const today = todayInClub()
  const [date, setDate] = useState(today)
  const [clientId, setClientId] = useState('')
  const [turnId, setTurnId] = useState('')
  const [promotionId, setPromotionId] = useState('')
  const [initialStatusId, setInitialStatusId] = useState(String(statusIds.Pendiente))
  const [formError, setFormError] = useState<string>()
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const clients = useQuery({ queryKey: ['clients'], queryFn: clientsApi.list })
  const promotions = useQuery({ queryKey: ['promotions'], queryFn: promotionsApi.list })
  const availability = useQuery({
    queryKey: ['reservation-availability', date],
    queryFn: () => reservationsApi.availability(date),
    enabled: Boolean(date),
  })

  useEffect(() => {
    setTurnId('')
    setPromotionId('')
  }, [date])

  const eligiblePromotions = useMemo(
    () => (promotions.data ?? []).filter((promotion) => promotion.isActive && promotion.dateFrom <= date && promotion.dateTo >= date),
    [date, promotions.data],
  )
  const selectedTurn = availability.data?.find((turn) => turn.availableTurnId === Number(turnId))
  const selectedPromotion = eligiblePromotions.find((promotion) => promotion.id === Number(promotionId))
  const finalPrice = calculatePreview(selectedTurn, selectedPromotion)

  const create = useMutation({
    mutationFn: (payload: ReservationCreatePayload) => reservationsApi.create(payload),
    onSuccess: onCreated,
    onError: (error) => setFormError(errorMessage(error)),
  })

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    setFormError(undefined)
    const parsed = reservationSchema.safeParse({
      clientId: Number(clientId),
      availableTurnId: Number(turnId),
      promotionId: promotionId ? Number(promotionId) : null,
      reservationDate: date,
      reservationStatusId: Number(initialStatusId),
    })
    if (!parsed.success) {
      setFieldErrors(toFieldErrors(parsed.error))
      return
    }
    setFieldErrors({})
    create.mutate(parsed.data)
  }

  return (
    <div className="rounded-2xl border border-[#99F6E4] bg-white shadow-[0_20px_50px_rgba(15,118,110,0.10)]">
      <div className="flex items-start justify-between gap-4 border-b border-[#CCFBF1] px-5 py-4">
        <div>
          <h4 className="text-lg font-bold text-[#0F172A]">Nueva reserva</h4>
          <p className="mt-1 text-sm text-[#475569]">El precio definitivo se valida y guarda en el servidor.</p>
        </div>
        <button aria-label="Cerrar formulario" className="rounded-lg p-2 text-[#475569] transition hover:bg-[#F1F5F9] hover:text-[#0F172A] active:translate-y-px" onClick={onClose} type="button">
          <X className="size-5" />
        </button>
      </div>

      <form className="grid gap-5 p-5 lg:grid-cols-[minmax(0,1fr)_280px]" onSubmit={submit}>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field error={fieldErrors.reservationDate} label="Fecha">
            <input className={inputClass} min={today} onChange={(event) => setDate(event.target.value)} required type="date" value={date} />
          </Field>
          <Field error={fieldErrors.clientId} label="Cliente">
            <select className={inputClass} disabled={clients.isLoading} onChange={(event) => setClientId(event.target.value)} required value={clientId}>
              <option value="">Seleccionar cliente</option>
              {(clients.data ?? []).filter((client) => client.isActive).map((client) => <option key={client.id} value={client.id}>{client.lastName}, {client.firstName}</option>)}
            </select>
          </Field>
          <Field error={fieldErrors.availableTurnId} label="Cancha y horario">
            <select className={inputClass} disabled={availability.isLoading || availability.isError} onChange={(event) => setTurnId(event.target.value)} required value={turnId}>
              <option value="">{availability.isLoading ? 'Buscando turnos...' : 'Seleccionar turno'}</option>
              {(availability.data ?? []).map((turn) => <option key={turn.availableTurnId} value={turn.availableTurnId}>{turn.startTime.slice(0, 5)} - {turn.endTime.slice(0, 5)} | {turn.courtName} ({money.format(turn.basePrice)})</option>)}
            </select>
            {availability.isSuccess && availability.data.length === 0 ? <p className="text-xs font-medium text-[#B45309]">No hay turnos disponibles para esta fecha.</p> : null}
            {availability.isError ? <p className="text-xs font-medium text-[#B91C1C]">{errorMessage(availability.error)}</p> : null}
          </Field>
          <Field error={fieldErrors.promotionId} label="Promoción">
            <select className={inputClass} onChange={(event) => setPromotionId(event.target.value)} value={promotionId}>
              <option value="">Sin promoción</option>
              {eligiblePromotions.map((promotion) => <option key={promotion.id} value={promotion.id}>{promotion.name} ({promotion.discountPercentage}%)</option>)}
            </select>
          </Field>
          <Field error={fieldErrors.reservationStatusId} label="Estado inicial">
            <select className={inputClass} onChange={(event) => setInitialStatusId(event.target.value)} value={initialStatusId}>
              <option value={statusIds.Pendiente}>Pendiente</option>
              <option value={statusIds.Confirmada}>Confirmada</option>
            </select>
          </Field>
          <div className="flex items-end justify-end gap-2 sm:col-span-2">
            <button className={secondaryButtonClass} onClick={onClose} type="button">Cancelar</button>
            <button className={primaryButtonClass} disabled={create.isPending || !clientId || !turnId} type="submit">{create.isPending ? 'Guardando...' : 'Crear reserva'}</button>
          </div>
          {formError ? <div className="sm:col-span-2"><ErrorBanner message={formError} /></div> : null}
        </div>

        <aside className="rounded-xl bg-[#F0FDFA] p-4 ring-1 ring-[#CCFBF1]">
          <p className="text-xs font-bold uppercase tracking-wide text-[#0F766E]">Resumen de precio</p>
          <dl className="mt-4 space-y-3 text-sm">
            <PriceRow label="Precio base" value={selectedTurn ? money.format(selectedTurn.basePrice) : '-'} />
            <PriceRow label="Descuento" value={selectedPromotion ? `${selectedPromotion.discountPercentage}%` : 'Sin descuento'} />
            <div className="border-t border-[#99F6E4] pt-3">
              <PriceRow emphasis label="Total estimado" value={selectedTurn ? money.format(finalPrice) : '-'} />
            </div>
          </dl>
        </aside>
      </form>
    </div>
  )
}

function ReservationsTable({ items, isChanging, onChangeStatus }: { items: Reservation[]; isChanging: boolean; onChangeStatus: (id: number, statusId: number) => void }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[920px] text-left text-sm">
        <thead className="bg-white text-xs font-bold uppercase tracking-wide text-[#64748B]">
          <tr><th className="px-4 py-3">Fecha y turno</th><th className="px-4 py-3">Cliente</th><th className="px-4 py-3">Cancha</th><th className="px-4 py-3">Estado</th><th className="px-4 py-3">Promoción</th><th className="px-4 py-3 text-right">Precio</th><th className="px-4 py-3 text-right">Acciones</th></tr>
        </thead>
        <tbody className="divide-y divide-[#E2E8F0]">
          {items.map((item) => (
            <tr className="transition hover:bg-[#F8FAFC]" key={item.id}>
              <td className="px-4 py-4"><p className="font-bold text-[#0F172A]">{dateFormatter.format(new Date(`${item.reservationDate}T12:00:00`))}</p><p className="mt-1 text-xs text-[#64748B]">{item.startTime.slice(0, 5)} - {item.endTime.slice(0, 5)}</p></td>
              <td className="px-4 py-4 font-semibold text-[#334155]">{item.clientName}</td>
              <td className="px-4 py-4 text-[#475569]">{item.courtName}</td>
              <td className="px-4 py-4"><StatusBadge status={item.status} /></td>
              <td className="px-4 py-4 text-[#475569]">{item.promotionName ?? 'Sin promoción'}</td>
              <td className="px-4 py-4 text-right"><p className="font-bold text-[#0F172A]">{money.format(item.finalPrice)}</p>{item.finalPrice !== item.basePrice ? <p className="mt-1 text-xs text-[#64748B] line-through">{money.format(item.basePrice)}</p> : null}</td>
              <td className="px-4 py-4"><StatusActions disabled={isChanging} item={item} onChange={onChangeStatus} /></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function StatusActions({ item, disabled, onChange }: { item: Reservation; disabled: boolean; onChange: (id: number, statusId: number) => void }) {
  return <div className="flex justify-end gap-2">
    {item.status !== 'Cancelada' && <Link className="inline-flex items-center gap-1.5 whitespace-nowrap rounded-lg border border-[#C4B5FD] px-2.5 py-2 text-xs font-bold text-[#6D28D9] hover:bg-[#F5F3FF]" to={`/payments?reservationId=${item.id}`}><CreditCard className="size-3.5"/>Cobrar</Link>}
    {item.status === 'Pendiente' && <ActionButton icon={Check} label="Confirmar" disabled={disabled} onClick={() => onChange(item.id, statusIds.Confirmada)} />}
    {item.status === 'Confirmada' && <ActionButton icon={Flag} label="Finalizar" disabled={disabled} onClick={() => onChange(item.id, statusIds.Finalizada)} />}
    {(item.status === 'Pendiente' || item.status === 'Confirmada') && <ActionButton destructive icon={CircleX} label="Cancelar" disabled={disabled} onClick={() => onChange(item.id, statusIds.Cancelada)} />}
  </div>
}

function ActionButton({ icon: Icon, label, onClick, disabled, destructive = false }: { icon: typeof Check; label: string; onClick: () => void; disabled: boolean; destructive?: boolean }) {
  return <button aria-label={label} className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-lg border px-2.5 py-2 text-xs font-bold transition active:translate-y-px disabled:cursor-not-allowed disabled:opacity-50 ${destructive ? 'border-[#FECACA] text-[#B91C1C] hover:bg-[#FEF2F2]' : 'border-[#99F6E4] text-[#0F766E] hover:bg-[#F0FDFA]'}`} disabled={disabled} onClick={onClick} title={label} type="button"><Icon className="size-3.5" />{label}</button>
}

function StatusBadge({ status }: { status: ReservationStatus }) {
  const styles: Record<ReservationStatus, string> = {
    Pendiente: 'bg-[#FFF7ED] text-[#9A3412] ring-[#FED7AA]',
    Confirmada: 'bg-[#ECFDF5] text-[#047857] ring-[#A7F3D0]',
    Cancelada: 'bg-[#FEF2F2] text-[#B91C1C] ring-[#FECACA]',
    Finalizada: 'bg-[#F1F5F9] text-[#475569] ring-[#CBD5E1]',
  }
  return <span className={`inline-flex rounded-lg px-2.5 py-1 text-xs font-bold ring-1 ring-inset ${styles[status]}`}>{status}</span>
}

function Tab({ active, label, onClick }: { active: boolean; label: string; onClick: () => void }) {
  return <button aria-selected={active} className={`border-b-2 px-4 pb-3 pt-1 text-sm font-bold transition ${active ? 'border-[#0F766E] text-[#0F766E]' : 'border-transparent text-[#64748B] hover:text-[#334155]'}`} onClick={onClick} role="tab" type="button">{label}</button>
}

function Field({ label, children, error }: { label: string; children: React.ReactNode; error?: string }) {
  return <label className="grid gap-2 text-sm font-bold text-[#334155]"><span>{label}</span>{children}{error ? <span className="text-xs font-medium text-[#B91C1C]" role="alert">{error}</span> : null}</label>
}

function PriceRow({ label, value, emphasis = false }: { label: string; value: string; emphasis?: boolean }) {
  return <div className="flex items-center justify-between gap-3"><dt className={emphasis ? 'font-bold text-[#0F172A]' : 'text-[#475569]'}>{label}</dt><dd className={emphasis ? 'text-lg font-extrabold text-[#0F766E]' : 'font-bold text-[#0F172A]'}>{value}</dd></div>
}

function ErrorBanner({ message }: { message: string }) {
  return <div className="m-4 rounded-xl border border-[#FECACA] bg-[#FEF2F2] px-4 py-3 text-sm font-semibold text-[#991B1B]" role="alert">{message}</div>
}

function ReservationSkeleton() {
  return <div className="space-y-3 p-4" aria-label="Cargando reservas">{[1, 2, 3].map((item) => <div className="h-16 animate-pulse rounded-xl bg-[#F1F5F9]" key={item} />)}</div>
}

function EmptyState({ view, onCreate }: { view: ReservationView; onCreate: () => void }) {
  return <div className="grid place-items-center px-5 py-14 text-center"><div className="grid size-12 place-items-center rounded-xl bg-[#F0FDFA] text-[#0F766E]"><CalendarCheck className="size-6" /></div><h4 className="mt-4 text-lg font-bold text-[#0F172A]">{view === 'active' ? 'No hay reservas vigentes' : 'El historial está vacío'}</h4><p className="mt-2 max-w-md text-sm leading-6 text-[#64748B]">{view === 'active' ? 'Creá una reserva para empezar a organizar la agenda del club.' : 'Las reservas canceladas o finalizadas aparecerán acá.'}</p>{view === 'active' ? <button className={`${secondaryButtonClass} mt-4`} onClick={onCreate} type="button">Crear reserva</button> : null}</div>
}

function calculatePreview(turn?: ReservationAvailability, promotion?: Promotion) {
  if (!turn) return 0
  return Math.round(turn.basePrice * (1 - (promotion?.discountPercentage ?? 0) / 100) * 100) / 100
}

const inputClass = 'w-full rounded-xl border border-[#CBD5E1] bg-white px-3 py-2.5 text-sm font-medium text-[#0F172A] outline-none transition placeholder:text-[#64748B] focus:border-[#0F766E] focus:ring-2 focus:ring-[#99F6E4] disabled:cursor-not-allowed disabled:bg-[#F1F5F9]'
const primaryButtonClass = 'whitespace-nowrap rounded-xl bg-[#0F766E] px-4 py-2.5 text-sm font-bold text-white transition hover:bg-[#115E59] active:translate-y-px disabled:cursor-not-allowed disabled:opacity-50'
const secondaryButtonClass = 'whitespace-nowrap rounded-xl border border-[#CBD5E1] bg-white px-4 py-2.5 text-sm font-bold text-[#334155] transition hover:bg-[#F8FAFC] active:translate-y-px'
