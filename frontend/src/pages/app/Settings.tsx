import { useEffect, useState } from 'react'
import { PageHeader } from '../../components/PageHeader'
import { Alert, Badge, Spinner } from '../../components/ui'
import { useChangePassword, useCheckout, usePortal, useUpdateAccount } from '../../hooks/queries'
import { useAuth } from '../../lib/auth'
import { apiError } from '../../lib/api'
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
  const [billingError, setBillingError] = useState('')

  useEffect(() => {
    if (me) setForm({ fullName: me.fullName ?? '', companyName: me.companyName ?? '', timeZoneId: me.timeZoneId })
  }, [me])

  if (!me) return null

  async function saveAccount(e: React.FormEvent) {
    e.preventDefault()
    setSavedMsg('')
    await update.mutateAsync(form)
    await refreshMe()
    setSavedMsg('Saved.')
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

        {billingError && <Alert>{billingError}</Alert>}

        <div className="flex flex-wrap gap-3">
          {me.plan !== 'Starter' && (
            <button className="btn-secondary" onClick={() => startCheckout('Starter')} disabled={checkout.isPending}>Choose Starter — $29/mo</button>
          )}
          {me.plan !== 'Pro' && (
            <button className="btn-primary" onClick={() => startCheckout('Pro')} disabled={checkout.isPending}>
              {checkout.isPending ? <Spinner className="h-4 w-4" /> : 'Upgrade to Pro — $79/mo'}
            </button>
          )}
          <button className="btn-ghost" onClick={openPortal} disabled={portal.isPending}>Manage billing</button>
        </div>
        <p className="text-xs text-slate-400">Billing is handled securely by Stripe. Manage or cancel anytime from the portal.</p>
      </div>
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
      <p className="text-xs text-slate-400">At least 10 characters, with an uppercase letter, a lowercase letter, and a digit.</p>
      <button type="submit" className="btn-primary" disabled={change.isPending}>
        {change.isPending ? <Spinner className="h-4 w-4" /> : 'Change password'}
      </button>
    </form>
  )
}
