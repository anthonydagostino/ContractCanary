import { Link } from 'react-router-dom'
import { PageHeader } from '../../components/PageHeader'
import { NoticeTable } from '../../components/NoticeTable'
import { EmptyState, PageLoader, Pagination } from '../../components/ui'
import { useNotices } from '../../hooks/queries'
import { useState } from 'react'

export function Saved() {
  const [page, setPage] = useState(1)
  const { data, isLoading } = useNotices({ savedOnly: true, page, pageSize: 25, sort: 'posted', direction: 'desc' })

  return (
    <div>
      <PageHeader title="Saved opportunities" subtitle="Everything you've starred, in one place." />
      {isLoading ? (
        <PageLoader />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title="No saved opportunities yet"
          hint="Star opportunities from the dashboard to keep track of them here."
          action={<Link to="/app" className="btn-primary">Browse opportunities</Link>}
        />
      ) : (
        <div className="space-y-4">
          <NoticeTable items={data.items} />
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </div>
      )}
    </div>
  )
}
