import { useState } from 'react'
import { PageHeader } from '../../components/PageHeader'
import { Alert, Badge, PageLoader, Spinner } from '../../components/ui'
import { useAdminMetrics } from '../../hooks/queries'
import { api, apiError } from '../../lib/api'
import { formatDateTime } from '../../lib/format'

export function Admin() {
  const { data: m, isLoading, refetch } = useAdminMetrics()
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState('')
  const [error, setError] = useState('')

  async function runIngest() {
    setBusy(true); setMsg(''); setError('')
    try {
      const { data } = await api.post('/admin/ingest/run')
      setMsg(`Ingest ${data.status}: +${data.noticesInserted} notices, ${data.matchesCreated} matches.`)
      refetch()
    } catch (err) {
      setError(apiError(err))
    } finally {
      setBusy(false)
    }
  }

  if (isLoading || !m) return <PageLoader />

  const stats: { label: string; value: string | number; hint?: string }[] = [
    { label: 'Users', value: m.totalUsers.toLocaleString(), hint: `${m.adminUsers} admin` },
    { label: 'Active subscribers', value: m.activeSubscribers, hint: `${m.trialing} trialing` },
    { label: 'Starter / Pro', value: `${m.starterSubscribers} / ${m.proSubscribers}` },
    { label: 'Past due', value: m.pastDue },
    { label: 'Notices', value: m.totalNotices.toLocaleString(), hint: `${m.noticesLast24h} in 24h` },
    { label: 'Total matches', value: m.totalMatches.toLocaleString() },
    { label: 'Emails sent', value: m.emailsSentTotal.toLocaleString(), hint: `${m.emailsSentLast24h} in 24h` },
    { label: 'Email failures (24h)', value: m.emailFailuresLast24h },
  ]

  return (
    <div>
      <PageHeader
        title="Admin"
        subtitle="Operational metrics for the platform."
        action={<button className="btn-secondary" onClick={runIngest} disabled={busy}>{busy ? <Spinner className="h-4 w-4" /> : 'Run ingest now'}</button>}
      />

      {msg && <div className="mb-4"><Alert tone="green">{msg}</Alert></div>}
      {error && <div className="mb-4"><Alert>{error}</Alert></div>}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((s) => (
          <div key={s.label} className="card p-5">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">{s.label}</p>
            <p className="mt-1 text-2xl font-bold text-slate-900">{s.value}</p>
            {s.hint && <p className="text-xs text-slate-500">{s.hint}</p>}
          </div>
        ))}
      </div>

      <div className="card mt-6 p-6">
        <p className="text-sm font-semibold text-slate-900">Last ingest run</p>
        {m.lastIngestRun ? (
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <div className="flex items-center gap-2">
              <Badge tone={m.lastIngestRun.status === 'Succeeded' ? 'green' : m.lastIngestRun.status === 'Failed' ? 'red' : 'amber'}>{m.lastIngestRun.status}</Badge>
              <span className="text-sm text-slate-600">{m.lastIngestRun.source}</span>
            </div>
            <p className="text-sm text-slate-600">Started {formatDateTime(m.lastIngestRun.startedAt)}</p>
            <p className="text-sm text-slate-600">Inserted {m.lastIngestRun.noticesInserted} · Updated {m.lastIngestRun.noticesUpdated}</p>
            <p className="text-sm text-slate-600">Matches created {m.lastIngestRun.matchesCreated}</p>
            {m.lastIngestRun.error && <p className="text-sm text-red-600 sm:col-span-2">Error: {m.lastIngestRun.error}</p>}
          </div>
        ) : (
          <p className="mt-2 text-sm text-slate-500">No ingest runs yet.</p>
        )}
      </div>
    </div>
  )
}
