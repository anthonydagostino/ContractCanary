import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ChipMultiSelect, TagInput, Typeahead } from './Typeahead'
import { Pagination, StarButton } from './ui'

/**
 * Regression tests for the untyped-button-submits-form bug class: these
 * components render buttons and get composed inside the profile editor's
 * <form>, so clicking a chip, option, or star must never submit the form.
 */

function Harness({ onSubmit, children }: { onSubmit: () => void; children: React.ReactNode }) {
  return (
    <form onSubmit={(e) => { e.preventDefault(); onSubmit() }}>
      {children}
      <button type="submit">Save</button>
    </form>
  )
}

describe('form safety harness (positive control)', () => {
  it('an untyped button inside a form DOES submit — proving these tests can catch the bug', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    render(
      <Harness onSubmit={onSubmit}>
        <button>untyped</button>
      </Harness>,
    )
    await user.click(screen.getByText('untyped'))
    expect(onSubmit).toHaveBeenCalledTimes(1)
  })
})

describe('TagInput inside a form', () => {
  it('removing a tag chip does not submit the form', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    const onChange = vi.fn()
    render(
      <Harness onSubmit={onSubmit}>
        <TagInput label="Keywords" placeholder="Add…" values={['hvac', 'roofing']} onChange={onChange} />
      </Harness>,
    )
    await user.click(screen.getByRole('button', { name: 'Remove hvac' }))
    expect(onChange).toHaveBeenCalledWith(['roofing'])
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('pressing Enter to commit a tag does not submit the form', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    const onChange = vi.fn()
    render(
      <Harness onSubmit={onSubmit}>
        <TagInput label="Keywords" placeholder="Add…" values={[]} onChange={onChange} />
      </Harness>,
    )
    await user.type(screen.getByPlaceholderText('Add…'), 'hvac{Enter}')
    expect(onChange).toHaveBeenCalledWith(['hvac'])
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('associates its label with the input', () => {
    render(<TagInput label="Keywords" placeholder="Add…" values={[]} onChange={() => {}} />)
    expect(screen.getByLabelText('Keywords')).toBeInTheDocument()
  })
})

describe('Typeahead inside a form', () => {
  const useSearch = () => ({ data: [{ code: '236220', title: 'Commercial Building Construction' }], isFetching: false })
  const toOption = (item: unknown) => {
    const i = item as { code: string; title: string }
    return { value: i.code, label: i.title }
  }

  it('picking an option and removing a chip do not submit the form', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    const onChange = vi.fn()
    render(
      <Harness onSubmit={onSubmit}>
        <Typeahead label="NAICS" placeholder="Search…" values={['541511']} onChange={onChange} useSearch={useSearch} toOption={toOption} />
      </Harness>,
    )
    await user.click(screen.getByRole('combobox'))
    await user.click(screen.getByRole('option', { name: /236220/ }))
    expect(onChange).toHaveBeenCalledWith(['541511', '236220'])

    await user.click(screen.getByRole('button', { name: 'Remove 541511' }))
    expect(onChange).toHaveBeenCalledWith([])
    expect(onSubmit).not.toHaveBeenCalled()
  })
})

describe('ChipMultiSelect inside a form', () => {
  it('toggling a chip does not submit the form', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    const onChange = vi.fn()
    render(
      <Harness onSubmit={onSubmit}>
        <ChipMultiSelect label="Set-asides" options={[{ value: 'SBA', label: 'Small business' }]} values={[]} onChange={onChange} />
      </Harness>,
    )
    await user.click(screen.getByRole('button', { name: 'Small business' }))
    expect(onChange).toHaveBeenCalledWith(['SBA'])
    expect(onSubmit).not.toHaveBeenCalled()
  })
})

describe('shared ui buttons inside a form', () => {
  it('StarButton and Pagination clicks do not submit the form', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    const onStar = vi.fn()
    const onPage = vi.fn()
    render(
      <Harness onSubmit={onSubmit}>
        <StarButton saved={false} onClick={onStar} />
        <Pagination page={2} totalPages={3} onChange={onPage} />
      </Harness>,
    )
    await user.click(screen.getByRole('button', { name: 'Save opportunity' }))
    await user.click(screen.getByRole('button', { name: 'Next' }))
    expect(onStar).toHaveBeenCalledTimes(1)
    expect(onPage).toHaveBeenCalledWith(3)
    expect(onSubmit).not.toHaveBeenCalled()
  })
})
