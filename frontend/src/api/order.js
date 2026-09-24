import http from './http'

// ---------- 订单 ----------
export function createOrder(payload) {
  return http.post('/orders', payload)
}

export function listOrders(userId) {
  return http.get(`/orders/user/${userId}`)
}

export function cancelOrder(orderId) {
  return http.delete(`/orders/${orderId}`)
}

// ---------- 地址 ----------
export function createAddress(payload) {
  return http.post('/user/addresses', payload)
}

export function listAddresses(userId) {
  return http.get(`/user/${userId}/addresses`)
}
