import {
  BarChart3,
  CalendarDays,
  CreditCard,
  FileText,
  LayoutDashboard,
  LogOut,
  Percent,
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
]

export function AppLayout() {
  const { logout, user } = useAuth()
  const visibleItems = navItems.filter((item) => user && item.roles.includes(user.role))

  return (
    <main className="min-h-screen bg-slate-50 text-slate-950">
      <div className="mx-auto flex min-h-screen max-w-7xl">
        <aside className="hidden w-64 shrink-0 border-r border-slate-200 bg-white px-4 py-5 lg:block">
          <div className="mb-8">
            <p className="text-sm font-medium text-emerald-700">Padelito v2</p>
            <h1 className="mt-1 text-xl font-semibold">Backoffice</h1>
          </div>
          <nav className="space-y-1">
            {visibleItems.map((item) => (
              <NavLink
                className={({ isActive }) =>
                  [
                    'flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition',
                    isActive
                      ? 'bg-emerald-50 text-emerald-800'
                      : 'text-slate-600 hover:bg-slate-100 hover:text-slate-950',
                  ].join(' ')
                }
                key={item.path}
                to={item.path}
              >
                <item.icon aria-hidden="true" className="size-4" />
                {item.label}
              </NavLink>
            ))}
          </nav>
        </aside>

        <section className="flex min-w-0 flex-1 flex-col">
          <header className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 bg-white px-5 py-4">
            <div>
              <p className="text-xs font-medium uppercase text-slate-500">Etapa 3</p>
              <h2 className="text-lg font-semibold">Catalogos operativos</h2>
            </div>
            <div className="flex items-center gap-3">
              <div className="min-w-0 text-right">
                <p className="truncate text-sm font-semibold">{user?.username}</p>
                <p className="text-xs text-slate-500">{user?.role}</p>
              </div>
              <button
                className="inline-flex size-10 items-center justify-center rounded-md border border-slate-200 bg-white text-slate-600 hover:bg-slate-100 hover:text-slate-950"
                onClick={logout}
                title="Cerrar sesion"
                type="button"
              >
                <LogOut aria-hidden="true" className="size-4" />
              </button>
            </div>
          </header>

          <nav className="flex gap-2 overflow-x-auto border-b border-slate-200 bg-white px-4 py-3 lg:hidden">
            {visibleItems.map((item) => (
              <NavLink
                className={({ isActive }) =>
                  [
                    'inline-flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium whitespace-nowrap',
                    isActive
                      ? 'bg-emerald-50 text-emerald-800'
                      : 'text-slate-600 hover:bg-slate-100',
                  ].join(' ')
                }
                key={item.path}
                to={item.path}
              >
                <item.icon aria-hidden="true" className="size-4" />
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="flex-1 p-5">
            <Outlet />
          </div>
        </section>
      </div>
    </main>
  )
}
