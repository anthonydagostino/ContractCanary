import type { SetAsideCode } from './types'

// Profile-selectable set-asides (mirrors backend SamMappings.ProfileSelectableSetAsides).
export const SET_ASIDE_OPTIONS: { value: SetAsideCode; label: string }[] = [
  { value: 'TotalSmallBusiness', label: 'Small Business' },
  { value: 'PartialSmallBusiness', label: 'Partial Small Business' },
  { value: 'EightA', label: '8(a)' },
  { value: 'HubZone', label: 'HUBZone' },
  { value: 'Sdvosb', label: 'SDVOSB' },
  { value: 'Wosb', label: 'WOSB' },
  { value: 'Edwosb', label: 'EDWOSB' },
  { value: 'VeteranOwned', label: 'Veteran-Owned' },
]
