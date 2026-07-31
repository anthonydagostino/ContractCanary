import { describe, expect, it } from 'vitest'
import { buildNoticeParams } from './noticeParams'

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
