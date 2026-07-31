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
          <h1 className="text-4xl font-extrabold tracking-tight text-slate-900">Simple, honest pricing</h1>
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
                <div key={plan.tier} className={clsx('card relative p-8', featured && 'ring-2 ring-brand-600')}>
                  {featured && (
                    <span className="absolute -top-3 left-8 badge bg-brand-600 text-white">Most popular</span>
                  )}
                  <h2 className="text-xl font-bold text-slate-900">{plan.name}</h2>
                  <p className="mt-1 text-sm text-slate-500">{plan.blurb}</p>
                  <p className="mt-6">
                    <span className="text-4xl font-extrabold text-slate-900">${plan.monthlyPriceUsd}</span>
                    <span className="text-slate-500">/month</span>
                  </p>
                  <Link to="/register" className={clsx('mt-6 w-full', featured ? 'btn-primary' : 'btn-secondary')}>
                    Start free trial
                  </Link>
                  <ul className="mt-8 space-y-3">
                    {plan.features.map((f) => (
                      <li key={f} className="flex items-start gap-2 text-sm text-slate-600">
                        <svg className="mt-0.5 h-4 w-4 flex-none text-brand-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><path d="M5 13l4 4L19 7" strokeLinecap="round" strokeLinejoin="round" /></svg>
                        {f}
                      </li>
                    ))}
                  </ul>
                </div>
              )
            })}
          </div>
        )}

        <p className="mt-10 text-center text-sm text-slate-400">
          Prices in USD. Billing is handled securely by Stripe. Data comes from the free public SAM.gov API.
        </p>
      </section>
    </MarketingLayout>
  )
}
