import { Link } from 'react-router-dom'
import { PageHeader } from '../../components/PageHeader'
import { Badge, EmptyState, PageLoader } from '../../components/ui'
import { useNotices } from '../../hooks/queries'
import { deadlineLabel, formatDateTime } from '../../lib/format'

export function Deadlines() {
  // Saved items sorted by response deadline (soonest first). API sorts nulls last.
  const { data, isLoading } = useNotices({ savedOnly: true, sort: 'deadline', direction: 'asc', page: 1, pageSize: 100 })

  const withDeadline = (data?.items ?? []).filter((n) => n.responseDeadline)
  const noDeadline = (data?.items ?? []).filter((n) => !n.responseDeadline)

  return (
    <div>
      <PageHeader title="Deadline tracker" subtitle="Your saved opportunities, sorted by response deadline." />

      {isLoading ? (
        <PageLoader />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title="Nothing to track yet"
          hint="Save opportunities and their deadlines will show up here, soonest first."
          action={<Link to="/app" className="btn-primary">Browse opportunities</Link>}
        />
      ) : (
        <div className="space-y-3">
          {withDeadline.map((n) => {
            const dl = deadlineLabel(n.responseDeadline)
            return (
              <Link key={n.noticeId} to={`/app/opportunities/${n.noticeId}`} className="card flex items-center gap-4 p-4 transition hover:shadow-md">
                <div className={`flex h-14 w-16 flex-none flex-col items-center justify-center rounded-lg ${dl.tone === 'red' ? 'bg-red-50 text-red-700' : dl.tone === 'amber' ? 'bg-amber-50 text-amber-800' : dl.tone === 'green' ? 'bg-green-50 text-green-700' : 'bg-slate-100 text-slate-500'}`}>
                  <span className="text-xs font-medium">{dl.text.split(' ')[0]}</span>
                  <span className="text-[10px] uppercase">{dl.text.includes('day') ? 'days' : ''}</span>
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium text-slate-900">{n.title}</p>
                  <p className="truncate text-sm text-slate-500">{n.agencyPath}</p>
                </div>
                <div className="hidden text-right sm:block">
                  <p className="text-sm text-slate-700">{formatDateTime(n.responseDeadline)}</p>
                  <Badge tone={dl.tone}>{dl.text}</Badge>
                </div>
              </Link>
            )
          })}

          {noDeadline.length > 0 && (
            <div className="pt-4">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">No deadline</p>
              {noDeadline.map((n) => (
                <Link key={n.noticeId} to={`/app/opportunities/${n.noticeId}`} className="card mb-2 flex items-center gap-4 p-4 hover:shadow-md">
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium text-slate-900">{n.title}</p>
                    <p className="truncate text-sm text-slate-500">{n.agencyPath}</p>
                  </div>
                  <Badge tone="gray">No deadline</Badge>
                </Link>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
