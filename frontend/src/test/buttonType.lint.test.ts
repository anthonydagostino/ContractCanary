// @vitest-environment node
import { describe, expect, it } from 'vitest'
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join, relative, resolve } from 'node:path'

/**
 * Regression guard for a real bug class: an HTML <button> defaults to
 * type="submit", so an untyped button inside a <form> submits the form when
 * clicked. We shipped that bug on the profile editor's chip-remove buttons.
 * This test scans every .tsx source file and fails if any <button> opening
 * tag lacks an explicit type attribute, wherever it lives — components get
 * reused inside forms even when they weren't written there.
 */

// Vitest's root is frontend/, so the source tree is ./src.
const SRC_DIR = resolve(process.cwd(), 'src')

function tsxFiles(dir: string): string[] {
  const out: string[] = []
  for (const name of readdirSync(dir)) {
    const full = join(dir, name)
    if (statSync(full).isDirectory()) out.push(...tsxFiles(full))
    // Skip test files: formSafety.test.tsx deliberately renders an untyped
    // button as a positive control proving the harness catches the bug.
    else if (name.endsWith('.tsx') && !name.endsWith('.test.tsx')) out.push(full)
  }
  return out
}

/**
 * Extract every <button ...> opening tag from JSX source. A simple regex breaks
 * on `>` inside attribute expressions (`onClick={() => …}`, `disabled={a >= b}`)
 * and inside string attributes, so we walk characters tracking brace depth and
 * string/template-literal state; at depth zero outside a string, `>` is the tag end.
 */
export function extractButtonTags(source: string): string[] {
  const tags: string[] = []
  const re = /<button\b/g
  let m: RegExpExecArray | null
  while ((m = re.exec(source)) !== null) {
    let depth = 0
    let quote: string | null = null
    let i = m.index + '<button'.length
    while (i < source.length) {
      const ch = source[i]
      if (quote) {
        if (ch === quote) quote = null
      } else if (ch === '"' || ch === "'" || ch === '`') {
        quote = ch
      } else if (ch === '{') {
        depth++
      } else if (ch === '}') {
        depth--
      } else if (ch === '>' && depth === 0) {
        tags.push(source.slice(m.index, i + 1))
        break
      }
      i++
    }
  }
  return tags
}

describe('button type lint', () => {
  it('every <button> in src/**/*.tsx declares an explicit type', () => {
    const offenders: string[] = []
    for (const file of tsxFiles(SRC_DIR)) {
      const source = readFileSync(file, 'utf8')
      for (const tag of extractButtonTags(source)) {
        if (!/\btype=/.test(tag)) {
          const line = source.slice(0, source.indexOf(tag)).split('\n').length
          offenders.push(`${relative(SRC_DIR, file)}:${line} → ${tag.replace(/\s+/g, ' ').slice(0, 90)}`)
        }
      }
    }
    expect(offenders, `Untyped <button> defaults to type="submit" and will submit an enclosing form.\nAdd type="button" (or type="submit" if intended):\n${offenders.join('\n')}`).toEqual([])
  })

  it('extractButtonTags handles arrows, comparisons, and string attributes', () => {
    const src = `
      <button onClick={() => go(a >= b)} aria-label="More >" className={x ? 'a' : 'b'}>Go</button>
      <button type="button">Ok</button>
    `
    const tags = extractButtonTags(src)
    expect(tags).toHaveLength(2)
    expect(tags[0]).toContain('aria-label="More >"')
    expect(tags[0].endsWith('\'b\'}>')).toBe(true)
    expect(tags[1]).toBe('<button type="button">')
  })
})
