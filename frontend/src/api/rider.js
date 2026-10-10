import http from './http'

// 可接任务池（待派送订单）
export function listAvailableDeliveries() {
  return http.get('/rider/available')
}

// 当前骑手自己的配送任务
export function listMyDeliveries() {
  return http.get('/rider/deliveries')
}

// 接单
export function acceptDelivery(orderId) {
  return http.post(`/rider/deliveries/${orderId}/accept`)
}

// 推进配送状态：action = PickUp / StartDelivering / Complete
export function advanceDelivery(orderId, action) {
  return http.put(`/rider/deliveries/${orderId}/status`, { action })
}
