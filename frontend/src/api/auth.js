import http from './http'

export function registerCustomer(payload) {
  return http.post('/auth/customer/register', payload)
}

export function login(payload) {
  return http.post('/auth/login', payload)
}
