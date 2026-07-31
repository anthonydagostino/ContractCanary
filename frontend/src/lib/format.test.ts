import { describe, expect, it } from 'vitest'
import { deadlineLabel } from './format'

function inDays(days: number): string {
  return new Date(Date.now() + days * 86400_000).toISOString()
}

describe('deadlineLabel', () => {
  it('has no deadline when undefined', () => {
    expect(deadlineLabel(undefined)).toEqual({ text: 'No deadline', tone: 'gray' })
  })

  it('marks closed deadlines gray', () => {
    expect(deadlineLabel(inDays(-2)).tone).toBe('gray')
  })

  it('marks imminent deadlines red', () => {
    expect(deadlineLabel(inDays(2)).tone).toBe('red')
  })

  it('marks near deadlines amber', () => {
    expect(deadlineLabel(inDays(7)).tone).toBe('amber')
  })

  it('marks distant deadlines green', () => {
    expect(deadlineLabel(inDays(30)).tone).toBe('green')
  })
})
