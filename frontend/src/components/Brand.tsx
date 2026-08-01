import { Link } from 'react-router-dom'
import { branding } from '../config/branding'

/**
 * The ContractCanary mark: a gold canary on a deep-ink rounded tile.
 * Self-contained (includes its own background) so it reads on any surface.
 */
export function Logo({ className = 'h-8 w-8' }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 32 32" fill="none" aria-hidden role="img">
      <rect width="32" height="32" rx="8" fill="#0b1220" />
      {/* tail */}
      <polygon points="10,16.5 3,15 9.5,21" fill="#f7c948" />
      {/* body */}
      <ellipse cx="15" cy="18.5" rx="7" ry="6" fill="#f7c948" />
      {/* head */}
      <circle cx="20" cy="12.6" r="4.6" fill="#f7c948" />
      {/* wing */}
      <ellipse cx="14.5" cy="19" rx="4.3" ry="2.6" fill="#de911d" transform="rotate(-18 14.5 19)" />
      {/* beak */}
      <polygon points="24,11.6 27.6,12.9 24,14.2" fill="#de911d" />
      {/* eye */}
      <circle cx="21" cy="11.7" r="1.05" fill="#0b1220" />
    </svg>
  )
}

export function Wordmark({ to = '/', className = '' }: { to?: string; className?: string }) {
  // Split "ContractCanary" so the "Canary" half carries the signature gold.
  const name = branding.productName
  const split = name.toLowerCase().indexOf('canary')
  const first = split > 0 ? name.slice(0, split) : name
  const second = split > 0 ? name.slice(split) : ''
  return (
    <Link to={to} className={`flex items-center gap-2 ${className}`}>
      <Logo className="h-8 w-8" />
      <span className="text-lg font-bold tracking-tight text-ink-900">
        {first}
        {second && <span className="text-canary-700">{second}</span>}
      </span>
    </Link>
  )
}
