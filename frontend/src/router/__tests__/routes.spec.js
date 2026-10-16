import { describe, expect, it } from 'vitest'
import { routes } from '@/router/routes'

describe('application route access rules', () => {
  it('exposes public login and customer registration routes', () => {
    expect(routes.find((route) => route.name === 'login').path).toBe('/login')
    expect(routes.find((route) => route.name === 'register').path).toBe('/register')
    expect(routes.find((route) => route.name === 'register-merchant').path).toBe('/register/merchant')
    expect(routes.find((route) => route.name === 'register-rider').path).toBe('/register/rider')
  })

  it('protects merchant dish management with the Merchant role', () => {
    const merchantRoute = routes.find((route) => route.name === 'merchant-dishes')
    expect(merchantRoute.meta).toEqual({ requiresAuth: true, role: 'Merchant' })
  })

  it('protects the administration console with the Administrator role', () => {
    const adminRoute = routes.find((route) => route.name === 'admin-dashboard')
    expect(adminRoute.path).toBe('/admin')
    expect(adminRoute.meta).toEqual({ requiresAuth: true, role: 'Administrator' })
  })

  it('protects merchant order fulfilment with the Merchant role', () => {
    const merchantOrders = routes.find((route) => route.name === 'merchant-orders')
    expect(merchantOrders.path).toBe('/merchant/orders')
    expect(merchantOrders.meta).toEqual({ requiresAuth: true, role: 'Merchant' })
  })

  it('keeps merchant and menu browsing public', () => {
    const customerRoute = routes.find((route) => route.name === 'customer-merchants')
    expect(customerRoute.meta.requiresAuth).toBe(false)
  })

  it('retains the main branch special-offer and order routes with role restrictions', () => {
    expect(routes.find((route) => route.name === 'merchant-special-offers').meta).toEqual({
      requiresAuth: true,
      role: 'Merchant',
    })
    expect(routes.find((route) => route.name === 'user-orders').meta).toEqual({
      requiresAuth: true,
      role: 'Customer',
    })
    expect(routes.find((route) => route.name === 'customer-checkout').meta).toEqual({
      requiresAuth: true,
      role: 'Customer',
    })
  })
})
