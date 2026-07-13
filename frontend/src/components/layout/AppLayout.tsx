import {
  BarChart3,
  CalendarDays,
  CreditCard,
  FileText,
  History,
  LayoutDashboard,
  LogOut,
  Percent,
  Settings,
  Shield,
  Users,
  UserSquare,
  type LucideIcon,
} from 'lucide-react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'
import type { Role } from '@/types/api'

type NavItem = {
  label: string
  path: string
  icon: LucideIcon
  roles: Role[]
}

const allRoles: Role[] = ['Administrador', 'Recepcion', 'Empleado']

const navItems: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: LayoutDashboard, roles: allRoles },
  { label: 'Clientes', path: '/clients', icon: Users, roles: ['Administrador', 'Recepcion'] },
  { label: 'Empleados', path: '/employees', icon: UserSquare, roles: ['Administrador'] },
  { label: 'Usuarios', path: '/users', icon: Shield, roles: ['Administrador'] },
  { label: 'Canchas', path: '/courts', icon: Settings, roles: ['Administrador'] },
  { label: 'Turnos', path: '/turns', icon: CalendarDays, roles: ['Administrador'] },
  { label: 'Promociones', path: '/promotions', icon: Percent, roles: ['Administrador'] },
  { label: 'Reservas', path: '/reservations', icon: BarChart3, roles: ['Administrador', 'Recepcion'] },
  { label: 'Pagos', path: '/payments', icon: CreditCard, roles: ['Administrador', 'Recepcion'] },
  { label: 'Reportes', path: '/reports', icon: FileText, roles: ['Administrador', 'Recepcion'] },
  { label: 'Auditoría', path: '/audit', icon: History, roles: ['Administrador'] },
]

export function AppLayout() {
  const { logout, user } = useAuth()
  const location = useLocation()
  const visibleItems = navItems.filter((item) => user && item.roles.includes(user.role))
  const currentItem = navItems.find(item => location.pathname.startsWith(item.path)) ?? navItems[0]
  const userInitial = user?.username?.charAt(0).toUpperCase() ?? 'A'

  return (
    <main className="min-h-[100dvh] bg-[#F8FAFC] bg-[radial-gradient(circle_at_18%_12%,rgba(15,118,110,0.08),transparent_30%)] text-[#0F172A]">
      <div className="mx-auto flex min-h-screen max-w-7xl gap-4 p-4">
        <aside className="hidden w-64 shrink-0 rounded-2xl border border-[#E2E8F0] bg-white/95 px-4 py-5 shadow-[0_18px_50px_rgba(15,23,42,0.08)] lg:flex lg:flex-col">
          <div className="mb-8 flex items-start gap-3 px-1">
            <div className="grid size-9 place-items-center rounded-xl bg-white shadow-[0_12px_24px_rgba(15,118,110,0.16)]">
              <img alt="Padelito" className="size-9" src="/assets/brand/logo-mark.svg" />
            </div>
            <div>
              <p className="text-sm font-bold text-[#0F766E]">Padelito v2</p>
              <h1 className="mt-1 text-xl font-bold tracking-tight text-[#0F172A]">Backoffice</h1>
            </div>
          </div>
          <nav className="space-y-1">
            {visibleItems.map((item) => (
              <NavLink
                className={({ isActive }) =>
                  [
                    'relative flex w-full items-center gap-3 overflow-hidden rounded-xl px-3 py-2 text-sm font-semibold transition-all duration-200',
                    isActive
                      ? 'bg-[linear-gradient(90deg,rgba(15,118,110,0.12),rgba(15,118,110,0.05))] text-[#0F766E] shadow-[inset_0_0_0_1px_rgba(15,118,110,0.08)] before:absolute before:left-0 before:top-0 before:h-full before:w-1 before:bg-[linear-gradient(180deg,#0F766E_0%,#7C3AED_100%)]'
                      : 'text-[#334155] hover:bg-[#F8FAFC] hover:text-[#0F172A]',
                  ].join(' ')
                }
                key={item.path}
                to={item.path}
              >
                <item.icon aria-hidden="true" className="size-4 shrink-0" strokeWidth={1.9} />
                <span>{item.label}</span>
              </NavLink>
            ))}
          </nav>
          <div className="mt-auto rounded-2xl border border-[#E2E8F0] bg-[#F8FAFC] p-3 shadow-[inset_0_1px_0_rgba(255,255,255,0.8)]">
            <div className="flex items-center gap-3">
              <div className="grid size-9 place-items-center rounded-xl shadow-[0_10px_20px_rgba(15,118,110,0.18)]">
                <img alt="" aria-hidden="true" className="size-9" src="/assets/brand/padel-ball-icon.svg" />
              </div>
              <div className="min-w-0">
                <p className="truncate text-sm font-bold text-[#0F172A]">Gestion inteligente</p>
                <p className="truncate text-xs font-medium text-[#475569]">para tu club de padel</p>
              </div>
            </div>
          </div>
        </aside>

        <section className="flex min-w-0 flex-1 flex-col gap-4">
          <header className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-[#E2E8F0] bg-white px-5 py-4 shadow-[0_18px_45px_rgba(15,23,42,0.08)]">
            <div>
              <span className="text-xs font-extrabold uppercase tracking-wider text-[#0F766E]">Padelito</span>
              <h2 className="mt-1 text-lg font-bold tracking-tight text-[#0F172A]">{currentItem.label}</h2>
            </div>
            <div className="flex items-stretch overflow-hidden rounded-xl border border-[#E2E8F0] bg-white shadow-[0_10px_24px_rgba(15,23,42,0.06)]">
              <div className="flex min-w-0 items-center gap-3 px-3 py-2">
                <div className="grid size-9 shrink-0 place-items-center rounded-full bg-[#0F766E] text-sm font-bold text-white shadow-[0_10px_20px_rgba(15,118,110,0.2)]">
                  {userInitial}
                </div>
                <div className="min-w-0 pr-2">
                  <p className="truncate text-sm font-bold text-[#0F172A]">{user?.username}</p>
                  <p className="truncate text-xs font-medium text-[#475569]">{user?.role}</p>
                </div>
              </div>
              <button
                className="inline-flex w-12 items-center justify-center border-l border-[#E2E8F0] text-[#334155] transition hover:bg-[#F8FAFC] hover:text-[#0F172A]"
                onClick={logout}
                title="Cerrar sesión"
                type="button"
              >
                <LogOut aria-hidden="true" className="size-4" strokeWidth={1.9} />
              </button>
            </div>
          </header>

          <nav className="flex gap-2 overflow-x-auto rounded-2xl border border-[#E2E8F0] bg-white p-3 shadow-[0_14px_34px_rgba(15,23,42,0.07)] lg:hidden">
            {visibleItems.map((item) => (
              <NavLink
                className={({ isActive }) =>
                  [
                    'inline-flex items-center gap-2 rounded-xl px-3.5 py-2.5 text-sm font-semibold whitespace-nowrap transition',
                    isActive
                      ? 'bg-[linear-gradient(90deg,rgba(15,118,110,0.12),rgba(124,58,237,0.10))] text-[#0F766E] shadow-[inset_0_-2px_0_#7C3AED]'
                      : 'text-[#475569] hover:bg-[#F8FAFC] hover:text-[#0F172A]',
                  ].join(' ')
                }
                key={item.path}
                to={item.path}
              >
                <item.icon aria-hidden="true" className="size-4" strokeWidth={1.9} />
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="flex-1 rounded-2xl border border-white/60 bg-white/35 p-5 shadow-[inset_0_1px_0_rgba(255,255,255,0.65)]">
            <Outlet />
          </div>
        </section>
      </div>
    </main>
  )
}
