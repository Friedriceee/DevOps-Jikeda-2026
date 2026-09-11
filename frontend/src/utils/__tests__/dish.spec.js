import { describe, expect, it } from 'vitest'
import {
  buildDishPayload,
  buildDishUpdatePayload,
  isValidDishName,
  isValidInventory,
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
    })
  })
})
