# GROWTH_STRATEGY — how ContractCanary gets its first 100 customers

Written for **you, the owner**, in the same spirit as HUMAN_TODO.md: concrete steps,
honest costs, and no growth-hacker fantasy. This is the plan for **whenever** you
decide to start — nothing here expires, and every phase stands alone.

**The one-sentence strategy:** be findable at the exact moment a small business
decides to try federal contracting, look 10× cheaper than GovWin because you are,
and let a free-trial-without-credit-card do the closing.

---

## 1. Who we're selling to (get this right and everything else gets easier)

**The buyer:** the owner or BD person at a 1–25 employee business that sells (or
wants to sell) services to the federal government. HVAC, janitorial, grounds
maintenance, IT services, security guards, construction trades, staffing,
consulting. Often holds or is pursuing a set-aside: SDVOSB, 8(a), WOSB, HUBZone.

**Their situation:** they registered in SAM.gov (or are about to), tried searching
it manually, found it miserable, and either gave up or waste hours a week on it.
They have heard of GovWin/Deltek and laughed at the price. They are not on Product
Hunt or Hacker News. They ARE on LinkedIn, in govcon Facebook groups, on
r/govcon, at APEX Accelerator workshops, and watching govcon YouTube.

**Three personas to write copy for:**
1. **The brand-new registrant** — just got their SAM registration/CAGE code.
   Doesn't know what a Sources Sought is. Wants someone to tell them where the
   opportunities are. *Easiest to win; lowest price sensitivity to $29.*
2. **The frustrated manual searcher** — checks SAM.gov weekly, misses things,
   found out about an RFP three days before the deadline. *Your digest demo sells
   itself here.*
3. **The graduating small contractor** — won 1–3 contracts, thinking about
   pipeline seriously, priced GovWin and choked. *This is the Pro/$79 buyer;
   Recompete Radar is the pitch.*

---

## 2. Positioning & message

**Elevator pitch:** *"ContractCanary watches SAM.gov for you and emails you a
daily digest of only the federal opportunities that match your business — plus,
on Pro, the incumbent contracts in your market that are about to expire. It's the
useful 90% of a $10,000/year market-intelligence suite for $29–79 a month."*

**One-liners by context:**
- vs. doing nothing: *"Your next contract is already posted. You just haven't seen it."*
- vs. manual SAM.gov: *"Stop searching. Start seeing only what fits."*
- vs. GovWin/Deltek: *"The parts a small contractor actually needs, at 1% of the price."*
- Recompete Radar: *"See tomorrow's bids before they're posted."*

**Objections you'll hear, and the answers (already built into the site):**
- *"Isn't this data free?"* — Yes, and we say so on the site. You're paying to
  never have to look for it. (Honesty here is a trust weapon; use it.)
- *"How is this different from SAM.gov's own saved searches?"* — SAM's email
  alerts are crude, noisy, and miss amendment/deadline changes; no matching
  logic, no recompetes, no pipeline tracking, no AI summaries.
- *"Will this win me contracts?"* — No, and never claim it will (the Terms
  already disclaim it). It wins them *time* and *earlier positioning*.

**Rule for all marketing copy:** capability claims, not outcome promises. "Find
opportunities faster," never "win more contracts." This keeps you consistent with
the FTC posture the site already takes.

---

## 3. Unit economics (what you can afford to spend)

Estimates — replace with real numbers after ~50 customers:

| Input | Estimate | Notes |
|---|---|---|
| Blended price | ~$45/mo | mix of $29 and $79 |
| Stripe fees | ~3.4% | 2.9% + 30¢ + Radar's 5¢ |
| Infra cost | ~$12–25/mo TOTAL | fixed; doesn't grow per customer for a long time |
| Monthly churn guess | 6–10% | niche B2B SaaS at this price; hope for better |
| → Lifetime value (LTV) | **$400–650** | price ÷ churn, rough |
| → Sane CAC ceiling | **≤$130** | keep acquisition ≤ ⅓ of LTV |

**What this means practically:** you can afford roughly $100–130 of ads or effort
per *paying* customer. If trials convert at ~25%, that's ~$25–35 per trial
signup. Any channel that beats that scales; any channel that can't gets killed.
Break-even on the whole business is ~1 customer (infra is ~$12/mo) — everything
after customer #1 is testing budget.

---

## 4. Phase 0 — before spending a dollar (one afternoon)

Marketing sends people to the site; these make sure the site converts and you can
see what's happening. Most are already on your HUMAN_TODO list:

- [ ] **Postmark live** (emails ARE the product — nothing launches before this)
- [ ] **Backups + uptime monitor** (an outage during your launch week is the nightmare scenario)
- [ ] **Test the full paid path once yourself** (subscribe with a real card, cancel)
- [ ] **Turn on AI summaries** (`AI_ENABLED=true` + Anthropic key) — it's advertised on the comparison table; make it true. Cost is pennies per notice.
- [ ] **Analytics.** The site currently has none (the privacy policy proudly says so).
      Use a privacy-respecting one so the policy stays honest with one small edit:
      **Plausible** (~$9/mo) or self-hosted **Umami** (free, runs on your droplet).
      Ask engineering to add it + update the privacy page in the same commit.
      Without this you cannot tell which channel works, which makes every ad
      dollar a guess.
- [ ] **Google Search Console** (free): verify contract-canary.com, submit the
      sitemap. This is how Google finds your pages and how you see what searches
      you appear for.
- [ ] **Set up a "from the founder" email** (you@contract-canary.com forward) for
      outreach — replies to a real person convert.

---

## 5. Phase 1 — $0/month: plant the free stuff that compounds (start anytime)

These cost time, not money, and they keep paying forever. Do these WHILE deciding
whether to ever spend on ads.

### 5a. Programmatic SEO (the highest-leverage free asset — engineering builds it)
People search Google for exactly what you sell, with NAICS codes and industries in
the query: *"janitorial government contracts"*, *"NAICS 561720 opportunities"*,
*"how to find federal HVAC contracts"*, *"SDVOSB set aside opportunities"*. The
five-figure suites don't bother competing for this long tail.

Ask engineering (me) for: **public landing pages per industry/NAICS** — e.g.
`/contracts/janitorial-services`, `/contracts/naics/561720` — each showing live
counts and a few *recent example* opportunities from data we already ingest, a
plain-English "how to win these" paragraph, and a signup CTA. ~50–200 pages,
generated from the reference NAICS table. This is days of work, free traffic
forever, and the single best thing to build when you say go.

Support it with 5–10 hand-written guides (I can draft; you review): "First 90
days after your SAM registration", "What is a Sources Sought (and why answering
one wins contracts)", "GovWin alternatives for small business", "What is a
recompete". The "alternatives" page is important: people literally search
"GovWin alternative" and those searchers are your persona #3 with a budget.

**Honest expectation:** SEO takes 3–6 months to move. Plant it early precisely
*because* you're not launching yet.

### 5b. Free listings (an hour total)
- **G2, Capterra/GetApp, Software Advice** — create free vendor profiles. Ask
  every happy early user for a review; 5 reviews on G2 beats most ads for trust.
- **Google Business Profile** — free, helps brand searches look legitimate.

### 5c. Communities (steady drip, 30 min/week, do NOT spam)
- **r/govcon** (~50k contractors and feds) — answer beginners' "how do I find
  opportunities" questions genuinely; mention the tool only when directly
  relevant, or in your flair/profile. One good answer a week.
- **Govcon Facebook groups** ("Government Contracting for Beginners" etc.) and
  **LinkedIn** — same rule: be the helpful person who obviously runs a tool, not
  the tool pitch that pretends to be helpful.
- **Your own LinkedIn** — post 1–2×/week: interesting opportunities you noticed,
  recompete factoids ("$2.1M Army IT contract expires in March — someone's going
  to win that rebid"), lessons from building the product. Founder content is free
  and this niche's feed is not crowded.

### 5d. APEX Accelerators (formerly PTACs) — the sleeper channel
Every state has government-funded offices whose whole job is advising small
businesses on federal contracting, for free. Their counselors constantly get
asked *"how do I find opportunities?"* and they recommend tools. Email the ones
in NJ and neighboring states: offer counselors free Pro accounts and a one-page
handout for their workshops. One warm counselor = a steady trickle of perfect-fit
signups at $0 CAC. Same play for **SCORE mentors** and **SBA district office
workshop presenters**.

---

## 6. Phase 2 — first paid experiments ($300–600/mo, run each for 4–6 weeks)

Only after Phase 0 analytics exist, and only one channel at a time so you can
attribute results.

### 6a. Google Ads on high-intent searches (the first ad dollar goes here)
- Campaign 1 — category intent: "government contract alerts", "SAM.gov alerts",
  "find federal contracts", "government bid notification service"
- Campaign 2 — competitor intent: "GovWin alternative", "GovWin pricing",
  "GovTribe alternative" (send these to the comparison section)
- Expect **$3–10 per click** in this niche; at a 5–10% visitor→trial rate that's
  roughly **$30–100 per trial** — marginal against your CAC ceiling, which is
  why you test with $10–20/day, watch which keywords actually convert in
  Plausible + Stripe, and cut the rest. Exact-match keywords only at first;
  add negative keywords aggressively ("jobs", "grants", "free").
- Skip LinkedIn ads (gorgeous targeting, $8–15 CPCs, wrong price point) and skip
  Facebook prospecting (audience too hard to target) at this stage.

### 6b. Govcon newsletter & podcast sponsorships ($100–500 per placement)
Small niche newsletters and YouTube/podcast creators (govcon coaching space:
Govcon Giants, Neil McDonnell's GovCon Chamber audience, various "govcon coach"
YouTubers) sell cheap sponsorships and their audiences are 100% your ICP. One
$200 placement that brings 10 trials → 2–3 customers beats a month of bad ads.
Offer their audience an extended 30-day trial code so you can attribute it.

### 6c. Referral/affiliate (engineering builds when asked)
Govcon coaches and consultants advise exactly your persona #1 daily. A simple
affiliate deal (e.g. 30% for 6 months, or flat $25/conversion) turns them into a
sales force. Needs referral codes + tracking — ask me and it's a day of work.

---

## 7. Phase 3 — scale what worked (only after something in Phase 2 repeatedly pays back)

- Pour budget into the winning channel until CAC degrades.
- **Annual pricing** (2 months free) — improves cash flow and cuts churn;
  engineering task, half a day.
- **Free tool as lead magnet** (engineering): e.g. a public "Recompete lookup —
  see federal contracts in your industry expiring this year" teaser page (3
  results free, signup for the rest). Turns the Radar into a traffic asset.
- Guest appearances on govcon podcasts (free once you have a story: "we built
  the anti-GovWin").
- Revisit cold outreach (below) with lessons learned.

---

## 8. Cold outreach playbook (optional, effective, handle with care)

New SAM.gov entity registrations are **public data** — a stream of businesses
that *just* decided to enter federal contracting: your persona #1, at the exact
right moment.

- **LinkedIn-first is safest:** connect with the owner, no pitch in the request;
  after accept, one short human message ("Saw you just registered in SAM —
  the first 90 days are confusing, this guide might help" → your guide, not your
  pricing page).
- **Cold email is legal under CAN-SPAM** (it's not spam-law-compliant in some
  other countries — stick to US recipients) but must: use your real identity and
  postal address, have a working unsubscribe, and never use a deceptive subject.
  Keep it to ~20–30/day, personalized (industry + state), from a separate domain
  variant (e.g. `contractcanary.io`) so your product email domain's reputation
  is never at risk. Expect 1–3% conversion to trial; it's a grind, but it's a
  $0-cash channel with perfect targeting.
- Never buy generic email lists. The SAM stream is better and free.

---

## 9. Metrics: the five numbers that matter (check weekly, in order)

1. **Trials started** (by source — this is why analytics exists)
2. **Activation rate** — % of trials that create a match profile within 24h.
   If this is low, marketing isn't the problem; onboarding is. (Target >60%.)
3. **Trial→paid conversion** (target 15–30%; below 10% = product/pricing
   problem, stop buying traffic)
4. **Churn** (monthly; ask every canceler the one-question "why?" — the digest
   makes retention naturally sticky *if* their profile is well-tuned, so most
   churn will be fixable profile quality)
5. **CAC per channel** vs the ~$130 ceiling → kill or scale decisions

**Kill criteria, decided in advance:** any paid channel that after 6 weeks and
~$500 hasn't produced a paying customer under ~2× the CAC ceiling gets paused.
No sunk-cost extensions.

---

## 10. Budget scenarios

| | $0/mo (time only) | ~$350/mo | ~$1,000/mo |
|---|---|---|---|
| SEO pages + guides | ✅ build now | ✅ | ✅ |
| Communities + LinkedIn | ✅ 1–2 h/wk | ✅ | ✅ |
| APEX/SCORE outreach | ✅ | ✅ | ✅ |
| Analytics | Umami self-hosted | Plausible $9 | Plausible $9 |
| Google Ads | — | ~$250 test | ~$500 scaled |
| Newsletter/podcast spots | — | 1 × ~$100 | 2–3 × ~$150 |
| Affiliate payouts | — | — | ~$150 |
| **Realistic outcome after 3 months** | seeds planted, first organic trickle | first attributable customers, know your CAC | 15–40 trials/mo if a channel works |

**Honest expectations:** month 1 of *any* launch: mostly silence — normal. This
niche is won by showing up consistently for 6 months, not by a launch spike.
The compounding assets (SEO pages, reviews, APEX relationships, LinkedIn
presence) are what make month 6 different from month 1.

---

## 11. What to ask engineering for, when you're ready (each is ~a day or two)

In rough priority order:
1. Analytics (Plausible/Umami) + privacy-policy line update — **prerequisite for everything paid**
2. Programmatic NAICS/industry landing pages + sitemap — the SEO engine
3. Trial-extension coupon codes (for newsletter/podcast attribution)
4. Referral/affiliate codes + tracking
5. Annual pricing
6. Public "recompete teaser" lead-magnet page
7. Blog scaffold for the guides

Everything on this list is deliberately NOT built yet — no point maintaining
growth machinery before you want growth.

---

*Estimates marked as such throughout; replace with your real numbers as they
arrive. Companion docs: HUMAN_TODO.md (launch ops), docs/ (product).*
