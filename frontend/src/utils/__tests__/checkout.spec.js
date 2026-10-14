import { describe, expect, it } from 'vitest'
import {
  PAYMENT_METHOD_APPROVE,
  PAYMENT_METHOD_DECLINE,
  buildCheckoutPayload,
  canPay,
  checkoutErrorMessage,
  createIdempotencyKey,
  paymentMethodOptions,
  shouldReuseIdempotencyKey,
} from '@/utils/checkout'

describe('createIdempotencyKey', () => {
  it('produces keys the server accepts (8-64 chars of letters, digits, - and _)', () => {
    for (let i = 0; i < 20; i += 1) {
      expect(createIdempotencyKey()).toMatch(/^[A-Za-z0-9_-]{8,64}$/)
    }
  })

  it('produces a different key every time', () => {
    const keys = new Set(Array.from({ length: 50 }, createIdempotencyKey))
    expect(keys.size).toBe(50)
  })
})

describe('buildCheckoutPayload', () => {
  it('contains only the customer choices, never a price, total, item list or user id', () => {
    const payload = buildCheckoutPayload({
      addressId: '7', couponCode: ' welcome5 ', needUtensils: true, paymentMethod: PAYMENT_METHOD_APPROVE,
    })

    expect(payload).toEqual({
      addressId: 7, couponCode: 'welcome5', needUtensils: 1, paymentMethod: 'SimulatedCard',
    })
    expect(Object.keys(payload).sort()).toEqual(['addressId', 'couponCode', 'needUtensils', 'paymentMethod'])
  })

  it('sends a null coupon when none is chosen and defaults to the approving card', () => {
    expect(buildCheckoutPayload({ addressId: 1, couponCode: '   ' })).toEqual({
      addressId: 1, couponCode: null, needUtensils: 0, paymentMethod: 'SimulatedCard',
    })
  })
})

describe('paymentMethodOptions', () => {
  it('hides the decline test card unless explicitly requested', () => {
    expect(paymentMethodOptions().map((option) => option.value)).toEqual([PAYMENT_METHOD_APPROVE])
    expect(paymentMethodOptions({ includeTest: true }).map((option) => option.value))
      .toEqual([PAYMENT_METHOD_APPROVE, PAYMENT_METHOD_DECLINE])
  })
})

describe('canPay', () => {
  const ready = { addressId: 3, hasItems: true, hasQuote: true }

  it('needs an address, items and a server quote', () => {
    expect(canPay(ready)).toBe(true)
    expect(canPay({ ...ready, addressId: null })).toBe(false)
    expect(canPay({ ...ready, hasItems: false })).toBe(false)
    expect(canPay({ ...ready, hasQuote: false })).toBe(false)
  })

  it('blocks a second click while a quote or payment is in flight', () => {
    expect(canPay({ ...ready, quoting: true })).toBe(false)
    expect(canPay({ ...ready, submitting: true })).toBe(false)
  })
})

describe('checkoutErrorMessage', () => {
  const failure = (status, data) => ({ response: { status, data } })

  it('maps known error codes to customer-friendly English', () => {
    expect(checkoutErrorMessage(failure(402, { code: 'PaymentDeclined' }))).toContain('declined')
    expect(checkoutErrorMessage(failure(409, { code: 'InsufficientStock' }))).toContain('enough stock')
    expect(checkoutErrorMessage(failure(409, { code: 'CartChanged' }))).toContain('cart changed')
  })

  it('falls back to the server message, then to a generic one', () => {
    expect(checkoutErrorMessage(failure(400, { code: 'CouponExpired', message: 'This coupon has expired.' })))
      .toBe('This coupon has expired.')
    expect(checkoutErrorMessage(failure(500, {}))).toBe('Checkout failed. Please try again.')
    expect(checkoutErrorMessage(new Error('Network Error'))).toContain('Network problem')
  })
})

describe('shouldReuseIdempotencyKey', () => {
  it('reuses the key only when the outcome is unknown', () => {
    expect(shouldReuseIdempotencyKey(new Error('Network Error'))).toBe(true)
    expect(shouldReuseIdempotencyKey({ response: { status: 503 } })).toBe(true)
    expect(shouldReuseIdempotencyKey({ response: { status: 402 } })).toBe(false)
    expect(shouldReuseIdempotencyKey({ response: { status: 409 } })).toBe(false)
  })
})
