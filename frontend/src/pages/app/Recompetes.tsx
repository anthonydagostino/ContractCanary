import { Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import axios from 'axios'
import { PageHeader } from '../../components/PageHeader'
import { Badge, EmptyState, PageLoader, Pagination } from '../../components/ui'
import { useRecompetes } from '../../hooks/queries'
import { useAuth } from '../../lib/auth'
import { formatDate, formatMoney } from '../../lib/format'
import type { RecompeteItem } from '../../lib/types'

export function Recompetes() {
  const { me } = useAuth()
  const [page, setPage] = useState(1)
  const entitled = !!me?.limits.canSeeRecompetes
  const { data, isLoading, isError, isFetching, error, refetch } = useRecompetes(page, entitled)

  // If the result set shrinks between requests (daily award refresh, profile
  // edit), a now-out-of-range page would render an empty page with no
  // pagination control to escape from — clamp back into range.
  useEffect(() => {
    if (data && data.totalPages > 0 && page > data.totalPages) setPage(data.totalPages)
  }, [data, page])

  // Entitlement can lapse mid-session (trial/grace expiry); the server then
  // answers 402 and the right response is the upgrade pitch, not an error.
  const lapsed = isError && axios.isAxiosError(error) && error.response?.status === 402

  return (
    <div>
      <PageHeader
        title="Recompete Radar"
        subtitle="Incumbent contracts in your NAICS codes that expire soon — agencies typically rebid 12–18 months before the end date, so these are tomorrow's opportunities before they're posted."
      />

      {!entitled || lapsed ? (
        <UpsellCard />
      ) : isLoading ? (
        <PageLoader />
      ) : isError ? (
        // A transient failure must not masquerade as "no matches" (bug class
        // previously found on the opportunity page).
        <EmptyState
          title="Couldn't load recompete data"
          hint="A network or server error occurred — your matches are still there."
          action={<button type="button" className="btn-secondary" onClick={() => refetch()}>Try again</button>}
        />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title="No expiring incumbent contracts match your profiles yet"
          hint="Award data refreshes daily against the NAICS codes in your active match profiles. Add or broaden a profile to widen the radar."
          action={<Link to="/app/profiles" className="btn-secondary">Review my profiles</Link>}
        />
      ) : (
        <div className="space-y-4">
          <p className="text-sm text-slate-500">
            {data.total.toLocaleString()} expiring incumbent contract{data.total === 1 ? '' : 's'} in your codes, soonest first.
            Source: USAspending.gov (public award data).
            {isFetching && <span className="text-slate-300"> · updating…</span>}
          </p>
          <div className="space-y-3">
            {data.items.map((r) => <RecompeteCard key={r.awardId} item={r} />)}
          </div>
          {/* Drive pagination from local state: placeholderData shows the previous
              page's payload during a transition, and its stale echoed page number
              would let a double-click re-request the same page. */}
          <Pagination page={page} totalPages={data.totalPages} onChange={setPage} />
        </div>
      )}
    </div>
  )
}

function RecompeteCard({ item }: { item: RecompeteItem }) {
  return (
    <div className="card p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="font-semibold text-ink-900">{item.recipientName || 'Undisclosed recipient'}</p>
          <p className="mt-0.5 text-sm text-slate-500">{item.awardingAgency || 'Federal agency'}</p>
        </div>
        <div className="text-right">
          <p className="text-lg font-bold tabular-nums text-ink-900">{formatMoney(item.obligatedAmount)}</p>
          <p className="text-xs text-slate-500">obligated to date</p>
        </div>
      </div>

      <div className="mt-2 flex flex-wrap items-center gap-1.5">
        {item.recompeteWindowOpen
          ? <Badge tone="red">Recompete window likely open</Badge>
          : <Badge tone="amber">Window opens ~{formatDate(item.recompeteWindowOpens)}</Badge>}
        <Badge tone="gray">Ends {formatDate(item.periodOfPerformanceEnd)}</Badge>
        {item.naicsCode && <Badge tone="blue">NAICS {item.naicsCode}</Badge>}
        {item.popState && <Badge tone="gray">{item.popState}</Badge>}
        {item.matchedProfileNames.map((n) => <Badge key={n} tone="indigo">{n}</Badge>)}
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-3 text-sm">
        <a href={item.usaSpendingUrl} target="_blank" rel="noreferrer" className="text-brand-700 hover:underline">
          View award on USAspending ↗
        </a>
        {item.displayAwardId && <span className="font-mono text-xs text-slate-500">{item.displayAwardId}</span>}
      </div>
      <p className="mt-2 text-xs text-slate-500">
        Watch SAM.gov for a Sources Sought or Presolicitation from this agency as the end date approaches —
        your matching profiles will catch it the moment it posts.
      </p>
    </div>
  )
}

function UpsellCard() {
  return (
    <div className="card mx-auto max-w-xl p-8 text-center">
      <span className="eyebrow">Pro feature</span>
      <h2 className="mt-2 text-xl font-bold text-ink-900">See tomorrow's opportunities before they're posted</h2>
      <p className="mt-3 text-sm leading-6 text-slate-600">
        Recompete Radar watches public award data (USAspending.gov) for incumbent contracts in your NAICS codes
        that are about to expire. Agencies usually rebid 12–18 months before the end date — knowing who holds the
        work, what it's worth, and when it ends puts you in the race before the RFP exists.
      </p>
      <Link to="/app/settings" className="btn-primary mt-6 inline-flex">Upgrade to Pro — $79/mo</Link>
      <p className="mt-2 text-xs text-slate-500">Cancel anytime.</p>
    </div>
  )
}
