import { useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  AlertTriangle,
  Banknote,
  CalendarDays,
  Clock3,
  Flame,
  MapPin,
  Percent,
  TrendingUp,
  UserRound,
  type LucideIcon,
} from 'lucide-react'
import { dashboardApi, type RevenueIntelligenceFilters } from '@/api/dashboard.api'
import type { DashboardRevenueIntelligence } from '@/types/api'

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const percent = new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 })
const inputClass =
  'h-9 rounded-lg border border-[#3e4943] bg-[#171f33] px-3 text-sm font-semibold text-[#dae2fd] outline-none transition focus:border-[#7ad9ad] focus:ring-2 focus:ring-[#7ad9ad]/15'

export function DashboardPage() {
  const [filters, setFilters] = useState<RevenueIntelligenceFilters>({})
  const [draft, setDraft] = useState<RevenueIntelligenceFilters>({})
  const summaryQuery = useQuery({ queryKey: ['dashboard'], queryFn: dashboardApi.summary })
  const intelligenceQuery = useQuery({
    queryKey: ['dashboard-revenue-intelligence', filters],
    queryFn: () => dashboardApi.revenueIntelligence(filters),
  })

  if (summaryQuery.isLoading || intelligenceQuery.isLoading) {
    return <DashboardSkeleton />
  }

  if (summaryQuery.isError || intelligenceQuery.isError) {
    const error = summaryQuery.error ?? intelligenceQuery.error
    return (
      <div className="rounded-xl border border-[#ffb4ab]/30 bg-[#93000a]/25 p-4 text-sm font-semibold text-[#ffdad6]">
        {error?.message}
      </div>
    )
  }

  const data = summaryQuery.data!
  const intelligence = intelligenceQuery.data!
  const cards = [
    ['Active customers', data.activeClients, UserRound],
    ['Active courts', data.activeCourts, MapPin],
    ['Today reservations', data.reservationsToday, CalendarDays],
    ['Today revenue', money.format(data.incomeToday), Banknote],
  ] as const
  const kpis = [
    ['Collected revenue', money.format(intelligence.summary.totalRevenue), TrendingUp, 'text-[#6fe0b2]', 'bg-[#003824]'],
    ['Outstanding balance', money.format(intelligence.summary.pendingBalance), AlertTriangle, 'text-[#ffd166]', 'bg-[#3c2f12]'],
    ['Average occupancy', `${percent.format(intelligence.summary.averageOccupancyRate)}%`, MapPin, 'text-[#9cc9ff]', 'bg-[#18314f]'],
    ['Cancellations', `${percent.format(intelligence.summary.cancellationRate)}%`, Percent, 'text-[#ffb4ab]', 'bg-[#4a1d20]'],
  ] as const

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setFilters(draft)
  }

  return (
    <div className="space-y-8">
      <header className="flex flex-wrap items-end justify-between gap-4 px-0.5 pt-2">
        <div>
          <h2 className="font-display text-[23px] font-bold leading-7 tracking-[-0.02em] text-[#f2f4f6]">Dashboard</h2>
          <p className="mt-0.5 text-sm font-medium text-[#9fa6b1]">
            Summary for {formatDate(data.operationalDate)} and revenue intelligence from {formatDate(intelligence.dateFrom)} to{' '}
            {formatDate(intelligence.dateTo)}.
          </p>
        </div>
        <form className="flex flex-wrap items-end gap-2" onSubmit={applyFilters}>
          <label className="grid gap-1 text-xs font-bold uppercase tracking-wide text-[#9fa6b1]">
            From
            <input
              className={inputClass}
              onChange={(event) => setDraft({ ...draft, dateFrom: event.target.value || undefined })}
              type="date"
            />
          </label>
          <label className="grid gap-1 text-xs font-bold uppercase tracking-wide text-[#9fa6b1]">
            To
            <input
              className={inputClass}
              onChange={(event) => setDraft({ ...draft, dateTo: event.target.value || undefined })}
              type="date"
            />
          </label>
          <button
            className="inline-flex h-9 items-center gap-2 rounded-lg bg-[#dae2fd] px-3 text-sm font-extrabold text-[#10192d] transition active:scale-[.98]"
            type="submit"
          >
            <Clock3 aria-hidden="true" className="size-4" />
            Apply
          </button>
        </form>
      </header>

      <section aria-label="Operations summary" className="grid gap-x-6 gap-y-[30px] px-3 md:grid-cols-2">
        {cards.map(([label, value, Icon]) => (
          <MetricCard Icon={Icon} key={label} label={label} value={value} />
        ))}
      </section>

      <section aria-label="Padelytics" className="space-y-5">
        <div className="flex items-center justify-between gap-3">
          <div>
            <h3 className="text-lg font-black text-[#f2f4f6]">Padelytics</h3>
            <p className="text-sm font-medium text-[#9fa6b1]">Occupancy, demand, balances, and promotions for better decisions.</p>
          </div>
        </div>

        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {kpis.map(([label, value, Icon, tone, bg]) => (
            <article className="rounded-lg border border-[#3e4943] bg-[#171f33]/90 p-4 shadow-[0_18px_45px_rgba(6,14,32,0.18)]" key={label}>
              <div className="flex items-center justify-between gap-3">
                <p className="text-sm font-semibold text-[#aab2bd]">{label}</p>
                <span className={`grid size-9 place-items-center rounded-lg ${bg} ${tone}`}>
                  <Icon aria-hidden="true" className="size-5" strokeWidth={1.9} />
                </span>
              </div>
              <p className="font-display mt-3 text-2xl font-black text-white">{value}</p>
            </article>
          ))}
        </div>

        <div className="grid gap-5 xl:grid-cols-[1.15fr_.85fr]">
          <CourtRanking intelligence={intelligence} />
          <PromotionCard intelligence={intelligence} />
        </div>

        <div className="grid gap-5 xl:grid-cols-[1.2fr_.8fr]">
          <DemandHeatmap intelligence={intelligence} />
          <DemandWindows intelligence={intelligence} />
        </div>
      </section>

      <LatestReservations data={data} />
    </div>
  )
}

function MetricCard({ Icon, label, value }: { Icon: LucideIcon; label: string; value: ReactNode }) {
  return (
    <article className="flex h-[69px] items-center justify-between rounded-lg border border-[#3e4943] bg-[#171f33]/90 px-3 shadow-[0_14px_35px_rgba(6,14,32,0.2),inset_0_1px_0_rgba(255,255,255,0.025)] backdrop-blur-xl">
      <div>
        <p className="text-sm font-medium leading-5 text-[#edf1f3]">{label}</p>
        <p className="font-display mt-0.5 text-2xl font-semibold leading-7 tracking-[-0.02em] text-white">{value}</p>
      </div>
      <div className="grid size-9 shrink-0 place-items-center rounded-lg bg-[#003824] text-[#4edea3] shadow-[inset_0_1px_0_rgba(255,255,255,0.08)]">
        <Icon aria-hidden="true" className="size-[22px]" fill="currentColor" strokeWidth={1.8} />
      </div>
    </article>
  )
}

function CourtRanking({ intelligence }: { intelligence: DashboardRevenueIntelligence }) {
  return (
    <section className="overflow-hidden rounded-lg border border-[#3e4943] bg-[#171f33]/90">
      <h3 className="px-4 pt-4 text-sm font-bold text-[#f1f4f5]">Revenue by court</h3>
      {intelligence.courts.length === 0 ? (
        <EmptyState text="No active courts are available for analysis." />
      ) : (
        <div className="mt-2 overflow-x-auto">
          <table className="w-full min-w-[640px] table-fixed text-left text-sm">
            <thead className="text-[#8f96a2]">
              <tr>
                <th className="w-[30%] px-4 py-2 font-medium">Court</th>
                <th className="w-[18%] px-4 py-2 font-medium">Occupancy</th>
                <th className="w-[30%] px-4 py-2 font-medium">Time slots</th>
                <th className="w-[22%] px-4 py-2 text-right font-medium">Revenue</th>
              </tr>
            </thead>
            <tbody>
              {intelligence.courts.map((court) => (
                <tr className="border-t border-white/[0.09] text-[#edf0f2]" key={court.courtId}>
                  <td className="px-4 py-3 font-semibold">{court.courtName}</td>
                  <td className="px-4 py-3 font-bold text-[#6fe0b2]">{percent.format(court.occupancyRate)}%</td>
                  <td className="px-4 py-3">
                    <div className="h-2 overflow-hidden rounded-full bg-white/[0.08]">
                      <div className="h-full rounded-full bg-[#6fe0b2]" style={{ width: `${Math.min(100, court.occupancyRate)}%` }} />
                    </div>
                    <span className="mt-1 block text-xs text-[#9fa6b1]">
                      {court.reservedSlots}/{court.availableSlots} slots
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right font-black">{money.format(court.revenue)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

function PromotionCard({ intelligence }: { intelligence: DashboardRevenueIntelligence }) {
  const promotion = intelligence.bestPromotion
  return (
    <section className="rounded-lg border border-[#3e4943] bg-[#171f33]/90 p-4">
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-sm font-bold text-[#f1f4f5]">Best promotion</h3>
        <span className="grid size-9 place-items-center rounded-lg bg-[#3b2b57] text-[#d8b4fe]">
          <Flame aria-hidden="true" className="size-5" />
        </span>
      </div>
      {!promotion ? (
        <EmptyState text="No reservations with promotions in this period yet." />
      ) : (
        <div className="mt-4 space-y-4">
          <div>
            <p className="text-2xl font-black text-white">{promotion.promotionName}</p>
            <p className="mt-1 text-sm font-medium text-[#9fa6b1]">
              {promotion.reservationCount} linked {promotion.reservationCount === 1 ? 'reservation' : 'reservations'}
            </p>
          </div>
          <div className="grid grid-cols-3 gap-2">
            <MiniStat label="Collected" value={money.format(promotion.collectedRevenue)} />
            <MiniStat label="Gross" value={money.format(promotion.grossRevenue)} />
            <MiniStat label="Discount" value={money.format(promotion.discountTotal)} />
          </div>
        </div>
      )}
    </section>
  )
}

function DemandHeatmap({ intelligence }: { intelligence: DashboardRevenueIntelligence }) {
  const hours = [...new Set(intelligence.demand.map((item) => item.hour))].sort((a, b) => a - b)
  const days = [...new Set(intelligence.demand.map((item) => item.dayOfWeek))]
    .sort((a, b) => a - b)
    .map((day) => intelligence.demand.find((item) => item.dayOfWeek === day)!)
  return (
    <section className="rounded-lg border border-[#3e4943] bg-[#171f33]/90 p-4">
      <h3 className="text-sm font-bold text-[#f1f4f5]">Demand by day and hour</h3>
      {hours.length === 0 ? (
        <EmptyState text="No active reservations are available for the demand matrix." />
      ) : (
        <div className="mt-4 overflow-x-auto">
          <div className="grid min-w-[620px] gap-2" style={{ gridTemplateColumns: `88px repeat(${hours.length}, minmax(64px, 1fr))` }}>
            <div />
            {hours.map((hour) => (
              <div className="text-center text-xs font-bold text-[#9fa6b1]" key={hour}>
                {hour}:00
              </div>
            ))}
            {days.map((day) => (
              <HeatmapRow day={day.dayName} demand={intelligence.demand} hours={hours} key={day.dayOfWeek} />
            ))}
          </div>
        </div>
      )}
    </section>
  )
}

function HeatmapRow({
  day,
  demand,
  hours,
}: {
  day: string
  demand: DashboardRevenueIntelligence['demand']
  hours: number[]
}) {
  return (
    <>
      <div className="flex h-11 items-center text-xs font-bold text-[#dae2fd]">{day}</div>
      {hours.map((hour) => {
        const cell = demand.find((item) => item.dayName === day && item.hour === hour)
        const intensity = Math.min(0.9, 0.12 + (cell?.occupancyRate ?? 0) / 120)
        return (
          <div
            className="grid h-11 place-items-center rounded-lg border border-white/[0.06] text-xs font-black text-white"
            key={`${day}-${hour}`}
            style={{ backgroundColor: `rgba(111, 224, 178, ${cell ? intensity : 0.05})` }}
            title={cell ? `${cell.reservationCount} ${cell.reservationCount === 1 ? 'reservation' : 'reservations'}, ${percent.format(cell.occupancyRate)}% occupancy` : 'No reservations'}
          >
            {cell?.reservationCount ?? 0}
          </div>
        )
      })}
    </>
  )
}

function DemandWindows({ intelligence }: { intelligence: DashboardRevenueIntelligence }) {
  return (
    <section className="grid gap-4">
      <WindowList title="Peak demand" windows={intelligence.peakDemand} />
      <WindowList title="Off-peak opportunity" windows={intelligence.offPeakDemand} />
    </section>
  )
}

function WindowList({ title, windows }: { title: string; windows: DashboardRevenueIntelligence['peakDemand'] }) {
  return (
    <div className="rounded-lg border border-[#3e4943] bg-[#171f33]/90 p-4">
      <h3 className="text-sm font-bold text-[#f1f4f5]">{title}</h3>
      {windows.length === 0 ? (
        <EmptyState text="Not enough data yet." />
      ) : (
        <div className="mt-3 space-y-2">
          {windows.map((window) => (
            <div className="flex items-center justify-between rounded-lg bg-white/[0.045] px-3 py-2" key={`${title}-${window.dayOfWeek}-${window.hour}`}>
              <div>
                <p className="text-sm font-bold text-white">
                  {window.dayName} {window.hour}:00
                </p>
                <p className="text-xs font-medium text-[#9fa6b1]">
                  {window.reservationCount} {window.reservationCount === 1 ? 'reservation' : 'reservations'}
                </p>
              </div>
              <p className="text-sm font-black text-[#6fe0b2]">{percent.format(window.occupancyRate)}%</p>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function LatestReservations({ data }: { data: Awaited<ReturnType<typeof dashboardApi.summary>> }) {
  return (
    <section className="overflow-hidden rounded-lg border border-[#3e4943] bg-[#171f33]/90 shadow-[0_18px_45px_rgba(6,14,32,0.22)] backdrop-blur-xl">
      <h3 className="px-3 pt-3 text-sm font-semibold text-[#f1f4f5]">Latest reservations today</h3>
      {data.latestReservations.length === 0 ? (
        <div className="grid min-h-44 place-items-center px-6 text-center">
          <div>
            <CalendarDays aria-hidden="true" className="mx-auto size-7 text-[#4edea3]" strokeWidth={1.7} />
            <p className="mt-3 text-sm font-semibold text-[#d9dee2]">No reservations yet today.</p>
          </div>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="mt-1 w-full min-w-[680px] table-fixed text-left text-sm">
            <thead className="text-[#8f96a2]">
              <tr>
                <th className="w-1/4 px-3 py-2 font-medium">Time</th>
                <th className="w-1/4 px-3 py-2 font-medium">Courts</th>
                <th className="w-1/4 px-3 py-2 font-medium">Customer</th>
                <th className="w-1/4 px-3 py-2 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {data.latestReservations.map((reservation) => (
                <tr className="border-t border-white/[0.09] text-[#edf0f2] transition-colors hover:bg-[#2b3339]/75" key={reservation.id}>
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
  )
}

function MiniStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg bg-white/[0.045] p-3">
      <p className="text-xs font-bold uppercase tracking-wide text-[#9fa6b1]">{label}</p>
      <p className="mt-1 truncate text-sm font-black text-white">{value}</p>
    </div>
  )
}

function EmptyState({ text }: { text: string }) {
  return <p className="py-8 text-center text-sm font-semibold text-[#9fa6b1]">{text}</p>
}

function DashboardSkeleton() {
  return (
    <div aria-label="Loading dashboard" aria-live="polite">
      <div className="h-7 w-36 animate-pulse rounded-md bg-white/[0.08]" />
      <div className="mt-2 h-4 w-80 max-w-full animate-pulse rounded bg-white/[0.06]" />
      <div className="mt-7 grid gap-x-6 gap-y-[30px] px-3 md:grid-cols-2">
        {[0, 1, 2, 3].map((item) => (
          <div className="h-[69px] animate-pulse rounded-lg border border-white/[0.06] bg-white/[0.05]" key={item} />
        ))}
      </div>
      <div className="mt-8 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        {[0, 1, 2, 3].map((item) => (
          <div className="h-32 animate-pulse rounded-lg border border-white/[0.06] bg-white/[0.05]" key={item} />
        ))}
      </div>
      <div className="mt-5 h-80 animate-pulse rounded-lg border border-white/[0.06] bg-white/[0.05]" />
    </div>
  )
}

function formatDate(value: string) {
  return new Date(`${value}T12:00:00`).toLocaleDateString('en-US')
}
