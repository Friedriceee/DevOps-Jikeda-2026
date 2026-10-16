import { beforeEach, describe, expect, it, vi } from 'vitest'

const http = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn(), put: vi.fn() }))
vi.mock('@/api/http', () => ({ default: http }))

import { createAdminCoupon, issueCoupon, listAdminAccounts, listAdminOrders, listAuditLogs, setAdminAccountStatus } from '@/api/admin'

describe('administrator API', () => {
  beforeEach(() => vi.clearAllMocks())

  it('queries administrator resources with filters', async () => {
    await listAdminAccounts({ role: 'Customer' })
    await listAdminOrders({ status: 'Paid' })
    await listAuditLogs({ actor: 'admin' })
    expect(http.get).toHaveBeenNthCalledWith(1, '/admin/accounts', { params: { role: 'Customer' } })
    expect(http.get).toHaveBeenNthCalledWith(2, '/admin/orders', { params: { status: 'Paid' } })
    expect(http.get).toHaveBeenNthCalledWith(3, '/admin/audit-logs', { params: { actor: 'admin' } })
  })

  it('sends account and coupon mutations to the protected API', async () => {
    await setAdminAccountStatus(7, false)
    await createAdminCoupon({ code: 'SAVE5' })
    await issueCoupon(3, 7, 2)
    expect(http.put).toHaveBeenCalledWith('/admin/accounts/7/status', { isActive: false })
    expect(http.post).toHaveBeenCalledWith('/admin/coupons', { code: 'SAVE5' })
    expect(http.post).toHaveBeenCalledWith('/admin/coupons/3/issue', { userId: 7, quantity: 2 })
  })
})
