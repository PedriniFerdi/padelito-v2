import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download, FileSpreadsheet } from 'lucide-react'
import { reportsApi, type ReportFilters } from '@/api/reports.api'
import { apiFetch } from '@/api/http'
import type { ReservationStatus } from '@/types/api'

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const field = 'w-full rounded-xl border border-[#CBD5E1] bg-white px-3 py-2.5 text-sm text-[#0F172A] outline-none focus:border-[#0F766E] focus:ring-2 focus:ring-[#99F6E4]'

export function ReportsPage() {
  const [filters, setFilters] = useState<ReportFilters>({})
  const [draft, setDraft] = useState<ReportFilters>({})
  const [exporting, setExporting] = useState(false)
  const report = useQuery({ queryKey: ['reports', filters], queryFn: () => reportsApi.reservations(filters) })
  const statuses = useQuery({ queryKey: ['reservation-statuses'], queryFn: () => apiFetch<{id:number;name:ReservationStatus}[]>('/api/catalogs/reservation-statuses') })

  async function download() {
    setExporting(true)
    try {
      const blob = await reportsApi.exportReservations(filters)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'reservation-report.csv'
      link.click()
      URL.revokeObjectURL(url)
    } finally { setExporting(false) }
  }

  const summary = report.data?.summary
  return <div className="space-y-6">
    <header className="flex flex-wrap items-end justify-between gap-4">
      <div><div className="flex items-center gap-2 text-sm font-bold text-[#0F766E]"><FileSpreadsheet className="size-4" /> Operations</div><h1 className="mt-2 text-3xl font-black tracking-tight">Reservation report</h1><p className="mt-1 text-sm text-[#475569]">Revenue, collections, and open balances in one operational view.</p></div>
      <button className="inline-flex items-center gap-2 whitespace-nowrap rounded-xl bg-[#0F766E] px-4 py-2.5 text-sm font-bold text-white transition active:scale-[.98] disabled:opacity-50" disabled={exporting || report.isLoading} onClick={download}><Download className="size-4" /> {exporting ? 'Preparing...' : 'Export CSV'}</button>
    </header>
    <form className="grid gap-3 rounded-2xl border border-[#E2E8F0] bg-white p-4 md:grid-cols-4" onSubmit={event => { event.preventDefault(); setFilters(draft) }}>
      <label className="text-sm font-bold">From<input className={`${field} mt-1`} onChange={e => setDraft({...draft,dateFrom:e.target.value||undefined})} type="date" /></label>
      <label className="text-sm font-bold">To<input className={`${field} mt-1`} onChange={e => setDraft({...draft,dateTo:e.target.value||undefined})} type="date" /></label>
      <label className="text-sm font-bold">Status<select className={`${field} mt-1`} onChange={e => setDraft({...draft,statusId:Number(e.target.value)||undefined})}><option value="">All</option>{statuses.data?.map(status => <option key={status.id} value={status.id}>{status.name}</option>)}</select></label>
      <button className="self-end rounded-xl border border-[#99F6E4] px-4 py-2.5 text-sm font-bold text-[#0F766E] transition active:scale-[.98]">Apply filters</button>
    </form>
    {report.isError ? <p className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm font-semibold text-red-700">{report.error.message}</p> : <>
      <section className="grid gap-px overflow-hidden rounded-2xl border border-[#E2E8F0] bg-[#E2E8F0] sm:grid-cols-2 xl:grid-cols-4">
        {[['Reservations', summary?.reservationCount ?? 0], ['Reserved value', money.format(summary?.finalPriceTotal ?? 0)], ['Collected', money.format(summary?.totalPaid ?? 0)], ['Outstanding balance', money.format(summary?.pendingBalance ?? 0)]].map(([label,value]) => <article className="bg-white p-5" key={label}><p className="text-sm font-semibold text-[#64748B]">{label}</p><p className="mt-1 text-2xl font-black">{value}</p></article>)}
      </section>
      <section className="overflow-hidden rounded-2xl border border-[#E2E8F0] bg-white">{report.isLoading ? <div className="space-y-3 p-6">{[1,2,3].map(i => <div className="h-10 animate-pulse rounded-xl bg-[#F1F5F9]" key={i} />)}</div> : !report.data?.rows.length ? <div className="p-10 text-center"><p className="font-bold">No reservations found</p><p className="mt-1 text-sm text-[#64748B]">Try another date range or status.</p></div> : <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-[#F8FAFC] text-xs uppercase text-[#475569]"><tr><th className="px-4 py-3">Date</th><th className="px-4 py-3">Reservation</th><th className="px-4 py-3">Status</th><th className="px-4 py-3 text-right">Final</th><th className="px-4 py-3 text-right">Paid</th><th className="px-4 py-3 text-right">Balance</th></tr></thead><tbody>{report.data.rows.map(row => <tr className="border-t border-[#F1F5F9]" key={row.reservationId}><td className="px-4 py-3 whitespace-nowrap">{new Date(`${row.reservationDate}T00:00:00`).toLocaleDateString('en-US')}<span className="block text-xs text-[#64748B]">{row.startTime.slice(0,5)}-{row.endTime.slice(0,5)}</span></td><td className="px-4 py-3"><b>#{row.reservationId} {row.clientName}</b><span className="block text-xs text-[#64748B]">{row.courtName}</span></td><td className="px-4 py-3"><span className="rounded-lg bg-[#ECFDF5] px-2 py-1 text-xs font-bold text-[#0F766E]">{row.status}</span><span className="mt-1 block text-xs text-[#64748B]">{row.paymentStatus}</span></td><td className="px-4 py-3 text-right font-bold">{money.format(row.finalPrice)}</td><td className="px-4 py-3 text-right">{money.format(row.totalPaid)}</td><td className="px-4 py-3 text-right">{money.format(row.pendingBalance)}</td></tr>)}</tbody></table></div>}</section>
    </>}
  </div>
}
