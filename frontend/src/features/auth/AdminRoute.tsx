import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'

export function AdminRoute() {
  const { user } = useAuth()
  return user?.role === 'Admin' ? <Outlet /> : <Navigate replace to="/dashboard" />
}
