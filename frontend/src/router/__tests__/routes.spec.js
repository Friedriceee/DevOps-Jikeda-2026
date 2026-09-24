import { describe, expect, it } from 'vitest'
import { routes } from '@/router/routes'

describe('application route access rules', () => {
  it('protects merchant dish management with the Merchant role', () => {
    const merchantRoute = routes.find((route) => route.name === 'merchant-dishes')
    expect(merchantRoute.meta).toEqual({ requiresAuth: true, role: 'Merchant' })
  })

  it('keeps merchant and menu browsing public', () => {
    const customerRoute = routes.find((route) => route.name === 'customer-merchants')
    expect(customerRoute.meta.requiresAuth).toBe(false)
  })
})
