import { Link, useNavigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { Wordmark } from './Brand'
import { branding } from '../config/branding'
import { useAuth } from '../lib/auth'

export function MarketingLayout({ children }: { children: ReactNode }) {
  const { me } = useAuth()
  const navigate = useNavigate()
  return (
    <div className="min-h-screen bg-white">
      <header className="border-b border-slate-100">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
          <Wordmark />
          <nav className="flex items-center gap-2">
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
      <main>{children}</main>
      <footer className="border-t border-slate-100 bg-slate-50">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-4 px-4 py-8 text-sm text-slate-500 sm:flex-row">
          <div className="flex items-center gap-2">
            <Wordmark />
          </div>
          <div className="flex flex-wrap items-center gap-4">
            <Link to="/pricing" className="hover:text-slate-700">Pricing</Link>
            <Link to="/terms" className="hover:text-slate-700">Terms</Link>
            <Link to="/privacy" className="hover:text-slate-700">Privacy</Link>
            <a href={`mailto:${branding.supportEmail}`} className="hover:text-slate-700">Support</a>
          </div>
          <p>© {new Date().getFullYear()} {branding.productName}</p>
        </div>
      </footer>
    </div>
  )
}
