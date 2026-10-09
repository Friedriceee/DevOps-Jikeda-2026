import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

const api = vi.hoisted(() => ({
  addCartItem: vi.fn(),
  clearCart: vi.fn(),
  deleteCartItem: vi.fn(),
  deleteMerchantCartItems: vi.fn(),
  getCart: vi.fn(),
  updateCartItem: vi.fn(),
}))

vi.mock('@/api/cart', () => api)

import { useCartStore } from '@/stores/cart'

describe('cart store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  const cartSnapshot = (overrides = {}) => ({
    merchants: [{
      merchantId: 1,
      merchantName: 'Demo Merchant',
      items: [{ id: 7, dishId: 10, dishName: 'A', unitPrice: 12, dishNum: 2, lineTotal: 24 }],
      subtotal: 24,
      discount: 3,
      total: 21,
    }],
    totalCount: 2,
    subtotal: 24,
    discount: 3,
    total: 21,
    ...overrides,
  })

  it('loads the server cart and exposes its flattened order items and totals', async () => {
    api.getCart.mockResolvedValueOnce(cartSnapshot())
    const cart = useCartStore()
    await cart.load()

    expect(cart.items).toHaveLength(1)
    expect(cart.items[0]).toMatchObject({ id: 7, merchantId: 1, name: 'A', price: 12, dishNum: 2 })
    expect(cart.totalCount).toBe(2)
    expect(cart.subtotal).toBe(24)
    expect(cart.discount).toBe(3)
    expect(cart.total).toBe(21)
  })

  it('adds a dish through the API and replaces the local state with the response', async () => {
    api.addCartItem.mockResolvedValueOnce(cartSnapshot({ totalCount: 3, subtotal: 36, total: 33 }))
    const cart = useCartStore()
    await cart.addItem({ id: 10, name: 'A' })

    expect(api.addCartItem).toHaveBeenCalledWith({ dishId: 10, dishNum: 1 })
    expect(cart.totalCount).toBe(3)
    expect(cart.total).toBe(33)
  })

  it('updates and removes an item by its server-generated cart-item id', async () => {
    api.updateCartItem.mockResolvedValueOnce(cartSnapshot({ totalCount: 1, subtotal: 12, discount: 0, total: 12 }))
    api.deleteCartItem.mockResolvedValueOnce(cartSnapshot({ merchants: [], totalCount: 0, subtotal: 0, discount: 0, total: 0 }))
    const cart = useCartStore()
    await cart.setQuantity(7, 1)
    await cart.removeItem(7)

    expect(api.updateCartItem).toHaveBeenCalledWith(7, { dishNum: 1 })
    expect(api.deleteCartItem).toHaveBeenCalledWith(7)
    expect(cart.items).toEqual([])
  })

  it('clears one merchant or the complete cart through the API', async () => {
    api.deleteMerchantCartItems.mockResolvedValueOnce(cartSnapshot({ merchants: [], totalCount: 0, subtotal: 0, discount: 0, total: 0 }))
    api.clearCart.mockResolvedValueOnce(cartSnapshot({ merchants: [], totalCount: 0, subtotal: 0, discount: 0, total: 0 }))
    const cart = useCartStore()
    await cart.clearMerchant(1)
    await cart.clear()

    expect(api.deleteMerchantCartItems).toHaveBeenCalledWith(1)
    expect(api.clearCart).toHaveBeenCalledOnce()
    expect(cart.items).toHaveLength(0)
  })
})
