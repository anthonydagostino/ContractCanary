import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { PageHeader } from '../../components/PageHeader'
import { NoticeTable } from '../../components/NoticeTable'
import { EmptyState, Pagination, PageLoader, Badge } from '../../components/ui'
import { noticesCsvUrl, useNotices, useNoticeTypes, useProfiles, useUserStats } from '../../hooks/queries'
import { getAccessToken } from '../../lib/api'
import { useAuth } from '../../lib/auth'
import type { NoticeQueryParams, NoticeType } from '../../lib/types'

export function Dashboard() {
  const { me } = useAuth()
  const { data: profiles } = useProfiles()
  const { data: noticeTypes } = useNoticeTypes()
  const { data: stats } = useUserStats()

  const [searchInput, setSearchInput] = useState('')
  const [q, setQ] = useState<NoticeQueryParams>({ page: 1, pageSize: 25, sort: 'posted', direction: 'desc', matchedOnly: false })

  // Debounce the search box into the query.
  useEffect(() => {
    const t = setTimeout(() => setQ((p) => ({ ...p, search: searchInput || undefined, page: 1 })), 300)
    return () => clearTimeout(t)
  }, [searchInput])

  const { data, isLoading, isFetching } = useNotices(q)

  const patch = (partial: Partial<NoticeQueryParams>) => setQ((p) => ({ ...p, ...partial, page: 1 }))
  const toggleType = (t: NoticeType) => {
    const cur = q.noticeTypes ?? []
    patch({ noticeTypes: cur.includes(t) ? cur.filter((x) => x !== t) : [...cur, t] })
  }

  const csvHref = useMemo(() => noticesCsvUrl(q), [q])
  async function downloadCsv() {
    const res = await fetch(csvHref, { headers: { Authorization: `Bearer ${getAccessToken()}` } })
    if (!res.ok) return
    const blob = await res.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url; a.download = 'oppsignal-export.csv'; a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div>
      <PageHeader
        title="Opportunities"
        subtitle="Search and filter federal contract opportunities. Star the ones you're tracking."
        action={me?.limits.canExportCsv ? (
          <button className="btn-secondary" onClick={downloadCsv}>Export CSV</button>
        ) : (
          <span className="text-xs text-slate-400">CSV export is a Pro feature</span>
        )}
      />

      {/* First-run onboarding: no alert profiles yet */}
      {profiles && profiles.length === 0 && (
        <div className="mb-4 rounded-xl border border-canary-200 bg-canary-50/70 p-5 sm:flex sm:items-center sm:justify-between sm:gap-6">
          <div>
            <span className="eyebrow">Get started</span>
            <h2 className="mt-1 text-lg font-semibold text-ink-900">Create your first alert to get matched opportunities</h2>
            <p className="mt-1 max-w-xl text-sm text-slate-600">
              Tell us your NAICS codes, keywords, agencies, and set-asides. We’ll match new SAM.gov opportunities to you and email a daily digest of the ones that fit.
            </p>
          </div>
          <Link to="/app/profiles/new" className="btn-primary mt-3 whitespace-nowrap sm:mt-0">Set up my alert →</Link>
        </div>
      )}

      {/* Your signal */}
      {stats && (
        <div className="mb-4">
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              value={stats.newMatchesThisWeek} label="New matches this week" tone="canary"
              onClick={() => patch({ matchedOnly: true, profileId: undefined, sort: 'posted', direction: 'desc' })}
            />
            <StatCard
              value={stats.closingSoon} label="Closing within 7 days" tone="amber"
              onClick={() => patch({ matchedOnly: true, profileId: undefined, sort: 'deadline', direction: 'asc' })}
            />
            <StatCard
              value={stats.matchedActive} label="Matched & open" tone="ink"
              onClick={() => patch({ matchedOnly: true, profileId: undefined })}
            />
            <StatCard value={stats.saved} label="Saved" tone="slate" to="/app/saved" />
          </div>
          <p className="mt-2 text-xs text-slate-400">
            Watching {stats.totalActive.toLocaleString()} open federal opportunities for you.
          </p>
        </div>
      )}

      {/* Filter bar */}
      <div className="card mb-4 p-4">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center">
          <div className="relative flex-1">
            <svg className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="7" /><path d="M21 21l-4.3-4.3" /></svg>
            <input
              className="input pl-9"
              placeholder="Search title, description, solicitation number…"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <label className="flex items-center gap-2 rounded-lg border border-slate-300 px-3 py-2 text-sm">
              <input type="checkbox" checked={!!q.matchedOnly} onChange={(e) => patch({ matchedOnly: e.target.checked, profileId: undefined })} />
              My matches
            </label>
            <select
              className="input w-auto"
              value={q.profileId ?? ''}
              onChange={(e) => patch({ profileId: e.target.value || undefined, matchedOnly: false })}
            >
              <option value="">All profiles</option>
              {profiles?.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
            <select
              className="input w-auto"
              value={`${q.sort}:${q.direction}`}
              onChange={(e) => { const [sort, direction] = e.target.value.split(':'); patch({ sort: sort as 'posted' | 'deadline', direction: direction as 'asc' | 'desc' }) }}
            >
              <option value="posted:desc">Newest first</option>
              <option value="posted:asc">Oldest first</option>
              <option value="deadline:asc">Deadline soonest</option>
            </select>
          </div>
        </div>
        {noticeTypes && (
          <div className="mt-3 flex flex-wrap gap-2">
            {noticeTypes.map((t) => {
              const active = (q.noticeTypes ?? []).includes(t.value)
              return (
                <button
                  key={t.value}
                  onClick={() => toggleType(t.value)}
                  className={'rounded-full border px-3 py-1 text-xs transition ' + (active ? 'border-brand-600 bg-brand-600 text-white' : 'border-slate-300 bg-white text-slate-600 hover:border-slate-400')}
                >
                  {t.label}
                </button>
              )
            })}
          </div>
        )}
      </div>

      {isLoading ? (
        <PageLoader />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title="No opportunities match these filters"
          hint="Try clearing filters, or create a match profile so we can surface the right ones."
        />
      ) : (
        <div className="space-y-4">
          <div className="flex items-center justify-between text-sm text-slate-500">
            <span>{data.total.toLocaleString()} result{data.total === 1 ? '' : 's'} {isFetching && <span className="text-slate-300">· updating…</span>}</span>
            {q.matchedOnly && <Badge tone="blue">Matched to your profiles</Badge>}
          </div>
          <NoticeTable items={data.items} />
          <Pagination page={data.page} totalPages={data.totalPages} onChange={(page) => setQ((p) => ({ ...p, page }))} />
        </div>
      )}
    </div>
  )
}

function StatCard({ value, label, tone, onClick, to }: {
  value: number
  label: string
  tone: 'canary' | 'amber' | 'ink' | 'slate'
  onClick?: () => void
  to?: string
}) {
  const toneCls = { canary: 'text-canary-700', amber: 'text-amber-600', ink: 'text-ink-900', slate: 'text-slate-700' }[tone]
  const cls = 'card block w-full p-4 text-left transition hover:-translate-y-0.5 hover:shadow-lift'
  const inner = (
    <>
      <span className={`block text-3xl font-bold tabular-nums ${toneCls}`}>{value.toLocaleString()}</span>
      <span className="mt-0.5 block text-xs font-medium text-slate-500">{label}</span>
    </>
  )
  return to ? <Link to={to} className={cls}>{inner}</Link> : <button type="button" onClick={onClick} className={cls}>{inner}</button>
}
