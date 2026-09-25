import { beforeEach, describe, expect, it, vi } from 'vitest'

const http = vi.hoisted(() => ({ post: vi.fn() }))

vi.mock('@/api/http', () => ({ default: http }))

import { login, registerCustomer } from '@/api/auth'

describe('authentication API', () => {
  beforeEach(() => vi.clearAllMocks())

  it('posts the username and password to the login endpoint', async () => {
    const payload = { username: 'alice', password: 'secret' }
    http.post.mockResolvedValueOnce({ accessToken: 'jwt-token' })

    await expect(login(payload)).resolves.toEqual({ accessToken: 'jwt-token' })
    expect(http.post).toHaveBeenCalledWith('/auth/login', payload)
  })

  it('posts customer details to the registration endpoint', async () => {
    const payload = {
      username: 'alice',
      password: 'strong-password',
      displayName: 'Alice',
      phoneNumber: '81234567',
    }
    http.post.mockResolvedValueOnce({ accountId: 7, customerId: 12 })

    await expect(registerCustomer(payload)).resolves.toEqual({ accountId: 7, customerId: 12 })
    expect(http.post).toHaveBeenCalledWith('/auth/customer/register', payload)
  })
})
