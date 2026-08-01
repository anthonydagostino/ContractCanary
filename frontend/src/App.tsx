import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { Landing } from './pages/Landing'
import { Pricing } from './pages/Pricing'
import { LegalPage } from './pages/Legal'
import { Login } from './pages/auth/Login'
import { Register } from './pages/auth/Register'
import { VerifyEmail } from './pages/auth/VerifyEmail'
import { ForgotPassword } from './pages/auth/ForgotPassword'
import { ResetPassword } from './pages/auth/ResetPassword'
import { Dashboard } from './pages/app/Dashboard'
import { OpportunityDetail } from './pages/app/OpportunityDetail'
import { Profiles } from './pages/app/Profiles'
import { ProfileEditor } from './pages/app/ProfileEditor'
import { Saved } from './pages/app/Saved'
import { Alerts } from './pages/app/Alerts'
import { Deadlines } from './pages/app/Deadlines'
import { Settings } from './pages/app/Settings'
import { Admin } from './pages/app/Admin'
import { NotFound } from './pages/NotFound'

export function App() {
  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/pricing" element={<Pricing />} />
      <Route path="/terms" element={<LegalPage doc="terms" />} />
      <Route path="/privacy" element={<LegalPage doc="privacy" />} />

      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/verify-email" element={<VerifyEmail />} />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/reset-password" element={<ResetPassword />} />

      <Route path="/app" element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>
        <Route index element={<Dashboard />} />
        <Route path="opportunities/:noticeId" element={<OpportunityDetail />} />
        <Route path="profiles" element={<Profiles />} />
        <Route path="profiles/new" element={<ProfileEditor />} />
        <Route path="profiles/:id" element={<ProfileEditor />} />
        <Route path="saved" element={<Saved />} />
        <Route path="alerts" element={<Alerts />} />
        <Route path="deadlines" element={<Deadlines />} />
        <Route path="settings" element={<Settings />} />
        <Route path="admin" element={<ProtectedRoute adminOnly><Admin /></ProtectedRoute>} />
      </Route>

      <Route path="*" element={<NotFound />} />
    </Routes>
  )
}
