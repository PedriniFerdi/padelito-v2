import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from 'react'
import { fetchCurrentUser, login as loginRequest } from '@/api/auth.api'
import { setAuthTokenProvider, setUnauthorizedHandler } from '@/api/http'
import type { CurrentUser, LoginRequest } from '@/types/api'

const TOKEN_KEY = 'padelito.auth.token'
const USER_KEY = 'padelito.auth.user'

type AuthContextValue = {
  user: CurrentUser | null
  token: string | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (request: LoginRequest) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: PropsWithChildren) {
  const [token, setToken] = useState(() => localStorage.getItem(TOKEN_KEY))
  const [user, setUser] = useState<CurrentUser | null>(() => {
    const storedUser = localStorage.getItem(USER_KEY)
    return storedUser ? (JSON.parse(storedUser) as CurrentUser) : null
  })
  const [isLoading, setIsLoading] = useState(Boolean(token))

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
    setToken(null)
    setUser(null)
  }, [])

  useEffect(() => {
    setAuthTokenProvider(() => localStorage.getItem(TOKEN_KEY))
    setUnauthorizedHandler(logout)
  }, [logout])

  useEffect(() => {
    let isMounted = true

    async function refreshUser() {
      if (!token) {
        setIsLoading(false)
        return
      }

      try {
        const currentUser = await fetchCurrentUser()
        if (!isMounted) {
          return
        }

        localStorage.setItem(USER_KEY, JSON.stringify(currentUser))
        setUser(currentUser)
      } catch {
        if (isMounted) {
          logout()
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
  }, [logout, token])

  const login = useCallback(async (request: LoginRequest) => {
    const response = await loginRequest(request)
    localStorage.setItem(TOKEN_KEY, response.token)
    localStorage.setItem(USER_KEY, JSON.stringify(response.user))
    setToken(response.token)
    setUser(response.user)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: Boolean(token && user),
      isLoading,
      login,
      logout,
    }),
    [isLoading, login, logout, token, user],
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
