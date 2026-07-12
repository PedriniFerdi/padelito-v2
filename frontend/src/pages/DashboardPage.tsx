import { useQuery } from '@tanstack/react-query'
import { CalendarCheck, CircleDollarSign, LayoutDashboard, MapPin, Users } from 'lucide-react'
import { dashboardApi } from '@/api/dashboard.api'

const money = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })

export function DashboardPage() {
  const query = useQuery({ queryKey: ['dashboard'], queryFn: dashboardApi.summary })
  if (query.isLoading) return <div className="p-6 text-sm font-semibold text-[#64748B]">Cargando dashboard...</div>
  if (query.isError) return <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm font-semibold text-red-800">{query.error.message}</div>
  const data = query.data!
  const cards = [
    ['Clientes activos', data.activeClients, Users], ['Canchas activas', data.activeCourts, MapPin],
    ['Reservas de hoy', data.reservationsToday, CalendarCheck], ['Ingresos de hoy', money.format(data.incomeToday), CircleDollarSign],
  ] as const
  return <div className="space-y-6">
    <header><div className="flex items-center gap-2 text-sm font-bold text-[#0F766E]"><LayoutDashboard className="size-4" /> Operación diaria</div><h2 className="mt-2 text-3xl font-black tracking-tight text-[#0F172A]">Dashboard</h2><p className="mt-1 text-sm text-[#64748B]">Resumen del {new Date(`${data.operationalDate}T12:00:00`).toLocaleDateString('es-AR')}.</p></header>
    <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">{cards.map(([label, value, Icon]) => <article className="rounded-2xl border border-[#E2E8F0] bg-white p-5 shadow-sm" key={label}><div className="grid size-10 place-items-center rounded-xl bg-[#F0FDFA] text-[#0F766E]"><Icon className="size-5" /></div><p className="mt-4 text-sm font-semibold text-[#64748B]">{label}</p><p className="mt-1 text-2xl font-black text-[#0F172A]">{value}</p></article>)}</section>
    <section className="overflow-hidden rounded-2xl border border-[#E2E8F0] bg-white"><div className="border-b border-[#E2E8F0] px-5 py-4"><h3 className="font-bold text-[#0F172A]">Últimas reservas de hoy</h3></div>{data.latestReservations.length === 0 ? <p className="p-8 text-center text-sm text-[#64748B]">Todavía no hay reservas para hoy.</p> : <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-[#F8FAFC] text-xs uppercase text-[#64748B]"><tr><th className="px-5 py-3">Horario</th><th className="px-5 py-3">Cliente</th><th className="px-5 py-3">Cancha</th><th className="px-5 py-3">Estado</th><th className="px-5 py-3 text-right">Precio</th></tr></thead><tbody>{data.latestReservations.map(r => <tr className="border-t border-[#F1F5F9]" key={r.id}><td className="px-5 py-3 font-bold">{r.startTime.slice(0,5)}</td><td className="px-5 py-3">{r.clientName}</td><td className="px-5 py-3">{r.courtName}</td><td className="px-5 py-3">{r.status}</td><td className="px-5 py-3 text-right font-bold">{money.format(r.finalPrice)}</td></tr>)}</tbody></table></div>}</section>
  </div>
}
