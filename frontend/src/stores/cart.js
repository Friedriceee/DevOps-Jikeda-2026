import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import {
  addCartItem,
  clearCart,
  deleteCartItem,
  deleteMerchantCartItems,
  getCart,
  updateCartItem,
} from '@/api/cart'

/**
 * 服务端购物车的前端状态。金额、活动和商家归属由 Cart API 计算，
 * 本地只保存该 API 的最新快照，避免旧项目中前端自行计算金额的偏差。
 */
export const useCartStore = defineStore('cart', () => {
  const groups = ref([])
  const summary = ref(emptyCart())
  const loading = ref(false)

  const items = computed(() => groups.value.flatMap((group) =>
    group.items.map((item) => ({
      ...item,
      merchantId: group.merchantId,
      merchantName: group.merchantName,
      name: item.dishName,
      price: item.unitPrice,
    })),
  ))
  const totalCount = computed(() => summary.value.totalCount)
  const subtotal = computed(() => summary.value.subtotal)
  const discount = computed(() => summary.value.discount)
  const total = computed(() => summary.value.total)

  function applyCart(cart) {
    const next = cart ?? emptyCart()
    groups.value = next.merchants ?? []
    summary.value = {
      totalCount: Number(next.totalCount ?? 0),
      subtotal: Number(next.subtotal ?? 0),
      discount: Number(next.discount ?? 0),
      total: Number(next.total ?? 0),
    }
  }

  async function load() {
    loading.value = true
    try {
      applyCart(await getCart())
    } finally {
      loading.value = false
    }
  }

  async function addItem(dish) {
    const dishId = dish.id ?? dish.dishId
    applyCart(await addCartItem({ dishId, dishNum: 1 }))
  }

  async function setQuantity(cartItemId, dishNum) {
    applyCart(await updateCartItem(cartItemId, { dishNum: Number(dishNum) }))
  }

  async function removeItem(cartItemId) {
    applyCart(await deleteCartItem(cartItemId))
  }

  async function clearMerchant(merchantId) {
    applyCart(await deleteMerchantCartItems(merchantId))
  }

  async function clear() {
    applyCart(await clearCart())
  }

  return {
    groups,
    items,
    loading,
    totalCount,
    subtotal,
    discount,
    total,
    load,
    addItem,
    setQuantity,
    removeItem,
    clearMerchant,
    clear,
  }
})

function emptyCart() {
  return { merchants: [], totalCount: 0, subtotal: 0, discount: 0, total: 0 }
}
