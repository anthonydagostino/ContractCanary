import { Link } from 'react-router-dom'
import { MarketingLayout } from '../components/MarketingLayout'
import { branding } from '../config/branding'
import { useMeta } from '../hooks/queries'

const steps = [
  { title: 'Describe what you win', body: 'Add your NAICS codes, keywords, target agencies, set-asides, and states — in a couple of minutes.' },
  { title: 'We watch SAM.gov', body: 'Our pipeline pulls new and updated federal opportunity notices on a schedule and matches them to your profiles.' },
  { title: 'You get a daily signal', body: 'One clean email each morning with only the opportunities that fit — plus a searchable dashboard and deadline tracker.' },
]

const features = [
  ['Deterministic matching', 'NAICS or PSC, keywords, agency, set-aside, place of performance, notice type — no black-box AI, just the rules you set.'],
  ['Daily digest email', 'Grouped by match profile, in your timezone. Zero-match days send nothing. No noise.'],
  ['Never see it twice', 'Each opportunity is deduped per profile, so you are never notified about the same notice again.'],
  ['Deadline tracker', 'Star opportunities and track response deadlines so nothing slips.'],
  ['CSV export (Pro)', 'Export any filtered search to CSV for your capture team.'],
  ['Built for small business', 'Everything GovWin charges five figures for, focused on the 90% you actually need.'],
]

export function Landing() {
  const { data: meta } = useMeta()
  const trialDays = meta?.trialDays ?? 14

  return (
    <MarketingLayout>
      {/* Hero */}
      <section className="relative overflow-hidden">
        <div className="absolute inset-0 -z-10 bg-gradient-to-b from-brand-50/70 to-white" />
        <div className="mx-auto max-w-6xl px-4 py-20 sm:py-28">
          <div className="mx-auto max-w-3xl text-center">
            <span className="badge bg-brand-100 text-brand-700">{branding.hero.eyebrow}</span>
            <h1 className="mt-5 text-4xl font-extrabold tracking-tight text-slate-900 sm:text-5xl">
              {branding.hero.heading}
            </h1>
            <p className="mx-auto mt-5 max-w-2xl text-lg text-slate-600">{branding.hero.sub}</p>
            <div className="mt-8 flex items-center justify-center gap-3">
              <Link to="/register" className="btn-primary px-6 py-3 text-base">Start your {trialDays}-day free trial</Link>
              <Link to="/pricing" className="btn-secondary px-6 py-3 text-base">See pricing</Link>
            </div>
            <p className="mt-3 text-sm text-slate-400">No credit card required for the trial.</p>
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="mx-auto max-w-6xl px-4 py-16">
        <div className="grid gap-6 sm:grid-cols-3">
          {steps.map((s, i) => (
            <div key={s.title} className="card p-6">
              <div className="mb-3 flex h-9 w-9 items-center justify-center rounded-full bg-brand-600 text-sm font-bold text-white">{i + 1}</div>
              <h3 className="font-semibold text-slate-900">{s.title}</h3>
              <p className="mt-2 text-sm text-slate-600">{s.body}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Features */}
      <section className="bg-slate-50">
        <div className="mx-auto max-w-6xl px-4 py-16">
          <h2 className="text-center text-3xl font-bold tracking-tight text-slate-900">Everything you need to never miss a bid</h2>
          <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {features.map(([title, body]) => (
              <div key={title} className="card p-6">
                <div className="mb-3 flex h-8 w-8 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M5 13l4 4L19 7" strokeLinecap="round" strokeLinejoin="round" /></svg>
                </div>
                <h3 className="font-semibold text-slate-900">{title}</h3>
                <p className="mt-2 text-sm text-slate-600">{body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="mx-auto max-w-4xl px-4 py-20 text-center">
        <h2 className="text-3xl font-bold tracking-tight text-slate-900">Your next contract is already posted.</h2>
        <p className="mt-3 text-slate-600">Start tracking it today.</p>
        <Link to="/register" className="btn-primary mt-6 inline-flex px-6 py-3 text-base">Create your free account</Link>
      </section>
    </MarketingLayout>
  )
}
