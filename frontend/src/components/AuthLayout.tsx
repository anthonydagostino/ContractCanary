import type { ReactNode } from 'react'
import { Logo, Wordmark } from './Brand'
import { branding } from '../config/branding'

export function AuthLayout({ title, subtitle, children, footer }: {
  title: string
  subtitle?: string
  children: ReactNode
  footer?: ReactNode
}) {
  return (
    <div className="flex min-h-screen">
      {/* Left: form */}
      <div className="flex w-full flex-col justify-center px-6 py-12 lg:w-1/2">
        <div className="mx-auto w-full max-w-sm">
          <div className="mb-8"><Wordmark /></div>
          <h1 className="text-2xl font-bold tracking-tight text-ink-900">{title}</h1>
          {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
          <div className="mt-8">{children}</div>
          {footer && <div className="mt-6 text-sm text-slate-500">{footer}</div>}
        </div>
      </div>
      {/* Right: brand panel */}
      <div className="relative hidden overflow-hidden bg-gradient-to-br from-ink-800 to-ink-900 lg:flex lg:w-1/2 lg:flex-col lg:justify-center lg:px-16">
        <div className="absolute -right-24 -top-24 h-72 w-72 rounded-full bg-canary-400/20 blur-3xl" />
        <blockquote className="relative max-w-md text-white">
          <Logo className="h-12 w-12" />
          <p className="mt-6 text-2xl font-semibold leading-relaxed">“{branding.tagline}”</p>
          <p className="mt-6 leading-relaxed text-slate-300">{branding.description}</p>
        </blockquote>
      </div>
    </div>
  )
}
