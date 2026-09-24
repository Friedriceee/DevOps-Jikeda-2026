import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'
import { useAuthStore } from '@/stores/auth'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE ?? '/api',
  timeout: 10000,
})

// 请求拦截：为需要登录的接口自动附加 JWT
http.interceptors.request.use((config) => {
  const authStore = useAuthStore()
  if (authStore.token) {
    config.headers.Authorization = `Bearer ${authStore.token}`
  }
  return config
})

// 响应拦截：拆掉统一响应外壳，失败时弹提示
http.interceptors.response.use(
  (response) => {
    const body = response.data
    if (body && typeof body === 'object' && 'success' in body) {
      if (!body.success) {
        ElMessage.error(body.message || '请求失败')
        return Promise.reject(new Error(body.message || '请求失败'))
      }
      return body.data
    }
    return body
  },
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore().clearSession()
      ElMessage.error('登录已失效，请重新登录')
      if (router.currentRoute.value.name !== 'login') {
        router.push({
          name: 'login',
          query: { redirect: router.currentRoute.value.fullPath },
        })
      }
    }
    const msg = error.response?.data?.message || '网络错误，请稍后重试'
    if (error.response?.status !== 401) ElMessage.error(msg)
    return Promise.reject(error)
  },
)

export default http
