import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

const api = vi.hoisted(() => ({
  getPricingQuote: vi.fn(),
  listPricingCoupons: vi.fn(),
}))

vi.mock('@/api/pricing', () => api)

import { usePricingStore } from '@/stores/pricing'

describe('pricing store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('loads coupons and exposes the backend quote', async () => {
    api.listPricingCoupons.mockResolvedValueOnce([{ id: 1, code: 'WELCOME5' }])
    api.getPricingQuote.mockResolvedValueOnce({
      subtotal: 40,
      merchantDiscount: 5,
      couponDiscount: 5,
      deliveryFee: 5,
      total: 35,
    })
    const pricing = usePricingStore()

    await pricing.loadCoupons()
    await pricing.refreshQuote('WELCOME5')

    expect(pricing.coupons).toEqual([{ id: 1, code: 'WELCOME5' }])
    expect(api.getPricingQuote).toHaveBeenCalledWith('WELCOME5')
    expect(pricing.total).toBeUndefined()
    expect(pricing.quote.total).toBe(35)
    expect(pricing.hasQuote).toBe(true)
  })

  it('keeps a server pricing error for inline checkout feedback', async () => {
    const requestError = Object.assign(new Error('Request failed'), {
      response: { data: { message: 'Spend at least ¥80.00 after restaurant discounts to use this coupon.' } },
    })
    api.getPricingQuote.mockRejectedValueOnce(requestError)
    const pricing = usePricingStore()

    await expect(pricing.refreshQuote('SAVE12')).rejects.toThrow('Request failed')

    expect(pricing.quote).toBeNull()
    expect(pricing.error).toContain('Spend at least ¥80.00')
    expect(pricing.loading).toBe(false)
  })

  it('resets the selected coupon and quote after checkout', async () => {
    api.getPricingQuote.mockResolvedValueOnce({ total: 20 })
    const pricing = usePricingStore()
    await pricing.refreshQuote('WELCOME5')

    pricing.reset()

    expect(pricing.quote).toBeNull()
    expect(pricing.selectedCouponCode).toBe('')
    expect(pricing.error).toBe('')
  })
})
