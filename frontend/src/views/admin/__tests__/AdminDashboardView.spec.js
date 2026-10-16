import { flushPromises, shallowMount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AdminDashboardView from '@/views/admin/AdminDashboardView.vue'

const mocks = vi.hoisted(() => ({
  listAdminAccounts: vi.fn(), listAdminCoupons: vi.fn(), listAdminOrders: vi.fn(), listAuditLogs: vi.fn(),
  setAdminAccountRole: vi.fn(), setAdminAccountStatus: vi.fn(), setAdminCouponStatus: vi.fn(),
  createAdminCoupon: vi.fn(), updateAdminCoupon: vi.fn(), listCouponAllocations: vi.fn(), issueCoupon: vi.fn(), success: vi.fn(),
}))

vi.mock('@/api/admin', () => mocks)
vi.mock('element-plus', () => ({ ElMessage: { success: mocks.success } }))

const stubs = {
  'el-button': { template: '<button><slot/></button>' },
  'el-tabs': { template: '<div><slot/></div>' },
  'el-tab-pane': { props: ['label'], template: '<section><h2>{{ label }}</h2><slot/></section>' },
  'el-table': { template: '<div><slot/></div>' },
  'el-table-column': { template: '<div />' },
  'el-select': { template: '<select><slot/></select>' },
  'el-option': { template: '<option />' },
  'el-input': { template: '<input />' },
  'el-tag': { template: '<span><slot/></span>' },
  'el-alert': { props: ['title'], template: '<p>{{ title }}</p>' },
  'el-dialog': { template: '<div><slot/><slot name="footer"/></div>' },
  'el-form': { template: '<form><slot/></form>' },
  'el-form-item': { props: ['label'], template: '<label>{{ label }}<slot/></label>' },
  'el-input-number': { template: '<input />' },
  'el-date-picker': { template: '<input />' },
}

describe('AdminDashboardView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.listAdminAccounts.mockResolvedValue({ items: [], totalCount: 0 })
    mocks.listAdminCoupons.mockResolvedValue([])
    mocks.listCouponAllocations.mockResolvedValue([])
    mocks.listAdminOrders.mockResolvedValue({ items: [], totalCount: 0 })
    mocks.listAuditLogs.mockResolvedValue({ items: [], totalCount: 0 })
  })

  it('loads all administrator resources and presents the console in English', async () => {
    const wrapper = shallowMount(AdminDashboardView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()

    expect(mocks.listAdminAccounts).toHaveBeenCalledOnce()
    expect(mocks.listAdminCoupons).toHaveBeenCalledOnce()
    expect(mocks.listCouponAllocations).toHaveBeenCalledOnce()
    expect(mocks.listAdminOrders).toHaveBeenCalledOnce()
    expect(mocks.listAuditLogs).toHaveBeenCalledOnce()
    expect(wrapper.text()).toContain('Operations console')
    expect(wrapper.text()).toContain('Accounts')
    expect(wrapper.text()).toContain('Coupons')
    expect(wrapper.text()).toContain('Orders')
    expect(wrapper.text()).toContain('Audit logs')
    expect(wrapper.text()).toContain('Audit records are append-only')
  })
})
