import clsx from 'clsx'
import type { ReactNode } from 'react'

export function Spinner({ className }: { className?: string }) {
  return (
    <svg className={clsx('animate-spin', className)} width="20" height="20" viewBox="0 0 24 24" fill="none">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
    </svg>
  )
}

export function PageLoader() {
  return (
    <div className="flex items-center justify-center py-24 text-slate-400">
      <Spinner className="h-6 w-6" />
    </div>
  )
}

const badgeTones: Record<string, string> = {
  gray: 'bg-slate-100 text-slate-700',
  blue: 'bg-brand-50 text-brand-700',
  green: 'bg-green-100 text-green-700',
  amber: 'bg-amber-100 text-amber-800',
  red: 'bg-red-100 text-red-700',
  indigo: 'bg-indigo-100 text-indigo-700',
}

export function Badge({ children, tone = 'gray', className }: { children: ReactNode; tone?: keyof typeof badgeTones | string; className?: string }) {
  return <span className={clsx('badge', badgeTones[tone] ?? badgeTones.gray, className)}>{children}</span>
}

export function EmptyState({ title, hint, action }: { title: string; hint?: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-slate-300 bg-white/50 py-16 text-center">
      <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="11" cy="11" r="7" /><path d="M21 21l-4.3-4.3" />
        </svg>
      </div>
      <p className="text-sm font-semibold text-slate-700">{title}</p>
      {hint && <p className="mt-1 max-w-sm text-sm text-slate-500">{hint}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  )
}

export function Alert({ tone = 'red', children }: { tone?: 'red' | 'green' | 'blue' | 'amber'; children: ReactNode }) {
  const tones = {
    red: 'bg-red-50 text-red-800 border-red-200',
    green: 'bg-green-50 text-green-800 border-green-200',
    blue: 'bg-brand-50 text-brand-800 border-brand-200',
    amber: 'bg-amber-50 text-amber-900 border-amber-200',
  }
  return <div className={clsx('rounded-lg border px-3 py-2 text-sm', tones[tone])}>{children}</div>
}

export function Pagination({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (p: number) => void }) {
  if (totalPages <= 1) return null
  return (
    <div className="flex items-center justify-between gap-2 text-sm">
      <button className="btn-secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>Previous</button>
      <span className="text-slate-500">Page {page} of {totalPages}</span>
      <button className="btn-secondary" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>Next</button>
    </div>
  )
}

export function StarButton({ saved, onClick, className }: { saved: boolean; onClick: (e: React.MouseEvent) => void; className?: string }) {
  return (
    <button
      onClick={onClick}
      aria-label={saved ? 'Unsave' : 'Save'}
      className={clsx('rounded-md p-1.5 transition hover:bg-slate-100', saved ? 'text-amber-500' : 'text-slate-300 hover:text-slate-400', className)}
    >
      <svg width="18" height="18" viewBox="0 0 24 24" fill={saved ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2">
        <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
      </svg>
    </button>
  )
}
