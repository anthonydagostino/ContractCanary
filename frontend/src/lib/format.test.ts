// Force a US timezone so the UTC-midnight regression is actually exercised:
// in a UTC environment the buggy local rendering coincidentally looks right.
process.env.TZ = 'America/New_York'

import { describe, expect, it } from 'vitest'
import { daysUntil, deadlineLabel, deadlineTile, formatDate, formatMoney } from './format'

describe('formatDate', () => {
  it('renders date-only values (midnight UTC) as the calendar date they name', () => {
    // Regression: SAM.gov postedDate "2026-06-10" arrives as 2026-06-10T00:00:00Z;
    // local rendering in America/New_York showed "Jun 9, 2026".
    expect(formatDate('2026-06-10T00:00:00Z')).toBe('Jun 10, 2026')
    expect(formatDate('2026-06-10T00:00:00.000Z')).toBe('Jun 10, 2026')
    expect(formatDate('2026-06-10T00:00:00+00:00')).toBe('Jun 10, 2026')
    expect(formatDate('2026-06-10')).toBe('Jun 10, 2026')
  })

  it('renders true datetimes in the local zone', () => {
    // 3am UTC on Jun 10 is the evening of Jun 9 in New York — a real instant,
    // so local rendering is correct here.
    expect(formatDate('2026-06-10T03:00:00Z')).toBe('Jun 9, 2026')
  })

  it('handles missing and malformed input', () => {
    expect(formatDate(undefined)).toBe('—')
    expect(formatDate(null)).toBe('—')
    expect(formatDate('not-a-date')).toBe('—')
  })
})

describe('daysUntil (calendar days, injected clock)', () => {
  const now = new Date(2026, 5, 10, 15, 0) // Jun 10 2026, 3:00pm local

  it('is 0 for any time later today', () => {
    expect(daysUntil(new Date(2026, 5, 10, 17, 0).toISOString(), now)).toBe(0)
    expect(daysUntil(new Date(2026, 5, 10, 23, 59).toISOString(), now)).toBe(0)
  })

  it('is 1 for tomorrow morning even when less than 24h away', () => {
    expect(daysUntil(new Date(2026, 5, 11, 9, 0).toISOString(), now)).toBe(1)
  })

  it('is 0 for earlier today and -1 for yesterday', () => {
    expect(daysUntil(new Date(2026, 5, 10, 10, 0).toISOString(), now)).toBe(0)
    expect(daysUntil(new Date(2026, 5, 9, 23, 0).toISOString(), now)).toBe(-1)
  })

  it('is null when absent or malformed', () => {
    expect(daysUntil(undefined, now)).toBeNull()
    expect(daysUntil('garbage', now)).toBeNull()
  })
})

describe('deadlineLabel (injected clock)', () => {
  const now = new Date(2026, 5, 10, 15, 0)

  it('shows Due today for a deadline later today', () => {
    // Regression: ceil-based math called this "1 day left".
    expect(deadlineLabel(new Date(2026, 5, 10, 17, 0).toISOString(), now)).toEqual({ text: 'Due today', tone: 'red' })
  })

  it('shows Closed for a deadline that passed earlier today', () => {
    // Regression: ceil gave -0, and -0 === 0 showed "Due today" for a closed notice.
    expect(deadlineLabel(new Date(2026, 5, 10, 10, 0).toISOString(), now)).toEqual({ text: 'Closed', tone: 'gray' })
  })

  it('shows Closed for a deadline one minute ago', () => {
    expect(deadlineLabel(new Date(2026, 5, 10, 14, 59).toISOString(), now).text).toBe('Closed')
  })

  it('shows 1 day left for tomorrow, red within 3 days, amber within 10, green beyond', () => {
    expect(deadlineLabel(new Date(2026, 5, 11, 9, 0).toISOString(), now)).toEqual({ text: '1 day left', tone: 'red' })
    expect(deadlineLabel(new Date(2026, 5, 13, 9, 0).toISOString(), now).tone).toBe('red')
    expect(deadlineLabel(new Date(2026, 5, 17, 9, 0).toISOString(), now).tone).toBe('amber')
    expect(deadlineLabel(new Date(2026, 6, 20, 9, 0).toISOString(), now).tone).toBe('green')
  })

  it('has no deadline when undefined or malformed', () => {
    expect(deadlineLabel(undefined, now)).toEqual({ text: 'No deadline', tone: 'gray' })
    expect(deadlineLabel('garbage', now)).toEqual({ text: 'No deadline', tone: 'gray' })
  })
})

describe('formatMoney', () => {
  it('renders compact USD across magnitudes', () => {
    expect(formatMoney(2_100_000)).toBe('$2.1M')
    expect(formatMoney(5_000_000)).toBe('$5M')
    expect(formatMoney(1_200_000_000)).toBe('$1.2B')
    expect(formatMoney(450_000)).toBe('$450K')
    expect(formatMoney(980)).toBe('$980')
  })

  it('is null-safe', () => {
    expect(formatMoney(null)).toBe('—')
    expect(formatMoney(undefined)).toBe('—')
    expect(formatMoney(NaN)).toBe('—')
  })

  it('puts the sign outside the currency symbol for deobligations', () => {
    // Award data legitimately contains negative (deobligated) amounts.
    expect(formatMoney(-2_100_000)).toBe('-$2.1M')
    expect(formatMoney(-1500)).toBe('-$1.5K')
    expect(formatMoney(-980)).toBe('-$980')
  })

  it('promotes units at rounding boundaries instead of showing $1000M', () => {
    expect(formatMoney(999_999_999)).toBe('$1B')
    expect(formatMoney(999_999.9)).toBe('$1M')
    expect(formatMoney(999_499)).toBe('$999K')
  })
})

describe('deadlineTile', () => {
  it('splits day counts into number + caption', () => {
    expect(deadlineTile('12 days left')).toEqual({ top: '12', caption: 'days' })
    expect(deadlineTile('1 day left')).toEqual({ top: '1', caption: 'day' })
  })

  it('renders Due today as Due/today, not Due/days', () => {
    // Regression: `"Due today".includes('day')` is true, so the tile showed "Due" over "DAYS".
    expect(deadlineTile('Due today')).toEqual({ top: 'Due', caption: 'today' })
  })

  it('renders Closed and No deadline without a caption', () => {
    expect(deadlineTile('Closed')).toEqual({ top: 'Closed', caption: '' })
    expect(deadlineTile('No deadline')).toEqual({ top: 'No deadline', caption: '' })
  })
})
