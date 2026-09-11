import http from './http'

export function createDish(payload) {
  return http.post('/merchant/dishes', payload)
}

export function listDishes(merchantId) {
  return http.get(`/merchant/${merchantId}/dishes`)
}

export function updateDish(dishId, payload) {
  return http.put(`/merchant/dishes/${dishId}`, payload)
}

export function deleteDish(dishId, merchantId) {
  return http.delete(`/merchant/dishes/${dishId}`, { params: { merchantId } })
}
