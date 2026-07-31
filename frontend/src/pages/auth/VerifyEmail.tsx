import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { AuthLayout } from '../../components/AuthLayout'
import { Alert, PageLoader } from '../../components/ui'
import { api, apiError } from '../../lib/api'

export function VerifyEmail() {
  const [params] = useSearchParams()
  const [status, setStatus] = useState<'working' | 'ok' | 'error'>('working')
  const [message, setMessage] = useState('')
  const ran = useRef(false)

  useEffect(() => {
    if (ran.current) return
    ran.current = true
    const userId = params.get('userId')
    const token = params.get('token')
    if (!userId || !token) {
      setStatus('error'); setMessage('This confirmation link is missing information.')
      return
    }
    api.post('/auth/confirm-email', { userId, token })
      .then(() => setStatus('ok'))
      .catch((err) => { setStatus('error'); setMessage(apiError(err, 'This link is invalid or has expired.')) })
  }, [params])

  return (
    <AuthLayout title="Email verification">
      {status === 'working' && <PageLoader />}
      {status === 'ok' && (
        <div className="space-y-4">
          <Alert tone="green">Your email is confirmed. You can now sign in.</Alert>
          <Link to="/login" className="btn-primary w-full">Sign in</Link>
        </div>
      )}
      {status === 'error' && (
        <div className="space-y-4">
          <Alert>{message}</Alert>
          <Link to="/login" className="btn-secondary w-full">Back to sign in</Link>
        </div>
      )}
    </AuthLayout>
  )
}
