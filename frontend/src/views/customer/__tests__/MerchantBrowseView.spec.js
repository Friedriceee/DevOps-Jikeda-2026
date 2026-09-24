import { flushPromises, shallowMount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MerchantBrowseView from '@/views/customer/MerchantBrowseView.vue'

const { listMerchants, listMerchantMenu } = vi.hoisted(() => ({
  listMerchants: vi.fn(),
  listMerchantMenu: vi.fn(),
}))

vi.mock('@/api/customer', () => ({
  listMerchants,
  listMerchantMenu,
}))

vi.mock('element-plus', () => ({
  ElMessage: { info: vi.fn() },
}))

const stubs = {
  'el-alert': { template: '<div><slot /></div>' },
  'el-button': { template: '<button><slot /></button>' },
  'el-card': { template: '<div><slot /><slot name="header" /></div>' },
  'el-col': { template: '<div><slot /></div>' },
  'el-empty': { template: '<div><slot /></div>' },
  'el-row': { template: '<div><slot /></div>' },
  'el-skeleton': { template: '<div />' },
  'el-tag': { template: '<span><slot /></span>' },
}

describe('MerchantBrowseView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    listMerchants.mockResolvedValue([
      { id: 1, name: 'Demo Merchant', address: 'Central', openingHours: '10:00-22:00' },
    ])
    listMerchantMenu.mockResolvedValue([
      { id: 11, name: 'Available Noodles', price: 12, inventory: 4, isActive: true },
      { id: 12, name: 'Sold Out Rice', price: 16, inventory: 0, isActive: true },
      { id: 13, name: 'Inactive Soup', price: 10, inventory: 5, isActive: false },
    ])
  })

  it('loads a merchant menu and hides inactive dishes', async () => {
    const wrapper = shallowMount(MerchantBrowseView, { global: { stubs } })
    await flushPromises()

    expect(listMerchants).toHaveBeenCalledOnce()
    expect(listMerchantMenu).toHaveBeenCalledWith(1)
    expect(wrapper.text()).toContain('Available Noodles')
    expect(wrapper.text()).toContain('Sold Out Rice')
    expect(wrapper.text()).not.toContain('Inactive Soup')
  })
})
