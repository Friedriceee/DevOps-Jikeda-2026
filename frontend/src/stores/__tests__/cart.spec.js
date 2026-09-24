import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCartStore } from '@/stores/cart'

describe('cart store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  const dishA = { merchantId: 1, dishId: 10, name: 'A', price: 12 }
  const dishB = { merchantId: 1, dishId: 11, name: 'B', price: 8 }

  it('adds a new item with quantity 1', () => {
    const cart = useCartStore()
    cart.addItem(dishA)

    expect(cart.items).toHaveLength(1)
    expect(cart.items[0].dishNum).toBe(1)
    expect(cart.totalCount).toBe(1)
  })

  it('increments quantity when adding the same dish again', () => {
    const cart = useCartStore()
    cart.addItem(dishA)
    cart.addItem(dishA)

    expect(cart.items).toHaveLength(1)
    expect(cart.items[0].dishNum).toBe(2)
    expect(cart.totalCount).toBe(2)
  })

  it('keeps different dishes as separate items', () => {
    const cart = useCartStore()
    cart.addItem(dishA)
    cart.addItem(dishB)

    expect(cart.items).toHaveLength(2)
    expect(cart.subtotal).toBe(20) // 12 + 8
  })

  it('decrements then removes an item', () => {
    const cart = useCartStore()
    cart.addItem(dishA)
    cart.addItem(dishA)

    cart.removeItem(1, 10) // 2 -> 1
    expect(cart.items[0].dishNum).toBe(1)

    cart.removeItem(1, 10) // 1 -> removed
    expect(cart.items).toHaveLength(0)
  })

  it('clears the cart', () => {
    const cart = useCartStore()
    cart.addItem(dishA)
    cart.addItem(dishB)
    cart.clear()

    expect(cart.items).toHaveLength(0)
    expect(cart.totalCount).toBe(0)
  })
})
