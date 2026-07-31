import { Link } from 'react-router-dom'
import { branding } from '../config/branding'

export function Logo({ className = 'h-7 w-7' }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 32 32" fill="none" aria-hidden>
      <rect width="32" height="32" rx="7" fill="#1d4ed8" />
      <path d="M7 21l5-9 4 6 3-5 6 8" stroke="#fff" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
      <circle cx="26" cy="9" r="2.5" fill="#fff" />
    </svg>
  )
}

export function Wordmark({ to = '/' }: { to?: string }) {
  return (
    <Link to={to} className="flex items-center gap-2">
      <Logo />
      <span className="text-lg font-bold tracking-tight text-slate-900">{branding.productName}</span>
    </Link>
  )
}
