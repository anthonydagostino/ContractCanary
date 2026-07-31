export function formatDate(iso?: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (isNaN(d.getTime())) return '—'
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export function formatDateTime(iso?: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (isNaN(d.getTime())) return '—'
  return d.toLocaleString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit',
  })
}

/** Days until a deadline; negative if past. Null when no deadline. */
export function daysUntil(iso?: string | null): number | null {
  if (!iso) return null
  const d = new Date(iso)
  if (isNaN(d.getTime())) return null
  const ms = d.getTime() - Date.now()
  return Math.ceil(ms / (1000 * 60 * 60 * 24))
}

export function deadlineLabel(iso?: string | null): { text: string; tone: 'gray' | 'amber' | 'red' | 'green' } {
  const days = daysUntil(iso)
  if (days === null) return { text: 'No deadline', tone: 'gray' }
  if (days < 0) return { text: 'Closed', tone: 'gray' }
  if (days === 0) return { text: 'Due today', tone: 'red' }
  if (days <= 3) return { text: `${days} day${days === 1 ? '' : 's'} left`, tone: 'red' }
  if (days <= 10) return { text: `${days} days left`, tone: 'amber' }
  return { text: `${days} days left`, tone: 'green' }
}
