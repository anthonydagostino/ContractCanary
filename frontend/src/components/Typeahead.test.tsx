import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ChipMultiSelect, TagInput } from './Typeahead'

describe('ChipMultiSelect', () => {
  it('toggles values on and off', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(
      <ChipMultiSelect
        label="Set-asides"
        options={[{ value: 'a', label: 'Alpha' }, { value: 'b', label: 'Bravo' }]}
        values={[]}
        onChange={onChange}
      />,
    )
    await user.click(screen.getByText('Alpha'))
    expect(onChange).toHaveBeenCalledWith(['a'])
  })

  it('removes an already-selected value', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(
      <ChipMultiSelect
        label="Set-asides"
        options={[{ value: 'a', label: 'Alpha' }]}
        values={['a']}
        onChange={onChange}
      />,
    )
    await user.click(screen.getByText('Alpha'))
    expect(onChange).toHaveBeenCalledWith([])
  })
})

describe('TagInput', () => {
  it('commits a tag on Enter and dedupes', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<TagInput label="Keywords" placeholder="Add…" values={[]} onChange={onChange} />)
    const input = screen.getByPlaceholderText('Add…')
    await user.type(input, 'hvac{Enter}')
    expect(onChange).toHaveBeenCalledWith(['hvac'])
  })
})
