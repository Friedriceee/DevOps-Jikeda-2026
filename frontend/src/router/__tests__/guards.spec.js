import { describe, expect, it, vi } from 'vitest'
import { authGuard, postLoginDestination } from '@/router/guards'

function target(overrides = {}) {
  return {
    name: 'customer-cart',
    fullPath: '/customer/cart',
    meta: {},
    ...overrides,
  }
}

describe('auth route guard', () => {
  it('redirects unauthenticated users to login with the original path', () => {
    const result = authGuard(
      { isAuthenticated: false, hasRole: vi.fn() },
      target({ meta: { requiresAuth: true } }),
    )

    expect(result).toEqual({
      name: 'login',
      query: { redirect: '/customer/cart' },
    })
  })

  it('redirects authenticated users away from the login page', () => {
    const result = authGuard(
      { isAuthenticated: true, hasRole: vi.fn() },
      target({ name: 'login', fullPath: '/login' }),
    )

    expect(result).toEqual({ name: 'customer-merchants' })
  })

  it('redirects authenticated merchants to merchant management', () => {
    const result = authGuard(
      { isAuthenticated: true, role: 'Merchant', hasRole: vi.fn() },
      target({ name: 'login', fullPath: '/login' }),
    )

    expect(result).toEqual({ name: 'merchant-dishes' })
  })

  it('blocks users with the wrong role', () => {
    const result = authGuard(
      { isAuthenticated: true, hasRole: () => false },
      target({ meta: { role: 'Customer' } }),
    )

    expect(result).toEqual({ name: 'home' })
  })

  it('uses an internal redirect and chooses a role-based default destination', () => {
    expect(postLoginDestination('/customer/cart', 'Customer')).toBe('/customer/cart')
    expect(postLoginDestination(null, 'Merchant')).toBe('/merchant/dishes')
    expect(postLoginDestination(null, 'Customer')).toBe('/customer/merchants')
  })

  it('rejects external redirect URLs', () => {
    expect(postLoginDestination('https://example.com', 'Customer')).toBe('/customer/merchants')
    expect(postLoginDestination('//example.com', 'Merchant')).toBe('/merchant/dishes')
  })
})
