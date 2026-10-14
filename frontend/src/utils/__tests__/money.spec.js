import { describe, it, expect } from 'vitest'
import { formatYuan, isValidPrice } from '@/utils/money'

describe('formatYuan', () => {
  it('keeps two decimals and adds the prefix', () => {
    expect(formatYuan(12)).toBe('¥12.00')
    expect(formatYuan(3.5)).toBe('¥3.50')
  })

  it('returns ¥0.00 for invalid input', () => {
    expect(formatYuan('abc')).toBe('¥0.00')
    expect(formatYuan(undefined)).toBe('¥0.00')
  })
})

describe('isValidPrice', () => {
  it('accepts numbers greater than 0', () => {
    expect(isValidPrice(1)).toBe(true)
    expect(isValidPrice('9.9')).toBe(true)
  })

  it('rejects zero, negatives and non-numbers', () => {
    expect(isValidPrice(0)).toBe(false)
    expect(isValidPrice(-1)).toBe(false)
    expect(isValidPrice('abc')).toBe(false)
  })
})
