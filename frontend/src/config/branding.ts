// ─────────────────────────────────────────────────────────────────────────────
// BRANDING — one of the two branding config spots (the other is the backend
// `Branding` config section). Rename the product by editing this file, the
// backend Branding section, and the public domain. Nothing else references the
// product name directly.
// ─────────────────────────────────────────────────────────────────────────────
export const branding = {
  productName: 'OppSignal',
  tagline: 'Never miss a federal contract again.',
  description:
    'OppSignal turns free public procurement data into a daily, personalized signal — so small government contractors never miss a relevant opportunity again.',
  supportEmail: 'support@oppsignal.example',
  // Marketing one-liners used on the landing page.
  hero: {
    eyebrow: 'Federal contract intelligence for small business',
    heading: 'Never miss a federal contract that fits your business.',
    sub: 'We watch SAM.gov for you and email a personalized daily digest of new opportunities matched to your NAICS codes, keywords, agencies, and set-asides. No GovWin budget required.',
  },
} as const

export type Branding = typeof branding
