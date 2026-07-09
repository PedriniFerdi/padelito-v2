import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthProvider'

export function PublicOnlyRoute() {
  const auth = useAuth()

  if (auth.isLoading) {
    return (
      <main className="grid min-h-screen place-items-center bg-slate-50 text-sm font-medium text-slate-600">
        Cargando sesion...
      </main>
    )
  }

  return auth.isAuthenticated ? <Navigate replace to="/dashboard" /> : <Outlet />
}
