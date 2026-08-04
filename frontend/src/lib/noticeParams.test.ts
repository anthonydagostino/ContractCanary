import { describe, expect, it } from 'vitest'
import { buildNoticeParams, noticeQueryFromSearch } from './noticeParams'

describe('buildNoticeParams', () => {
  it('omits empty, false, and undefined values', () => {
    const p = buildNoticeParams({ page: 1, matchedOnly: false, search: '', savedOnly: undefined })
    expect(p.get('page')).toBe('1')
    expect(p.has('matchedOnly')).toBe(false)
    expect(p.has('search')).toBe(false)
    expect(p.has('savedOnly')).toBe(false)
  })

  it('serializes array filters as repeated keys', () => {
    const p = buildNoticeParams({ naics: ['541511', '541512'], noticeTypes: ['Solicitation', 'SourcesSought'] })
    expect(p.getAll('naics')).toEqual(['541511', '541512'])
    expect(p.getAll('noticeTypes')).toEqual(['Solicitation', 'SourcesSought'])
  })

  it('includes booleans only when true', () => {
    expect(buildNoticeParams({ matchedOnly: true }).get('matchedOnly')).toBe('true')
    expect(buildNoticeParams({ savedOnly: true }).get('savedOnly')).toBe('true')
  })

  it('passes through search, sort, and paging', () => {
    const p = buildNoticeParams({ search: 'hvac', sort: 'deadline', direction: 'asc', page: 3, pageSize: 25 })
    expect(p.get('search')).toBe('hvac')
    expect(p.get('sort')).toBe('deadline')
    expect(p.get('direction')).toBe('asc')
    expect(p.get('page')).toBe('3')
    expect(p.get('pageSize')).toBe('25')
  })
})

describe('noticeQueryFromSearch', () => {
  // Regression: Profiles → "View matches" links to /app?profileId=…, but the
  // dashboard ignored the URL entirely and showed the unfiltered list.
  it('reads profileId from the URL search string', () => {
    expect(noticeQueryFromSearch('?profileId=abc-123')).toEqual({ profileId: 'abc-123' })
  })

  it('returns no overrides for an empty or unrelated search string', () => {
    expect(noticeQueryFromSearch('')).toEqual({})
    expect(noticeQueryFromSearch('?utm_source=x')).toEqual({})
    expect(noticeQueryFromSearch('?profileId=')).toEqual({})
  })
})
