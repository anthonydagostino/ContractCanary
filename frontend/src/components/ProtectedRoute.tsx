import { Navigate, useLocation } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from '../lib/auth'
import { PageLoader } from './ui'

export function ProtectedRoute({ children, adminOnly = false }: { children: ReactNode; adminOnly?: boolean }) {
  const { me, loading } = useAuth()
  const location = useLocation()

  if (loading) return <PageLoader />
  if (!me) return <Navigate to="/login" state={{ from: location.pathname }} replace />
  if (adminOnly && !me.isAdmin) return <Navigate to="/app" replace />
  return <>{children}</>
}
