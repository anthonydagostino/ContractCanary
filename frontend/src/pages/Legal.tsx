import { MarketingLayout } from '../components/MarketingLayout'
import { branding } from '../config/branding'

/**
 * Plain-language drafts of the Terms of Service and Privacy Policy. They cover the
 * standard owner-protection clauses (AS-IS disclaimer, liability cap, no guarantee
 * of results, indemnity, arbitration, force majeure) and the CalOPPA-required
 * privacy disclosures. They are NOT legal advice and MUST be reviewed by a licensed
 * attorney before launch — see the banner. Keep in sync with docs/*.md.
 */
export function LegalPage({ doc }: { doc: 'terms' | 'privacy' }) {
  const isTerms = doc === 'terms'
  return (
    <MarketingLayout>
      <section className="mx-auto max-w-3xl px-4 py-16">
        <div className="mb-6 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          <strong>Draft — pending legal review.</strong> This document is a starting draft and is not legal advice.
          Have a licensed attorney review it before you rely on it.
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-slate-900">
          {isTerms ? 'Terms of Service' : 'Privacy Policy'}
        </h1>
        <p className="mt-2 text-sm text-slate-500">Last updated: {new Date().toLocaleDateString()}</p>

        <div className="prose prose-slate mt-8 max-w-none text-sm leading-6 text-slate-700">
          {isTerms ? <TermsBody /> : <PrivacyBody />}
        </div>
      </section>
    </MarketingLayout>
  )
}

const P = branding.productName
const Support = branding.supportEmail
const Entity = branding.companyLegalName
const State = branding.governingLawState || '[the state where the company is organized]'

function H({ children }: { children: React.ReactNode }) {
  return <h3 className="mt-7 font-semibold text-slate-900">{children}</h3>
}

function TermsBody() {
  return (
    <>
      <p>
        These Terms of Service (“Terms”) are a binding agreement between you and {Entity} (“{P}”, “we”, “us”).
        By creating an account, subscribing, or using {P} you agree to these Terms and to our Privacy Policy. If you
        do not agree, do not use the service. If you use {P} on behalf of a business, you represent that you are
        authorized to bind that business.
      </p>

      <H>1. The service</H>
      <p>
        {P} aggregates publicly available U.S. federal procurement data (including from SAM.gov) and delivers
        personalized notifications, matching, tracking, and optional AI-generated summaries. It is an
        informational and research tool only.
      </p>

      <H>2. Not affiliated with the government; verify before you rely</H>
      <p>
        {P} is an independent, privately owned service. We are <strong>not affiliated with, endorsed by, or sponsored
        by</strong> SAM.gov, the U.S. General Services Administration, or any government agency. The same data is
        available to the public for free from the government. Data may be incomplete, delayed, cached, or contain
        errors, and automated matching and AI summaries may be wrong. <strong>You are responsible for verifying every
        opportunity, deadline, amount, eligibility requirement, and other detail directly on the official SAM.gov
        listing before bidding or otherwise relying on it.</strong> Nothing here is legal, financial, tax, or
        procurement advice.
      </p>

      <H>3. Accounts</H>
      <p>
        You must provide accurate information and keep your credentials secure. You are responsible for all activity
        under your account. Notify us promptly at {Support} of any unauthorized use.
      </p>

      <H>4. Subscriptions, billing &amp; auto-renewal</H>
      <p>
        Paid plans are billed monthly in advance through our payment processor, Stripe. <strong>Your subscription
        renews automatically each month at the then-current price until you cancel.</strong> You can cancel at any
        time from Settings → Manage billing (Stripe Customer Portal) or by emailing {Support}; cancellation stops
        future renewals and your access continues through the end of the paid period. We will give advance notice
        before any price increase as required by law. Fees are non-refundable except where required by law or as
        stated in our posted refund practice; contact {Support} promptly about a billing error or an unwanted renewal
        and we will work with you in good faith.
      </p>

      <H>5. Free trials</H>
      <p>
        If we offer a free trial, we will disclose its length and terms at sign-up. Unless you cancel before it ends,
        a trial that requires payment details converts to a paid subscription at the disclosed price.
      </p>

      <H>6. Acceptable use</H>
      <p>
        You agree not to: resell or redistribute the service or its data feed; scrape, crawl, or use bots to evade
        limits; reverse engineer the service; share your account; probe or breach security; upload unlawful or
        infringing content; or use {P} to violate any law or third-party right. We may suspend or terminate accounts
        that violate this section.
      </p>

      <H>7. No guarantee of results</H>
      <p>
        {P} does <strong>not guarantee that you will find, qualify for, win, or be awarded any contract</strong>, or
        achieve any particular result or return. Your outcomes depend on many factors outside our control.
      </p>

      <H>8. Warranty disclaimer</H>
      <p className="uppercase">
        The service and all data are provided “as is” and “as available,” without warranties of any kind, whether
        express, implied, or statutory, including any implied warranties of merchantability, fitness for a particular
        purpose, title, and non-infringement. We do not warrant that the service will be uninterrupted, timely,
        secure, or error-free, or that any data is accurate, complete, or current.
      </p>

      <H>9. Limitation of liability</H>
      <p className="uppercase">
        To the maximum extent permitted by law, {P} and its owners will not be liable for any indirect, incidental,
        special, consequential, or punitive damages, or for any lost profits, lost business, lost contracts, or lost
        data, even if advised of the possibility. Our total liability for any claim relating to the service will not
        exceed the amount you paid us in the three (3) months before the event giving rise to the claim.
      </p>
      <p>
        Some jurisdictions do not allow certain limitations, so some of the above may not apply to you.
      </p>

      <H>10. Indemnification</H>
      <p>
        You agree to indemnify and hold harmless {P} and its owners from any third-party claims, losses, and expenses
        (including reasonable attorneys’ fees) arising from your use of the service, your content or data, your bids
        or proposals, or your violation of these Terms or any law.
      </p>

      <H>11. Service changes</H>
      <p>
        We may modify, add, remove, or discontinue features or the service (in whole or part) at any time. We will
        give reasonable notice of material adverse changes where practical.
      </p>

      <H>12. Termination</H>
      <p>
        You may stop using the service and delete your account at any time (Settings → Delete account). We may
        suspend or terminate access for breach, non-payment, abuse, or legal reasons. On termination your right to
        use the service ends; prepaid fees are non-refundable except where required by law.
      </p>

      <H>13. Force majeure</H>
      <p>
        We are not liable for any delay or failure to perform caused by events beyond our reasonable control,
        including failures, outages, rate limits, or data errors of upstream sources (SAM.gov, api.data.gov),
        Stripe, our email provider, hosting or cloud providers, network failures, and government shutdowns.
      </p>

      <H>14. Governing law &amp; dispute resolution</H>
      <p>
        These Terms are governed by the laws of {State}, without regard to conflict-of-laws rules. Except for
        claims that qualify for small-claims court, any dispute will be resolved by binding individual arbitration,
        and <strong>you and {P} waive any right to a jury trial and to participate in a class action</strong>. You
        may opt out of arbitration by emailing {Support} within 30 days of first accepting these Terms; if you opt
        out, disputes will be resolved in the state or federal courts located in {State}. <em>(This clause and its
        enforceability should be confirmed by your attorney.)</em>
      </p>

      <H>15. General</H>
      <p>
        If any provision is held unenforceable, the rest remains in effect (severability). These Terms and the
        Privacy Policy are the entire agreement between us. We may assign these Terms (e.g., in a sale of the
        business); you may not assign without our consent. Our failure to enforce a provision is not a waiver. We may
        update these Terms and will post the new version with an updated date and, for material changes, notify you
        by email or in-app; continued use after changes take effect means you accept them.
      </p>

      <H>16. Contact</H>
      <p>Questions about these Terms? Email {Support}.</p>
    </>
  )
}

function PrivacyBody() {
  return (
    <>
      <p>
        This Privacy Policy explains what {Entity} (“{P}”) collects, how we use it, and your choices. By using {P}
        you agree to this policy.
      </p>

      <H>1. Information we collect</H>
      <p>
        <strong>Account information</strong> you provide: name, email address, company name, timezone, and your
        password (stored only as a secure hash). <strong>Your preferences and content:</strong> match profiles
        (NAICS codes, keywords, agencies, set-asides, states), saved opportunities and notes, and pursuit stages.
        <strong> Automatically collected data:</strong> IP address, device/browser and log data, and an
        authentication token stored in your browser’s local storage to keep you signed in. Payment details (card
        numbers) are collected and processed by Stripe — <strong>we never receive or store your full card number.</strong>
      </p>

      <H>2. How we use it</H>
      <p>
        To operate and provide the service (authenticate you, match and deliver opportunities, send your digests and
        alerts, generate optional AI summaries), to process billing, to provide support, to maintain security and
        prevent abuse, and to comply with law.
      </p>

      <H>3. How we share it (service providers)</H>
      <p>
        We share data only with the processors that run the service, under contracts that limit their use of it:
        <strong> Stripe</strong> (payment processing), <strong>Postmark</strong> (transactional and digest email),
        <strong> SAM.gov / api.data.gov</strong> (the public data source), <strong>Anthropic</strong> (only if AI
        summaries are enabled — opportunity text may be processed to generate a summary), and our hosting/cloud
        provider. We may also disclose data if required by law or to protect our rights. <strong>We do not sell or
        “share” your personal information</strong> (as those terms are defined under California law), and we do not
        use it for third-party advertising.
      </p>

      <H>4. Cookies &amp; tracking</H>
      <p>
        We use a single strictly-necessary authentication token in your browser’s local storage to keep you signed
        in; it is not used for advertising or cross-site tracking. <strong>We do not use third-party analytics,
        advertising, or tracking cookies.</strong> Because we don’t track you across other sites, we do not respond
        to “Do Not Track” browser signals. You can clear your browser’s local storage at any time (which will sign
        you out).
      </p>

      <H>5. Data retention</H>
      <p>
        We keep your account data for as long as your account is active. If you delete your account, we delete your
        personal data promptly, except limited records we must retain for legal, tax, or fraud-prevention reasons
        (for example, payment records held by Stripe).
      </p>

      <H>6. Security</H>
      <p>
        We use reasonable technical and organizational measures to protect your data, including encryption in transit
        (HTTPS), hashed passwords, and access controls. No method of transmission or storage is 100% secure, so we
        cannot guarantee absolute security.
      </p>

      <H>7. Your rights &amp; choices</H>
      <p>
        You can <strong>access and download</strong> your data (Settings → Download my data), <strong>correct</strong>
        your account details (Settings → Profile), and <strong>delete</strong> your account and personal data at any
        time (Settings → Delete account). You can unsubscribe from the opportunity digest from any digest email or in
        Settings. Depending on where you live, you may have rights to access, correct, delete, or receive a copy of
        your personal information, and to be free from discrimination for exercising them; we honor these requests.
        To make a request you can’t complete in-app, email {Support}. We do not sell or share personal information,
        so there is nothing to opt out of on that basis.
      </p>

      <H>8. Data breach</H>
      <p>
        If a breach affecting your personal information occurs, we will notify affected users and any regulators as
        required by applicable law.
      </p>

      <H>9. Children</H>
      <p>
        {P} is a business tool and is <strong>not directed to children under 13</strong>. We do not knowingly collect
        personal information from children.
      </p>

      <H>10. Changes</H>
      <p>
        We may update this policy; we will post the new version with an updated date and, for material changes,
        notify you by email or in-app.
      </p>

      <H>11. Contact</H>
      <p>Privacy questions or requests? Email {Support}.</p>
    </>
  )
}
