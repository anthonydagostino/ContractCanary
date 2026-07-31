import { Link, useNavigate } from 'react-router-dom'
import { PageHeader } from '../../components/PageHeader'
import { Badge, EmptyState, PageLoader, Alert } from '../../components/ui'
import { useDeleteProfile, useProfiles } from '../../hooks/queries'
import { useAuth } from '../../lib/auth'

export function Profiles() {
  const { me } = useAuth()
  const { data: profiles, isLoading } = useProfiles()
  const del = useDeleteProfile()
  const navigate = useNavigate()

  const atLimit = !!me && !!profiles && profiles.length >= me.limits.maxProfiles

  return (
    <div>
      <PageHeader
        title="Match profiles"
        subtitle="Each profile is a saved search. We match new opportunities against every active profile."
        action={
          atLimit ? (
            <Link to="/app/settings" className="btn-secondary">Upgrade to add more</Link>
          ) : (
            <button className="btn-primary" onClick={() => navigate('/app/profiles/new')}>New profile</button>
          )
        }
      />

      {me && (
        <p className="mb-4 text-sm text-slate-500">
          Using {profiles?.length ?? 0} of {me.limits.maxProfiles} profiles on the <Badge tone="blue">{me.plan}</Badge> plan.
        </p>
      )}

      {isLoading ? (
        <PageLoader />
      ) : !profiles || profiles.length === 0 ? (
        <EmptyState
          title="No match profiles yet"
          hint="Create your first profile with your NAICS codes, keywords, and target agencies to start getting matches."
          action={<button className="btn-primary" onClick={() => navigate('/app/profiles/new')}>Create a profile</button>}
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          {profiles.map((p) => (
            <div key={p.id} className="card p-5">
              <div className="flex items-start justify-between">
                <div>
                  <div className="flex items-center gap-2">
                    <h3 className="font-semibold text-slate-900">{p.name}</h3>
                    {!p.isActive && <Badge tone="gray">Paused</Badge>}
                    {p.isPriority && <Badge tone="indigo">Priority</Badge>}
                  </div>
                  <p className="mt-1 text-sm text-slate-500">{p.matchCount.toLocaleString()} matches</p>
                </div>
              </div>
              <div className="mt-3 flex flex-wrap gap-1.5">
                {p.naics.slice(0, 4).map((c) => <Badge key={c} tone="blue">{c}</Badge>)}
                {p.naics.length > 4 && <Badge tone="gray">+{p.naics.length - 4} NAICS</Badge>}
                {p.keywords.slice(0, 3).map((k) => <Badge key={k} tone="gray">“{k}”</Badge>)}
              </div>
              <div className="mt-4 flex gap-2">
                <Link to={`/app/profiles/${p.id}`} className="btn-secondary">Edit</Link>
                <Link to={`/app?profileId=${p.id}`} className="btn-ghost">View matches</Link>
                <button
                  className="btn-ghost ml-auto text-red-600 hover:bg-red-50"
                  onClick={() => { if (confirm(`Delete “${p.name}”? This removes its matches.`)) del.mutate(p.id) }}
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {del.isError && <Alert>Could not delete that profile.</Alert>}
    </div>
  )
}
