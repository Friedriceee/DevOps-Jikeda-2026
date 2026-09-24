import http from './http'

export function login(payload) {
  return http.post('/auth/login', payload)
}
