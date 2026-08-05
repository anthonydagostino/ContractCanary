// ─────────────────────────────────────────────────────────────────────────────
// BRANDING — one of the two branding config spots (the other is the backend
// `Branding` config section). Rename the product by editing this file, the
// backend Branding section, and the public domain. Nothing else references the
// product name directly.
// ─────────────────────────────────────────────────────────────────────────────
export const branding = {
  productName: 'ContractCanary',
  tagline: 'Federal contract opportunities, matched to your business.',
  description:
    'ContractCanary turns free public procurement data into a daily, personalized signal that helps small government contractors find and organize relevant opportunities faster.',
  supportEmail: 'support@contract-canary.com',
  // Legal/company info. Sole proprietorship operating under the product name;
  // a lawyer should confirm the Terms details (the /terms and /privacy pages
  // carry a pending-review banner until then).
  companyLegalName: 'Anthony D’Agostino, doing business as ContractCanary',
  governingLawState: 'New Jersey',
  // Marketing one-liners used on the landing page. Phrased as capability claims
  // (what the tool does), not outcome guarantees, to stay within FTC advertising rules.
  hero: {
    eyebrow: 'Federal contract intelligence for small business',
    heading: 'Find the federal contracts that fit your business.',
    sub: 'We watch SAM.gov for you and email a personalized daily digest of new opportunities matched to your NAICS codes, keywords, agencies, and set-asides — so you spend less time searching. No GovWin budget required.',
  },
} as const

export type Branding = typeof branding
