import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { AuthLayout } from '../../components/AuthLayout'
import { Alert, Spinner } from '../../components/ui'
import { api, apiError } from '../../lib/api'

export function ResetPassword() {
  const [params] = useSearchParams()
  const email = params.get('email') ?? ''
  const token = params.get('token') ?? ''
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState('')

  const invalid = !email || !token

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true); setError('')
    try {
      await api.post('/auth/reset-password', { email, token, newPassword: password })
      setDone(true)
    } catch (err) {
      setError(apiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthLayout title="Choose a new password" footer={<Link to="/login" className="font-medium text-brand-700 hover:underline">Back to sign in</Link>}>
      {invalid ? (
        <Alert>This reset link is invalid or incomplete. Request a new one from the sign-in page.</Alert>
      ) : done ? (
        <div className="space-y-4">
          <Alert tone="green">Your password has been reset.</Alert>
          <Link to="/login" className="btn-primary w-full">Sign in</Link>
        </div>
      ) : (
        <form onSubmit={submit} className="space-y-4">
          {error && <Alert>{error}</Alert>}
          <div>
            <label className="label" htmlFor="password">New password</label>
            <input id="password" type="password" autoComplete="new-password" required className="input" value={password} onChange={(e) => setPassword(e.target.value)} />
            <p className="mt-1 text-xs text-slate-400">At least 10 characters, with upper, lower, and a number.</p>
          </div>
          <button type="submit" className="btn-primary w-full" disabled={busy}>
            {busy ? <Spinner className="h-4 w-4" /> : 'Reset password'}
          </button>
        </form>
      )}
    </AuthLayout>
  )
}
