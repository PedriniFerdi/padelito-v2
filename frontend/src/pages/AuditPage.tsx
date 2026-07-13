import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { History, Search } from 'lucide-react'
import { Link } from 'react-router-dom'
import { auditApi, type AuditFilters } from '@/api/audit.api'

const field = 'w-full rounded-xl border border-[#CBD5E1] bg-white px-3 py-2.5 text-sm text-[#0F172A] outline-none focus:border-[#0F766E] focus:ring-2 focus:ring-[#99F6E4]'

export function AuditPage() {
  const [filters, setFilters] = useState<AuditFilters>({})
  const [draft, setDraft] = useState<AuditFilters>({})
  const audits = useQuery({ queryKey: ['audit', filters], queryFn: () => auditApi.list(filters) })
  return <div className="space-y-6">
    <header><div className="flex items-center gap-2 text-sm font-bold text-[#0F766E]"><History className="size-4" /> Control interno</div><h1 className="mt-2 text-3xl font-black tracking-tight">Auditoría de reservas</h1><p className="mt-1 text-sm text-[#475569]">Trazabilidad de altas y cambios realizados por el equipo.</p></header>
    <form className="grid gap-3 rounded-2xl border border-[#E2E8F0] bg-white p-4 md:grid-cols-3 xl:grid-cols-6" onSubmit={e => {e.preventDefault();setFilters(draft)}}>
      <label className="text-sm font-bold">Desde<input className={`${field} mt-1`} onChange={e=>setDraft({...draft,dateFrom:e.target.value||undefined})} type="date" /></label><label className="text-sm font-bold">Hasta<input className={`${field} mt-1`} onChange={e=>setDraft({...draft,dateTo:e.target.value||undefined})} type="date" /></label><label className="text-sm font-bold">Reserva<input className={`${field} mt-1`} min="1" onChange={e=>setDraft({...draft,reservationId:Number(e.target.value)||undefined})} type="number" /></label><label className="text-sm font-bold">Acción<select className={`${field} mt-1`} onChange={e=>setDraft({...draft,action:e.target.value||undefined})}><option value="">Todas</option><option value="Creacion">Creación</option><option value="CambioEstado">Cambio de estado</option></select></label><label className="text-sm font-bold">Usuario<input className={`${field} mt-1`} onChange={e=>setDraft({...draft,username:e.target.value||undefined})} /></label><button className="inline-flex items-center justify-center gap-2 self-end rounded-xl bg-[#0F766E] px-4 py-2.5 text-sm font-bold text-white active:scale-[.98]"><Search className="size-4" /> Buscar</button>
    </form>
    <section className="overflow-hidden rounded-2xl border border-[#E2E8F0] bg-white">{audits.isLoading ? <div className="space-y-3 p-6">{[1,2,3].map(i=><div className="h-16 animate-pulse rounded-xl bg-[#F1F5F9]" key={i}/>)}</div> : audits.isError ? <p className="p-5 text-sm font-semibold text-red-700">{audits.error.message}</p> : !audits.data?.length ? <div className="p-10 text-center"><p className="font-bold">No se encontraron eventos</p><p className="mt-1 text-sm text-[#64748B]">Ajustá los filtros para ampliar la búsqueda.</p></div> : <div className="divide-y divide-[#F1F5F9]">{audits.data.map(item=><article className="grid gap-3 p-5 md:grid-cols-[180px_1fr_auto] md:items-start" key={item.id}><div><p className="text-sm font-bold">{new Date(item.createdAt).toLocaleString('es-AR')}</p><p className="mt-1 text-xs font-semibold text-[#0F766E]">{item.username}</p></div><div><p className="font-bold">{item.action === 'Creacion' ? 'Creación' : 'Cambio de estado'}</p><p className="mt-1 text-sm text-[#475569]">{item.description}</p><p className="mt-2 text-xs text-[#64748B]">{item.clientName} - {item.courtName} - {new Date(`${item.reservationDate}T00:00:00`).toLocaleDateString('es-AR')}</p></div><Link className="whitespace-nowrap text-sm font-bold text-[#0F766E]" to={`/reservations?reservationId=${item.reservationId}`}>Reserva #{item.reservationId}</Link></article>)}</div>}</section>
  </div>
}
