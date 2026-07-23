import { useQuery } from '@tanstack/react-query'
import { Banknote, CalendarDays, MapPin, UserRound } from 'lucide-react'
import { dashboardApi } from '@/api/dashboard.api'

const money = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })

export function DashboardPage() {
  const query = useQuery({ queryKey: ['dashboard'], queryFn: dashboardApi.summary })

  if (query.isLoading) {
    return <DashboardSkeleton />
  }

  if (query.isError) {
    return (
      <div className="rounded-xl border border-[#ffb4ab]/30 bg-[#93000a]/25 p-4 text-sm font-semibold text-[#ffdad6]">
        {query.error.message}
      </div>
    )
  }

  const data = query.data!
  const cards = [
    ['Clientes activos', data.activeClients, UserRound],
    ['Canchas activas', data.activeCourts, MapPin],
    ['Reservas de hoy', data.reservationsToday, CalendarDays],
    ['Ingresos de hoy', money.format(data.incomeToday), Banknote],
  ] as const

  return (
    <div>
      <header className="px-0.5 pt-2">
        <h2 className="font-display text-[23px] font-bold leading-7 tracking-[-0.02em] text-[#f2f4f6]">Dashboard</h2>
        <p className="mt-0.5 text-sm font-medium text-[#9fa6b1]">
          Resumen del {new Date(`${data.operationalDate}T12:00:00`).toLocaleDateString('es-AR')}.
        </p>
      </header>

      <section aria-label="Resumen operativo" className="mt-7 grid gap-x-6 gap-y-[30px] px-3 md:grid-cols-2">
        {cards.map(([label, value, Icon]) => (
          <article
            className="flex h-[69px] items-center justify-between rounded-lg border border-[#3e4943] bg-[#171f33]/90 px-3 shadow-[0_14px_35px_rgba(6,14,32,0.2),inset_0_1px_0_rgba(255,255,255,0.025)] backdrop-blur-xl"
            key={label}
          >
            <div>
              <p className="text-sm font-medium leading-5 text-[#edf1f3]">{label}</p>
              <p className="font-display mt-0.5 text-2xl font-semibold leading-7 tracking-[-0.02em] text-white">{value}</p>
            </div>
            <div className="grid size-9 shrink-0 place-items-center rounded-lg bg-[#003824] text-[#4edea3] shadow-[inset_0_1px_0_rgba(255,255,255,0.08)]">
              <Icon aria-hidden="true" className="size-[22px]" fill="currentColor" strokeWidth={1.8} />
            </div>
          </article>
        ))}
      </section>

      <section className="mt-8 overflow-hidden rounded-lg border border-[#3e4943] bg-[#171f33]/90 shadow-[0_18px_45px_rgba(6,14,32,0.22)] backdrop-blur-xl">
        <h3 className="px-3 pt-3 text-sm font-semibold text-[#f1f4f5]">Últimas reservas de hoy</h3>
        {data.latestReservations.length === 0 ? (
          <div className="grid min-h-44 place-items-center px-6 text-center">
            <div>
              <CalendarDays aria-hidden="true" className="mx-auto size-7 text-[#4edea3]" strokeWidth={1.7} />
              <p className="mt-3 text-sm font-semibold text-[#d9dee2]">Todavía no hay reservas para hoy.</p>
            </div>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="mt-1 w-full min-w-[680px] table-fixed text-left text-sm">
              <thead className="text-[#8f96a2]">
                <tr>
                  <th className="w-1/4 px-3 py-2 font-medium">Tiempo</th>
                  <th className="w-1/4 px-3 py-2 font-medium">Canchas</th>
                  <th className="w-1/4 px-3 py-2 font-medium">Cliente</th>
                  <th className="w-1/4 px-3 py-2 font-medium">Estado</th>
                </tr>
              </thead>
              <tbody>
                {data.latestReservations.map((reservation) => (
                  <tr
                    className="border-t border-white/[0.09] text-[#edf0f2] transition-colors hover:bg-[#2b3339]/75"
                    key={reservation.id}
                  >
                    <td className="px-3 py-2 font-medium">{reservation.startTime.slice(0, 5)}</td>
                    <td className="px-3 py-2">{reservation.courtName}</td>
                    <td className="truncate px-3 py-2">{reservation.clientName}</td>
                    <td className="px-3 py-2 font-medium text-[#4edea3]">{reservation.status}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )
}

function DashboardSkeleton() {
  return (
    <div aria-label="Cargando dashboard" aria-live="polite">
      <div className="h-7 w-36 animate-pulse rounded-md bg-white/[0.08]" />
      <div className="mt-2 h-4 w-44 animate-pulse rounded bg-white/[0.06]" />
      <div className="mt-7 grid gap-x-6 gap-y-[30px] px-3 md:grid-cols-2">
        {[0, 1, 2, 3].map((item) => (
          <div className="h-[69px] animate-pulse rounded-lg border border-white/[0.06] bg-white/[0.05]" key={item} />
        ))}
      </div>
      <div className="mt-8 h-72 animate-pulse rounded-lg border border-white/[0.06] bg-white/[0.05]" />
    </div>
  )
}
