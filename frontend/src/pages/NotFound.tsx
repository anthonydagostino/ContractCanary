import { Link } from 'react-router-dom'
import { MarketingLayout } from '../components/MarketingLayout'

export function NotFound() {
  return (
    <MarketingLayout>
      <section className="mx-auto max-w-xl px-4 py-24 text-center">
        <p className="text-6xl font-extrabold text-brand-600">404</p>
        <h1 className="mt-4 text-2xl font-bold text-slate-900">Page not found</h1>
        <p className="mt-2 text-slate-600">The page you’re looking for doesn’t exist.</p>
        <Link to="/" className="btn-primary mt-6 inline-flex">Back home</Link>
      </section>
    </MarketingLayout>
  )
}
