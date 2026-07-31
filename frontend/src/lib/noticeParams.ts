import type { NoticeQueryParams } from './types'

/** Serialize notice query params into a URLSearchParams (repeated keys for arrays). Pure + testable. */
export function buildNoticeParams(q: NoticeQueryParams): URLSearchParams {
  const p = new URLSearchParams()
  const add = (k: string, v?: string | number | boolean) => {
    if (v !== undefined && v !== '' && v !== false) p.append(k, String(v))
  }
  add('search', q.search)
  add('agency', q.agency)
  add('matchedOnly', q.matchedOnly)
  add('savedOnly', q.savedOnly)
  add('profileId', q.profileId)
  add('sort', q.sort)
  add('direction', q.direction)
  add('page', q.page)
  add('pageSize', q.pageSize)
  q.naics?.forEach((v) => p.append('naics', v))
  q.psc?.forEach((v) => p.append('psc', v))
  q.states?.forEach((v) => p.append('states', v))
  q.setAsides?.forEach((v) => p.append('setAsides', v))
  q.noticeTypes?.forEach((v) => p.append('noticeTypes', v))
  return p
}

export const noticeQueryString = (q: NoticeQueryParams) => buildNoticeParams(q).toString()
