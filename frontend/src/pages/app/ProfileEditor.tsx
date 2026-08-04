import { useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { PageHeader } from '../../components/PageHeader'
import { Alert, PageLoader, Spinner } from '../../components/ui'
import { ChipMultiSelect, TagInput, Typeahead } from '../../components/Typeahead'
import {
  useAgencySearch, useNaicsSearch, useNoticeTypes, useProfile, usePscSearch, useSaveProfile,
} from '../../hooks/queries'
import { useAuth } from '../../lib/auth'
import { apiError } from '../../lib/api'
import { SET_ASIDE_OPTIONS } from '../../lib/options'
import { US_STATES } from '../../lib/states'
import type { AgencyDto, NaicsDto, NoticeType, ProfileInput, PscDto, SetAsideCode } from '../../lib/types'

const empty: ProfileInput = {
  name: '', naics: [], psc: [], keywords: [], agencyPaths: [],
  setAsides: [], states: [], noticeTypes: [], isActive: true, isPriority: false,
}

export function ProfileEditor() {
  const { id } = useParams()
  const editing = !!id
  const navigate = useNavigate()
  const location = useLocation()
  const { me } = useAuth()
  const { data: existing, isLoading } = useProfile(id)
  const { data: noticeTypes } = useNoticeTypes()
  const save = useSaveProfile()

  // A new profile can arrive pre-filled from an opportunity ("Alert me about
  // opportunities like this"). Editing an existing profile ignores any prefill.
  const prefill = (location.state as { prefill?: Partial<ProfileInput> } | null)?.prefill
  const [form, setForm] = useState<ProfileInput>(() => (prefill ? { ...empty, ...prefill } : empty))
  const [error, setError] = useState('')

  // Hydrate the form once per profile. Re-running on every query delivery
  // would wipe in-progress edits when a background refetch lands.
  const [hydratedId, setHydratedId] = useState<string | null>(null)
  useEffect(() => {
    if (existing && existing.id !== hydratedId) {
      const { id: _id, matchCount: _m, createdAt: _c, updatedAt: _u, ...input } = existing
      setForm(input)
      setHydratedId(existing.id)
    }
  }, [existing, hydratedId])

  const patch = (p: Partial<ProfileInput>) => setForm((f) => ({ ...f, ...p }))

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    try {
      await save.mutateAsync({ id, input: form })
      navigate('/app/profiles')
    } catch (err) {
      setError(apiError(err, 'Could not save the profile.'))
    }
  }

  if (editing && isLoading) return <PageLoader />

  return (
    <div className="mx-auto max-w-3xl">
      <PageHeader title={editing ? 'Edit match profile' : 'New match profile'} subtitle="A notice matches when it satisfies (NAICS OR PSC) AND every other filter you set." />

      <form onSubmit={submit} className="space-y-6">
        {error && <Alert>{error}</Alert>}

        {prefill && !editing && (
          <div className="card border-canary-200 bg-canary-50/60 p-4 text-sm text-ink-800">
            Pre-filled from an opportunity. Adjust the filters below and save to start getting daily
            alerts for opportunities like it.
          </div>
        )}

        <div className="card p-6">
          <label className="label" htmlFor="name">Profile name</label>
          <input id="name" className="input" required maxLength={120} value={form.name}
            onChange={(e) => patch({ name: e.target.value })} placeholder="e.g. IT Services — Army" />
        </div>

        <div className="card space-y-5 p-6">
          <p className="text-sm font-semibold text-slate-900">What to match on</p>
          <Typeahead
            label="NAICS codes"
            placeholder="Search NAICS by code or title…"
            values={form.naics}
            onChange={(naics) => patch({ naics })}
            useSearch={useNaicsSearch}
            toOption={(i) => ({ value: (i as NaicsDto).code, label: (i as NaicsDto).title })}
            help="Match the notice's NAICS. This OR PSC must hit."
          />
          <Typeahead
            label="PSC / classification codes (optional)"
            placeholder="Search PSC by code or title…"
            values={form.psc}
            onChange={(psc) => patch({ psc })}
            useSearch={usePscSearch}
            toOption={(i) => ({ value: (i as PscDto).code, label: (i as PscDto).title })}
          />
          <TagInput
            label="Keywords (optional)"
            placeholder="Type a keyword and press Enter…"
            values={form.keywords}
            onChange={(keywords) => patch({ keywords })}
            help="Matched against the notice title and description (case-insensitive)."
          />
        </div>

        <div className="card space-y-5 p-6">
          <p className="text-sm font-semibold text-slate-900">Narrow it down (optional)</p>
          <Typeahead
            label="Agencies / departments"
            placeholder="Search agencies…"
            values={form.agencyPaths}
            onChange={(agencyPaths) => patch({ agencyPaths })}
            useSearch={useAgencySearch}
            toOption={(i) => ({ value: (i as AgencyDto).name, label: (i as AgencyDto).tier === 1 ? 'Department' : 'Sub-agency' })}
          />
          <ChipMultiSelect<SetAsideCode>
            label="Set-aside types"
            options={SET_ASIDE_OPTIONS}
            values={form.setAsides}
            onChange={(setAsides) => patch({ setAsides })}
          />
          {noticeTypes && (
            <ChipMultiSelect<NoticeType>
              label="Notice types"
              options={noticeTypes.map((t) => ({ value: t.value, label: t.label }))}
              values={form.noticeTypes}
              onChange={(noticeTypes) => patch({ noticeTypes })}
            />
          )}
          <ChipMultiSelect
            label="Place of performance (states)"
            options={US_STATES}
            values={form.states}
            onChange={(states) => patch({ states })}
          />
        </div>

        <div className="card space-y-4 p-6">
          <label className="flex items-center gap-3">
            <input type="checkbox" checked={form.isActive} onChange={(e) => patch({ isActive: e.target.checked })} />
            <span className="text-sm text-slate-700">Active — include this profile in matching and digests</span>
          </label>
          <label className="flex items-center gap-3">
            <input type="checkbox" checked={form.isPriority} disabled={!me?.limits.canPrioritize}
              onChange={(e) => patch({ isPriority: e.target.checked })} />
            <span className="text-sm text-slate-700">
              Priority ingest {me?.limits.canPrioritize ? '' : <span className="text-xs text-slate-500">(Pro only)</span>}
            </span>
          </label>
        </div>

        <div className="flex items-center gap-3">
          <button type="submit" className="btn-primary" disabled={save.isPending}>
            {save.isPending ? <Spinner className="h-4 w-4" /> : editing ? 'Save changes' : 'Create profile'}
          </button>
          <button type="button" className="btn-ghost" onClick={() => navigate('/app/profiles')}>Cancel</button>
        </div>
      </form>
    </div>
  )
}
