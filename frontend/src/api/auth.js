import http from './http'

export function login(payload) {
  return http.post('/auth/login', payload)
}

export function registerCustomer(payload) {
  return http.post('/auth/customer/register', payload)
}
