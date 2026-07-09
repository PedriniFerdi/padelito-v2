import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { PublicOnlyRoute } from '@/features/auth/PublicOnlyRoute'
import { ClientsPage, CourtsPage, EmployeesPage, PromotionsPage, TurnsPage, UsersPage } from '@/pages/CatalogPages'
import { LoginPage } from '@/pages/LoginPage'
import { PlaceholderPage } from '@/pages/PlaceholderPage'

const pages = [
  {
    path: 'dashboard',
    title: 'Dashboard',
    description: 'Resumen operativo inicial. Las metricas reales llegan en la etapa de pagos y dashboard.',
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
          <Route element={<ClientsPage />} path="clients" />
          <Route element={<EmployeesPage />} path="employees" />
          <Route element={<UsersPage />} path="users" />
          <Route element={<CourtsPage />} path="courts" />
          <Route element={<TurnsPage />} path="turns" />
          <Route element={<PromotionsPage />} path="promotions" />
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
