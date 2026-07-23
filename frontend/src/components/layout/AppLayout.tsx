import { useState } from 'react'
import {
  BarChart3,
  CalendarDays,
  CreditCard,
  FileText,
  History,
  LayoutDashboard,
  LogOut,
  Percent,
  Search,
  Settings,
  Shield,
  Users,
  UserSquare,
  type LucideIcon,
} from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'
import type { Role } from '@/types/api'

type NavItem = {
  label: string
  path: string
  icon: LucideIcon
  roles: Role[]
}

const allRoles: Role[] = ['Administrador', 'Recepcion', 'Empleado']

const navGroups: NavItem[][] = [
  [
    { label: 'Dashboard', path: '/dashboard', icon: LayoutDashboard, roles: allRoles },
    { label: 'Clientes', path: '/clients', icon: Users, roles: ['Administrador', 'Recepcion'] },
    { label: 'Empleados', path: '/employees', icon: UserSquare, roles: ['Administrador'] },
    { label: 'Usuarios', path: '/users', icon: Shield, roles: ['Administrador'] },
  ],
  [
    { label: 'Canchas', path: '/courts', icon: Settings, roles: ['Administrador'] },
    { label: 'Turnos', path: '/turns', icon: CalendarDays, roles: ['Administrador'] },
  ],
  [
    { label: 'Promociones', path: '/promotions', icon: Percent, roles: ['Administrador'] },
    { label: 'Reservas', path: '/reservations', icon: BarChart3, roles: ['Administrador', 'Recepcion'] },
    { label: 'Pagos', path: '/payments', icon: CreditCard, roles: ['Administrador', 'Recepcion'] },
    { label: 'Reportes', path: '/reports', icon: FileText, roles: ['Administrador', 'Recepcion'] },
    { label: 'Auditoría', path: '/audit', icon: History, roles: ['Administrador'] },
  ],
]

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  [
    'relative flex h-10 w-full items-center gap-3 px-4 text-sm font-semibold transition-[background-color,color] duration-200',
    isActive
      ? 'bg-[#2d3449] text-white after:absolute after:inset-y-0 after:right-0 after:w-0.5 after:bg-[#dae2fd]'
      : 'text-[#bdc9c1] hover:bg-white/[0.045] hover:text-white',
  ].join(' ')

export function AppLayout() {
  const { logout, user } = useAuth()
  const [navigationSearch, setNavigationSearch] = useState('')
  const normalizedSearch = navigationSearch.trim().toLocaleLowerCase('es')
  const visibleGroups = navGroups
    .map((group) =>
      group.filter(
        (item) =>
          user &&
          item.roles.includes(user.role) &&
          (!normalizedSearch || item.label.toLocaleLowerCase('es').includes(normalizedSearch)),
      ),
    )
    .filter((group) => group.length > 0)
  const visibleItems = visibleGroups.flat()
  const userInitial = user?.username?.charAt(0).toUpperCase() ?? 'A'

  return (
    <main className="padelito-app-theme min-h-[100dvh] bg-[#0b1326] text-[#dae2fd]">
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-[164px] flex-col border-r border-white/[0.08] bg-[#131b2e]/95 backdrop-blur-2xl lg:flex">
        <div className="flex h-[62px] shrink-0 items-center border-b border-white/[0.08] px-4">
          <img alt="Padelito" className="size-8" src="/assets/brand/logo-mark.svg" />
          <h1 className="font-display ml-1.5 whitespace-nowrap text-[19px] font-extrabold tracking-[-0.035em] text-[#f4f6f8]">
            Padelito <span className="text-sm text-[#4edea3]">v2</span>
          </h1>
        </div>

        <nav aria-label="Navegación principal" className="min-h-0 flex-1 overflow-y-auto py-3">
          {visibleGroups.map((group, groupIndex) => (
            <div
              className={groupIndex === 1 ? 'mt-9' : groupIndex === 2 ? 'mt-2 border-t border-white/[0.12] pt-2' : ''}
              key={group.map((item) => item.path).join('-')}
            >
              {group.map((item) => (
                <NavLink className={navLinkClass} key={item.path} to={item.path}>
                  <item.icon aria-hidden="true" className="size-5 shrink-0 text-current" strokeWidth={1.8} />
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          ))}
          {visibleItems.length === 0 ? (
            <p className="px-4 py-3 text-xs font-medium leading-5 text-[#9aa2ad]">No hay secciones que coincidan.</p>
          ) : null}
        </nav>
      </aside>

      <section className="relative min-h-[100dvh] lg:pl-[164px]">
        <div aria-hidden="true" className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_88%_-8%,rgba(255,255,255,0.035),transparent_38%)] lg:left-[164px]" />

        <header className="sticky top-0 z-20 flex h-[62px] items-center justify-between gap-4 border-b border-white/[0.09] bg-[#0b1326]/90 px-3 backdrop-blur-2xl">
          <label className="relative block w-full max-w-[350px]" htmlFor="navigation-search">
            <span className="sr-only">Buscar sección</span>
            <Search
              aria-hidden="true"
              className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-[#8c93a1]"
              strokeWidth={1.8}
            />
            <input
              autoComplete="off"
              className="h-[34px] w-full rounded-lg border border-[#3e4943] bg-[#171f33] pl-9 pr-3 text-sm font-medium text-[#dae2fd] outline-none transition-[border-color,box-shadow] placeholder:text-[#88948b] focus:border-[#7ad9ad] focus:ring-2 focus:ring-[#7ad9ad]/15"
              id="navigation-search"
              onChange={(event) => setNavigationSearch(event.target.value)}
              placeholder="Buscar"
              type="search"
              value={navigationSearch}
            />
          </label>

          <div className="flex h-full shrink-0 items-center">
            <div className="flex items-center gap-2.5 px-2 sm:px-3">
              <div className="relative grid size-9 shrink-0 place-items-center rounded-full bg-[#2d3449] text-sm font-bold text-white shadow-[0_7px_20px_rgba(6,14,32,0.35)]">
                {userInitial}
                <span
                  aria-label="Usuario conectado"
                  className="absolute bottom-0 right-0 size-2.5 rounded-full border-2 border-[#171a22] bg-[#4edea3]"
                  role="status"
                />
              </div>
              <div className="hidden min-w-0 sm:block">
                <p className="truncate text-sm font-semibold leading-5 text-[#f0f2f5]">{user?.username}</p>
                <p className="truncate text-xs font-medium leading-4 text-[#969da8]">{user?.role}</p>
              </div>
            </div>
            <button
              className="grid h-full w-11 place-items-center text-[#9da5b0] transition-colors hover:bg-white/[0.045] hover:text-[#6fe0b2] focus-visible:outline-2 focus-visible:outline-offset-[-4px] focus-visible:outline-[#53d6a2]"
              onClick={logout}
              title="Cerrar sesión"
              type="button"
            >
              <LogOut aria-hidden="true" className="size-5" strokeWidth={1.8} />
            </button>
          </div>
        </header>

        <nav className="relative z-10 flex gap-1 overflow-x-auto border-b border-white/[0.08] bg-[#131b2e]/95 p-2 backdrop-blur-xl lg:hidden">
          {visibleItems.map((item) => (
            <NavLink
              className={({ isActive }) =>
                [
                  'inline-flex h-10 shrink-0 items-center gap-2 rounded-lg px-3 text-sm font-semibold whitespace-nowrap transition-colors',
                  isActive ? 'bg-[#2d3449] text-white' : 'text-[#bdc9c1] hover:bg-white/[0.05]',
                ].join(' ')
              }
              key={item.path}
              to={item.path}
            >
              <item.icon aria-hidden="true" className="size-4 text-current" strokeWidth={1.8} />
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="relative z-10 min-h-[calc(100dvh-62px)] p-3 sm:p-6">
          <Outlet />
        </div>
      </section>
    </main>
  )
}
