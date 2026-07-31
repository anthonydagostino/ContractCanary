import { useEffect, useMemo, useRef, useState } from 'react'
import { Badge, Spinner } from './ui'

export interface Option { value: string; label: string; hint?: string }

interface Props {
  label: string
  placeholder: string
  values: string[]
  onChange: (values: string[]) => void
  useSearch: (q: string) => { data?: unknown[]; isFetching: boolean }
  toOption: (item: unknown) => Option
  help?: string
}

/** Debounced, keyboard-free multi-select typeahead over a server-backed search. */
export function Typeahead({ label, placeholder, values, onChange, useSearch, toOption, help }: Props) {
  const [q, setQ] = useState('')
  const [debounced, setDebounced] = useState('')
  const [open, setOpen] = useState(false)
  const boxRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const t = setTimeout(() => setDebounced(q), 200)
    return () => clearTimeout(t)
  }, [q])

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (boxRef.current && !boxRef.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onClick)
    return () => document.removeEventListener('mousedown', onClick)
  }, [])

  const { data, isFetching } = useSearch(debounced)
  const options = useMemo(() => (data ?? []).map(toOption), [data, toOption])
  const labelFor = useMemo(() => {
    const m = new Map<string, string>()
    options.forEach((o) => m.set(o.value, o.label))
    return m
  }, [options])

  function add(v: string) {
    if (!values.includes(v)) onChange([...values, v])
    setQ('')
  }
  function remove(v: string) {
    onChange(values.filter((x) => x !== v))
  }

  return (
    <div ref={boxRef} className="relative">
      <label className="label">{label}</label>
      {values.length > 0 && (
        <div className="mb-2 flex flex-wrap gap-1.5">
          {values.map((v) => (
            <span key={v} className="badge bg-brand-50 text-brand-700">
              {labelFor.get(v) ?? v}
              <button onClick={() => remove(v)} className="ml-1 text-brand-400 hover:text-brand-700" aria-label={`Remove ${v}`}>×</button>
            </span>
          ))}
        </div>
      )}
      <input
        className="input"
        placeholder={placeholder}
        value={q}
        onChange={(e) => { setQ(e.target.value); setOpen(true) }}
        onFocus={() => setOpen(true)}
      />
      {help && <p className="mt-1 text-xs text-slate-400">{help}</p>}
      {open && (q.length > 0 || options.length > 0) && (
        <div className="absolute z-20 mt-1 max-h-64 w-full overflow-auto rounded-lg border border-slate-200 bg-white shadow-lg">
          {isFetching && <div className="flex items-center gap-2 px-3 py-2 text-sm text-slate-400"><Spinner className="h-4 w-4" /> Searching…</div>}
          {!isFetching && options.length === 0 && <div className="px-3 py-2 text-sm text-slate-400">No matches</div>}
          {options.map((o) => {
            const selected = values.includes(o.value)
            return (
              <button
                key={o.value}
                onClick={() => (selected ? remove(o.value) : add(o.value))}
                className="flex w-full items-start justify-between gap-2 px-3 py-2 text-left text-sm hover:bg-slate-50"
              >
                <span>
                  <span className="font-medium text-slate-800">{o.value}</span>
                  <span className="ml-2 text-slate-500">{o.label}</span>
                  {o.hint && <span className="ml-2 text-xs text-slate-400">{o.hint}</span>}
                </span>
                {selected && <Badge tone="blue">Added</Badge>}
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}

interface TagInputProps {
  label: string
  placeholder: string
  values: string[]
  onChange: (v: string[]) => void
  help?: string
  transform?: (v: string) => string
}

/** Free-text tag input (keywords). Enter or comma commits a tag. */
export function TagInput({ label, placeholder, values, onChange, help, transform }: TagInputProps) {
  const [text, setText] = useState('')
  function commit() {
    const v = (transform ? transform(text) : text).trim()
    if (v && !values.includes(v)) onChange([...values, v])
    setText('')
  }
  return (
    <div>
      <label className="label">{label}</label>
      {values.length > 0 && (
        <div className="mb-2 flex flex-wrap gap-1.5">
          {values.map((v) => (
            <span key={v} className="badge bg-slate-100 text-slate-700">
              {v}
              <button onClick={() => onChange(values.filter((x) => x !== v))} className="ml-1 text-slate-400 hover:text-slate-700" aria-label={`Remove ${v}`}>×</button>
            </span>
          ))}
        </div>
      )}
      <input
        className="input"
        placeholder={placeholder}
        value={text}
        onChange={(e) => setText(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ',') { e.preventDefault(); commit() }
          if (e.key === 'Backspace' && !text && values.length) onChange(values.slice(0, -1))
        }}
        onBlur={commit}
      />
      {help && <p className="mt-1 text-xs text-slate-400">{help}</p>}
    </div>
  )
}

interface ChipProps<T extends string> {
  label: string
  options: { value: T; label: string }[]
  values: T[]
  onChange: (v: T[]) => void
}

/** Toggle-chip multi-select for a fixed option set (set-asides, notice types, states). */
export function ChipMultiSelect<T extends string>({ label, options, values, onChange }: ChipProps<T>) {
  function toggle(v: T) {
    onChange(values.includes(v) ? values.filter((x) => x !== v) : [...values, v])
  }
  return (
    <div>
      <label className="label">{label}</label>
      <div className="flex flex-wrap gap-2">
        {options.map((o) => {
          const active = values.includes(o.value)
          return (
            <button
              key={o.value}
              type="button"
              onClick={() => toggle(o.value)}
              className={
                'rounded-full border px-3 py-1 text-sm transition ' +
                (active ? 'border-brand-600 bg-brand-600 text-white' : 'border-slate-300 bg-white text-slate-600 hover:border-slate-400')
              }
            >
              {o.label}
            </button>
          )
        })}
      </div>
    </div>
  )
}
