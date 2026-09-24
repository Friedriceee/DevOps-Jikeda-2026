import { beforeEach, describe, expect, it, vi } from 'vitest'

const http = vi.hoisted(() => ({
  delete: vi.fn(),
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
}))

vi.mock('@/api/http', () => ({ default: http }))

import {
  addCartItem,
  clearCart,
  deleteCartItem,
  deleteMerchantCartItems,
  getCart,
  updateCartItem,
} from '@/api/cart'

describe('cart API', () => {
  beforeEach(() => vi.clearAllMocks())

  it('uses the persistent-cart endpoints', async () => {
    http.get.mockResolvedValueOnce({ merchants: [] })
    http.post.mockResolvedValueOnce({ merchants: [] })
    http.put.mockResolvedValueOnce({ merchants: [] })
    http.delete.mockResolvedValue({ merchants: [] })

    await getCart()
    await addCartItem({ dishId: 10, dishNum: 1 })
    await updateCartItem(7, { dishNum: 3 })
    await deleteCartItem(7)
    await deleteMerchantCartItems(2)
    await clearCart()

    expect(http.get).toHaveBeenCalledWith('/cart')
    expect(http.post).toHaveBeenCalledWith('/cart/items', { dishId: 10, dishNum: 1 })
    expect(http.put).toHaveBeenCalledWith('/cart/items/7', { dishNum: 3 })
    expect(http.delete).toHaveBeenNthCalledWith(1, '/cart/items/7')
    expect(http.delete).toHaveBeenNthCalledWith(2, '/cart/merchants/2')
    expect(http.delete).toHaveBeenNthCalledWith(3, '/cart')
  })
})
