import { Link } from 'react-router-dom'
import clsx from 'clsx'
import { MarketingLayout } from '../components/MarketingLayout'
import { useMeta } from '../hooks/queries'
import { PageLoader } from '../components/ui'

export function Pricing() {
  const { data: meta, isLoading } = useMeta()

  return (
    <MarketingLayout>
      <section className="mx-auto max-w-5xl px-4 py-16">
        <div className="mx-auto max-w-2xl text-center">
          <span className="eyebrow">Pricing</span>
          <h1 className="mt-3 text-4xl font-extrabold tracking-tight text-ink-900">Simple, honest pricing</h1>
          <p className="mt-4 text-lg text-slate-600">
            Start with a {meta?.trialDays ?? 14}-day free trial. No credit card required. Cancel anytime.
          </p>
        </div>

        {isLoading || !meta ? (
          <PageLoader />
        ) : (
          <div className="mt-12 grid gap-6 sm:grid-cols-2">
            {meta.plans.map((plan) => {
              const featured = plan.tier === 'Pro'
              return (
                <div key={plan.tier} className={clsx('card relative p-8', featured && 'ring-2 ring-canary-400 shadow-lift')}>
                  {featured && (
                    <span className="absolute -top-3 left-8 badge bg-canary-400 text-ink-900">Most popular</span>
                  )}
                  <h2 className="text-xl font-bold text-ink-900">{plan.name}</h2>
                  <p className="mt-1 text-sm text-slate-500">{plan.blurb}</p>
                  <p className="mt-6">
                    <span className="text-4xl font-extrabold text-ink-900">${plan.monthlyPriceUsd}</span>
                    <span className="text-slate-500">/month</span>
                  </p>
                  <Link to="/register" className={clsx('mt-6 w-full', featured ? 'btn-primary' : 'btn-secondary')}>
                    Start free trial
                  </Link>
                  <ul className="mt-8 space-y-3">
                    {plan.features.map((f) => (
                      <li key={f} className="flex items-start gap-2 text-sm text-slate-600">
                        <svg className="mt-0.5 h-4 w-4 flex-none text-canary-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><path d="M5 13l4 4L19 7" strokeLinecap="round" strokeLinejoin="round" /></svg>
                        {f}
                      </li>
                    ))}
                  </ul>
                </div>
              )
            })}
          </div>
        )}

        <div className="mx-auto mt-10 max-w-2xl rounded-xl border border-slate-200 bg-slate-50 p-5 text-sm leading-6 text-slate-600">
          <p>
            <span className="font-semibold text-ink-900">Which plan?</span>{' '}
            <span className="font-medium text-ink-900">Starter</span> fits one line of business — one match
            profile, one clean daily digest. <span className="font-medium text-ink-900">Pro</span> is for
            contractors serious about pipeline: 5 profiles, CSV export, priority ingest, and{' '}
            <span className="font-medium text-ink-900">Recompete Radar</span> — expiring incumbent contracts in
            your NAICS codes from public award data, so you see likely rebids months before they're posted.
          </p>
        </div>

        <p className="mt-8 text-center text-sm text-slate-500">
          Prices in USD. Plans renew monthly until canceled — cancel anytime from Settings, no annual lock-in.
          Billing is handled securely by Stripe. Data comes from free public government sources (SAM.gov, USAspending.gov).
        </p>
      </section>
    </MarketingLayout>
  )
}
