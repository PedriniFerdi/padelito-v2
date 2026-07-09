import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { PublicOnlyRoute } from '@/features/auth/PublicOnlyRoute'
import { LoginPage } from '@/pages/LoginPage'
import { PlaceholderPage } from '@/pages/PlaceholderPage'

const pages = [
  {
    path: 'dashboard',
    title: 'Dashboard',
    description: 'Resumen operativo inicial. Las metricas reales llegan en la etapa de pagos y dashboard.',
  },
  {
    path: 'clients',
    title: 'Clientes',
    description: 'Gestion de jugadores y datos de contacto prevista para la etapa de catalogos operativos.',
  },
  {
    path: 'employees',
    title: 'Empleados',
    description: 'Administracion del personal interno del club, visible para rol Administrador.',
  },
  {
    path: 'users',
    title: 'Usuarios',
    description: 'Alta, roles y estado de usuarios internos. El backend ya valida autenticacion por JWT.',
  },
  {
    path: 'courts',
    title: 'Canchas',
    description: 'Catalogo de canchas y precios por hora para alimentar el flujo de reservas.',
  },
  {
    path: 'turns',
    title: 'Turnos',
    description: 'Configuracion de horarios disponibles por cancha para prevenir doble ocupacion.',
  },
  {
    path: 'promotions',
    title: 'Promociones',
    description: 'Descuentos activos y vigentes que se aplicaran al precio final de reservas.',
  },
  {
    path: 'reservations',
    title: 'Reservas',
    description: 'Nucleo funcional del MVP. En esta etapa queda protegido y listo para implementar.',
  },
  {
    path: 'payments',
    title: 'Pagos',
    description: 'Registro de pagos asociados a reservas, previsto para la etapa posterior al flujo de reservas.',
  },
  {
    path: 'reports',
    title: 'Reportes',
    description: 'Consultas operativas y reportes por fecha/estado cuando el flujo principal este completo.',
  },
]

export function AppRouter() {
  return (
    <Routes>
      <Route element={<PublicOnlyRoute />}>
        <Route element={<LoginPage />} path="/login" />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route element={<Navigate replace to="/dashboard" />} index />
          {pages.map((page) => (
            <Route
              element={<PlaceholderPage description={page.description} title={page.title} />}
              key={page.path}
              path={page.path}
            />
          ))}
        </Route>
      </Route>

      <Route element={<Navigate replace to="/dashboard" />} path="*" />
    </Routes>
  )
}
