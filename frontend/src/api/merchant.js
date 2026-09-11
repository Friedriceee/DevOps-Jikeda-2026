import http from './http'

export function createDish(payload) {
  return http.post('/merchant/dishes', payload)
}

export function listDishes(merchantId) {
  return http.get(`/merchant/${merchantId}/dishes`)
}
