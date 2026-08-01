# Decisions Log

One line of reasoning per decision. Newest at the bottom. These are the calls a
senior engineer / founder made autonomously so the build could keep moving.

1. **Codename & branding indirection.** Kept the name "OppSignal" everywhere but
   funneled all user-facing branding through two config spots (`BrandingOptions`
   backend + `frontend/src/config/branding.ts`) so the real name is a two-file
   change. — the owner picks the real name later.

2. **Layered solution (Domain/Application/Infrastructure/Api/Worker).** Chosen so
   the matching engine and plan policy live in dependency-free assemblies and can
   be exhaustively unit-tested without EF Core/HTTP/Stripe.

3. **Auth = ASP.NET Core Identity + JWT access/refresh tokens** (not cookie auth,
   not `MapIdentityApi`). A separately-hosted SPA is cleanest with bearer tokens,
   and hand-rolling the Identity flows gives full control over the verification /
   reset email content and links. Refresh tokens are rotated and stored hashed.

4. **NAICS seed = full official 2022 Census file (1,012 six-digit codes + full
   2–6 digit hierarchy).** Pulled from the U.S. Census 2022 NAICS code file and
   embedded as seed data, so typeahead is authoritative and offline.

5. **PSC seed = curated subset, not the full ~5,000-code manual.** PSC filtering
   is optional in the product and the full manual wasn't available from an
   allowed data source in this environment. Shipped a curated, accurate set of
   the major Federal Supply Classes / Service categories most relevant to the
   target verticals (IT `D`, prof-services `R`, maintenance `J`, construction
   `Y`/`Z`, facilities, etc.), with a documented CSV loader so the full PSC
   manual can be dropped in later. Recorded in HUMAN_TODO.

6. **Multi-valued profile filters stored as Postgres `text[]` columns** (NAICS,
   PSC, keywords, agencies, states, set-asides, notice-types) rather than child
   tables. Npgsql maps arrays natively, queries are simple array-contains, and a
   profile is naturally one row. Trade-off: no FK integrity on those values, which
   is fine — they're validated at the API against the reference tables.

7. **Fixture mode is the default `Ingest__Source`.** Guarantees a fresh clone
   runs end-to-end with zero credentials. Real SAM.gov is opt-in via env var once
   the owner has an API key.

8. **Ingest cadence: 3-hour interval, 3-day rolling window, `limit=1000` paging.**
   A handful of requests/day — comfortably inside the non-federal 1,000 req/day
   SAM.gov cap — while a 3-day window re-catches late upstream updates. A
   `RateLimitGuard` hard-caps requests per run as a backstop.

9. **Email digest via RazorLight rendering embedded `.cshtml`.** Satisfies the
   "Razor template" requirement while running fine outside MVC and inside a
   container (templates are embedded resources, compiled once and cached).

10. **Plan entitlement = `Status ∈ {Trialing, Active}` (+ short PastDue grace).**
    New users get an auto-created Starter *trial* subscription row on signup, so
    they can use the product immediately with no card, matching the 14-day
    no-card-trial requirement.

11. **JWT + refresh in the SPA:** access token held in memory, refresh token in an
    httpOnly-style flow via `/api/auth/refresh`. Kept simple and secure; documented
    as a reasonable v1 posture (not silent-refresh-via-iframe or third-party IdP).

12. **Awards table exists but is never populated in v1.** Built the empty table +
    a documented `IAwardIngestionStub` no-op so the v2 USAspending.gov recompete
    feature has a clean seam, per the explicit non-goal.

13. **Postgres full-text search (`tsvector` GIN) for the dashboard search box**,
    generated from title + description. Native, fast, no extra service. Keyword
    *matching* for profiles stays a deterministic substring check (spec wording),
    independent of the search index.

14. **`InvariantGlobalization` disabled** because per-user IANA timezone math
    (digest send window) and `MM/dd/yyyy` culture formatting for the SAM.gov API
    need real culture/timezone data.

15. **Stripe test-mode + fully mockable.** `IBillingService` isolates Stripe; unit
    and integration suites use a fake, so CI is fully offline. Real runs use
    test-mode keys/prices from env until the owner goes live.

16. **CSV export (Pro) is generated server-side** from the same filtered query the
    dashboard uses, streamed as `text/csv`, and gated by `PlanPolicy.CanExportCsv`.

17. **Fixture notice dates are anchored to "today".** The 600-notice corpus is
    spread over the last ~45 days relative to the clock's current date (stable
    within a day, keyed by date). This keeps the recent-window ingest always
    populated and makes the demo feel live ("posted today"). Trade-off: notices'
    posted dates drift day-over-day, so a notice can appear "updated" on a later
    day — harmless (dedup prevents duplicate matches / double-notify).

18. **Profile backfill matches are pre-marked notified.** When a profile is
    created/edited, existing-notice matches are (re)built with `NotifiedAt=now` so
    the backlog is immediately browsable in the dashboard but is NOT blasted out
    as a giant first digest. Only genuinely-new matches from scheduled ingest
    (`NotifiedAt=null`) go into the daily email.

19. **Integration tests: Testcontainers by default, env-Postgres fallback.** The
    fixture spins `postgres:16-alpine` via Testcontainers (the user's Docker
    environment). If `TEST_POSTGRES_ADMIN` is set it instead provisions a fresh
    uniquely-named database on that server — so the suite runs in CI/sandboxes
    where pulling the image is blocked. Documented in README.

20. **AI opportunity summaries are generated once per notice and cached, not
    per-user.** The summary describes the *opportunity* (identical for everyone),
    so a background worker job (`SummaryEnrichmentJob`, every ~10 min, newest-first,
    bounded per pass) summarizes each notice one time and stores it on the
    `notices` row; every user sees the same summary. Cost therefore scales with
    new notices (~hundreds/day), not with customer count. Personalization stays in
    the matching layer. Off by default (`Ai:Enabled=false`, no key) → the app is
    unchanged; set `AI_ENABLED=true` + `ANTHROPIC_API_KEY` to switch it on.

21. **AI provider = hosted Claude via raw HTTP, structured output, model
    configurable.** We call the Anthropic Messages API (`IOpportunitySummarizer`)
    rather than training/hosting a model — summarization is a solved frontier-LLM
    task and a hosted API is pay-per-use with zero infra. `output_config.format`
    (JSON schema) guarantees parseable results; the prompt is instructed to use
    only supplied facts (no invented deadlines/dollars — the UI shows the
    authoritative deadline from structured data). Default model
    `claude-haiku-4-5` (cheap bulk); set `AI_MODEL=claude-opus-5` for max quality.
    Failures return null and retry up to `Ai:MaxAttempts` so one bad record can't
    stall the batch. Prompt builder + response parser are pure and unit-tested.
