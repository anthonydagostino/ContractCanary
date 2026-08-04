import { useState } from 'react'
import { Link } from 'react-router-dom'
import { AuthLayout } from '../../components/AuthLayout'
import { Alert, Spinner } from '../../components/ui'
import { api, apiError } from '../../lib/api'
import { branding } from '../../config/branding'

const guessTz = () => {
  try { return Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/New_York' } catch { return 'America/New_York' }
}

export function Register() {
  const [form, setForm] = useState({ fullName: '', companyName: '', email: '', password: '' })
  const [accepted, setAccepted] = useState(false)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [done, setDone] = useState(false)

  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) => setForm({ ...form, [k]: e.target.value })

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setError(''); setBusy(true)
    try {
      await api.post('/auth/register', { ...form, timeZoneId: guessTz(), acceptedTerms: accepted })
      setDone(true)
    } catch (err) {
      setError(apiError(err, 'Could not create your account.'))
    } finally {
      setBusy(false)
    }
  }

  if (done) {
    return (
      <AuthLayout title="Check your email" subtitle={`We sent a confirmation link to ${form.email}.`}>
        <Alert tone="green">
          Click the link in that email to activate your account, then sign in. Didn’t get it? Check spam or{' '}
          <Link to="/login" className="font-medium underline">try signing in</Link> to resend.
        </Alert>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout
      title={`Start your free trial`}
      subtitle={`14 days of ${branding.productName}. No credit card required.`}
      footer={<>Already have an account? <Link to="/login" className="font-medium text-brand-700 hover:underline">Sign in</Link></>}
    >
      <form onSubmit={submit} className="space-y-4">
        {error && <Alert>{error}</Alert>}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label" htmlFor="fullName">Full name</label>
            <input id="fullName" className="input" value={form.fullName} onChange={set('fullName')} />
          </div>
          <div>
            <label className="label" htmlFor="companyName">Company</label>
            <input id="companyName" className="input" value={form.companyName} onChange={set('companyName')} />
          </div>
        </div>
        <div>
          <label className="label" htmlFor="email">Work email</label>
          <input id="email" type="email" autoComplete="email" required className="input" value={form.email} onChange={set('email')} />
        </div>
        <div>
          <label className="label" htmlFor="password">Password</label>
          <input id="password" type="password" autoComplete="new-password" required className="input" value={form.password} onChange={set('password')} />
          <p className="mt-1 text-xs text-slate-500">At least 10 characters, with upper, lower, and a number.</p>
        </div>
        <label className="flex items-start gap-2.5 text-xs text-slate-600">
          <input type="checkbox" required checked={accepted} onChange={(e) => setAccepted(e.target.checked)}
            className="mt-0.5 h-4 w-4 flex-none rounded border-slate-300" />
          <span>
            I agree to the <Link to="/terms" className="font-medium text-brand-700 underline">Terms of Service</Link> and{' '}
            <Link to="/privacy" className="font-medium text-brand-700 underline">Privacy Policy</Link>.
          </span>
        </label>
        <button type="submit" className="btn-primary w-full" disabled={busy || !accepted}>
          {busy ? <Spinner className="h-4 w-4" /> : 'Create account'}
        </button>
      </form>
    </AuthLayout>
  )
}
