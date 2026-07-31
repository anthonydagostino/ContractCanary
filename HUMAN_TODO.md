# HUMAN_TODO — the complete, click-by-click go-live guide

This is written for **you, the owner**, assuming you have **not** done a deploy like
this before. It spells out every account to create, every button to click, every
value to copy, and exactly which line of your config it goes into. Follow it top to
bottom.

The engineering is done. Everything below needs *your* accounts, money, or judgment.
**You never edit code** — every secret is read from an environment variable, so your
job is literally: create an account → copy a value → paste it into one file → restart.

---

## How to use this guide

- Do the parts **in order**. Later parts depend on earlier ones (e.g. you need your
  domain before Stripe URLs, and your server before you point DNS at it).
- Whenever you see **➜ PUT IT IN:** `SOME_VAR=...`, that means: open your `.env` file
  (you'll create it in Part 8) and set that line. Keep a scratch note (even a text
  file) of each value as you collect it — you'll paste them all at once in Part 8.
- Each part ends with a **✅ You'll know it worked when…** checkpoint. Don't move on
  until it's true.
- Third-party websites reword their buttons occasionally. I give you the **menu path
  and the URL**, which rarely change; if a label is slightly different, the nearby
  wording will match.

**Rough time & cost:** ~2–4 hours the first time. Ongoing cost to run it: a small
server (~$6–12/mo), AWS RDS database (~$15/mo or free tier for 12 months), domain
(~$10/yr), Stripe (2.9% + 30¢ per charge, nothing until you charge), Postmark (free
up to 100 emails/mo, then ~$15/mo). SAM.gov and Cloudflare DNS are free.

---

## Part A — What you need before you start

You need accounts (all free to create) at:

1. A **domain registrar / DNS** — this guide uses **Cloudflare** (free, and we'll use
   it for DNS records). If you already own a domain elsewhere, that's fine; you can
   still move just its DNS to Cloudflare, or add the same records at your current
   registrar.
2. A **server host** — a computer in the cloud that runs your app 24/7. This guide
   uses a **DigitalOcean** "droplet" running **Docker + Portainer**. If you already
   have a Portainer host, skip to using it in Part 2.
3. **SAM.gov** — for the free government-data API key.
4. **Stripe** — to take payments.
5. **Postmark** — to send email (verification + daily digests).
6. **AWS** — for the production PostgreSQL database (RDS).

You'll also want on **your own laptop**:
- A **web browser** (obviously) and a **terminal**:
  - **Mac:** press ⌘+Space, type "Terminal", Enter.
  - **Windows:** install "Windows Terminal" from the Microsoft Store, or use
    "PowerShell" (search it in the Start menu).
- A **text editor** for the `.env` file (VS Code, Notepad++, or even Notepad).

### "Do I need Docker installed?"

- **On your own laptop:** only if you want to test the app locally first (Part B —
  recommended but optional).
- **On the server:** **yes** — the server runs everything with Docker. Part 2 installs
  it for you. Docker is the thing that runs the app; Portainer is a friendly web
  dashboard on top of Docker so you don't have to live in the terminal.

You do **not** need to install .NET, Node, or PostgreSQL anywhere — those only exist
inside the Docker images, which build automatically.

---

## Part B — (Recommended) Test it on your own laptop first, free

This proves the whole product works before you spend a cent or touch a server. It
runs in "fixture mode" with fake-but-realistic data and no credentials.

1. **Install Docker Desktop** on your laptop:
   - Go to **https://www.docker.com/products/docker-desktop/** → click **Download**
     for your OS (Mac Apple-Silicon vs Intel matters — pick the right one; Windows is
     one download).
   - Run the installer, accept defaults, and **launch Docker Desktop**. Wait until the
     whale icon in your menu bar / system tray stops animating (that means Docker is
     running).
2. **Get the code onto your laptop.** In your terminal, run (replace the URL with your
   repo's clone URL from GitHub — the green **Code** button → HTTPS):
   ```bash
   git clone https://github.com/anthonydagostino/ContractCanary.git
   cd ContractCanary
   ```
   (If you don't have `git`, install it from https://git-scm.com/downloads, or on the
   GitHub page use **Code → Download ZIP** and unzip it, then `cd` into the folder.)
3. **Start everything:**
   ```bash
   docker compose up --build
   ```
   The first run downloads base images and compiles the app — give it **3–8 minutes**.
   It's ready when the logs stop scrolling and you see lines mentioning `api`,
   `worker`, and `web` running. Leave this terminal open (it's running the app).
4. **Open the app:** in your browser go to **http://localhost:8088**.
5. **Sign in with the built-in demo account:**
   - Email: `demo@oppsignal.dev`  Password: `DemoPassword123!`
   - You'll see ~600 opportunities and 3 match profiles with matches.
   - For the admin dashboard: `admin@oppsignal.dev` / `AdminPassword123!`.
6. **See the emails it "sent"** (in local test mode emails are written to a file, not
   actually emailed). Open a **second** terminal in the same folder:
   ```bash
   docker compose exec api ls /app/maildrop
   ```
   Copy any `.html` filename it lists, then open it in your browser to preview a real
   rendered digest/verification email.
7. **Stop it** when done: go back to the first terminal and press **Ctrl+C**, then run
   `docker compose down`.

✅ **You'll know it worked when** you can log in at localhost:8088 as the demo user and
click around the dashboard. If so, the product is healthy and the rest of this guide is
just wiring in your real accounts.

---

## Part 0 — Confirm your employer's outside-activity / ethics rules (DO THIS FIRST)

Only you can do this, and it's the one thing engineering can't fix later. Before you
launch a paid side business — especially one selling into the federal space — check
your employer's **outside-activity / conflict-of-interest / ethics policy** and file any
required disclosure or get approval. If you're a federal employee or contractor
yourself, this matters even more. Get this cleared before you take money.

✅ **You'll know it worked when** you have written confirmation (or a clear policy) that
running this is allowed.

---

## Part 1 — Pick the real name and set up your domain (Cloudflare)

### 1a. Choose the product name and buy a domain

1. Decide the real product name (right now it's codenamed **OppSignal**).
2. Buy a domain. Easiest: **https://dash.cloudflare.com** → sign up / log in →
   **top nav "Domain Registration" → "Register Domains"** → search your name → buy
   (~$10/yr). If you already bought a domain elsewhere, instead click **"Add a site"**
   on the Cloudflare home, type your domain, choose the **Free** plan, and follow
   Cloudflare's instructions to point your registrar's **nameservers** at the two
   Cloudflare gives you (this hands DNS control to Cloudflare; it can take a few hours).

You'll use a subdomain like **`app.yourdomain.com`** for the product.

### 1b. Rename the product in the code (2 spots + docs)

Only these files reference the product name. Edit them (in your local clone) and push:

1. `frontend/src/config/branding.ts` — change `productName`, `tagline`,
   `supportEmail`, and the marketing copy.
2. `frontend/index.html` — the `<title>` line (optional but nice).
3. `docs/TERMS_OF_SERVICE.md` and `docs/PRIVACY_POLICY.md` — replace the
   `{{PLACEHOLDER}}` fields (company name, etc.).

The backend product name/support email/domain come from environment variables (Part 8),
so you don't edit backend code — just the two frontend spots above.

> Don't worry about the DNS **A record** yet — you'll add it in Part 10 once your server
> exists and has an IP address.

✅ **You'll know it worked when** you own the domain and Cloudflare shows it as **Active**
(green) on your Cloudflare dashboard.

---

## Part 2 — Get a server running Docker + Portainer

**If you already have a Portainer host**, skip to "2c. Confirm it's ready."

### 2a. Create a cloud server (DigitalOcean example)

1. Go to **https://www.digitalocean.com** → sign up (you'll add a card; small usage is
   cheap).
2. Top-right **"Create" → "Droplets"**.
3. Choose:
   - **Region:** closest to you / your customers (e.g. New York).
   - **Image:** **Ubuntu 24.04 (LTS) x64**.
   - **Droplet type:** Basic. **CPU:** Regular. **Size:** the **$12/mo (2 GB RAM)**
     option (the $6 one can be tight while building images; you can resize later).
   - **Authentication:** choose **Password** (simplest) and set a strong root password
     you'll remember, **or** SSH key if you know what that is.
   - **Hostname:** e.g. `oppsignal-prod`.
4. Click **Create Droplet**. After ~1 minute you'll get an **IP address** (e.g.
   `203.0.113.10`). **Write this IP down** — you'll need it several times.

### 2b. Install Docker + Portainer on the server

1. Connect to the server from your laptop terminal (use your droplet IP):
   ```bash
   ssh root@YOUR_SERVER_IP
   ```
   Type `yes` if asked about authenticity, then the password from step 3.
2. Install Docker (paste this whole block, press Enter):
   ```bash
   curl -fsSL https://get.docker.com | sh
   ```
   Wait for it to finish (~1 min).
3. Install Portainer (the web dashboard for Docker):
   ```bash
   docker volume create portainer_data
   docker run -d -p 9443:9443 --name portainer --restart=always \
     -v /var/run/docker.sock:/var/run/docker.sock \
     -v portainer_data:/data portainer/portainer-ce:latest
   ```
4. In your browser go to **`https://YOUR_SERVER_IP:9443`**. Your browser will warn
   about the certificate (it's self-signed) — click **Advanced → Proceed**. Create the
   Portainer **admin username and password** (remember these). Choose **"Get Started" →
   the "local" environment**.

### 2c. Confirm it's ready

You now have a Docker host with a Portainer UI. Keep the Portainer URL and login handy —
you'll deploy the app there in Part 9.

✅ **You'll know it worked when** you can log into Portainer at
`https://YOUR_SERVER_IP:9443` and see the **local** environment with a Docker version.

---

## Part 3 — Get your free SAM.gov API key

This is the free government data feed. Until you do this, the app happily runs on
fixture (fake) data.

1. Go to **https://sam.gov** → **Sign In** (top right) → create an account
   (login.gov is used for identity; follow the prompts). This can take a few minutes.
2. Once signed in, click your **name/profile (top right) → "Account Details"**.
3. Scroll to the **"API Key"** section → click **"Request API Key"** (or "Generate").
   Copy the key it shows you — a long string. **You may only see it once**, so paste it
   into your scratch note immediately.

➜ **PUT IT IN:**
```
INGEST_SOURCE=Sam
SAM_API_KEY=<the long key you copied>
```

> Your keyed account allows **1,000 requests/day**; the app uses only a handful. If you
> ever hit a limit, the ingest just retries next cycle.

✅ **You'll know it worked when** you have the API key saved in your notes. (You'll
verify it truly works in Part 10 after deploy.)

---

## Part 4 — Set up Stripe (payments)

Do all of this in **Test mode** first (no real charges). There's a **Test mode** toggle
near the top of the Stripe dashboard — make sure it's **ON** while you set this up.

1. Go to **https://dashboard.stripe.com** → sign up / log in. Confirm **Test mode** is on.

2. **Create the two plans.** Left sidebar **"Product catalog"** (older UIs: "Products")
   → **"+ Add product"**:
   - **Product 1:** Name `Starter`. Under **Pricing**: choose **Recurring**, price
     **$29.00**, billing period **Monthly**. Click **Save product**.
   - Open the product you just made; in its **Pricing** section you'll see a price with
     an ID starting **`price_`** — copy it. That's your Starter price ID.
   - **Product 2:** repeat with Name `Pro`, price **$79.00 / Monthly**. Copy its
     **`price_...`** ID too.

3. **Get your API keys.** Top-right **"Developers"** → **"API keys"**:
   - Copy the **Publishable key** (`pk_test_...`).
   - Click **"Reveal test key"** on the **Secret key** (`sk_test_...`) and copy it.

4. **Create the webhook** (this is how Stripe tells your app when someone subscribes).
   **Developers → "Webhooks" → "+ Add endpoint"**:
   - **Endpoint URL:** `https://app.yourdomain.com/api/billing/webhook`
     (use your real subdomain from Part 1).
   - **"Select events"** → add these five:
     `checkout.session.completed`, `customer.subscription.created`,
     `customer.subscription.updated`, `customer.subscription.deleted`,
     `invoice.payment_failed`.
   - Click **"Add endpoint"**.
   - On the new endpoint's page, find **"Signing secret" → "Reveal"** → copy it
     (`whsec_...`).

➜ **PUT IT IN:**
```
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_STARTER_PRICE_ID=price_...   (from the $29 product)
STRIPE_PRO_PRICE_ID=price_...       (from the $79 product)
STRIPE_SUCCESS_URL=https://app.yourdomain.com/app/settings?checkout=success
STRIPE_CANCEL_URL=https://app.yourdomain.com/app/settings?checkout=cancel
STRIPE_PORTAL_RETURN_URL=https://app.yourdomain.com/app/settings
```

> **The 14-day free trial (no card) is built into the app** — you don't configure it in
> Stripe.
>
> **When you're ready to take real money:** flip Stripe to **Live mode**, redo steps 2–4
> in Live (new products, keys, and webhook), and swap the five `..._test_...` / test
> values above for their **live** equivalents. Nothing in the code changes.

✅ **You'll know it worked when** you have all five Stripe values in your notes.

---

## Part 5 — Set up Postmark (email) + add DNS records

Postmark sends your verification emails and daily digests. (Prefer Amazon SES? See the
note at the end of this part.)

1. Go to **https://postmarkapp.com** → sign up. Postmark may ask a couple of questions
   about your use case; answer honestly ("transactional email for a SaaS app").
2. It creates a **Server** for you (think of it as a mailbox project). Open
   **Servers → your server → "API Tokens"** and copy the **Server API Token**.
3. **Verify your sending domain** (this is what makes email land in inboxes, not spam):
   - Left nav **"Sender Signatures"** (or **"Domains"**) → **"Add Domain"** → type
     `yourdomain.com` → **Verify Domain**.
   - Postmark shows you **DNS records** to add — typically a **DKIM** record (a `TXT` or
     `CNAME`) and a **Return-Path** (a `CNAME`). Keep this Postmark tab open.
   - In a new tab open **Cloudflare → your domain → "DNS" → "Records" → "Add record"**
     and create each record exactly as Postmark shows (Type, Name, and Value/Target).
     Set **Proxy status to "DNS only" (grey cloud)** for these. Save each.
   - Back in Postmark, click **"Verify"** (it may take a few minutes to some hours for
     DNS to propagate; you can continue with other parts meanwhile).
4. Decide your "from" address, e.g. `digests@yourdomain.com`.

➜ **PUT IT IN:**
```
EMAIL_PROVIDER=Postmark
POSTMARK_SERVER_TOKEN=<the Server API Token>
EMAIL_FROM_EMAIL=digests@yourdomain.com
EMAIL_FROM_NAME=<Your product name>
```

> **Note:** Postmark accounts start in a "pending approval" state and can only email
> *your own* address until approved — request approval from their dashboard before real
> launch. Until Postmark is set up, leave `EMAIL_PROVIDER=Dev` and emails are written to
> a file on the server instead of sent.
>
> **SES alternative:** if you'd rather use Amazon SES, verify your domain in SES and ask
> a developer to add an `SesEmailSender` (it's ~80 lines, mirroring the existing Postmark
> one — the `IEmailSender` seam is already there). Everything else stays the same.

✅ **You'll know it worked when** Postmark shows your domain as **Verified** (green).

---

## Part 6 — Create the production database (AWS RDS PostgreSQL)

This is the real database your app uses in production.

1. Go to **https://console.aws.amazon.com/rds** → sign in (create an AWS account if
   needed; you'll add a card — RDS has a 12-month free tier).
2. Top-right, pick a **Region** near your server (remember which one).
3. Click **"Create database"** → **"Standard create"**.
4. **Engine:** **PostgreSQL**. **Version:** pick a **16.x**.
5. **Templates:** choose **"Free tier"** (or "Dev/Test" if free tier isn't offered).
6. **Settings:**
   - **DB instance identifier:** `oppsignal-db`.
   - **Master username:** `oppsignal`.
   - **Master password:** click "Self managed", set a **strong password**, and save it
     to your notes (call it `DB_PASSWORD`).
7. **Instance configuration:** the smallest burstable class (e.g. **db.t4g.micro**).
8. **Storage:** defaults are fine (20 GB).
9. **Connectivity:**
   - **Public access:** **Yes** (simplest, so your server can reach it — we'll lock it
     down to your server's IP in a moment).
   - Leave VPC defaults.
10. **Additional configuration** (expand it): set **Initial database name** to
    `oppsignal`.
11. Click **"Create database"**. It takes ~5–10 minutes to become **Available**.
12. **Open the firewall to your server:**
    - Click your new DB → the **"Connectivity & security"** tab → click the **VPC
      security group** link.
    - **Inbound rules → Edit inbound rules → Add rule:** Type **PostgreSQL** (port 5432),
      Source = **Custom** and enter **`YOUR_SERVER_IP/32`** (your DigitalOcean IP from
      Part 2). Save. *(This lets only your server connect.)*
13. **Get the connection string:** on the DB page copy the **Endpoint** (looks like
    `oppsignal-db.abc123.us-east-1.rds.amazonaws.com`).

➜ **PUT IT IN** (fill in the endpoint and your DB password):
```
CONNECTIONSTRINGS_POSTGRES=Host=YOUR_ENDPOINT;Port=5432;Database=oppsignal;Username=oppsignal;Password=YOUR_DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
SEED_DEMO=false
```

> The app **creates all its tables and reference data automatically** on first start —
> you don't run any database scripts.

✅ **You'll know it worked when** the RDS instance shows **Available** and its security
group has an inbound PostgreSQL rule allowing your server's IP.

---

## Part 7 — Generate the app's secret key

Your app signs login tokens with a secret. Generate a strong random one.

- **Mac / Linux terminal:**
  ```bash
  openssl rand -base64 48
  ```
- **Windows PowerShell:**
  ```powershell
  [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }))
  ```

Copy the output.

➜ **PUT IT IN:**
```
JWT_SIGNING_KEY=<the random string>
```

✅ **You'll know it worked when** you have a long random string saved.

---

## Part 8 — Assemble your `.env` file

Now collect everything into one file the deploy will read.

1. In your local clone, find the file **`.env.example`**. **Make a copy named `.env`**
   (`cp .env.example .env` on Mac/Linux; on Windows just copy/paste and rename in the
   file explorer — enable "show file extensions" so it isn't `.env.txt`).
2. Open `.env` in your text editor and fill in every value from your scratch notes.
   Also set these top ones:
   ```
   SITE_ADDRESS=app.yourdomain.com
   BRANDING_WEB_BASE_URL=https://app.yourdomain.com
   BRANDING_PRODUCT_NAME=<Your product name>
   BRANDING_SUPPORT_EMAIL=support@yourdomain.com
   BRANDING_COMPANY_LEGAL_NAME=<Your LLC/company legal name>
   ```
3. Double-check: no line still says `CHANGE_ME` or is blank (except optional ones).

**Quick map of which part fills which lines:**

| From part | Variables |
| --------- | --------- |
| 1 | `SITE_ADDRESS`, `BRANDING_*`, `BRANDING_WEB_BASE_URL` |
| 3 | `INGEST_SOURCE=Sam`, `SAM_API_KEY` |
| 4 | `STRIPE_SECRET_KEY`, `STRIPE_PUBLISHABLE_KEY`, `STRIPE_WEBHOOK_SECRET`, `STRIPE_STARTER_PRICE_ID`, `STRIPE_PRO_PRICE_ID`, the 3 Stripe URLs |
| 5 | `EMAIL_PROVIDER=Postmark`, `POSTMARK_SERVER_TOKEN`, `EMAIL_FROM_EMAIL`, `EMAIL_FROM_NAME` |
| 6 | `CONNECTIONSTRINGS_POSTGRES`, `SEED_DEMO=false` |
| 7 | `JWT_SIGNING_KEY` |

> ⚠️ **Never commit `.env` to GitHub** — it's already in `.gitignore` so git ignores it.
> It holds all your secrets.

✅ **You'll know it worked when** every non-optional line in `.env` has a real value.

---

## Part 9 — Deploy the app on your server (Portainer)

1. Log into **Portainer** (`https://YOUR_SERVER_IP:9443`).
2. Left menu **"Stacks" → "+ Add stack"**.
3. **Name:** `oppsignal`.
4. **Build method:** choose **"Repository"** (recommended) *or* **"Web editor"**:
   - **Repository (recommended):** set **Repository URL** to your GitHub repo, **Reference**
     to your branch (e.g. `refs/heads/main` — merge your work to main first, or use the
     build branch), and **Compose path** to `docker-compose.prod.yml`. If your repo is
     private, add your GitHub credentials/token under **Authentication**.
   - **Web editor:** open `docker-compose.prod.yml` from your clone, copy its entire
     contents, and paste them into the editor box.
5. **Environment variables:** scroll to the **"Environment variables"** section →
   click **"Advanced mode"** → paste the **entire contents of your `.env` file** into the
   box (it accepts the `KEY=value` lines directly).
6. Click **"Deploy the stack"**. The first build takes **5–10 minutes** (it compiles the
   backend and frontend). Watch **Stacks → oppsignal → the containers** turn green
   (`api`, `worker`, `web` should be **running**).
7. If a container is red/restarting, click it → **"Logs"** to see why (see Troubleshooting).

✅ **You'll know it worked when** all three containers (`api`, `worker`, `web`) show
**running** in Portainer.

---

## Part 10 — Point your domain at the server and verify

1. **Add the DNS record.** In **Cloudflare → your domain → "DNS" → "Records" →
   "Add record":**
   - **Type:** `A`
   - **Name:** `app`  (this makes `app.yourdomain.com`)
   - **IPv4 address:** `YOUR_SERVER_IP`
   - **Proxy status:** **DNS only (grey cloud)** ← important, so the app can get its own
     HTTPS certificate. (Orange-cloud proxying can block the certificate step; grey is
     the simple, correct choice.)
   - **Save.**
2. **Open the firewall** on the server for web traffic. In your SSH session
   (`ssh root@YOUR_SERVER_IP`):
   ```bash
   ufw allow 80 && ufw allow 443 && ufw allow 22 && ufw allow 9443 && ufw --force enable
   ```
   (DigitalOcean also has a "Firewalls" networking feature if you prefer the UI — allow
   TCP 80, 443, 22, 9443.)
3. **Wait for DNS + certificate.** Give it a few minutes. The app's built-in Caddy proxy
   automatically requests a free Let's Encrypt certificate the first time someone hits
   the domain over HTTPS.
4. **Open `https://app.yourdomain.com`.** You should see your landing page with a valid
   padlock (secure). If you get a certificate warning at first, wait 1–2 minutes and
   refresh — Caddy is still issuing the cert.
5. **Smoke-test the real thing:**
   - Click **Start free trial**, register with **your own email**.
   - Check your inbox for the **verification email** (from Postmark). Click the link →
     it should confirm your email. *(No email? See Troubleshooting.)*
   - Sign in. Create a **match profile** with a NAICS code you care about.
   - In Stripe (**Test mode**), your webhook should already be receiving events; do a
     test **Upgrade to Pro** from the app's **Settings** — Stripe Checkout opens; use
     Stripe's test card **4242 4242 4242 4242**, any future expiry, any CVC. After
     paying you should return to the app and see your plan reflected.

✅ **You'll know it worked when** you can register a real account over HTTPS, receive the
verification email, and complete a test subscription.

---

## Part 11 — Make yourself an admin

Register your account normally (Part 10), then flip the admin flag once. From your SSH
session on the server:

```bash
# find the running api container name (usually oppsignal-api-1)
docker ps --format '{{.Names}}' | grep api
```

The admin flag lives in the database. The simplest way is a one-off SQL command using a
temporary Postgres client container pointed at your RDS (replace the connection pieces):

```bash
docker run --rm postgres:16-alpine psql \
  "postgresql://oppsignal:YOUR_DB_PASSWORD@YOUR_ENDPOINT:5432/oppsignal?sslmode=require" \
  -c "UPDATE \"AspNetUsers\" SET \"IsAdmin\" = true WHERE \"Email\" = 'you@yourdomain.com';"
```

Log out and back in; you'll now see the **Admin** item in the sidebar (users,
subscribers, notices ingested, emails sent, last ingest status).

✅ **You'll know it worked when** the **Admin** page loads for your account.

---

## Part 12 — Legal review before you charge real money

The included **Terms of Service** and **Privacy Policy** (in `docs/`, and shown at
`/terms` and `/privacy` in the app) are **solid drafts, not legal advice**. Before you
switch Stripe to Live and take real payments, have a lawyer review and adapt them to
your entity, state, refund policy, and data practices. The in-app pages show a "pending
legal review" banner — remove it (in `frontend/src/pages/Legal.tsx`) once reviewed.

✅ **You'll know it worked when** counsel has signed off and you've removed the banner.

---

## Troubleshooting

- **A container is red / keeps restarting (Portainer → container → Logs):**
  - `Npgsql ... could not connect` → your `CONNECTIONSTRINGS_POSTGRES` is wrong, or the
    RDS security group doesn't allow your server's IP (Part 6, step 12). Confirm the
    endpoint, password, and that RDS status is **Available**.
  - `Sam:ApiKey is not configured` → you set `INGEST_SOURCE=Sam` but left `SAM_API_KEY`
    blank. Either add the key or set `INGEST_SOURCE=Fixture` temporarily.
- **HTTPS certificate won't issue / browser warning persists:**
  - The Cloudflare record for `app` must be **grey cloud (DNS only)**, and ports **80 and
    443** must be open to the internet (Part 10). Caddy needs port 80 reachable to prove
    domain ownership. Wait 2–3 minutes after fixing and refresh.
- **No verification email arrives:**
  - Postmark domain not **Verified** yet (Part 5), or your Postmark account is still in
    approval limbo (new accounts can only email your own address until approved). Check
    **Postmark → Activity** to see whether the message was sent/blocked.
- **Stripe subscription doesn't reflect in the app:**
  - The webhook URL must be exactly `https://app.yourdomain.com/api/billing/webhook` and
    the `STRIPE_WEBHOOK_SECRET` must match that endpoint's signing secret. Check
    **Stripe → Developers → Webhooks → your endpoint → recent deliveries** for errors.
- **Changed a value in `.env`?** In Portainer: **Stacks → oppsignal → Editor**, update
  the environment variables, and click **"Update the stack"** (with "re-pull and
  redeploy" if you also changed code). No rebuild of your machine needed.

---

## Appendix — the full environment reference

Every variable, with a one-line description, is documented in
[`.env.example`](.env.example). That file is the source of truth; this guide just tells
you where each value comes from.

**Going live checklist (final):** ethics cleared (Part 0) · domain live (1) · server up
(2) · SAM key (3) · Stripe **switched to Live** + values swapped (4) · Postmark domain
verified + account approved (5) · RDS available & locked to server IP (6) · JWT secret
set (7) · `.env` complete (8) · stack deployed (9) · DNS + TLS verified & test signup
works (10) · you're an admin (11) · legal reviewed & banner removed (12).
