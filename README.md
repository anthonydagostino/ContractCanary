# OppSignal

> **Working codename.** OppSignal is a government-contract opportunity
> intelligence platform for small US contractors. It turns the free public
> SAM.gov procurement feed into a daily, personalized signal — so a 5-to-50-person
> shop never misses a relevant federal opportunity again. Rename it by editing two
> config spots (see [Branding](#branding)).

<p>
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8-512BD4">
  <img alt="React 18" src="https://img.shields.io/badge/React-18-149ECA">
  <img alt="PostgreSQL 16" src="https://img.shields.io/badge/PostgreSQL-16-336791">
  <img alt="Tests" src="https://img.shields.io/badge/tests-105%2B%20green-2ea44f">
</p>

---

## Quick start (one command, zero credentials)

Requires only Docker.

```bash
docker compose up --build
```

Open **http://localhost:8088** and sign in with the seeded demo account:

| Account | Email | Password | Notes |
| ------- | ----- | -------- | ----- |
| Demo    | `demo@oppsignal.dev`  | `DemoPassword123!`  | Pro plan, 3 match profiles, ~170 matches |
| Admin   | `admin@oppsignal.dev` | `AdminPassword123!` | Same, plus the admin metrics page |

The stack boots in **Fixture mode** — a deterministic generator produces 600
realistic notices modeled on the real SAM.gov schema, so the whole product works
end-to-end with **no API keys**. Emails (verification, digests) are written to a
volume by the dev email sender:

```bash
docker compose exec api ls /app/maildrop      # open any .html to preview
```

To exercise the daily digest immediately, sign in as **admin** and use
**Admin → Run ingest now**, or `POST /api/admin/digest/send-me`.

---

## What's in the box (v1)

- **Auth** — ASP.NET Core Identity, email + password, email verification, password
  reset, JWT access + rotating refresh tokens, rate-limited auth endpoints.
- **Match profiles** — NAICS (full official 2022 list), PSC, keywords, agencies,
  set-asides, states, notice types. Multi-select typeaheads over embedded seed data.
- **Ingest pipeline** — a .NET worker pulls the SAM.gov Opportunities v2 API on a
  schedule (rate-limit-aware), normalizes, and upserts into Postgres. Fixture mode
  swaps the real client for a generator via one env var.
- **Matching engine** — deterministic: (NAICS **or** PSC) **and** keyword, agency,
  set-aside, state, notice-type. No AI. Deduped per (notice, profile). *Exhaustively
  unit-tested.*
- **Daily digest email** — one email per user per day, in their timezone, grouped
  by profile, skipped entirely on zero matches. Responsive HTML via Razor templates.
  Dev (disk) and Postmark senders behind `IEmailSender`.
- **Web app** — React + TS + Vite + Tailwind + TanStack Query: marketing/pricing,
  auth, dashboard (server-paginated, filterable opportunity table), opportunity
  detail with match-reason attribution, profile editor, saved opportunities,
  deadline tracker, account & billing settings, admin metrics.
- **Billing** — Stripe Checkout + Customer Portal + full webhook lifecycle. Starter
  $29 (1 profile) / Pro $79 (5 profiles, priority ingest, CSV export). 14-day
  no-card trial. Plan limits enforced in the API. *Enforcement + webhook tested.*
- **Ops** — health checks, Serilog structured logging, global ProblemDetails errors,
  admin metrics.

See [`ARCHITECTURE.md`](ARCHITECTURE.md) for the full design and
[`DECISIONS.md`](DECISIONS.md) for the choices made along the way.

---

## Project layout

```
backend/                     .NET 8 solution (see ARCHITECTURE.md §2)
  src/OppSignal.Domain        entities, enums — no dependencies
  src/OppSignal.Application    services, matching engine, plan policy, ports
  src/OppSignal.Infrastructure EF Core + migrations + seed, SAM client + fixture,
                               email, Stripe, identity, DI
  src/OppSignal.Api            REST API, auth, Stripe webhooks, Program.cs
  src/OppSignal.Worker         Quartz ingest + digest jobs
  tests/                       xUnit unit + integration (Testcontainers)
frontend/                     React SPA
deploy/Caddyfile              reverse proxy + SPA host + TLS
docs/                         SAM API notes, Terms + Privacy drafts
Dockerfile.{api,worker,web}   images
docker-compose.yml            local/demo stack (fixture)
docker-compose.prod.yml       production stack (RDS + real integrations)
.env.example                  authoritative env var reference
HUMAN_TODO.md                 the paste-and-restart checklist for going live
```

---

## Local development (without Docker)

**Prereqrequisites:** .NET 8 SDK, Node 22, and a PostgreSQL 16 you can reach.

```bash
# 1. Backend API (migrates + seeds on start)
cd backend
export ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=oppsignal;Username=oppsignal;Password=oppsignal"
export ASPNETCORE_URLS="http://localhost:5080" ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/OppSignal.Api

# 2. Worker (optional locally — the API's admin endpoints can trigger ingest/digest)
dotnet run --project src/OppSignal.Worker

# 3. Frontend (proxies /api → http://localhost:5080)
cd ../frontend
npm install
npm run dev            # http://localhost:5173
```

Swagger is available at `http://localhost:5080/swagger` in Development.

---

## Testing

```bash
# Backend — unit tests (fast, no I/O)
cd backend && dotnet test tests/OppSignal.UnitTests

# Backend — integration tests. Uses Testcontainers (needs Docker) by default.
# In a sandbox where pulling images is blocked, point at an existing Postgres:
export TEST_POSTGRES_ADMIN="Host=localhost;Port=5432;Username=oppsignal;Password=oppsignal;Database=postgres"
dotnet test tests/OppSignal.IntegrationTests

# Frontend
cd frontend && npm test
```

The matching engine and plan-limit enforcement have exhaustive coverage
(truth tables + per-gate tests + full API enforcement).

---

## Branding

All user-facing branding lives in **exactly two** spots:

1. **Backend** — the `Branding` config section (bound to `BrandingOptions`),
   overridable by `Branding__*` env vars. Used by emails and API metadata.
2. **Frontend** — [`frontend/src/config/branding.ts`](frontend/src/config/branding.ts).

Rename the product by editing those two files and pointing your domain at the
stack. Nothing else references the product name directly.

---

## Environment variables

`.env.example` is the authoritative reference. Highlights:

| Variable | Purpose |
| -------- | ------- |
| `ConnectionStrings__Postgres` | Single DB connection string (RDS in prod) |
| `Jwt__SigningKey` | 32+ byte secret for signing access tokens |
| `Ingest__Source` | `Fixture` (default) or `Sam` |
| `Sam__ApiKey` | SAM.gov account API key (only for `Sam` mode) |
| `Email__Provider` | `Dev` (disk) or `Postmark` |
| `Email__Postmark__ServerToken` | Postmark server token |
| `Stripe__SecretKey` / `Stripe__WebhookSecret` | Stripe keys |
| `Stripe__StarterPriceId` / `Stripe__ProPriceId` | Stripe price ids |
| `Branding__WebBaseUrl` | Public web URL (email links) |
| `Cors__Origins__0` | Allowed SPA origin (only needed if web and API are cross-origin) |

Every secret is read from an env var — going live is **paste-and-restart**. The
exact source of each value is in [`HUMAN_TODO.md`](HUMAN_TODO.md).

---

## Deployment

The compose file is the deployment artifact (built for a Docker + Portainer host).
`docker-compose.prod.yml` runs api + worker + Caddy (TLS) and points at an external
Postgres (AWS RDS). Follow [`HUMAN_TODO.md`](HUMAN_TODO.md) for the ordered,
credential-by-credential go-live checklist.

---

## Data & legal

- Opportunity data comes from the **free public SAM.gov API**. This product is not
  affiliated with SAM.gov, GSA, or any government agency.
- The included Terms of Service and Privacy Policy
  ([`docs/`](docs/)) are **drafts** and must be reviewed by counsel before launch.
