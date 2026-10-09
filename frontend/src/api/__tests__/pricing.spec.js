import { beforeEach, describe, expect, it, vi } from 'vitest'

const http = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
}))

vi.mock('@/api/http', () => ({ default: http }))

import { getPricingQuote, listPricingCoupons } from '@/api/pricing'

describe('pricing API', () => {
  beforeEach(() => vi.clearAllMocks())

  it('loads coupons and requests a server-calculated quote', async () => {
    http.get.mockResolvedValueOnce([])
    http.post.mockResolvedValueOnce({ total: 35 })

    await listPricingCoupons()
    await getPricingQuote('WELCOME5')
    await getPricingQuote()

    expect(http.get).toHaveBeenCalledWith('/pricing/coupons')
    expect(http.post).toHaveBeenNthCalledWith(1, '/pricing/quote', { couponCode: 'WELCOME5' })
    expect(http.post).toHaveBeenNthCalledWith(2, '/pricing/quote', { couponCode: null })
  })
})
