import { MarketingLayout } from '../components/MarketingLayout'
import { branding } from '../config/branding'

/**
 * These are TEMPLATE drafts rendered from the same source as docs/TERMS_OF_SERVICE.md
 * and docs/PRIVACY_POLICY.md. They MUST be reviewed by counsel before launch.
 */
export function LegalPage({ doc }: { doc: 'terms' | 'privacy' }) {
  const isTerms = doc === 'terms'
  return (
    <MarketingLayout>
      <section className="mx-auto max-w-3xl px-4 py-16">
        <div className="mb-6 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          <strong>Template — pending legal review.</strong> This document is a starting draft and is not legal advice.
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-slate-900">
          {isTerms ? 'Terms of Service' : 'Privacy Policy'}
        </h1>
        <p className="mt-2 text-sm text-slate-400">Last updated: {new Date().toLocaleDateString()}</p>

        <div className="prose prose-slate mt-8 max-w-none text-sm leading-6 text-slate-700">
          {isTerms ? <TermsBody /> : <PrivacyBody />}
        </div>
      </section>
    </MarketingLayout>
  )
}

function TermsBody() {
  return (
    <>
      <p>Welcome to {branding.productName}. By creating an account you agree to these Terms.</p>
      <h3 className="mt-6 font-semibold text-slate-900">1. The service</h3>
      <p>{branding.productName} aggregates publicly available U.S. federal procurement data and delivers personalized
        notifications. We are not affiliated with SAM.gov, GSA, or any government agency. Data may be incomplete,
        delayed, or contain errors; you are responsible for verifying opportunities directly on SAM.gov.</p>
      <h3 className="mt-6 font-semibold text-slate-900">2. Accounts</h3>
      <p>You must provide accurate information and keep your credentials secure. You are responsible for activity on
        your account.</p>
      <h3 className="mt-6 font-semibold text-slate-900">3. Subscriptions &amp; billing</h3>
      <p>Paid plans are billed monthly in advance through Stripe. Trials convert to paid only if you subscribe. You can
        cancel at any time; access continues through the current period.</p>
      <h3 className="mt-6 font-semibold text-slate-900">4. Acceptable use</h3>
      <p>Do not resell the service, scrape it, or use it to violate any law. We may suspend accounts that abuse the
        platform.</p>
      <h3 className="mt-6 font-semibold text-slate-900">5. Disclaimer &amp; liability</h3>
      <p>The service is provided “as is” without warranties. To the maximum extent permitted by law, our liability is
        limited to the amount you paid in the prior three months.</p>
      <h3 className="mt-6 font-semibold text-slate-900">6. Contact</h3>
      <p>Questions? Email {branding.supportEmail}.</p>
    </>
  )
}

function PrivacyBody() {
  return (
    <>
      <p>This policy explains what {branding.productName} collects and why.</p>
      <h3 className="mt-6 font-semibold text-slate-900">1. What we collect</h3>
      <p>Account details (name, email, company), your match profiles, saved opportunities, and standard usage/technical
        logs. Payment details are handled by Stripe; we never see full card numbers.</p>
      <h3 className="mt-6 font-semibold text-slate-900">2. How we use it</h3>
      <p>To operate the service — match opportunities, send your digests, provide support, and process billing.</p>
      <h3 className="mt-6 font-semibold text-slate-900">3. Sharing</h3>
      <p>We share data only with processors that run the service (e.g. our email provider and Stripe), never sold to
        third parties.</p>
      <h3 className="mt-6 font-semibold text-slate-900">4. Your choices</h3>
      <p>You can edit or delete your profiles at any time and request account deletion by emailing
        {' '}{branding.supportEmail}.</p>
      <h3 className="mt-6 font-semibold text-slate-900">5. Contact</h3>
      <p>Privacy questions? Email {branding.supportEmail}.</p>
    </>
  )
}
