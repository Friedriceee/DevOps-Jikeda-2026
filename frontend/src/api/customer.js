import http from './http'

export function listMerchants() {
  return http.get('/merchants')
}

export function listMerchantMenu(merchantId) {
  return http.get(`/merchant/${merchantId}/dishes`)
}
