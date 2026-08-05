import { describe, expect, it, vi } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { useSaveProfile, useToggleSaved } from './queries'

vi.mock('../lib/api', () => ({
  api: {
    get: vi.fn(async () => ({ data: {} })),
    post: vi.fn(async () => ({ data: {} })),
    put: vi.fn(async () => ({ data: {} })),
    delete: vi.fn(async () => ({ data: {} })),
  },
  storeTokens: vi.fn(),
}))

/**
 * Regression tests for stale-cache bugs: a mutation that forgets to invalidate
 * a query key leaves the user staring at pre-mutation data for up to staleTime.
 * We shipped two: saving a profile didn't invalidate ['profile', id] (the
 * editor reopened with pre-save values), and toggling saved didn't invalidate
 * ['notice-similar'] (stale Saved badges in the similar list).
 */

function setup() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false, staleTime: 60_000 } } })
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={qc}>{children}</QueryClientProvider>
  )
  return { qc, wrapper }
}

describe('useSaveProfile cache invalidation', () => {
  it('invalidates the individual profile query so the editor refetches after save', async () => {
    const { qc, wrapper } = setup()
    qc.setQueryData(['profile', 'p1'], { id: 'p1', name: 'Before save' })
    qc.setQueryData(['profiles'], [])

    const { result } = renderHook(() => useSaveProfile(), { wrapper })
    await act(() => result.current.mutateAsync({ id: 'p1', input: { name: 'After' } as never }))

    expect(qc.getQueryState(['profile', 'p1'])?.isInvalidated).toBe(true)
    expect(qc.getQueryState(['profiles'])?.isInvalidated).toBe(true)
  })

  it('invalidates recompetes, whose results derive from profile NAICS codes', async () => {
    // Regression: the Recompetes empty state links to "Review my profiles";
    // adding a code and returning within staleTime showed the stale empty page.
    const { qc, wrapper } = setup()
    qc.setQueryData(['recompetes', 1], { items: [], total: 0, page: 1, totalPages: 0 })

    const { result } = renderHook(() => useSaveProfile(), { wrapper })
    await act(() => result.current.mutateAsync({ id: 'p1', input: { name: 'X' } as never }))

    expect(qc.getQueryState(['recompetes', 1])?.isInvalidated).toBe(true)
  })
})

describe('useToggleSaved cache invalidation', () => {
  it('invalidates similar-notices so Saved badges update', async () => {
    const { qc, wrapper } = setup()
    qc.setQueryData(['notice-similar', 'n1'], [])
    qc.setQueryData(['saved-pipeline'], [])
    qc.setQueryData(['user-stats'], { saved: 0 })

    const { result } = renderHook(() => useToggleSaved(), { wrapper })
    await act(() => result.current.mutateAsync({ noticeId: 'n1', save: true }))

    expect(qc.getQueryState(['notice-similar', 'n1'])?.isInvalidated).toBe(true)
    expect(qc.getQueryState(['saved-pipeline'])?.isInvalidated).toBe(true)
    expect(qc.getQueryState(['user-stats'])?.isInvalidated).toBe(true)
  })
})
