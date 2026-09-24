import { beforeEach, describe, expect, it, vi } from 'vitest'

const http = vi.hoisted(() => ({
  get: vi.fn(),
}))

vi.mock('@/api/http', () => ({ default: http }))

import { listMerchantMenu, listMerchants } from '@/api/customer'

describe('customer menu API', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('requests the public merchant list', async () => {
    http.get.mockResolvedValueOnce([{ id: 1, name: 'Demo Merchant' }])

    await expect(listMerchants()).resolves.toEqual([{ id: 1, name: 'Demo Merchant' }])
    expect(http.get).toHaveBeenCalledWith('/merchants')
  })

  it('requests a merchant menu by merchant id', async () => {
    http.get.mockResolvedValueOnce([{ id: 11, name: 'Noodles' }])

    await expect(listMerchantMenu(1)).resolves.toEqual([{ id: 11, name: 'Noodles' }])
    expect(http.get).toHaveBeenCalledWith('/merchant/1/dishes')
  })
})
