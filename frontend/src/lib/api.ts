import axios, { AxiosError, type AxiosRequestConfig } from 'axios'
import type { AuthTokens } from './types'

const REFRESH_KEY = 'oppsignal.refresh'

let accessToken: string | null = null
export const getAccessToken = () => accessToken
export const setAccessToken = (t: string | null) => { accessToken = t }

export const getRefreshToken = () => localStorage.getItem(REFRESH_KEY)
export const setRefreshToken = (t: string | null) =>
  t ? localStorage.setItem(REFRESH_KEY, t) : localStorage.removeItem(REFRESH_KEY)

export function storeTokens(tokens: AuthTokens) {
  setAccessToken(tokens.accessToken)
  setRefreshToken(tokens.refreshToken)
}

export function clearTokens() {
  setAccessToken(null)
  setRefreshToken(null)
}

export const api = axios.create({ baseURL: '/api' })

// A bare client (no interceptors) for the refresh call itself.
const bare = axios.create({ baseURL: '/api' })

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

let refreshing: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const rt = getRefreshToken()
  if (!rt) return null
  try {
    const { data } = await bare.post<AuthTokens>('/auth/refresh', { refreshToken: rt })
    storeTokens(data)
    return data.accessToken
  } catch {
    clearTokens()
    return null
  }
}

api.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as (AxiosRequestConfig & { _retried?: boolean }) | undefined
    const url = original?.url ?? ''
    const isAuthEndpoint = url.includes('/auth/login') || url.includes('/auth/refresh')

    if (error.response?.status === 401 && original && !original._retried && !isAuthEndpoint) {
      original._retried = true
      refreshing = refreshing ?? refreshAccessToken()
      const newToken = await refreshing
      refreshing = null
      if (newToken) {
        original.headers = original.headers ?? {}
        original.headers.Authorization = `Bearer ${newToken}`
        return api(original)
      }
    }
    return Promise.reject(error)
  },
)

/** Extract a human-readable message from an API error (ProblemDetails). */
export function apiError(err: unknown, fallback = 'Something went wrong.'): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { detail?: string; title?: string; errors?: Record<string, string[]> } | undefined
    if (data?.errors) {
      const first = Object.values(data.errors)[0]
      if (first?.length) return first[0]
    }
    return data?.detail || data?.title || err.message || fallback
  }
  return fallback
}
