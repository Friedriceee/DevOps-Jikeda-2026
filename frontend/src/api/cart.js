import http from './http'

export function getCart() {
  return http.get('/cart')
}

export function addCartItem(payload) {
  return http.post('/cart/items', payload)
}

export function updateCartItem(cartItemId, payload) {
  return http.put(`/cart/items/${cartItemId}`, payload)
}

export function deleteCartItem(cartItemId) {
  return http.delete(`/cart/items/${cartItemId}`)
}

export function deleteMerchantCartItems(merchantId) {
  return http.delete(`/cart/merchants/${merchantId}`)
}

export function clearCart() {
  return http.delete('/cart')
}
