import { useEffect, useState } from 'react'
import { PageHeader } from '../../components/PageHeader'
import { Alert, Badge, Spinner } from '../../components/ui'
import { useChangePassword, useCheckout, useDeleteAccount, useExportData, usePortal, useUpdateAccount } from '../../hooks/queries'
import { useAuth } from '../../lib/auth'
import { apiError } from '../../lib/api'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { branding } from '../../config/branding'
import { formatDate } from '../../lib/format'
import type { PlanTier } from '../../lib/types'

const TIMEZONES = [
  'America/New_York', 'America/Chicago', 'America/Denver', 'America/Phoenix',
  'America/Los_Angeles', 'America/Anchorage', 'Pacific/Honolulu', 'America/Puerto_Rico',
]

export function Settings() {
  const { me, refreshMe } = useAuth()
  const update = useUpdateAccount()
  const checkout = useCheckout()
  const portal = usePortal()

  const [form, setForm] = useState({ fullName: '', companyName: '', timeZoneId: 'America/New_York' })
  const [savedMsg, setSavedMsg] = useState('')
  const [saveError, setSaveError] = useState('')
  const [searchParams] = useSearchParams()
  const checkoutReturn = searchParams.get('checkout')
  const [billingError, setBillingError] = useState('')
  const [renewalConsent, setRenewalConsent] = useState(false)

  useEffect(() => {
    if (me) setForm({ fullName: me.fullName ?? '', companyName: me.companyName ?? '', timeZoneId: me.timeZoneId })
  }, [me])

  // Back from Stripe checkout: the plan is mirrored via webhook, which can land
  // a few seconds after the redirect. Refresh the session a few times so the
  // freshly-paid user sees their plan flip without a manual reload.
  useEffect(() => {
    if (checkoutReturn !== 'success') return
    const timers = [1500, 4000, 8000].map((ms) => setTimeout(() => { void refreshMe() }, ms))
    return () => timers.forEach(clearTimeout)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [checkoutReturn])

  if (!me) return null

  async function saveAccount(e: React.FormEvent) {
    e.preventDefault()
    setSavedMsg('')
    setSaveError('')
    try {
      await update.mutateAsync(form)
      await refreshMe()
      setSavedMsg('Saved.')
    } catch (err) {
      setSaveError(apiError(err, 'Could not save your profile. Please try again.'))
    }
  }

  async function startCheckout(plan: PlanTier) {
    setBillingError('')
    try {
      const { url } = await checkout.mutateAsync(plan as 'Starter' | 'Pro')
      window.location.href = url
    } catch (err) {
      setBillingError(apiError(err, 'Could not start checkout.'))
    }
  }

  async function openPortal() {
    setBillingError('')
    try {
      const { url } = await portal.mutateAsync()
      window.location.href = url
    } catch (err) {
      setBillingError(apiError(err, 'Could not open the billing portal.'))
    }
  }

  const zones = Array.from(new Set([me.timeZoneId, ...TIMEZONES]))

  return (
    <div className="mx-auto max-w-3xl">
      <PageHeader title="Account & billing" />

      {/* Account */}
      <form onSubmit={saveAccount} className="card mb-6 space-y-4 p-6">
        <p className="text-sm font-semibold text-slate-900">Profile</p>
        {savedMsg && <Alert tone="green">{savedMsg}</Alert>}
        {saveError && <Alert>{saveError}</Alert>}
        <div>
          <label className="label">Email</label>
          <input className="input bg-slate-50" value={me.email} disabled />
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="label">Full name</label>
            <input className="input" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
          </div>
          <div>
            <label className="label">Company</label>
            <input className="input" value={form.companyName} onChange={(e) => setForm({ ...form, companyName: e.target.value })} />
          </div>
        </div>
        <div>
          <label className="label">Timezone (for digest timing)</label>
          <select className="input" value={form.timeZoneId} onChange={(e) => setForm({ ...form, timeZoneId: e.target.value })}>
            {zones.map((z) => <option key={z} value={z}>{z}</option>)}
          </select>
        </div>
        <button type="submit" className="btn-primary" disabled={update.isPending}>
          {update.isPending ? <Spinner className="h-4 w-4" /> : 'Save profile'}
        </button>
      </form>

      {/* Password */}
      <ChangePasswordCard />

      {/* Billing */}
      <div className="card space-y-4 p-6">
        <div className="flex items-center justify-between">
          <p className="text-sm font-semibold text-slate-900">Subscription</p>
          <Badge tone={me.plan === 'Pro' ? 'indigo' : me.plan === 'Starter' ? 'blue' : 'gray'}>{me.plan}</Badge>
        </div>

        <div className="rounded-lg bg-slate-50 p-4 text-sm text-slate-600">
          <p>Status: <span className="font-medium text-slate-800">{me.subscriptionStatus}</span></p>
          {me.subscriptionStatus === 'Trialing' && me.trialEndsAt && <p>Trial ends {formatDate(me.trialEndsAt)}.</p>}
          {me.currentPeriodEndsAt && me.subscriptionStatus === 'Active' && (
            <p>{me.cancelAtPeriodEnd ? 'Cancels' : 'Renews'} {formatDate(me.currentPeriodEndsAt)}.</p>
          )}
        </div>

        {checkoutReturn === 'success' && <Alert tone="green">Checkout complete — your subscription updates here within a few seconds.</Alert>}
        {billingError && <Alert>{billingError}</Alert>}

        {me.plan !== 'Pro' && (
          <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs leading-5 text-slate-600">
            <p className="font-medium text-slate-700">Before you subscribe</p>
            <p className="mt-1">
              Paid plans are billed monthly in advance and <strong>renew automatically each month at the listed
              price until you cancel.</strong> You can cancel anytime under <strong>Manage billing</strong> (or email {' '}
              {branding.supportEmail}); cancelling stops future charges and you keep access through the paid period.
              Fees are non-refundable for partial months (see our <a href="/terms" className="underline">Terms</a>).
            </p>
            <label className="mt-2 flex items-start gap-2">
              <input type="checkbox" checked={renewalConsent} onChange={(e) => setRenewalConsent(e.target.checked)}
                className="mt-0.5 h-4 w-4 flex-none rounded border-slate-300" />
              <span>I understand my subscription renews automatically each month until I cancel.</span>
            </label>
          </div>
        )}

        <div className="flex flex-wrap gap-3">
          {me.plan !== 'Starter' && me.plan !== 'Pro' && (
            <button type="button" className="btn-secondary" onClick={() => startCheckout('Starter')} disabled={checkout.isPending || !renewalConsent}>
              Subscribe to Starter — $29/mo, auto-renews
            </button>
          )}
          {me.plan !== 'Pro' && (
            <button type="button" className="btn-primary" onClick={() => startCheckout('Pro')} disabled={checkout.isPending || !renewalConsent}>
              {checkout.isPending ? <Spinner className="h-4 w-4" /> : 'Subscribe to Pro — $79/mo, auto-renews'}
            </button>
          )}
          <button type="button" className="btn-ghost" onClick={openPortal} disabled={portal.isPending}>Manage billing</button>
        </div>
        <p className="text-xs text-slate-500">Billing is handled securely by Stripe. Cancel anytime from Manage billing.</p>
      </div>

      {/* Your data & account */}
      <DataAndAccountCard />
    </div>
  )
}

function DataAndAccountCard() {
  const exportData = useExportData()
  const deleteAccount = useDeleteAccount()
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  async function download() {
    setError('')
    try {
      const data = await exportData.mutateAsync()
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'contractcanary-my-data.json'
      a.click()
      URL.revokeObjectURL(url)
    } catch (err) {
      setError(apiError(err, 'Could not export your data.'))
    }
  }

  async function confirmDelete(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    try {
      await deleteAccount.mutateAsync(password)
      await logout()
      navigate('/')
    } catch (err) {
      setError(apiError(err, 'Could not delete your account.'))
    }
  }

  return (
    <div className="card mt-6 space-y-4 p-6">
      <p className="text-sm font-semibold text-slate-900">Your data</p>
      <div className="flex flex-wrap items-center gap-3">
        <button type="button" className="btn-secondary" onClick={download} disabled={exportData.isPending}>
          {exportData.isPending ? <Spinner className="h-4 w-4" /> : 'Download my data'}
        </button>
        <p className="text-xs text-slate-500">A JSON file of your account, match profiles, saved opportunities, and alerts.</p>
      </div>

      <hr className="border-slate-100" />

      <p className="text-sm font-semibold text-red-700">Delete account</p>
      <p className="text-xs text-slate-500">
        Permanently deletes your account and all your data, and cancels any active subscription. This cannot be undone.
      </p>
      {error && <Alert>{error}</Alert>}
      {!confirmOpen ? (
        <button type="button" className="btn-secondary border-red-200 text-red-700 hover:bg-red-50" onClick={() => setConfirmOpen(true)}>
          Delete my account…
        </button>
      ) : (
        <form onSubmit={confirmDelete} className="space-y-3 rounded-lg border border-red-200 bg-red-50/50 p-4">
          <label className="label">Confirm your password to delete everything</label>
          <input type="password" autoComplete="current-password" className="input" value={password}
            onChange={(e) => setPassword(e.target.value)} required />
          <div className="flex gap-2">
            <button type="submit" className="btn-primary bg-red-600 hover:bg-red-700" disabled={deleteAccount.isPending}>
              {deleteAccount.isPending ? <Spinner className="h-4 w-4" /> : 'Permanently delete account'}
            </button>
            <button type="button" className="btn-ghost" onClick={() => { setConfirmOpen(false); setPassword('') }}>Cancel</button>
          </div>
        </form>
      )}
    </div>
  )
}

function ChangePasswordCard() {
  const change = useChangePassword()
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setDone(false)
    if (next !== confirm) { setError('The new passwords do not match.'); return }
    try {
      await change.mutateAsync({ currentPassword: current, newPassword: next })
      setCurrent(''); setNext(''); setConfirm('')
      setDone(true)
    } catch (err) {
      setError(apiError(err, 'Could not change your password.'))
    }
  }

  return (
    <form onSubmit={submit} className="card mb-6 space-y-4 p-6">
      <p className="text-sm font-semibold text-slate-900">Password</p>
      {done && <Alert tone="green">Password changed. Other devices have been signed out.</Alert>}
      {error && <Alert>{error}</Alert>}
      <div>
        <label className="label">Current password</label>
        <input type="password" autoComplete="current-password" className="input" value={current}
          onChange={(e) => setCurrent(e.target.value)} required />
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label className="label">New password</label>
          <input type="password" autoComplete="new-password" className="input" value={next}
            onChange={(e) => setNext(e.target.value)} required minLength={10} />
        </div>
        <div>
          <label className="label">Confirm new password</label>
          <input type="password" autoComplete="new-password" className="input" value={confirm}
            onChange={(e) => setConfirm(e.target.value)} required minLength={10} />
        </div>
      </div>
      <p className="text-xs text-slate-500">At least 10 characters, with an uppercase letter, a lowercase letter, and a digit.</p>
      <button type="submit" className="btn-primary" disabled={change.isPending}>
        {change.isPending ? <Spinner className="h-4 w-4" /> : 'Change password'}
      </button>
    </form>
  )
}
