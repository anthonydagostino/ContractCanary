# OppSignal — Architecture

> Working codename **OppSignal**. All user-facing branding lives in exactly two
> config spots (see [Branding](#branding)); renaming the product is a two-file edit.

OppSignal is a government-contract opportunity intelligence platform for small US
contractors. It ingests free public procurement data (SAM.gov Contract
Opportunities), matches it against user-defined **match profiles** using
deterministic rules, and delivers a daily personalized digest email plus a
searchable web dashboard. It is a boring, secure, conventional B2B SaaS — no
scraping, no AI.

---

## 1. System overview

```
                         ┌──────────────────────────────────────────────┐
                         │                  Browser (SPA)                │
                         │        React 18 + TS + Vite + Tailwind         │
                         └───────────────┬────────────────────────────────┘
                                         │ HTTPS (JSON, JWT bearer)
                         ┌───────────────▼────────────────┐
                         │        Caddy reverse proxy       │  TLS, routing
                         │  /  -> web    /api -> api         │
                         └───────┬──────────────────┬───────┘
                                 │                  │
                   ┌─────────────▼───┐      ┌───────▼──────────────┐
                   │  OppSignal.Api   │      │  OppSignal.Worker     │
                   │  ASP.NET Core    │      │  Quartz jobs:         │
                   │  Web API         │      │   • Ingest            │
                   │  Identity + JWT  │      │   • Daily digest      │
                   │  Stripe webhooks │      │                       │
                   └───────┬──────────┘      └───────┬───────────────┘
                           │                         │
                           │   both share the same   │
                           │   EF Core data layer     │
                           └───────────┬─────────────┘
                                       │
                              ┌────────▼─────────┐
                              │  PostgreSQL 16    │  (RDS in prod)
                              └───────────────────┘

   External integrations (all behind interfaces, all mockable, all env-configured):
     • SAM.gov Opportunities v2 API  ── ISamOpportunitiesClient (Real | Fixture)
     • Postmark transactional email  ── IEmailSender          (Dev  | Postmark)
     • Stripe billing                ── IBillingService        (Stripe | fake in tests)
```

Two runnable processes share one data layer and one domain model:

| Process              | Project              | Responsibility                                            |
| -------------------- | -------------------- | --------------------------------------------------------- |
| **API**              | `OppSignal.Api`      | Auth, REST endpoints, Stripe webhooks, admin metrics.     |
| **Worker**           | `OppSignal.Worker`   | Scheduled ingest + matching, daily digest send (Quartz).  |

Splitting the worker from the API means ingest/digest load never competes with
request latency, and the two scale independently on the Docker/Portainer host.

---

## 2. Solution layout (Clean-ish layering)

```
backend/
  OppSignal.sln
  Directory.Build.props            # shared TFM / nullable / implicit usings
  src/
    OppSignal.Domain/              # entities, enums, value objects — ZERO dependencies
    OppSignal.Application/         # use-case services, interfaces (ports), DTOs,
                                   #   the matching engine, plan policy, validators
    OppSignal.Infrastructure/      # EF Core DbContext + migrations + seed data,
                                   #   SAM.gov client (+ fixture), email senders,
                                   #   Stripe billing, identity, DI composition root
    OppSignal.Api/                 # controllers, JWT auth, middleware, Program.cs
    OppSignal.Worker/              # Quartz host, ingest & digest jobs
  tests/
    OppSignal.UnitTests/           # matcher + plan-limit + pure-logic tests (fast)
    OppSignal.IntegrationTests/    # WebApplicationFactory + Testcontainers Postgres
frontend/                          # React SPA (see §9)
deploy/                            # Caddyfile, prod env template
docs/                              # Terms of Service / Privacy Policy drafts
```

**Dependency rule:** `Domain` depends on nothing. `Application` depends only on
`Domain` and defines *interfaces* (`ISamOpportunitiesClient`, `IEmailSender`,
`IBillingService`, `IAppDbContext`, `IClock`). `Infrastructure` implements them.
`Api`/`Worker` reference `Application` + `Infrastructure` and wire DI. This keeps
the matching engine and plan policy — the parts that must be exhaustively tested —
free of EF Core, HTTP, and Stripe so they unit-test in-memory with no I/O.

---

## 3. Data model

Natural keys and audit columns are used throughout. `Notice.NoticeId` is the
SAM.gov notice id (natural key); everything else uses GUID surrogate keys.

```
AppUser (ASP.NET Core Identity, Guid key)
  ├─ TimeZoneId (IANA, default "America/New_York")
  ├─ IsAdmin
  └─ 1───* MatchProfile
             ├─ NAICS codes            (string[])
             ├─ PSC codes              (string[], optional)
             ├─ Keywords               (string[], optional; title+description)
             ├─ AgencyPaths            (string[], optional; matched by prefix)
             ├─ SetAsides              (SetAsideCode[], optional)
             ├─ States                 (string[] 2-letter, optional; place of perf)
             ├─ NoticeTypes            (NoticeType[], optional)
             └─ IsActive, IsPriority (Pro), CreatedAt/UpdatedAt

Notice  (natural key = NoticeId)
  ├─ Title, SolicitationNumber, Type (NoticeType), BaseType
  ├─ AgencyPath (fullParentPathName), DepartmentName, SubTierName, OfficeName
  ├─ NaicsCode, PscCode (classificationCode)
  ├─ SetAside (SetAsideCode?), SetAsideDescription
  ├─ PostedDate, ResponseDeadline (UTC), ArchiveDate
  ├─ PopState, PopCity, PopCountry, PopZip   (place of performance)
  ├─ UiLink (sam.gov), DescriptionLink, Description (resolved text, nullable)
  ├─ PrimaryContactName/Email/Phone
  ├─ RawJson (jsonb — full upstream record, forward-compatible)
  └─ FirstSeenAt, LastSeenAt, SourceUpdatedAt

NoticeMatch (Notice × MatchProfile)         # dedup key: (NoticeId, MatchProfileId)
  ├─ MatchedAt
  ├─ NotifiedAt (nullable)  → set when included in a digest; guarantees no double-send
  └─ MatchReason (jsonb: which filters fired — for the detail view & debugging)

SavedNotice (AppUser × Notice)              # star/unstar; unique (UserId, NoticeId)
  └─ SavedAt, Note (nullable)

Subscription (mirror of Stripe state, 1:1 with AppUser)
  ├─ StripeCustomerId, StripeSubscriptionId, StripePriceId
  ├─ Plan (None|Starter|Pro), Status (Trialing|Active|PastDue|Canceled|...)
  ├─ TrialEndsAt, CurrentPeriodEndsAt, CancelAtPeriodEnd
  └─ UpdatedAt

IngestRun (audit of each pull)
  ├─ Source (Sam|Fixture), StartedAt, CompletedAt, Status (Running|Succeeded|Failed)
  ├─ WindowFrom, WindowTo, PagesFetched, NoticesSeen, NoticesInserted,
  │  NoticesUpdated, MatchesCreated
  └─ Error (nullable)

EmailLog (audit of every send)
  └─ UserId, ToAddress, Kind (Verification|PasswordReset|Digest|...),
     Subject, SentAt, Provider, ProviderMessageId, Success, Error

# Reference / seed tables
NaicsCode (Code PK, Title, Level 2–6, ParentCode)          # 1,012 six-digit + hierarchy
PscCode   (Code PK, Title, Category, IsService)            # curated seed (see DECISIONS)
Agency    (Code PK, Name, Tier, ParentCode)                # top federal departments/sub-tiers
SetAsideRef (Code PK, Name, Description)                    # SBA, 8A, SDVOSBC, WOSB, ...

# v2 seam (built empty, documented stub — NOT ingested in v1)
Award (natural key = award id)                             # USAspending.gov ingestion stub
```

Indexes: `Notice(PostedDate)`, `Notice(ResponseDeadline)`, `Notice(NaicsCode)`,
`Notice(PscCode)`, `Notice(SetAside)`, GIN on `Notice.SearchVector` (title +
description full-text) for the dashboard search, unique `NoticeMatch(NoticeId,
MatchProfileId)`, unique `SavedNotice(UserId, NoticeId)`.

Multi-valued profile filters (NAICS, keywords, …) are stored as Postgres `text[]`
columns (mapped by Npgsql) — simpler than child tables and queryable with array
operators. Documented in DECISIONS.

---

## 4. Ingest pipeline

`ISamOpportunitiesClient` has two implementations, selected by the
`Ingest__Source` env var (`Sam` | `Fixture`, default `Fixture`):

* **`SamOpportunitiesClient`** — talks to the real SAM.gov **Get Opportunities
  Public API v2** (`GET {base}/opportunities/v2/search`). Details in
  [SAM_API.md](docs/SAM_API.md). Key facts the client is built to:
  * `postedFrom` / `postedTo` are **required**, format `MM/dd/yyyy`, max 1-year window.
  * `api_key` passed as a query param (SAM.gov account key).
  * `limit` ≤ 1000, `offset` paging; response envelope is
    `{ totalRecords, limit, offset, opportunitiesData:[...] }`.
  * **Rate limit** for a non-federal keyed account: **1,000 requests/day.** The
    scheduler pulls a small rolling window a few times per day and pages at
    `limit=1000`, so a normal day is a handful of requests — comfortably inside
    the free tier. A `RateLimitGuard` hard-caps requests/run and logs if hit.
  * Resilience: typed `HttpClient` + Polly-style retry with backoff on 429/5xx,
    honoring `Retry-After`.

* **`FixtureOpportunitiesClient`** — deterministically generates **500+ varied
  fake notices** modeled field-for-field on the real schema (seeded RNG so runs
  are reproducible; supports the same date-window/paging contract). Lets the
  entire system run and demo with **zero credentials**.

The **`IngestService`** (Application) is source-agnostic: it opens an
`IngestRun`, pulls the window page by page, normalizes each raw record into a
`Notice` (parsing dates to UTC, mapping notice-type & set-aside strings to enums,
flattening place-of-performance), **upserts** by `NoticeId` (insert or update if
`SourceUpdatedAt` changed), then invokes the matcher for new/updated notices, and
finally closes the `IngestRun` with counts. Everything is idempotent.

---

## 5. Matching engine (deterministic, exhaustively tested)

Pure function over a `Notice` and a `MatchProfile`, in `OppSignal.Application`
with **no I/O dependencies** so it unit-tests against in-memory lists.

A notice matches a profile iff **all** of the following hold (an unset/empty
filter is treated as "no constraint" and passes):

```
(profile.Naics is empty  OR  notice.NaicsCode ∈ profile.Naics
        OR  profile.Psc is empty ? false : notice.PscCode ∈ profile.Psc)   ← NAICS OR PSC
AND (profile.Keywords is empty     OR  any keyword ⊂ (title + description), case-insensitive)
AND (profile.AgencyPaths is empty  OR  notice.AgencyPath starts-with any selected path)
AND (profile.SetAsides is empty    OR  notice.SetAside ∈ profile.SetAsides)
AND (profile.States is empty       OR  notice.PopState ∈ profile.States)
AND (profile.NoticeTypes is empty  OR  notice.Type ∈ profile.NoticeTypes)
```

The **NAICS-OR-PSC** clause is the one subtlety: if a profile sets *both* NAICS
and PSC filters, a notice matching *either* passes that clause (then must still
pass the AND filters). If only NAICS is set, PSC is ignored, and vice-versa. This
matches the spec ("satisfies the profile's NAICS OR PSC filter") and is pinned by
a dedicated truth-table test.

**Dedup:** matches are written to `NoticeMatch` with a unique `(NoticeId,
MatchProfileId)` constraint. A user is notified at most once per (notice,
profile): the digest only includes matches with `NotifiedAt == null`, and stamps
`NotifiedAt` after a successful send. Re-ingesting the same notice never creates a
duplicate match.

The `MatchEngine` is covered by a truth table over every filter (set/unset ×
pass/fail), the OR/AND interaction, keyword case-insensitivity & substring
semantics, agency prefix semantics, and dedup behavior.

---

## 6. Billing & plan limits

Stripe **Checkout** (subscription mode) + **Customer Portal**, mirrored into the
`Subscription` table via **webhooks** (`checkout.session.completed`,
`customer.subscription.created|updated|deleted`, `invoice.payment_failed`, …).
Webhook signatures verified with the endpoint secret.

| Plan        | Price   | Profiles | Extras                                               |
| ----------- | ------- | -------- | ---------------------------------------------------- |
| **Starter** | $29/mo  | 1        | daily digest                                         |
| **Pro**     | $79/mo  | 5        | priority ingest of saved searches, CSV export        |

**14-day free trial, no card required** — Checkout uses
`trial_period_days=14` + `payment_method_collection=if_required`.

The **`PlanPolicy`** (Application, pure) is the single source of truth for limits
(`MaxProfiles`, `CanExportCsv`, `CanPrioritize`). The API enforces it on
profile-create and on CSV export. `PlanPolicy` is exhaustively unit-tested; the
enforcement paths are integration-tested. Everything Stripe is behind
`IBillingService`, so the whole suite passes offline with a fake implementation;
against real Stripe it uses **test-mode** keys/price ids from env vars.

Trial/entitlement resolution: a user is *entitled* to a plan's features while
`Status ∈ {Trialing, Active}` (and `PastDue` within a grace window). New signups
get a Starter trial row automatically so they can use the product immediately.

---

## 7. Email

`IEmailSender` (Application port) with two implementations:

* **`DevEmailSender`** — writes each message as `.html` + `.txt` to `maildrop/`
  and logs a line to console. Zero credentials; used in compose fixture mode.
* **`PostmarkEmailSender`** — posts to Postmark's REST API, configured purely by
  env vars (`Email__Postmark__ServerToken`, `Email__FromAddress`, …).

Templates are rendered with **RazorLight** from embedded `.cshtml` resources into
responsive, MJML-quality HTML (inlined styles, table layout, dark-mode-safe). The
**daily digest** groups a user's new matches by profile with title, agency,
notice type, response deadline, set-aside, place of performance, a SAM.gov link,
and an in-app detail link. **Zero matches → no email is sent.** Verification and
password-reset emails use the same rendering path. Every send is written to
`EmailLog`.

---

## 8. Scheduling (Worker)

**Quartz.NET** hosted in `OppSignal.Worker`:

* **IngestJob** — every `Ingest__IntervalMinutes` (default 180 → 8×/day, well
  within 1,000 req/day). Pulls a rolling `Ingest__WindowDays` window (default 3,
  to catch late updates), upserts, matches. Non-overlapping (`[DisallowConcurrentExecution]`).
* **DigestJob** — runs hourly; for each user whose local time has just crossed
  the configured send hour (default 07:00 local), sends that user's digest of
  un-notified matches and stamps `NotifiedAt`. Hourly tick + per-user timezone
  math means one digest per user per day in their own timezone.

Both jobs are thin shells over Application services, so they're testable without
the scheduler.

---

## 9. Frontend

React 18 + TypeScript + Vite + Tailwind, TanStack Query for server state, React
Router. Served as static files behind Caddy; talks to the API with a JWT bearer
(access token in memory, refresh token rotated via `/api/auth/refresh`).

Pages: marketing landing + pricing, login / register / verify / forgot / reset,
dashboard (server-paginated, filterable opportunity table), opportunity detail,
profile editor (NAICS/PSC/agency typeahead over the seed reference data), saved
opportunities, deadline tracker (saved items by response date), account & billing
settings (Stripe portal launch), admin metrics. Clean, professional B2B styling;
mobile responsive. Vitest + React Testing Library cover the critical logic
(matching-preview, plan-gating, filter serialization).

---

## 10. Ops

* **Health:** `/health` (liveness) and `/health/ready` (DB check).
* **Logging:** Serilog structured JSON to console (container-friendly), request
  logging middleware, correlation ids.
* **Errors:** global exception middleware → RFC7807 ProblemDetails; no stack
  traces leak to clients in Production.
* **Rate limiting:** ASP.NET Core rate limiter on auth endpoints
  (login/register/forgot) by IP.
* **Admin metrics:** `/api/admin/metrics` (admin-only): users, active
  subscribers by plan, notices ingested, emails sent, last ingest-run status.
  Surfaced on an admin-only web page.

---

## 11. Configuration & secrets

All configuration is environment-variable driven (12-factor). The database is a
single connection-string env var (`ConnectionStrings__Postgres`) so production
points at AWS RDS with no code change. `.env.example` is the authoritative
reference; every secret the human must supply is listed in `HUMAN_TODO.md` and is
already read from an env var — the human's job is paste-and-restart.

### Branding

All user-facing product name / support email / marketing copy live in exactly two
places:

* Backend: `appsettings.json` → `Branding` section (bound to `BrandingOptions`),
  overridable by `Branding__*` env vars. Used by emails and API metadata.
* Frontend: `frontend/src/config/branding.ts`.

Rename the product by editing those two spots (and the domain in `.env`).

---

## 12. Testing strategy

* **Unit (`OppSignal.UnitTests`)** — matching engine (truth tables), `PlanPolicy`
  limits, notice normalization/mapping, digest grouping, timezone send-window
  logic, fixture determinism. No I/O.
* **Integration (`OppSignal.IntegrationTests`)** — `WebApplicationFactory` +
  **Testcontainers Postgres** (real schema & migrations). Covers auth flow,
  profile CRUD with plan-limit enforcement, notices search pagination, saved
  notices, Stripe webhook handling (posted fixture events), full ingest→match
  cycle in fixture mode. Stripe/email are faked.
* **Frontend (Vitest + RTL)** — filter serialization, plan-gating UI, form
  validation, match-preview logic.

`docker compose up` from a fresh clone in fixture mode seeds a demo user and
500+ notices, producing a fully clickable product. Login is documented in the
README.
