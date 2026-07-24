import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { PublicOnlyRoute } from '@/features/auth/PublicOnlyRoute'
import { ClientsPage, CourtsPage, EmployeesPage, PromotionsPage, TurnsPage, UsersPage } from '@/pages/CatalogPages'
import { LoginPage } from '@/pages/LoginPage'
import { ReservationsPage } from '@/pages/ReservationsPage'
import { OperationsBoardPage } from '@/pages/OperationsBoardPage'
import { DashboardPage } from '@/pages/DashboardPage'
import { PaymentsPage } from '@/pages/PaymentsPage'
import { ReportsPage } from '@/pages/ReportsPage'
import { AuditPage } from '@/pages/AuditPage'
import { AdminRoute } from '@/features/auth/AdminRoute'

export function AppRouter() {
  return (
    <Routes>
      <Route element={<PublicOnlyRoute />}>
        <Route element={<LoginPage />} path="/login" />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route element={<Navigate replace to="/operations" />} index />
          <Route element={<ClientsPage />} path="clients" />
          <Route element={<EmployeesPage />} path="employees" />
          <Route element={<UsersPage />} path="users" />
          <Route element={<CourtsPage />} path="courts" />
          <Route element={<TurnsPage />} path="turns" />
          <Route element={<PromotionsPage />} path="promotions" />
          <Route element={<OperationsBoardPage />} path="operations" />
          <Route element={<ReservationsPage />} path="reservations" />
          <Route element={<DashboardPage />} path="dashboard" />
          <Route element={<PaymentsPage />} path="payments" />
          <Route element={<ReportsPage />} path="reports" />
          <Route element={<AdminRoute />}>
            <Route element={<AuditPage />} path="audit" />
          </Route>
        </Route>
      </Route>

      <Route element={<Navigate replace to="/operations" />} path="*" />
    </Routes>
  )
}
