import { QueryClient, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, storeTokens } from '../lib/api'
import { noticeQueryString } from '../lib/noticeParams'
import type {
  AdminMetrics, AgencyDto, Alert, AuthTokens, MatchProfile, Me, Meta, NaicsDto, NoticeDetail, NoticeListItem,
  NoticeQueryParams, NoticeTypeDto, PagedResult, PipelineStatus, ProfileInput, PscDto,
  SavedNoticeItem, SetAsideDto, UserStats,
} from '../lib/types'

export const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false, staleTime: 30_000 } },
})

// ---- Meta (public) ----
export const useMeta = () =>
  useQuery({ queryKey: ['meta'], queryFn: async () => (await api.get<Meta>('/meta')).data, staleTime: Infinity })

// ---- Profiles ----
export const useProfiles = () =>
  useQuery({ queryKey: ['profiles'], queryFn: async () => (await api.get<MatchProfile[]>('/profiles')).data })

export const useProfile = (id?: string) =>
  useQuery({
    queryKey: ['profile', id],
    queryFn: async () => (await api.get<MatchProfile>(`/profiles/${id}`)).data,
    enabled: !!id,
  })

export function useSaveProfile() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, input }: { id?: string; input: ProfileInput }) =>
      id ? (await api.put<MatchProfile>(`/profiles/${id}`, input)).data
         : (await api.post<MatchProfile>('/profiles', input)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profiles'] })
      qc.invalidateQueries({ queryKey: ['notices'] })
    },
  })
}

export function useDeleteProfile() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.delete(`/profiles/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['profiles'] }),
  })
}

// ---- Notices ----
export const useUserStats = () =>
  useQuery({ queryKey: ['user-stats'], queryFn: async () => (await api.get<UserStats>('/notices/stats')).data })

export const useSavedNotices = () =>
  useQuery({ queryKey: ['saved-pipeline'], queryFn: async () => (await api.get<SavedNoticeItem[]>('/notices/saved')).data })

// ---- Change alerts ----
export const useAlerts = () =>
  useQuery({ queryKey: ['alerts'], queryFn: async () => (await api.get<Alert[]>('/alerts')).data })

export const useUnreadAlertCount = () =>
  useQuery({
    queryKey: ['alerts-unread'],
    queryFn: async () => (await api.get<{ count: number }>('/alerts/unread-count')).data,
    refetchInterval: 60_000,
  })

export function useMarkAlertsRead() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async () => api.post('/alerts/read-all'),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['alerts'] })
      qc.invalidateQueries({ queryKey: ['alerts-unread'] })
    },
  })
}

export function useUpdateStatus() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ noticeId, status }: { noticeId: string; status: PipelineStatus }) =>
      api.put(`/notices/${noticeId}/status`, { status }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['saved-pipeline'] }),
  })
}

export const useNotices = (q: NoticeQueryParams) =>
  useQuery({
    queryKey: ['notices', q],
    queryFn: async () =>
      (await api.get<PagedResult<NoticeListItem>>(`/notices?${noticeQueryString(q)}`)).data,
    placeholderData: (prev) => prev,
  })

export const useNoticeDetail = (noticeId?: string) =>
  useQuery({
    queryKey: ['notice', noticeId],
    queryFn: async () => (await api.get<NoticeDetail>(`/notices/${noticeId}`)).data,
    enabled: !!noticeId,
  })

export const useSimilarNotices = (noticeId?: string) =>
  useQuery({
    queryKey: ['notice-similar', noticeId],
    queryFn: async () => (await api.get<NoticeListItem[]>(`/notices/${noticeId}/similar`)).data,
    enabled: !!noticeId,
    staleTime: 60_000,
  })

export function useToggleSaved() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ noticeId, save, note }: { noticeId: string; save: boolean; note?: string }) =>
      save ? api.put(`/notices/${noticeId}/save`, { note }) : api.delete(`/notices/${noticeId}/save`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['notices'] })
      qc.invalidateQueries({ queryKey: ['notice'] })
      qc.invalidateQueries({ queryKey: ['user-stats'] })
      qc.invalidateQueries({ queryKey: ['saved-pipeline'] })
    },
  })
}

export function noticesCsvUrl(q: NoticeQueryParams): string {
  return `/api/notices/export.csv?${noticeQueryString(q)}`
}

// ---- Reference (typeaheads) ----
export const useNaicsSearch = (q: string) =>
  useQuery({
    queryKey: ['ref-naics', q],
    queryFn: async () => (await api.get<NaicsDto[]>(`/reference/naics?q=${encodeURIComponent(q)}&limit=20`)).data,
    staleTime: 5 * 60_000,
  })

export const usePscSearch = (q: string) =>
  useQuery({
    queryKey: ['ref-psc', q],
    queryFn: async () => (await api.get<PscDto[]>(`/reference/psc?q=${encodeURIComponent(q)}&limit=20`)).data,
    staleTime: 5 * 60_000,
  })

export const useAgencySearch = (q: string) =>
  useQuery({
    queryKey: ['ref-agency', q],
    queryFn: async () => (await api.get<AgencyDto[]>(`/reference/agencies?q=${encodeURIComponent(q)}&limit=40`)).data,
    staleTime: 5 * 60_000,
  })

export const useSetAsides = () =>
  useQuery({
    queryKey: ['ref-setasides'],
    queryFn: async () => (await api.get<SetAsideDto[]>('/reference/set-asides')).data,
    staleTime: Infinity,
  })

export const useNoticeTypes = () =>
  useQuery({
    queryKey: ['ref-noticetypes'],
    queryFn: async () => (await api.get<NoticeTypeDto[]>('/reference/notice-types')).data,
    staleTime: Infinity,
  })

// ---- Admin ----
export const useAdminMetrics = () =>
  useQuery({ queryKey: ['admin-metrics'], queryFn: async () => (await api.get<AdminMetrics>('/admin/metrics')).data })

// ---- Account ----
export function useChangePassword() {
  return useMutation({
    mutationFn: async (input: { currentPassword: string; newPassword: string }) => {
      const { data } = await api.post<AuthTokens>('/auth/change-password', input)
      storeTokens(data) // the old refresh token was revoked; adopt the new pair for this session
      return data
    },
  })
}

export function useUpdateAccount() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { fullName?: string; companyName?: string; timeZoneId?: string }) =>
      api.put('/auth/me', input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['me'] }),
  })
}

// ---- Billing ----
export function useCheckout() {
  return useMutation({
    mutationFn: async (plan: 'Starter' | 'Pro') =>
      (await api.post<{ url: string }>('/billing/checkout', { plan })).data,
  })
}
export function usePortal() {
  return useMutation({ mutationFn: async () => (await api.post<{ url: string }>('/billing/portal', {})).data })
}

export const useMe = () =>
  useQuery({ queryKey: ['me'], queryFn: async () => (await api.get<Me>('/auth/me')).data })
