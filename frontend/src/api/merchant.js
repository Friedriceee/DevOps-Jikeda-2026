import http from './http'

export function createDish(payload) {
  return http.post('/merchant/dishes', payload)
}

export function listDishes(merchantId) {
  return http.get(`/merchant/${merchantId}/dishes`)
}

export function updateDish(dishId, merchantId, payload) {
  return http.put(`/merchant/dishes/${dishId}`, payload, { params: { merchantId } })
}

export function deleteDish(dishId, merchantId) {
  return http.delete(`/merchant/dishes/${dishId}`, { params: { merchantId } })
}

export function createSpecialOffer(payload) {
  return http.post('/merchant/special-offers', payload)
}

export function listSpecialOffers(merchantId) {
  return http.get(`/merchant/${merchantId}/special-offers`)
}

export function updateSpecialOffer(offerId, merchantId, payload) {
  return http.put(
    `/merchant/special-offers/${offerId}`,
    payload,
    { params: { merchantId } },
  )
}

export function deleteSpecialOffer(offerId, merchantId) {
  return http.delete(
    `/merchant/special-offers/${offerId}`,
    { params: { merchantId } },
  )
}
