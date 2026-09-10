import axios from 'axios'
import { ElMessage } from 'element-plus'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE ?? '/api',
  timeout: 10000,
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
    const msg = error.response?.data?.message || '网络错误，请稍后重试'
    ElMessage.error(msg)
    return Promise.reject(error)
  },
)

export default http
