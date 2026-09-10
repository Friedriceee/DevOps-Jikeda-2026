import { describe, it, expect } from 'vitest'
import { formatYuan, isValidPrice } from '@/utils/money'

describe('formatYuan', () => {
  it('保留两位小数并加前缀', () => {
    expect(formatYuan(12)).toBe('¥12.00')
    expect(formatYuan(3.5)).toBe('¥3.50')
  })

  it('非法输入返回 ¥0.00', () => {
    expect(formatYuan('abc')).toBe('¥0.00')
    expect(formatYuan(undefined)).toBe('¥0.00')
  })
})

describe('isValidPrice', () => {
  it('大于 0 的数字为合法', () => {
    expect(isValidPrice(1)).toBe(true)
    expect(isValidPrice('9.9')).toBe(true)
  })

  it('0、负数、非数字为非法', () => {
    expect(isValidPrice(0)).toBe(false)
    expect(isValidPrice(-1)).toBe(false)
    expect(isValidPrice('abc')).toBe(false)
  })
})
