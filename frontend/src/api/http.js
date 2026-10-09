import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'
import { useAuthStore } from '@/stores/auth'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE ?? '/api',
  timeout: 10000,
})

export function addBearerToken(config, token) {
  const headers = config.headers ?? (config.headers = {})
  if (token) headers.Authorization = `Bearer ${token}`
  return config
}

export function isAnonymousAuthRequest(url = '') {
  return url === '/auth/login' || url === '/auth/customer/register'
}

export function unwrapApiResponse(response, notify = () => {}) {
  const body = response.data
  if (body && typeof body === 'object' && 'success' in body) {
    if (!body.success) {
      const message = body.message || 'Request failed.'
      notify(message)
      const error = new Error(message)
      error.userNotified = true
      throw error
    }
    return body.data
  }
  return body
}

export function handleApiError(error, { authStore, router, notify = () => {} }) {
  if (error.response?.status === 401) {
    authStore.clearSession()
    notify('Your session has expired. Please sign in again.')
    error.userNotified = true
    if (router.currentRoute.value.name !== 'login') {
      router.push({
        name: 'login',
        query: { redirect: router.currentRoute.value.fullPath },
      })
    }
  } else {
    notify(error.response?.data?.message || 'Network error. Please try again.')
    error.userNotified = true
  }
  return Promise.reject(error)
}

// 请求拦截：为需要登录的接口自动附加 JWT
http.interceptors.request.use((config) => {
  const authStore = useAuthStore()
  return isAnonymousAuthRequest(config.url) ? config : addBearerToken(config, authStore.token)
})

// 响应拦截：拆掉统一响应外壳，失败时弹提示
http.interceptors.response.use(
  (response) => unwrapApiResponse(response, (message) => ElMessage.error(message)),
  (error) => handleApiError(error, {
    authStore: useAuthStore(),
    router,
    notify: (message) => ElMessage.error(message),
  }),
)

export default http
