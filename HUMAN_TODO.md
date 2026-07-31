# HUMAN_TODO — going live

Everything below is something **only you** can do because it needs your accounts,
credentials, money, or judgment. The code already reads every value from an env
var, so your job is **paste-and-restart** — no code changes.

Work top to bottom. Each item says exactly where the value goes (the env var in
your production `.env`, created from [`.env.example`](.env.example)).

> Before any of this you can already run the whole product locally with zero
> credentials: `docker compose up --build` → http://localhost:8088. Do that first
> to confirm the stack is healthy.

---

## 0. Confirm the outside-activity / ethics disclosure (do this FIRST)

**Owner-only.** Before launching a commercial side venture, confirm your
employer's outside-activity / conflict-of-interest / ethics policy and complete
any required disclosure or approval. Selling to the federal government can carry
extra scrutiny. Don't skip this — it's the one item no amount of engineering can
handle for you.

---

## 1. Pick the real product name and buy the domain

Currently the product is codenamed **OppSignal**. When you choose the real name,
change it in **exactly these places**:

1. `frontend/src/config/branding.ts` — `productName`, `tagline`, `supportEmail`, marketing copy.
2. Production `.env` — `BRANDING_PRODUCT_NAME`, `BRANDING_TAGLINE`,
   `BRANDING_SUPPORT_EMAIL`, `BRANDING_COMPANY_LEGAL_NAME`, `BRANDING_WEB_BASE_URL`.
3. `SITE_ADDRESS` in `.env` (your domain, e.g. `app.newname.com`).
4. The `<title>` in `frontend/index.html` (optional polish).
5. `docs/TERMS_OF_SERVICE.md` and `docs/PRIVACY_POLICY.md` company/name references.

Buy the domain (Namecheap, Cloudflare, etc.). Point an **A/AAAA record** for
`app.<yourdomain>` at your Portainer host's public IP (needed for TLS in step 7).

---

## 2. Create a SAM.gov account + request an API key

1. Create/sign in at **https://sam.gov** and get an individual account.
2. Go to **Account Details → request a public API key** (also called the
   api.data.gov key). It's free.
3. Your keyed non-federal account gets **1,000 requests/day** — the ingest
   schedule (a few requests every 3 hours) stays well under it.

**Where it goes:**
- `SAM_API_KEY=<your key>`
- `INGEST_SOURCE=Sam`   (switches off fixture data)

Until you do this, leave `INGEST_SOURCE=Fixture` and the app runs on generated data.

---

## 3. Create a Stripe account + products, prices, and webhook

1. Create a **Stripe** account. Stay in **Test mode** until you're ready to charge.
2. **Products & prices** — create two recurring monthly products:
   - *Starter* — $29/month → copy its **price id** (`price_...`).
   - *Pro* — $79/month → copy its **price id**.
3. **API keys** (Developers → API keys): copy the **Secret key** and
   **Publishable key**.
4. **Webhook** (Developers → Webhooks → Add endpoint):
   - URL: `https://app.<yourdomain>/api/billing/webhook`
   - Events: `checkout.session.completed`, `customer.subscription.created`,
     `customer.subscription.updated`, `customer.subscription.deleted`,
     `invoice.payment_failed`.
   - After creating it, copy the **Signing secret** (`whsec_...`).

**Where it goes:**
- `STRIPE_SECRET_KEY=sk_...`
- `STRIPE_PUBLISHABLE_KEY=pk_...`
- `STRIPE_WEBHOOK_SECRET=whsec_...`
- `STRIPE_STARTER_PRICE_ID=price_...`
- `STRIPE_PRO_PRICE_ID=price_...`
- `STRIPE_SUCCESS_URL` / `STRIPE_CANCEL_URL` / `STRIPE_PORTAL_RETURN_URL` — already
  templated to your domain in `.env.example`; just confirm the domain.

**Go-live:** flip Stripe to Live mode, recreate the products/prices/webhook in Live,
and swap the four values above for their `live` equivalents. Nothing else changes.

The trial (14 days, no card) is handled in code — no Stripe config needed for it.

---

## 4. Create a Postmark account + verify your sending domain

(You can use Amazon SES instead — see the note at the end.)

1. Create a **Postmark** account and a **Server**; copy its **Server API Token**.
2. **Sender Signatures / Domains** → add your domain and verify it by adding the
   **DKIM** and **Return-Path (CNAME)** DNS records Postmark shows you. Verified
   domains dramatically improve deliverability.
3. Choose the From address (e.g. `digests@<yourdomain>`).

**Where it goes:**
- `EMAIL_PROVIDER=Postmark`
- `POSTMARK_SERVER_TOKEN=<token>`
- `EMAIL_FROM_EMAIL=digests@<yourdomain>`
- `EMAIL_FROM_NAME=<Your product name>`

Until then, leave `EMAIL_PROVIDER=Dev` and emails are written to the `maildrop`
volume instead of sent.

> **SES alternative:** if you prefer SES, verify your domain in SES, then implement
> the `IEmailSender` seam the same way `PostmarkEmailSender` does (it's ~80 lines)
> and register it. The interface and `EmailLog` auditing are already in place.

---

## 5. Provision production Postgres (AWS RDS)

1. Create a **PostgreSQL 16** instance on **RDS** (a small `db.t4g.micro` is plenty
   to start). Set a strong master password; create a database named `oppsignal`.
2. Put it in a security group your Portainer host can reach on 5432.
3. Build the connection string (SSL required):

```
Host=<rds-endpoint>;Port=5432;Database=oppsignal;Username=oppsignal;Password=<pw>;SSL Mode=Require;Trust Server Certificate=true
```

**Where it goes:**
- `CONNECTIONSTRINGS_POSTGRES=<the string above>`

The **api** container automatically applies EF Core migrations and seeds the
reference data (NAICS/PSC/agencies/set-asides) on first start. Keep `SEED_DEMO=false`
in production so no fake demo users/notices are created.

---

## 6. Generate the app secret

Generate a strong JWT signing key (32+ bytes) and paste it in:

```bash
openssl rand -base64 48
```

**Where it goes:** `JWT_SIGNING_KEY=<output>`

---

## 7. Deploy on your Portainer host with DNS + TLS

The compose file is the deployment artifact. Caddy (in the `web` service)
serves the SPA, reverse-proxies `/api` to the API, and **auto-provisions
Let's Encrypt TLS** for your domain — no manual certs.

1. On the host: `cp .env.example .env` and fill in everything from steps 1–6.
2. Make sure DNS `app.<yourdomain>` → host IP has propagated, and ports **80 and
   443** are open to the internet (Caddy needs 80 for the ACME challenge).
3. Deploy:

   ```bash
   docker compose -f docker-compose.prod.yml up -d --build
   ```

   In **Portainer**: *Stacks → Add stack → Upload/Repository*, select
   `docker-compose.prod.yml`, and paste your `.env` into the *Environment
   variables* section.
4. Visit `https://app.<yourdomain>` — you should get a valid certificate and the
   landing page. Register a real account and confirm the verification email
   arrives (Postmark).
5. In Stripe, send a **test webhook** (or run a real test-mode checkout) and confirm
   the subscription appears under **Admin → metrics** / the user's Settings page.

The reverse-proxy config is [`deploy/Caddyfile`](deploy/Caddyfile) — it already
handles the SPA history fallback, the `/api` proxy, gzip, and baseline security
headers. Set `SITE_ADDRESS` to your domain for automatic HTTPS.

**Make the first admin:** register normally, then flip the flag once in the DB:

```sql
UPDATE "AspNetUsers" SET "IsAdmin" = true WHERE "Email" = 'you@yourdomain.com';
```

---

## 8. Legal pass on the Terms of Service and Privacy Policy

The drafts in [`docs/TERMS_OF_SERVICE.md`](docs/TERMS_OF_SERVICE.md) and
[`docs/PRIVACY_POLICY.md`](docs/PRIVACY_POLICY.md) (also rendered at `/terms` and
`/privacy` in the app) are **solid starting templates, not legal advice**. Have
counsel review and adjust for your entity, state, data-handling, and refund policy
before you take real money. The in-app pages show a "pending legal review" banner
until you remove it.

---

## Quick reference — the whole `.env`

Copy [`.env.example`](.env.example) to `.env` and fill in:

| From step | Variables |
| --------- | --------- |
| 1 | `SITE_ADDRESS`, `BRANDING_*`, `BRANDING_WEB_BASE_URL` |
| 2 | `INGEST_SOURCE=Sam`, `SAM_API_KEY` |
| 3 | `STRIPE_SECRET_KEY`, `STRIPE_PUBLISHABLE_KEY`, `STRIPE_WEBHOOK_SECRET`, `STRIPE_STARTER_PRICE_ID`, `STRIPE_PRO_PRICE_ID` |
| 4 | `EMAIL_PROVIDER=Postmark`, `POSTMARK_SERVER_TOKEN`, `EMAIL_FROM_EMAIL`, `EMAIL_FROM_NAME` |
| 5 | `CONNECTIONSTRINGS_POSTGRES`, `SEED_DEMO=false` |
| 6 | `JWT_SIGNING_KEY` |

Then `docker compose -f docker-compose.prod.yml up -d --build` and you're live.
