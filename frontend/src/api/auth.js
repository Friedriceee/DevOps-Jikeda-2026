import http from './http'

export function registerCustomer(payload) {
  return http.post('/auth/customer/register', payload)
}

export function registerMerchant(payload) {
  return http.post('/merchant/register', payload)
}

export function registerRider(payload) {
  return http.post('/rider/register', payload)
}

export function login(payload) {
  return http.post('/auth/login', payload)
}
