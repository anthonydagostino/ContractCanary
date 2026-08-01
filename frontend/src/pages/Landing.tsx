import { Link } from 'react-router-dom'
import type { ReactNode } from 'react'
import { MarketingLayout } from '../components/MarketingLayout'
import { Logo } from '../components/Brand'
import { branding } from '../config/branding'
import { useMeta } from '../hooks/queries'

/* ---------- small inline icon set (stroke = currentColor) ---------- */
type IconProps = { className?: string }
const Icon = {
  Filter: (p: IconProps) => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={p.className}><path d="M22 3H2l8 9.46V19l4 2v-8.54L22 3z" /></svg>
  ),
  Mail: (p: IconProps) => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={p.className}><rect x="2" y="4" width="20" height="16" rx="2" /><path d="m22 7-10 6L2 7" /></svg>
  ),
  Shield: (p: IconProps) => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={p.className}><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" /><path d="m9 12 2 2 4-4" /></svg>
  ),
  Clock: (p: IconProps) => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={p.className}><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>
  ),
  Download: (p: IconProps) => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={p.className}><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" /><path d="M7 10l5 5 5-5" /><path d="M12 15V3" /></svg>
  ),
  Briefcase: (p: IconProps) => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={p.className}><rect x="2" y="7" width="20" height="14" rx="2" /><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" /></svg>
  ),
  Check: (p: IconProps) => (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" className={p.className}><path d="M20 6 9 17l-5-5" /></svg>
  ),
}

const steps = [
  { title: 'Tell us what you win', body: 'Add your NAICS codes, keywords, target agencies, set-asides, and states — it takes a couple of minutes.' },
  { title: 'We watch SAM.gov for you', body: 'Our pipeline pulls new and updated federal opportunity notices around the clock and matches them to your profiles.' },
  { title: 'You get a daily signal', body: 'One clean email each morning with only the opportunities that fit — plus a searchable dashboard and deadline tracker.' },
]

const features: { icon: (p: IconProps) => ReactNode; title: string; body: string }[] = [
  { icon: Icon.Filter, title: 'Rules you control', body: 'Match on NAICS or PSC, keywords, agency, set-aside, place of performance, and notice type. No black-box AI — just the rules you set.' },
  { icon: Icon.Mail, title: 'One daily digest', body: 'Grouped by match profile, in your timezone. Zero-match days send nothing at all. Signal, not noise.' },
  { icon: Icon.Shield, title: 'Never see it twice', body: 'Every opportunity is deduplicated per profile, so you are never pinged about the same notice again.' },
  { icon: Icon.Clock, title: 'Deadline tracker', body: 'Star the opportunities you are pursuing and track response deadlines so nothing slips through.' },
  { icon: Icon.Download, title: 'CSV export', body: 'Export any filtered search to CSV and hand it straight to your capture team. (Pro plan.)' },
  { icon: Icon.Briefcase, title: 'Built for small business', body: 'The parts of a five-figure market-intelligence suite that a small contractor actually needs — and nothing you don’t.' },
]

const comparison: { label: string; canary: string; suites: string; diy: string }[] = [
  { label: 'Monthly cost', canary: 'From $29', suites: 'Five figures / year', diy: 'Free (your time)' },
  { label: 'Setup', canary: 'A few minutes', suites: 'Sales calls + onboarding', diy: 'None' },
  { label: 'Built for', canary: 'Small contractors', suites: 'Large capture teams', diy: '—' },
  { label: 'Daily matched digest', canary: 'yes', suites: 'yes', diy: 'no' },
  { label: 'Plain-English AI summaries', canary: 'yes', suites: 'varies', diy: 'no' },
  { label: 'Long-term contract required', canary: 'no', suites: 'often', diy: 'no' },
]

const faqs: { q: string; a: string }[] = [
  {
    q: 'Is this an official government service?',
    a: 'No. ContractCanary is an independent service. We use the free, public SAM.gov Contract Opportunities data, and we are not affiliated with, endorsed by, or sponsored by SAM.gov or the U.S. Government.',
  },
  {
    q: 'How is this different from just checking SAM.gov myself?',
    a: 'SAM.gov posts thousands of new notices across the whole government. We filter that firehose down to only the opportunities that match your NAICS codes, keywords, agencies, and set-asides, and email them to you once a day — so you stop searching and start seeing only what fits.',
  },
  {
    q: 'Do I need to be technical to use it?',
    a: 'No. You enter your codes, keywords, and preferences once during setup, and we handle the monitoring and matching for you. If you know what your business does, you can set it up.',
  },
  {
    q: 'Where does the data come from, and how fresh is it?',
    a: 'Directly from the official SAM.gov Contract Opportunities API. New and updated notices are pulled and matched throughout the day, so your morning digest reflects what was posted.',
  },
  {
    q: 'Can I cancel anytime?',
    a: 'Yes. Start with a free trial, no credit card required. Cancel anytime — there is no annual lock-in.',
  },
  {
    q: 'What happens on a day with no matching opportunities?',
    a: 'We send nothing. Zero-match days produce no email. The whole point is signal, not noise.',
  },
]

/* ---------- realistic product preview: a daily digest ---------- */
function DigestPreview() {
  const items = [
    { title: 'IT Help Desk & Network Support Services', agency: 'Dept. of the Army', naics: '541519', setAside: 'SDVOSB', due: '18 days' },
    { title: 'HVAC Preventive Maintenance — VA Medical Center', agency: 'Veterans Affairs', naics: '238220', setAside: 'Small Business', due: '12 days' },
    { title: 'Grounds Maintenance, Regional Installations', agency: 'Dept. of Defense', naics: '561730', setAside: 'WOSB', due: '25 days' },
  ]
  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-lift">
      <div className="h-1.5 rounded-t-2xl bg-gradient-to-r from-canary-400 to-canary-600" />
      <div className="flex items-center justify-between gap-3 border-b border-slate-100 px-5 py-4">
        <div className="flex items-center gap-2.5">
          <Logo className="h-7 w-7" />
          <div>
            <p className="text-sm font-semibold text-ink-900">Your daily digest</p>
            <p className="text-xs text-slate-400">Thursday · 7:00 AM</p>
          </div>
        </div>
        <span className="badge bg-canary-100 text-canary-800">3 new matches</span>
      </div>
      <ul className="divide-y divide-slate-100">
        {items.map((it) => (
          <li key={it.title} className="px-5 py-3.5">
            <p className="text-sm font-semibold text-ink-900">{it.title}</p>
            <p className="mt-0.5 text-xs text-slate-500">{it.agency}</p>
            <div className="mt-2 flex flex-wrap items-center gap-1.5">
              <span className="badge bg-slate-100 text-slate-600">NAICS {it.naics}</span>
              <span className="badge bg-brand-50 text-brand-700">{it.setAside}</span>
              <span className="badge bg-amber-50 text-amber-800">Due in {it.due}</span>
            </div>
          </li>
        ))}
      </ul>
      <div className="rounded-b-2xl bg-slate-50 px-5 py-3 text-center text-xs font-medium text-ink-700">
        View all matches in your dashboard →
      </div>
    </div>
  )
}

export function Landing() {
  const { data: meta } = useMeta()
  const trialDays = meta?.trialDays ?? 14

  return (
    <MarketingLayout>
      {/* ---------------- Hero ---------------- */}
      <section className="relative overflow-hidden">
        <div className="absolute inset-0 -z-10 bg-gradient-to-b from-canary-50/70 via-white to-white" />
        <div className="absolute -right-40 -top-40 -z-10 h-96 w-96 rounded-full bg-canary-200/40 blur-3xl" />
        <div className="mx-auto grid max-w-6xl items-center gap-12 px-4 py-16 sm:py-24 lg:grid-cols-2">
          <div className="animate-fade-up">
            <span className="eyebrow">
              <span className="inline-block h-1.5 w-1.5 rounded-full bg-canary-500" />
              {branding.hero.eyebrow}
            </span>
            <h1 className="mt-4 text-4xl font-extrabold leading-[1.1] tracking-tight text-ink-900 sm:text-5xl">
              {branding.hero.heading}
            </h1>
            <p className="mt-5 max-w-xl text-lg leading-relaxed text-slate-600">{branding.hero.sub}</p>
            <div className="mt-8 flex flex-wrap items-center gap-3">
              <Link to="/register" className="btn-primary px-6 py-3 text-base">Start your {trialDays}-day free trial</Link>
              <Link to="/pricing" className="btn-secondary px-6 py-3 text-base">See pricing</Link>
            </div>
            <ul className="mt-6 flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-slate-500">
              <li className="flex items-center gap-1.5"><Icon.Check className="h-4 w-4 text-canary-600" /> No credit card required</li>
              <li className="flex items-center gap-1.5"><Icon.Check className="h-4 w-4 text-canary-600" /> Cancel anytime</li>
              <li className="flex items-center gap-1.5"><Icon.Check className="h-4 w-4 text-canary-600" /> Set up in minutes</li>
            </ul>
          </div>
          <div className="animate-fade-up lg:pl-6">
            <DigestPreview />
          </div>
        </div>
      </section>

      {/* ---------------- Trust strip ---------------- */}
      <section className="border-y border-slate-100 bg-white">
        <div className="mx-auto grid max-w-6xl gap-6 px-4 py-8 sm:grid-cols-3">
          <div className="flex items-start gap-3">
            <div className="mt-0.5 flex h-9 w-9 flex-none items-center justify-center rounded-lg bg-ink-900 text-canary-400"><Icon.Shield className="h-5 w-5" /></div>
            <div>
              <p className="text-sm font-semibold text-ink-900">Official government data</p>
              <p className="text-sm text-slate-500">Sourced directly from the SAM.gov Contract Opportunities API.</p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <div className="mt-0.5 flex h-9 w-9 flex-none items-center justify-center rounded-lg bg-ink-900 text-canary-400"><Icon.Clock className="h-5 w-5" /></div>
            <div>
              <p className="text-sm font-semibold text-ink-900">Refreshed around the clock</p>
              <p className="text-sm text-slate-500">New and updated notices are pulled and matched throughout the day.</p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <div className="mt-0.5 flex h-9 w-9 flex-none items-center justify-center rounded-lg bg-ink-900 text-canary-400"><Icon.Mail className="h-5 w-5" /></div>
            <div>
              <p className="text-sm font-semibold text-ink-900">In your inbox by morning</p>
              <p className="text-sm text-slate-500">One tailored digest a day — delivered at 7 AM in your timezone.</p>
            </div>
          </div>
        </div>
      </section>

      {/* ---------------- How it works ---------------- */}
      <section className="mx-auto max-w-6xl px-4 py-20">
        <div className="mx-auto max-w-2xl text-center">
          <span className="eyebrow">How it works</span>
          <h2 className="mt-3 text-3xl font-bold tracking-tight text-ink-900">From SAM.gov to your inbox in three steps</h2>
        </div>
        <div className="mt-12 grid gap-6 sm:grid-cols-3">
          {steps.map((s, i) => (
            <div key={s.title} className="card p-6">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-canary-400 text-base font-bold text-ink-900">{i + 1}</div>
              <h3 className="mt-4 font-semibold text-ink-900">{s.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-slate-600">{s.body}</p>
            </div>
          ))}
        </div>
      </section>

      {/* ---------------- Features ---------------- */}
      <section className="bg-slate-50">
        <div className="mx-auto max-w-6xl px-4 py-20">
          <div className="mx-auto max-w-2xl text-center">
            <span className="eyebrow">Features</span>
            <h2 className="mt-3 text-3xl font-bold tracking-tight text-ink-900">Everything you need to never miss a bid</h2>
          </div>
          <div className="mt-12 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {features.map((f) => (
              <div key={f.title} className="card p-6 transition hover:-translate-y-0.5 hover:shadow-lift">
                <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-canary-100 text-canary-700">
                  <f.icon className="h-5 w-5" />
                </div>
                <h3 className="mt-4 font-semibold text-ink-900">{f.title}</h3>
                <p className="mt-2 text-sm leading-relaxed text-slate-600">{f.body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ---------------- Credibility / positioning ---------------- */}
      <section className="mx-auto max-w-6xl px-4 py-20">
        <div className="grid items-center gap-12 lg:grid-cols-2">
          <div>
            <span className="eyebrow">Why contractors trust us</span>
            <h2 className="mt-3 text-3xl font-bold tracking-tight text-ink-900">The big-budget tools watch everything. We watch what fits you.</h2>
            <p className="mt-5 text-slate-600">
              GovWin and Bloomberg Government are built for teams with five-figure budgets and full-time analysts.
              {' '}{branding.productName} does one thing exceptionally well: it turns the same free, public federal data
              into a focused daily signal a small contractor can act on before the deadline.
            </p>
            <ul className="mt-6 space-y-3">
              {[
                'Deterministic matching you can read and audit — not a mystery score.',
                'Built on free, public government data. No scraping, no gray areas.',
                'Honest, flat pricing. Start free, cancel anytime, no annual lock-in.',
              ].map((t) => (
                <li key={t} className="flex items-start gap-2.5 text-slate-700">
                  <Icon.Check className="mt-0.5 h-5 w-5 flex-none text-canary-600" />
                  <span>{t}</span>
                </li>
              ))}
            </ul>
          </div>
          <div className="rounded-2xl bg-ink-900 p-8 text-white shadow-lift">
            <p className="text-sm font-medium uppercase tracking-wider text-canary-400">The math</p>
            <p className="mt-3 text-lg leading-relaxed text-slate-200">
              Miss one qualified opportunity and you miss a contract worth many times a year of
              {' '}{branding.productName}. The whole point is that you stop finding out too late.
            </p>
            <div className="mt-8 grid grid-cols-3 gap-4 border-t border-white/10 pt-6 text-center">
              <div>
                <p className="text-2xl font-extrabold text-canary-400">Daily</p>
                <p className="mt-1 text-xs text-slate-400">matched digest</p>
              </div>
              <div>
                <p className="text-2xl font-extrabold text-canary-400">7 AM</p>
                <p className="mt-1 text-xs text-slate-400">your timezone</p>
              </div>
              <div>
                <p className="text-2xl font-extrabold text-canary-400">$0</p>
                <p className="mt-1 text-xs text-slate-400">to start</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ---------------- Comparison ---------------- */}
      <section className="bg-slate-50">
        <div className="mx-auto max-w-5xl px-4 py-20">
          <div className="mx-auto max-w-2xl text-center">
            <span className="eyebrow">How we compare</span>
            <h2 className="mt-3 text-3xl font-bold tracking-tight text-ink-900">Built for you, not for a procurement department</h2>
          </div>
          <div className="mt-10 overflow-x-auto">
            <table className="w-full min-w-[640px] border-collapse text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200">
                  <th className="py-3 pr-4 font-medium text-slate-500"></th>
                  <th className="py-3 px-4">
                    <span className="font-bold text-ink-900">Contract<span className="text-canary-700">Canary</span></span>
                  </th>
                  <th className="py-3 px-4 font-semibold text-slate-600">Big-budget suites</th>
                  <th className="py-3 px-4 font-semibold text-slate-600">Checking SAM.gov yourself</th>
                </tr>
              </thead>
              <tbody>
                {comparison.map((row) => (
                  <tr key={row.label} className="border-b border-slate-100">
                    <td className="py-3 pr-4 font-medium text-ink-900">{row.label}</td>
                    <td className="py-3 px-4 bg-canary-50/50">
                      <Cell value={row.canary} highlight />
                    </td>
                    <td className="py-3 px-4"><Cell value={row.suites} /></td>
                    <td className="py-3 px-4"><Cell value={row.diy} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <p className="mt-4 text-center text-xs text-slate-400">Comparison reflects typical offerings; not an endorsement of or by any named product.</p>
        </div>
      </section>

      {/* ---------------- FAQ ---------------- */}
      <section className="mx-auto max-w-3xl px-4 py-20">
        <div className="text-center">
          <span className="eyebrow">FAQ</span>
          <h2 className="mt-3 text-3xl font-bold tracking-tight text-ink-900">Questions, answered</h2>
        </div>
        <div className="mt-10 divide-y divide-slate-200 border-y border-slate-200">
          {faqs.map((f) => (
            <details key={f.q} className="group py-4">
              <summary className="flex cursor-pointer items-center justify-between gap-4 font-medium text-ink-900 marker:content-['']">
                {f.q}
                <svg className="h-5 w-5 flex-none text-slate-400 transition group-open:rotate-45" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M12 5v14M5 12h14" /></svg>
              </summary>
              <p className="mt-3 text-sm leading-relaxed text-slate-600">{f.a}</p>
            </details>
          ))}
        </div>
      </section>

      {/* ---------------- Final CTA ---------------- */}
      <section className="mx-auto max-w-6xl px-4 pb-24">
        <div className="relative overflow-hidden rounded-3xl bg-ink-900 px-6 py-16 text-center shadow-lift sm:px-16">
          <div className="absolute -right-24 -top-24 h-64 w-64 rounded-full bg-canary-400/20 blur-3xl" />
          <h2 className="text-3xl font-bold tracking-tight text-white sm:text-4xl">Your next contract is already posted.</h2>
          <p className="mx-auto mt-4 max-w-xl text-lg text-slate-300">
            Start your {trialDays}-day free trial and let {branding.productName} find it for you — no credit card required.
          </p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link to="/register" className="btn-accent px-7 py-3 text-base">Create your free account</Link>
            <Link to="/pricing" className="btn px-7 py-3 text-base text-slate-200 hover:bg-white/10">See pricing</Link>
          </div>
        </div>
      </section>
    </MarketingLayout>
  )
}

function Cell({ value, highlight }: { value: string; highlight?: boolean }) {
  if (value === 'yes') return <span className="inline-flex items-center gap-1.5 text-emerald-700"><Icon.Check className="h-4 w-4" /> Yes</span>
  if (value === 'no') return <span className="text-slate-400">—</span>
  return <span className={highlight ? 'font-semibold text-ink-900' : 'text-slate-600'}>{value}</span>
}
