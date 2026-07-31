import type { ReactNode } from 'react'
import { Wordmark } from './Brand'
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
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">{title}</h1>
          {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
          <div className="mt-8">{children}</div>
          {footer && <div className="mt-6 text-sm text-slate-500">{footer}</div>}
        </div>
      </div>
      {/* Right: brand panel */}
      <div className="hidden bg-gradient-to-br from-brand-700 to-brand-900 lg:flex lg:w-1/2 lg:flex-col lg:justify-center lg:px-16">
        <blockquote className="max-w-md text-white">
          <p className="text-2xl font-semibold leading-relaxed">“{branding.tagline}”</p>
          <p className="mt-6 text-brand-100">{branding.description}</p>
        </blockquote>
      </div>
    </div>
  )
}
