// SAM.gov date-only fields (postedDate, archiveDate) are serialized by the API
// as midnight UTC. Rendering that instant in any US timezone shows the previous
// day, so calendar dates must be formatted in UTC while real datetimes
// (responseDeadline, trial/billing timestamps) stay in the viewer's zone.
const UTC_MIDNIGHT = /T00:00:00(\.0+)?(Z|\+00:00)$/

export function formatDate(iso?: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (isNaN(d.getTime())) return '—'
  const calendarDate = /^\d{4}-\d{2}-\d{2}$/.test(iso) || UTC_MIDNIGHT.test(iso)
  return d.toLocaleDateString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric',
    ...(calendarDate ? { timeZone: 'UTC' } : {}),
  })
}

export function formatDateTime(iso?: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (isNaN(d.getTime())) return '—'
  return d.toLocaleString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit',
  })
}

/**
 * Whole calendar days between now and the deadline in the viewer's zone;
 * 0 means it falls on today, negative means a past day. Null when absent.
 * Calendar math, not elapsed-hours math: a deadline at 5pm viewed at 3pm is
 * "today" (0), not "1 day away" as ceil(ms/86400000) would claim.
 */
export function daysUntil(iso?: string | null, now: Date = new Date()): number | null {
  if (!iso) return null
  const d = new Date(iso)
  if (isNaN(d.getTime())) return null
  const startOfDay = (x: Date) => new Date(x.getFullYear(), x.getMonth(), x.getDate()).getTime()
  return Math.round((startOfDay(d) - startOfDay(now)) / 86_400_000)
}

export function deadlineLabel(iso?: string | null, now: Date = new Date()): { text: string; tone: 'gray' | 'amber' | 'red' | 'green' } {
  if (!iso) return { text: 'No deadline', tone: 'gray' }
  const d = new Date(iso)
  if (isNaN(d.getTime())) return { text: 'No deadline', tone: 'gray' }
  // The instant decides open vs closed; calendar days decide the label. A
  // deadline that passed an hour ago is Closed, not "Due today".
  if (d.getTime() < now.getTime()) return { text: 'Closed', tone: 'gray' }
  const days = daysUntil(iso, now)!
  if (days <= 0) return { text: 'Due today', tone: 'red' }
  if (days <= 3) return { text: `${days} day${days === 1 ? '' : 's'} left`, tone: 'red' }
  if (days <= 10) return { text: `${days} days left`, tone: 'amber' }
  return { text: `${days} days left`, tone: 'green' }
}

/** Split a deadline label into the big-number tile used by the deadline tracker. */
export function deadlineTile(text: string): { top: string; caption: string } {
  const m = /^(\d+) days? left$/.exec(text)
  if (m) return { top: m[1], caption: m[1] === '1' ? 'day' : 'days' }
  if (text === 'Due today') return { top: 'Due', caption: 'today' }
  return { top: text, caption: '' }
}
