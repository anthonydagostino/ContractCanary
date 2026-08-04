import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, clearTokens, getRefreshToken, onAuthFailure, storeTokens } from './api'
import type { AuthTokens, Me } from './types'

interface AuthState {
  me: Me | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  refreshMe: () => Promise<void>
}

const AuthContext = createContext<AuthState | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [me, setMe] = useState<Me | null>(null)
  const [loading, setLoading] = useState(true)

  async function loadMe() {
    try {
      const { data } = await api.get<Me>('/auth/me')
      setMe(data)
    } catch {
      setMe(null)
    }
  }

  useEffect(() => {
    // On boot, if we have a refresh token, hydrate the session.
    async function boot() {
      if (getRefreshToken()) {
        await loadMe()
      }
      setLoading(false)
    }
    boot()
  }, [])

  // When the refresh token is rejected mid-session (expired, or revoked by a
  // password change elsewhere), drop `me` so ProtectedRoute sends the user to
  // login instead of leaving a dead session rendered.
  useEffect(() => onAuthFailure(() => setMe(null)), [])

  const value = useMemo<AuthState>(
    () => ({
      me,
      loading,
      async login(email, password) {
        const { data } = await api.post<AuthTokens>('/auth/login', { email, password })
        storeTokens(data)
        await loadMe()
      },
      async logout() {
        const rt = getRefreshToken()
        try {
          if (rt) await api.post('/auth/logout', { refreshToken: rt })
        } catch {
          /* ignore */
        }
        clearTokens()
        setMe(null)
      },
      refreshMe: loadMe,
    }),
    [me, loading],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
