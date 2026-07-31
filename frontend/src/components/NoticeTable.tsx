import { useNavigate } from 'react-router-dom'
import type { NoticeListItem } from '../lib/types'
import { Badge, StarButton } from './ui'
import { deadlineLabel, formatDate } from '../lib/format'
import { useToggleSaved } from '../hooks/queries'

export function NoticeTable({ items }: { items: NoticeListItem[] }) {
  const navigate = useNavigate()
  const toggle = useToggleSaved()

  return (
    <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
      <table className="min-w-full divide-y divide-slate-200 text-sm">
        <thead className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
          <tr>
            <th className="w-10 px-3 py-3"></th>
            <th className="px-3 py-3">Opportunity</th>
            <th className="hidden px-3 py-3 md:table-cell">Type / Set-aside</th>
            <th className="hidden px-3 py-3 lg:table-cell">NAICS</th>
            <th className="hidden px-3 py-3 sm:table-cell">Posted</th>
            <th className="px-3 py-3">Deadline</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {items.map((n) => {
            const dl = deadlineLabel(n.responseDeadline)
            return (
              <tr
                key={n.noticeId}
                className="cursor-pointer hover:bg-slate-50"
                onClick={() => navigate(`/app/opportunities/${n.noticeId}`)}
              >
                <td className="px-3 py-3" onClick={(e) => e.stopPropagation()}>
                  <StarButton
                    saved={n.isSaved}
                    onClick={() => toggle.mutate({ noticeId: n.noticeId, save: !n.isSaved })}
                  />
                </td>
                <td className="px-3 py-3">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-slate-900 line-clamp-2">{n.title}</span>
                    {n.isMatched && <Badge tone="blue">Match</Badge>}
                  </div>
                  <div className="mt-0.5 text-xs text-slate-500">{n.agencyPath || n.departmentName}</div>
                </td>
                <td className="hidden px-3 py-3 md:table-cell">
                  <div className="flex flex-col gap-1">
                    <Badge tone="indigo">{n.typeLabel}</Badge>
                    {n.setAside !== 'None' && n.setAsideLabel && <Badge tone="green">{n.setAsideLabel}</Badge>}
                  </div>
                </td>
                <td className="hidden px-3 py-3 font-mono text-xs text-slate-600 lg:table-cell">{n.naicsCode || '—'}</td>
                <td className="hidden px-3 py-3 text-slate-500 sm:table-cell">{formatDate(n.postedDate)}</td>
                <td className="px-3 py-3">
                  <Badge tone={dl.tone}>{dl.text}</Badge>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
