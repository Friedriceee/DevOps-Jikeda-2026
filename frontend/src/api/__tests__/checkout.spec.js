import { beforeEach, describe, expect, it, vi } from 'vitest'

const http = vi.hoisted(() => ({ post: vi.fn() }))
vi.mock('@/api/http', () => ({ default: http }))

import { submitCheckout } from '@/api/checkout'

describe('checkout API', () => {
  beforeEach(() => vi.clearAllMocks())

  it('posts to /checkout with the Idempotency-Key header and silences the global toast', async () => {
    http.post.mockResolvedValue({ orderId: 1 })

    await submitCheckout({ addressId: 7 }, 'chk-key-123')

    expect(http.post).toHaveBeenCalledWith(
      '/checkout',
      { addressId: 7 },
      { headers: { 'Idempotency-Key': 'chk-key-123' }, silent: true },
    )
  })
})
