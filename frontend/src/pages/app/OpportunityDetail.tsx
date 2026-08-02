import { Link, useParams } from 'react-router-dom'
import { useState } from 'react'
import { useNoticeDetail, useSimilarNotices, useToggleSaved } from '../../hooks/queries'
import { Badge, PageLoader, EmptyState, StarButton } from '../../components/ui'
import { deadlineLabel, formatDate, formatDateTime } from '../../lib/format'
import type { NoticeListItem } from '../../lib/types'

export function OpportunityDetail() {
  const { noticeId } = useParams()
  const { data: n, isLoading } = useNoticeDetail(noticeId)
  const { data: similar } = useSimilarNotices(noticeId)
  const toggle = useToggleSaved()
  const [note, setNote] = useState('')
  const [noteOpen, setNoteOpen] = useState(false)

  if (isLoading) return <PageLoader />
  if (!n) return <EmptyState title="Opportunity not found" action={<Link to="/app" className="btn-secondary">Back to opportunities</Link>} />

  const dl = deadlineLabel(n.responseDeadline)

  // One-click "alert me about opportunities like this" — pre-fill a new match
  // profile anchored on this notice's NAICS (or PSC if it has no NAICS). If it
  // has neither, we can't seed a useful filter, so the button is hidden.
  const anchorNaics = n.naicsCode ? [n.naicsCode] : []
  const anchorPsc = !n.naicsCode && n.pscCode ? [n.pscCode] : []
  const canAlert = anchorNaics.length > 0 || anchorPsc.length > 0
  const alertPrefill = {
    name: `Like: ${n.title.length > 48 ? n.title.slice(0, 48).trimEnd() + '…' : n.title}`,
    naics: anchorNaics,
    psc: anchorPsc,
  }

  function save() {
    toggle.mutate({ noticeId: n!.noticeId, save: !n!.isSaved, note: note || n!.savedNote })
    setNoteOpen(false)
  }

  return (
    <div className="mx-auto max-w-4xl">
      <Link to="/app" className="mb-4 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M15 18l-6-6 6-6" strokeLinecap="round" strokeLinejoin="round" /></svg>
        Opportunities
      </Link>

      <div className="card p-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <div className="mb-2 flex flex-wrap gap-2">
              <Badge tone="indigo">{n.typeLabel}</Badge>
              {n.setAside !== 'None' && n.setAsideLabel && <Badge tone="green">{n.setAsideLabel}</Badge>}
              <Badge tone={dl.tone}>{dl.text}</Badge>
              {!n.isActive && <Badge tone="gray">Inactive</Badge>}
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-900">{n.title}</h1>
            <p className="mt-1 text-sm text-slate-500">{n.agencyPath}</p>
          </div>
          <StarButton className="flex-none" saved={n.isSaved} onClick={() => (n.isSaved ? save() : setNoteOpen(true))} />
        </div>

        {noteOpen && (
          <div className="mt-4 rounded-lg border border-slate-200 p-3">
            <label className="label">Add a note (optional)</label>
            <textarea className="input" rows={2} value={note} onChange={(e) => setNote(e.target.value)} placeholder="e.g. Good fit — draft response by Friday" />
            <div className="mt-2 flex gap-2">
              <button className="btn-primary" onClick={save}>Save opportunity</button>
              <button className="btn-ghost" onClick={() => setNoteOpen(false)}>Cancel</button>
            </div>
          </div>
        )}

        {n.aiSummary && <AiOverview summary={n.aiSummary} keyPoints={n.aiKeyPoints} fitNote={n.aiFitNote} />}

        <div className="mt-6 grid gap-4 sm:grid-cols-2">
          <Field label="Solicitation #" value={n.solicitationNumber} mono />
          <Field label="NAICS" value={n.naicsCode} mono />
          <Field label="PSC" value={n.pscCode} mono />
          <Field label="Posted" value={formatDate(n.postedDate)} />
          <Field label="Response deadline" value={formatDateTime(n.responseDeadline)} />
          <Field label="Archive date" value={formatDate(n.archiveDate)} />
          <Field label="Place of performance" value={[n.popCity, n.popState, n.popZip].filter(Boolean).join(', ')} />
          <Field label="Set-aside" value={n.setAsideDescription} />
        </div>

        {(n.primaryContactName || n.primaryContactEmail) && (
          <div className="mt-6 rounded-lg bg-slate-50 p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Point of contact</p>
            <p className="mt-1 text-sm text-slate-800">{n.primaryContactName}</p>
            {n.primaryContactEmail && <a href={`mailto:${n.primaryContactEmail}`} className="text-sm text-brand-700 hover:underline">{n.primaryContactEmail}</a>}
            {n.primaryContactPhone && <p className="text-sm text-slate-600">{n.primaryContactPhone}</p>}
          </div>
        )}

        {n.description && (
          <div className="mt-6">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Description</p>
            <p className="mt-2 whitespace-pre-line text-sm leading-6 text-slate-700">{n.description}</p>
          </div>
        )}

        <div className="mt-6 flex flex-wrap gap-2">
          {n.uiLink && <a href={n.uiLink} target="_blank" rel="noreferrer" className="btn-primary">View on SAM.gov ↗</a>}
          {!n.isSaved && <button className="btn-secondary" onClick={() => setNoteOpen(true)}>Save opportunity</button>}
          {canAlert && (
            <Link to="/app/profiles/new" state={{ prefill: alertPrefill }} className="btn-secondary">
              Alert me about opportunities like this
            </Link>
          )}
        </div>
      </div>

      {n.matchedProfiles.length > 0 && (
        <div className="card mt-6 p-6">
          <p className="text-sm font-semibold text-slate-900">Why this matched</p>
          <div className="mt-3 space-y-3">
            {n.matchedProfiles.map((m) => (
              <MatchReason key={m.profileId} name={m.profileName} reason={m.matchReason} />
            ))}
          </div>
        </div>
      )}

      {similar && similar.length > 0 && (
        <div className="card mt-6 p-6">
          <div className="flex items-baseline justify-between gap-2">
            <p className="text-sm font-semibold text-slate-900">More open opportunities like this</p>
            <span className="text-xs text-slate-400">Same NAICS or agency</span>
          </div>
          <div className="mt-3 divide-y divide-slate-100">
            {similar.map((s) => <SimilarRow key={s.noticeId} item={s} />)}
          </div>
        </div>
      )}
    </div>
  )
}

function SimilarRow({ item }: { item: NoticeListItem }) {
  const dl = deadlineLabel(item.responseDeadline)
  return (
    <Link
      to={`/app/opportunities/${item.noticeId}`}
      className="-mx-2 flex items-start gap-3 rounded-lg px-2 py-3 transition hover:bg-slate-50"
    >
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium text-slate-800">{item.title}</p>
        <p className="mt-0.5 truncate text-xs text-slate-500">{item.agencyPath}</p>
        <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
          <Badge tone="indigo">{item.typeLabel}</Badge>
          {item.setAside !== 'None' && item.setAsideLabel && <Badge tone="green">{item.setAsideLabel}</Badge>}
          {item.isSaved && <Badge tone="amber">Saved</Badge>}
          {item.isMatched && <Badge tone="blue">Matched</Badge>}
        </div>
      </div>
      <Badge tone={dl.tone}>{dl.text}</Badge>
    </Link>
  )
}

function AiOverview({ summary, keyPoints, fitNote }: { summary: string; keyPoints: string[]; fitNote?: string }) {
  return (
    <div className="mt-6 rounded-xl border border-canary-200 bg-canary-50/70 p-5">
      <div className="flex items-center gap-2">
        <span className="flex h-6 w-6 flex-none items-center justify-center rounded-md bg-canary-400 text-ink-900">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor" aria-hidden><path d="M12 2l1.9 5.8L20 9.7l-4.9 3.6L17 20l-5-3.5L7 20l1.9-6.7L4 9.7l6.1-1.9L12 2z" /></svg>
        </span>
        <p className="eyebrow">AI overview</p>
      </div>
      <p className="mt-3 text-sm leading-6 text-ink-900">{summary}</p>
      {keyPoints.length > 0 && (
        <ul className="mt-3 space-y-1.5">
          {keyPoints.map((p, i) => (
            <li key={i} className="flex items-start gap-2 text-sm text-ink-800">
              <span className="mt-1.5 h-1.5 w-1.5 flex-none rounded-full bg-canary-500" />
              <span>{p}</span>
            </li>
          ))}
        </ul>
      )}
      {fitNote && (
        <p className="mt-3 rounded-lg border border-canary-200 bg-white/70 px-3 py-2 text-sm text-ink-800">
          <span className="font-semibold">Fit: </span>{fitNote}
        </p>
      )}
      <p className="mt-3 text-xs text-slate-500">
        AI-generated overview — always confirm details against the official SAM.gov notice.
      </p>
    </div>
  )
}

function Field({ label, value, mono }: { label: string; value?: string | null; mono?: boolean }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">{label}</p>
      <p className={'mt-0.5 text-sm text-slate-800 ' + (mono ? 'font-mono' : '')}>{value || '—'}</p>
    </div>
  )
}

function MatchReason({ name, reason }: { name: string; reason?: string }) {
  let parsed: Record<string, unknown> | null = null
  try { parsed = reason ? JSON.parse(reason) : null } catch { /* ignore */ }
  const chips: string[] = []
  if (parsed) {
    if (parsed.matchedNaics) chips.push(`NAICS ${parsed.matchedNaics}`)
    if (parsed.matchedPsc) chips.push(`PSC ${parsed.matchedPsc}`)
    if (Array.isArray(parsed.matchedKeywords)) chips.push(...(parsed.matchedKeywords as string[]).map((k) => `“${k}”`))
    if (parsed.matchedAgencyPath) chips.push(String(parsed.matchedAgencyPath))
    if (parsed.matchedSetAside) chips.push(String(parsed.matchedSetAside))
    if (parsed.matchedState) chips.push(String(parsed.matchedState))
    if (parsed.matchedNoticeType) chips.push(String(parsed.matchedNoticeType))
  }
  return (
    <div className="rounded-lg border border-slate-200 p-3">
      <p className="text-sm font-medium text-slate-800">{name}</p>
      {chips.length > 0 && (
        <div className="mt-1.5 flex flex-wrap gap-1.5">
          {chips.map((c, i) => <Badge key={i} tone="blue">{c}</Badge>)}
        </div>
      )}
    </div>
  )
}
