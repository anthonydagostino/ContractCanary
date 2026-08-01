import { Link, useNavigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { Logo, Wordmark } from './Brand'
import { branding } from '../config/branding'
import { useAuth } from '../lib/auth'

export function MarketingLayout({ children }: { children: ReactNode }) {
  const { me } = useAuth()
  const navigate = useNavigate()
  const year = new Date().getFullYear()

  return (
    <div className="flex min-h-screen flex-col bg-white">
      <header className="sticky top-0 z-40 border-b border-slate-100 bg-white/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3.5">
          <Wordmark />
          <nav className="flex items-center gap-1.5 sm:gap-2">
            <Link to="/pricing" className="btn-ghost hidden sm:inline-flex">Pricing</Link>
            {me ? (
              <button className="btn-primary" onClick={() => navigate('/app')}>Go to dashboard</button>
            ) : (
              <>
                <Link to="/login" className="btn-ghost">Sign in</Link>
                <Link to="/register" className="btn-primary">Start free trial</Link>
              </>
            )}
          </nav>
        </div>
      </header>

      <main className="flex-1">{children}</main>

      <footer className="mt-20 bg-ink-900 text-slate-300">
        <div className="mx-auto max-w-6xl px-4 py-14">
          <div className="grid gap-10 sm:grid-cols-2 lg:grid-cols-4">
            <div className="lg:col-span-2">
              <div className="flex items-center gap-2">
                <Logo className="h-8 w-8" />
                <span className="text-lg font-bold tracking-tight text-white">
                  Contract<span className="text-canary-400">Canary</span>
                </span>
              </div>
              <p className="mt-4 max-w-sm text-sm leading-relaxed text-slate-400">
                {branding.description}
              </p>
            </div>

            <div>
              <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-500">Product</h4>
              <ul className="mt-4 space-y-2.5 text-sm">
                <li><Link to="/pricing" className="text-slate-300 transition hover:text-white">Pricing</Link></li>
                <li><Link to="/login" className="text-slate-300 transition hover:text-white">Sign in</Link></li>
                <li><Link to="/register" className="text-slate-300 transition hover:text-white">Start free trial</Link></li>
              </ul>
            </div>

            <div>
              <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-500">Company</h4>
              <ul className="mt-4 space-y-2.5 text-sm">
                <li><Link to="/terms" className="text-slate-300 transition hover:text-white">Terms of Service</Link></li>
                <li><Link to="/privacy" className="text-slate-300 transition hover:text-white">Privacy Policy</Link></li>
                <li><a href={`mailto:${branding.supportEmail}`} className="text-slate-300 transition hover:text-white">Contact support</a></li>
              </ul>
            </div>
          </div>

          <div className="mt-12 border-t border-white/10 pt-6 text-xs leading-relaxed text-slate-500">
            <p>
              Opportunity data is sourced from{' '}
              <a href="https://sam.gov" target="_blank" rel="noreferrer" className="text-slate-400 underline decoration-slate-600 underline-offset-2 hover:text-slate-200">SAM.gov</a>{' '}
              (U.S. General Services Administration). {branding.productName} is an independent service and is
              not affiliated with, endorsed by, or sponsored by SAM.gov or the U.S. Government.
            </p>
            <p className="mt-3">© {year} {branding.productName}. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  )
}
