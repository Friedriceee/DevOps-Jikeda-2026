import { describe, expect, it } from 'vitest'
import {
  buildDishPayload,
  buildDishUpdatePayload,
  isValidDishName,
  isValidInventory,
  menuSettingsError,
  promotionInput,
} from '@/utils/dish'

describe('dish form helpers', () => {
  it('rejects empty or overlong names', () => {
    expect(isValidDishName('')).toBe(false)
    expect(isValidDishName('   ')).toBe(false)
    expect(isValidDishName('a'.repeat(51))).toBe(false)
    expect(isValidDishName('Kung Pao Chicken')).toBe(true)
  })

  it('accepts only non-negative integer inventory', () => {
    expect(isValidInventory(0)).toBe(true)
    expect(isValidInventory('12')).toBe(true)
    expect(isValidInventory(-1)).toBe(false)
    expect(isValidInventory(1.5)).toBe(false)
  })

  it('builds the API payload with trimmed optional values', () => {
    expect(buildDishPayload({
      name: '  Noodles  ',
      price: '12.5',
      category: '  Main  ',
      imageUrl: ' ',
      inventory: '8',
    }, 1)).toEqual({
      merchantId: 1,
      name: 'Noodles',
      price: 12.5,
      category: 'Main',
      imageUrl: null,
      inventory: 8,
      itemType: 'Standard', bundleSize: null, promotionStartsAtUtc: null, promotionEndsAtUtc: null,
    })
  })

  it('builds an update payload without merchantId', () => {
    expect(buildDishUpdatePayload({
      name: '  Rice  ',
      price: '8.5',
      category: '',
      imageUrl: 'https://example.com/rice.png',
      inventory: '4',
    })).toEqual({
      name: 'Rice',
      price: 8.5,
      category: null,
      imageUrl: 'https://example.com/rice.png',
      inventory: 4,
      itemType: 'Standard', bundleSize: null, promotionStartsAtUtc: null, promotionEndsAtUtc: null,
    })
  })

  it('sends only settings applicable to the selected item type', () => {
    expect(buildDishUpdatePayload({ itemType: 'Bundle', bundleSize: '3', promotionStartsAtUtc: 'old' }))
      .toMatchObject({ itemType: 'Bundle', bundleSize: 3, promotionStartsAtUtc: null, promotionEndsAtUtc: null })
    expect(buildDishUpdatePayload({ itemType: 'Standard', bundleSize: 3 })).toMatchObject({ bundleSize: null })
  })

  it('interprets unzoned promotion form values as UTC, not the browser timezone', () => {
    expect(buildDishPayload({ itemType: 'Promotional', promotionStartsAtUtc: '2026-10-10T00:00', promotionEndsAtUtc: '2026-10-11T00:00' }, 1))
      .toMatchObject({ promotionStartsAtUtc: '2026-10-10T00:00:00.000Z', promotionEndsAtUtc: '2026-10-11T00:00:00.000Z', bundleSize: null })
    expect(promotionInput('2026-10-10T08:00:00+08:00')).toBe('2026-10-10T00:00')
  })

  it('validates type-specific settings in English', () => {
    expect(menuSettingsError({ itemType: 'Standard' })).toBe('')
    expect(menuSettingsError({ itemType: 'Bundle', bundleSize: 2 })).toBe('')
    expect(menuSettingsError({ itemType: 'Bundle', bundleSize: 1.5 })).toContain('whole number')
    expect(menuSettingsError({ itemType: 'Unknown' })).toContain('Choose')
    expect(menuSettingsError({ itemType: 'Promotional' })).toContain('both promotion times')
    expect(menuSettingsError({ itemType: 'Promotional', promotionStartsAtUtc: '2026-10-10T00:00', promotionEndsAtUtc: '2026-10-09T00:00' })).toContain('after')
  })
})
