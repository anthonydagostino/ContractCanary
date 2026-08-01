import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { PageHeader } from '../../components/PageHeader'
import { Badge, EmptyState, PageLoader } from '../../components/ui'
import { deadlineLabel } from '../../lib/format'
import { useSavedNotices, useToggleSaved, useUpdateStatus } from '../../hooks/queries'
import type { PipelineStatus, SavedNoticeItem } from '../../lib/types'

const STATUSES: PipelineStatus[] = ['Reviewing', 'Pursuing', 'Submitted', 'Won', 'Lost', 'Passed']
const statusTone: Record<PipelineStatus, 'gray' | 'blue' | 'indigo' | 'green' | 'red'> = {
  Reviewing: 'gray', Pursuing: 'blue', Submitted: 'indigo', Won: 'green', Lost: 'red', Passed: 'gray',
}

export function Saved() {
  const { data, isLoading } = useSavedNotices()
  const [filter, setFilter] = useState<PipelineStatus | 'All'>('All')

  const counts = useMemo(() => {
    const c: Record<string, number> = { All: data?.length ?? 0 }
    for (const s of STATUSES) c[s] = 0
    for (const it of data ?? []) c[it.status] = (c[it.status] ?? 0) + 1
    return c
  }, [data])

  const items = (data ?? []).filter((it) => filter === 'All' || it.status === filter)

  return (
    <div>
      <PageHeader
        title="Saved & pipeline"
        subtitle="Track the opportunities you're pursuing from review through award."
      />

      {isLoading ? (
        <PageLoader />
      ) : !data || data.length === 0 ? (
        <EmptyState
          title="No saved opportunities yet"
          hint="Star opportunities from the dashboard, then track each one's stage here."
          action={<Link to="/app" className="btn-primary">Browse opportunities</Link>}
        />
      ) : (
        <>
          <div className="mb-4 flex flex-wrap gap-2">
            <FilterChip label="All" count={counts.All} active={filter === 'All'} onClick={() => setFilter('All')} />
            {STATUSES.map((s) => (
              <FilterChip key={s} label={s} count={counts[s]} active={filter === s} onClick={() => setFilter(s)} />
            ))}
          </div>

          {items.length === 0 ? (
            <p className="text-sm text-slate-500">Nothing in this stage.</p>
          ) : (
            <div className="space-y-3">
              {items.map((it) => <PipelineCard key={it.noticeId} item={it} />)}
            </div>
          )}
        </>
      )}
    </div>
  )
}

function FilterChip({ label, count, active, onClick }: { label: string; count: number; active: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className={
        'rounded-full border px-3 py-1 text-xs font-medium transition ' +
        (active ? 'border-ink-900 bg-ink-900 text-white' : 'border-slate-300 bg-white text-slate-600 hover:border-slate-400')
      }
    >
      {label} <span className={active ? 'text-slate-300' : 'text-slate-400'}>· {count}</span>
    </button>
  )
}

function PipelineCard({ item }: { item: SavedNoticeItem }) {
  const update = useUpdateStatus()
  const toggle = useToggleSaved()
  const dl = deadlineLabel(item.responseDeadline)

  return (
    <div className="card p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <Link to={`/app/opportunities/${item.noticeId}`} className="font-semibold text-ink-900 hover:underline">{item.title}</Link>
          {item.agencyPath && <p className="mt-0.5 truncate text-sm text-slate-500">{item.agencyPath}</p>}
        </div>
        <Badge tone={statusTone[item.status]}>{item.status}</Badge>
      </div>

      <div className="mt-2 flex flex-wrap items-center gap-1.5">
        <Badge tone="indigo">{item.typeLabel}</Badge>
        {item.setAside !== 'None' && item.setAsideLabel && <Badge tone="green">{item.setAsideLabel}</Badge>}
        <Badge tone={dl.tone}>{dl.text}</Badge>
        {item.naicsCode && <Badge tone="gray">NAICS {item.naicsCode}</Badge>}
        {!item.isActive && <Badge tone="gray">Inactive</Badge>}
      </div>

      {item.note && <p className="mt-2 text-sm text-slate-600">{item.note}</p>}

      <div className="mt-3 flex flex-wrap items-center gap-3">
        <label className="flex items-center gap-2 text-xs text-slate-500">
          Stage
          <select
            className="input w-auto py-1 text-sm"
            value={item.status}
            onChange={(e) => update.mutate({ noticeId: item.noticeId, status: e.target.value as PipelineStatus })}
          >
            {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </label>
        <Link to={`/app/opportunities/${item.noticeId}`} className="text-sm text-slate-500 hover:text-slate-700">View</Link>
        <button
          className="text-sm text-slate-400 hover:text-red-600"
          onClick={() => toggle.mutate({ noticeId: item.noticeId, save: false })}
        >
          Remove
        </button>
      </div>
    </div>
  )
}
