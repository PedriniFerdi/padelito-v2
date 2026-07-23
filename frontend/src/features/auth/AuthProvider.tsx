import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from 'react'
import { fetchCurrentUser, login as loginRequest, logout as logoutRequest } from '@/api/auth.api'
import { setUnauthorizedHandler } from '@/api/http'
import type { CurrentUser, LoginRequest } from '@/types/api'

type AuthContextValue = {
  user: CurrentUser | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (request: LoginRequest) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const clearSession = useCallback(() => {
    setUser(null)
  }, [])

  const logout = useCallback(() => {
    clearSession()
    void logoutRequest().catch(() => undefined)
  }, [clearSession])

  useEffect(() => {
    setUnauthorizedHandler(clearSession)
  }, [clearSession])

  useEffect(() => {
    let isMounted = true

    async function refreshUser() {
      try {
        const currentUser = await fetchCurrentUser()
        if (!isMounted) {
          return
        }

        setUser(currentUser)
      } catch {
        if (isMounted) {
          clearSession()
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    void refreshUser()

    return () => {
      isMounted = false
    }
  }, [clearSession])

  const login = useCallback(async (request: LoginRequest) => {
    const response = await loginRequest(request)
    setUser(response.user)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isLoading,
      login,
      logout,
    }),
    [isLoading, login, logout, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// oxlint-disable-next-line react/only-export-components -- El hook comparte el contexto privado del provider.
export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.')
  }

  return context
}
