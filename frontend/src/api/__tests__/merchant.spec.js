import { beforeEach, describe, expect, it, vi } from 'vitest'

const http = vi.hoisted(() => ({
  get: vi.fn(),
  put: vi.fn(),
}))

vi.mock('@/api/http', () => ({ default: http }))

import { listMerchantOrders, transitionMerchantOrder } from '@/api/merchant'

describe('merchant fulfilment API', () => {
  beforeEach(() => vi.clearAllMocks())

  it('loads only the signed-in merchant orders', async () => {
    http.get.mockResolvedValue([])

    await listMerchantOrders()

    expect(http.get).toHaveBeenCalledWith('/merchant/orders')
  })

  it('sends an explicit order action and optional rejection reason', async () => {
    http.put.mockResolvedValue({ orderId: 42, status: 'Rejected' })

    await transitionMerchantOrder(42, 'Reject', 'Kitchen is closing.')

    expect(http.put).toHaveBeenCalledWith('/merchant/orders/42/status', {
      action: 'Reject',
      reason: 'Kitchen is closing.',
    })
  })
})
