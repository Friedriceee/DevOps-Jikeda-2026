import { describe, expect, it } from 'vitest'
import {
  buildOrderPayload,
  canPlaceOrder,
  cartSubtotal,
  cartTotalCount,
  isPending,
  orderStatusText,
  orderTotalPrice,
} from '@/utils/order'

describe('order status helpers', () => {
  it('maps status numbers to Chinese text', () => {
    expect(orderStatusText(0)).toBe('待支付')
    expect(orderStatusText(1)).toBe('已付款')
    expect(orderStatusText(2)).toBe('配送中')
    expect(orderStatusText(3)).toBe('已送达')
    expect(orderStatusText(99)).toBe('未知状态')
  })

  it('treats only status 0 as pending (cancellable)', () => {
    expect(isPending(0)).toBe(true)
    expect(isPending(1)).toBe(false)
    expect(isPending('0')).toBe(true)
  })
})

describe('cart calculations', () => {
  const cart = [
    { merchantId: 1, dishId: 10, price: 12.5, dishNum: 2 },
    { merchantId: 1, dishId: 11, price: 8, dishNum: 3 },
  ]

  it('sums total item count', () => {
    expect(cartTotalCount(cart)).toBe(5)
    expect(cartTotalCount([])).toBe(0)
    expect(cartTotalCount(null)).toBe(0)
  })

  it('computes subtotal with 2-decimal rounding', () => {
    // 12.5*2 + 8*3 = 25 + 24 = 49
    expect(cartSubtotal(cart)).toBe(49)
    expect(cartSubtotal([{ price: 0.1, dishNum: 3 }])).toBe(0.3)
  })

  it('adds rider price to subtotal for total price', () => {
    expect(orderTotalPrice(cart, 3)).toBe(52)
    expect(orderTotalPrice(cart, 0)).toBe(49)
    expect(orderTotalPrice([], 5)).toBe(5)
  })
})

describe('canPlaceOrder', () => {
  it('requires a selected address and a non-empty cart', () => {
    expect(canPlaceOrder({ addressId: 1, cart: [{ dishId: 1 }] })).toBe(true)
    expect(canPlaceOrder({ addressId: 0, cart: [{ dishId: 1 }] })).toBe(false)
    expect(canPlaceOrder({ addressId: 1, cart: [] })).toBe(false)
    expect(canPlaceOrder({})).toBe(false)
  })
})

describe('buildOrderPayload', () => {
  it('builds a payload matching backend CreateOrderRequest', () => {
    const payload = buildOrderPayload({
      userId: '1',
      addressId: '2',
      cart: [
        { merchantId: 1, dishId: 10, price: 12.5, dishNum: 2 },
      ],
      riderPrice: 3,
      needUtensils: 1,
      orderTimestamp: '2026-01-01T00:00:00.000Z',
    })

    expect(payload).toEqual({
      userId: 1,
      addressId: 2,
      price: 28, // 12.5*2 + 3
      orderTimestamp: '2026-01-01T00:00:00.000Z',
      needUtensils: 1,
      riderPrice: 3,
      couponId: 0,
      expirationDate: new Date(0).toISOString(),
      shoppingCart: [
        { merchantId: 1, dishId: 10, dishNum: 2 },
      ],
    })
  })

  it('includes coupon fields when a coupon is used', () => {
    const payload = buildOrderPayload({
      userId: 1,
      addressId: 1,
      cart: [{ merchantId: 1, dishId: 1, price: 10, dishNum: 1 }],
      couponId: 7,
      expirationDate: '2026-06-01T00:00:00.000Z',
      orderTimestamp: '2026-01-01T00:00:00.000Z',
    })
    expect(payload.couponId).toBe(7)
    expect(payload.expirationDate).toBe('2026-06-01T00:00:00.000Z')
  })
})
