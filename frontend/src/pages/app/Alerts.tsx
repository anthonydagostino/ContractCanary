import { useEffect, useRef } from 'react'
import { Link } from 'react-router-dom'
import { PageHeader } from '../../components/PageHeader'
import { Badge, EmptyState, PageLoader } from '../../components/ui'
import { formatDateTime } from '../../lib/format'
import { useAlerts, useMarkAlertsRead } from '../../hooks/queries'
import type { Alert } from '../../lib/types'

export function Alerts() {
  const { data, isLoading } = useAlerts()
  const markRead = useMarkAlertsRead()
  const marked = useRef(false)

  // Opening the page clears the unread badge.
  useEffect(() => {
    if (!marked.current && data && data.some((a) => !a.read)) {
      marked.current = true
      markRead.mutate()
    }
  }, [data, markRead])

  return (
    <div>
      <PageHeader
        title="Alerts"
        subtitle="Changes to opportunities you're tracking — deadlines moved, or an opportunity cancelled."
      />
      {isLoading ? (
        <PageLoader />
      ) : !data || data.length === 0 ? (
        <EmptyState
          title="No alerts yet"
          hint="When an opportunity you've matched or saved changes — a moved deadline or a cancellation — it shows up here."
          action={<Link to="/app" className="btn-primary">Browse opportunities</Link>}
        />
      ) : (
        <div className="space-y-3">
          {data.map((a) => <AlertCard key={a.id} alert={a} />)}
        </div>
      )}
    </div>
  )
}

function AlertCard({ alert }: { alert: Alert }) {
  const cancelled = alert.type === 'Cancelled'
  return (
    <div className="card p-4">
      <div className="flex items-start gap-3">
        <span className={
          'mt-0.5 flex h-8 w-8 flex-none items-center justify-center rounded-lg ' +
          (cancelled ? 'bg-red-100 text-red-600' : 'bg-amber-100 text-amber-700')
        }>
          {cancelled ? (
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M18 6L6 18M6 6l12 12" /></svg>
          ) : (
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>
          )}
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone={cancelled ? 'red' : 'amber'}>{alert.typeLabel}</Badge>
            {!alert.read && <span className="h-1.5 w-1.5 rounded-full bg-canary-500" title="New" />}
            <span className="text-xs text-slate-500">{formatDateTime(alert.createdAt)}</span>
          </div>
          <p className="mt-1 text-sm font-medium text-ink-900">{alert.message}</p>
          <Link to={`/app/opportunities/${alert.noticeId}`} className="mt-0.5 block truncate text-sm text-slate-500 hover:text-slate-700">
            {alert.noticeTitle}
          </Link>
        </div>
      </div>
    </div>
  )
}
