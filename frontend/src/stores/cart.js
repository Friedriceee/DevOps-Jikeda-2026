import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { cartSubtotal, cartTotalCount } from '@/utils/order'

/**
 * 购物车 store。存放待下单的菜品项，供下单页读取。
 * 菜品项结构：{ merchantId, dishId, name, price, dishNum }
 */
export const useCartStore = defineStore('cart', () => {
  const items = ref([])

  const totalCount = computed(() => cartTotalCount(items.value))
  const subtotal = computed(() => cartSubtotal(items.value))

  function addItem(dish) {
    const existing = items.value.find(
      (item) => item.merchantId === dish.merchantId && item.dishId === dish.dishId,
    )
    if (existing) {
      existing.dishNum += 1
    } else {
      items.value.push({ ...dish, dishNum: 1 })
    }
  }

  function removeItem(merchantId, dishId) {
    const index = items.value.findIndex(
      (item) => item.merchantId === merchantId && item.dishId === dishId,
    )
    if (index === -1) return
    if (items.value[index].dishNum > 1) {
      items.value[index].dishNum -= 1
    } else {
      items.value.splice(index, 1)
    }
  }

  function clear() {
    items.value = []
  }

  return { items, totalCount, subtotal, addItem, removeItem, clear }
})
