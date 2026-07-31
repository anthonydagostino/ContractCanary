import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import clsx from 'clsx'
import { Logo } from './Brand'
import { branding } from '../config/branding'
import { useAuth } from '../lib/auth'
import { Badge } from './ui'
import { daysUntil } from '../lib/format'

const nav = [
  { to: '/app', label: 'Opportunities', end: true, icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6' },
  { to: '/app/profiles', label: 'Match profiles', icon: 'M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4' },
  { to: '/app/saved', label: 'Saved', icon: 'M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z' },
  { to: '/app/deadlines', label: 'Deadlines', icon: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z' },
  { to: '/app/settings', label: 'Settings', icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z' },
]

export function AppLayout() {
  const { me, logout } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)

  const trialDaysLeft = me?.subscriptionStatus === 'Trialing' ? daysUntil(me.trialEndsAt) : null

  async function doLogout() {
    await logout()
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Topbar (mobile) */}
      <div className="flex items-center justify-between border-b border-slate-200 bg-white px-4 py-3 lg:hidden">
        <div className="flex items-center gap-2"><Logo className="h-6 w-6" /><span className="font-bold">{branding.productName}</span></div>
        <button className="btn-ghost" onClick={() => setOpen((o) => !o)} aria-label="Menu">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M4 6h16M4 12h16M4 18h16" /></svg>
        </button>
      </div>

      <div className="mx-auto flex max-w-[1400px]">
        {/* Sidebar */}
        <aside className={clsx(
          'fixed inset-y-0 left-0 z-30 w-64 transform border-r border-slate-200 bg-white transition-transform lg:static lg:translate-x-0',
          open ? 'translate-x-0' : '-translate-x-full',
        )}>
          <div className="flex h-full flex-col p-4">
            <div className="mb-6 hidden items-center gap-2 px-2 lg:flex">
              <Logo /><span className="text-lg font-bold tracking-tight text-slate-900">{branding.productName}</span>
            </div>
            <nav className="flex-1 space-y-1">
              {nav.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  onClick={() => setOpen(false)}
                  className={({ isActive }) => clsx(
                    'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition',
                    isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100',
                  )}
                >
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"><path d={item.icon} strokeLinecap="round" strokeLinejoin="round" /></svg>
                  {item.label}
                </NavLink>
              ))}
              {me?.isAdmin && (
                <NavLink to="/app/admin" onClick={() => setOpen(false)} className={({ isActive }) => clsx(
                  'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition',
                  isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100',
                )}>
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"><path d="M9 19v-6a2 2 0 012-2h2a2 2 0 012 2v6m-6 0H5a2 2 0 01-2-2V9a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-4" strokeLinecap="round" strokeLinejoin="round" /></svg>
                  Admin
                </NavLink>
              )}
            </nav>

            <div className="mt-4 rounded-lg border border-slate-200 p-3">
              <div className="flex items-center justify-between">
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-slate-800">{me?.fullName || me?.email}</p>
                  <p className="truncate text-xs text-slate-400">{me?.companyName}</p>
                </div>
                <Badge tone={me?.plan === 'Pro' ? 'indigo' : me?.plan === 'Starter' ? 'blue' : 'gray'}>{me?.plan}</Badge>
              </div>
              {trialDaysLeft !== null && trialDaysLeft >= 0 && (
                <p className="mt-2 text-xs text-amber-700">Trial: {trialDaysLeft} day{trialDaysLeft === 1 ? '' : 's'} left</p>
              )}
              <button onClick={doLogout} className="mt-3 w-full text-left text-xs text-slate-500 hover:text-slate-700">Sign out</button>
            </div>
          </div>
        </aside>

        {open && <div className="fixed inset-0 z-20 bg-slate-900/20 lg:hidden" onClick={() => setOpen(false)} />}

        <main className="min-w-0 flex-1 px-4 py-6 sm:px-6 lg:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
