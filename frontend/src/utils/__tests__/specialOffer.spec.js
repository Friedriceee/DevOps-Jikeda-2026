import { describe, expect, it } from 'vitest'
import {
  buildSpecialOfferPayload,
  buildSpecialOfferUpdatePayload,
  isValidSpecialOffer,
} from '@/utils/specialOffer'

describe('special offer form helpers', () => {
  it('accepts a positive threshold and a smaller positive remission', () => {
    expect(isValidSpecialOffer(50, 5)).toBe(true)
    expect(isValidSpecialOffer('50.00', '5.00')).toBe(true)
  })

  it('rejects zero, negative, equal, greater, non-numeric, and over-precision values', () => {
    expect(isValidSpecialOffer(0, 1)).toBe(false)
    expect(isValidSpecialOffer(50, 0)).toBe(false)
    expect(isValidSpecialOffer(50, 50)).toBe(false)
    expect(isValidSpecialOffer(50, 60)).toBe(false)
    expect(isValidSpecialOffer('x', 1)).toBe(false)
    expect(isValidSpecialOffer(50.001, 5)).toBe(false)
  })

  it('builds a numeric create payload', () => {
    expect(buildSpecialOfferPayload({
      minPrice: ' 50.00 ',
      amountRemission: '5.00',
    }, 1)).toEqual({
      merchantId: 1,
      minPrice: 50,
      amountRemission: 5,
    })
  })

  it('builds an update payload without merchantId', () => {
    expect(buildSpecialOfferUpdatePayload({
      minPrice: '80',
      amountRemission: '8',
    })).toEqual({
      minPrice: 80,
      amountRemission: 8,
    })
  })
})

