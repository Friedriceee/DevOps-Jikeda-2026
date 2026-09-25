import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => {
  const authStore = { token: null, clearSession: vi.fn() }
  const router = {
    currentRoute: { value: { name: 'customer-cart', fullPath: '/customer/cart' } },
    push: vi.fn(),
  }
  const http = {
    interceptors: {
      request: { use: vi.fn() },
      response: { use: vi.fn() },
    },
  }
  return {
    authStore,
    router,
    http,
    axiosCreate: vi.fn(() => http),
    useAuthStore: vi.fn(() => authStore),
    notifyError: vi.fn(),
  }
})

vi.mock('axios', () => ({ default: { create: mocks.axiosCreate } }))
vi.mock('element-plus', () => ({ ElMessage: { error: mocks.notifyError } }))
vi.mock('@/router', () => ({ default: mocks.router }))
vi.mock('@/stores/auth', () => ({ useAuthStore: mocks.useAuthStore }))

import {
  addBearerToken,
  handleApiError,
  isAnonymousAuthRequest,
  unwrapApiResponse,
} from '@/api/http'

const requestInterceptor = mocks.http.interceptors.request.use.mock.calls[0][0]
const responseInterceptors = mocks.http.interceptors.response.use.mock.calls[0]

describe('HTTP authentication and response handling', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.authStore.token = null
    mocks.router.currentRoute.value = { name: 'customer-cart', fullPath: '/customer/cart' }
  })

  it('adds a Bearer token and leaves anonymous requests unchanged', () => {
    const config = { headers: {} }
    expect(addBearerToken(config, 'abc')).toEqual({ headers: { Authorization: 'Bearer abc' } })

    const anonymousConfig = { headers: {} }
    expect(addBearerToken(anonymousConfig, null)).toEqual({ headers: {} })
  })

  it('does not attach an old token to login or registration requests', () => {
    expect(isAnonymousAuthRequest('/auth/login')).toBe(true)
    expect(isAnonymousAuthRequest('/auth/customer/register')).toBe(true)
    expect(isAnonymousAuthRequest('/cart')).toBe(false)
  })

  it('wires the auth store token into the actual Axios request interceptor', () => {
    mocks.authStore.token = 'interceptor-token'
    const config = requestInterceptor({ headers: {} })

    expect(config.headers.Authorization).toBe('Bearer interceptor-token')

    const anonymousConfig = requestInterceptor({ url: '/auth/customer/register', headers: {} })
    expect(anonymousConfig.headers.Authorization).toBeUndefined()
  })

  it('unwraps successful API envelopes and passes through raw responses', () => {
    expect(unwrapApiResponse({ data: { success: true, data: [{ id: 1 }] } })).toEqual([{ id: 1 }])
    expect(unwrapApiResponse({ data: [{ id: 2 }] })).toEqual([{ id: 2 }])
  })

  it('wires successful responses through the actual Axios response interceptor', () => {
    const [onSuccess] = responseInterceptors
    expect(onSuccess({ data: { success: true, data: { id: 9 } } })).toEqual({ id: 9 })
  })

  it('rejects failed API envelopes and displays their message', () => {
    expect(() => unwrapApiResponse(
      { data: { success: false, message: 'Invalid credentials' } },
      mocks.notifyError,
    )).toThrow('Invalid credentials')
    expect(mocks.notifyError).toHaveBeenCalledWith('Invalid credentials')
  })

  it('clears the session and redirects to login after HTTP 401', async () => {
    const error = { response: { status: 401 } }
    await expect(handleApiError(error, {
      authStore: mocks.authStore,
      router: mocks.router,
      notify: mocks.notifyError,
    })).rejects.toBe(error)

    expect(mocks.authStore.clearSession).toHaveBeenCalledOnce()
    expect(mocks.router.push).toHaveBeenCalledWith({
      name: 'login',
      query: { redirect: '/customer/cart' },
    })
    expect(mocks.notifyError).toHaveBeenCalledWith('Your session has expired. Please sign in again.')
  })

  it('shows other server errors without clearing the session', async () => {
    const error = { response: { status: 403, data: { message: 'Forbidden' } } }
    await expect(handleApiError(error, {
      authStore: mocks.authStore,
      router: mocks.router,
      notify: mocks.notifyError,
    })).rejects.toBe(error)

    expect(mocks.authStore.clearSession).not.toHaveBeenCalled()
    expect(mocks.router.push).not.toHaveBeenCalled()
    expect(mocks.notifyError).toHaveBeenCalledWith('Forbidden')
  })
})
