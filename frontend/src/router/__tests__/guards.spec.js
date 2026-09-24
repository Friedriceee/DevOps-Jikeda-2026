import { describe, expect, it, vi } from 'vitest'
import { authGuard } from '@/router/guards'

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

  it('blocks users with the wrong role', () => {
    const result = authGuard(
      { isAuthenticated: true, hasRole: () => false },
      target({ meta: { role: 'Customer' } }),
    )

    expect(result).toEqual({ name: 'home' })
  })
})
