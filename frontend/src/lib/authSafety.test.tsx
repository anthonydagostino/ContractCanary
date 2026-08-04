import { afterEach, describe, expect, it, vi } from 'vitest'
import { act, render, screen, waitFor } from '@testing-library/react'
import { AuthProvider, useAuth } from './auth'

// Keep the real emitAuthFailure/onAuthFailure/token helpers; stub only the
// HTTP client so no network is attempted.
vi.mock('./api', async (importOriginal) => {
  const mod = await importOriginal<typeof import('./api')>()
  return {
    ...mod,
    api: {
      get: vi.fn(async (url: string) => {
        if (url === '/auth/me') return { data: { email: 'user@example.com', timeZoneId: 'America/New_York' } }
        return { data: {} }
      }),
      post: vi.fn(async () => ({ data: {} })),
    },
  }
})

import { emitAuthFailure, getRefreshToken, setRefreshToken } from './api'

function WhoAmI() {
  const { me, loading } = useAuth()
  if (loading) return <p>loading</p>
  return <p>{me ? `signed-in:${me.email}` : 'signed-out'}</p>
}

afterEach(() => {
  setRefreshToken(null)
  vi.restoreAllMocks()
})

describe('auth failure signal', () => {
  it('clears the session when the refresh token is rejected mid-session', async () => {
    // Regression: a rejected refresh cleared tokens but left `me` set, so the
    // app stayed rendered with every request failing until a manual reload.
    setRefreshToken('some-refresh-token')
    render(
      <AuthProvider>
        <WhoAmI />
      </AuthProvider>,
    )
    await waitFor(() => expect(screen.getByText('signed-in:user@example.com')).toBeInTheDocument())

    act(() => emitAuthFailure())

    expect(screen.getByText('signed-out')).toBeInTheDocument()
  })
})

describe('storage safety', () => {
  it('getRefreshToken returns null instead of throwing when localStorage is blocked', () => {
    // Regression: a throwing localStorage during boot left the app on the
    // loading spinner forever (setLoading(false) never ran).
    const spy = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('blocked', 'SecurityError')
    })
    expect(getRefreshToken()).toBeNull()
    spy.mockRestore()
  })

  it('setRefreshToken swallows storage failures', () => {
    const spy = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('quota', 'QuotaExceededError')
    })
    expect(() => setRefreshToken('t')).not.toThrow()
    spy.mockRestore()
  })
})
