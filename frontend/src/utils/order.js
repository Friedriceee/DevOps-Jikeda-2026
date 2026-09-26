/**
 * 订单相关的纯函数工具，便于单元测试。
 * 与后端 CreateOrderRequest / OrderStatus 对应。
 */

// Order status values match the backend OrderStatus enum.
const STATUS_TEXT = {
  0: 'Pending payment',
  1: 'Paid',
  2: 'Out for delivery',
  3: 'Delivered',
}

/**
 * 把订单状态数值转成中文文案。
 * @param {number} status
 * @returns {string}
 */
export function orderStatusText(status) {
  return STATUS_TEXT[Number(status)] ?? 'Unknown status'
}

/**
 * 是否为待支付订单（只有待支付订单可取消）。
 * @param {number} status
 * @returns {boolean}
 */
export function isPending(status) {
  return Number(status) === 0
}

/**
 * 计算购物车内菜品总数量。
 * @param {Array<{dishNum: number|string}>} cart
 * @returns {number}
 */
export function cartTotalCount(cart) {
  if (!Array.isArray(cart)) return 0
  return cart.reduce((sum, item) => sum + Number(item?.dishNum ?? 0), 0)
}

/**
 * 计算购物车小计（菜品单价 * 数量之和）。
 * @param {Array<{price: number|string, dishNum: number|string}>} cart
 * @returns {number}
 */
export function cartSubtotal(cart) {
  if (!Array.isArray(cart)) return 0
  const total = cart.reduce(
    (sum, item) => sum + Number(item?.price ?? 0) * Number(item?.dishNum ?? 0),
    0,
  )
  return Math.round(total * 100) / 100
}

/**
 * 订单总价 = 购物车小计 + 配送费。
 * @param {Array} cart
 * @param {number|string} riderPrice
 * @returns {number}
 */
export function orderTotalPrice(cart, riderPrice = 0) {
  const total = cartSubtotal(cart) + Number(riderPrice || 0)
  return Math.round(total * 100) / 100
}

/**
 * 校验能否下单：必须选了收货地址且购物车非空。
 * @param {{addressId: number, cart: Array}} params
 * @returns {boolean}
 */
export function canPlaceOrder({ addressId, cart } = {}) {
  return Number(addressId) > 0 && Array.isArray(cart) && cart.length > 0
}

/**
 * 构建创建订单的请求体，对应后端 CreateOrderRequest。
 * @returns {object}
 */
export function buildOrderPayload({
  userId,
  addressId,
  cart = [],
  riderPrice = 0,
  needUtensils = 1,
  couponId = 0,
  expirationDate = null,
  orderTimestamp = null,
  price = null,
}) {
  return {
    userId: Number(userId),
    addressId: Number(addressId),
    price: price === null ? orderTotalPrice(cart, riderPrice) : Number(price),
    orderTimestamp: orderTimestamp ?? new Date().toISOString(),
    needUtensils: Number(needUtensils),
    riderPrice: Number(riderPrice || 0),
    couponId: Number(couponId || 0),
    expirationDate: expirationDate ?? new Date(0).toISOString(),
    shoppingCart: cart.map((item) => ({
      merchantId: Number(item.merchantId),
      dishId: Number(item.dishId),
      dishNum: Number(item.dishNum),
    })),
  }
}
