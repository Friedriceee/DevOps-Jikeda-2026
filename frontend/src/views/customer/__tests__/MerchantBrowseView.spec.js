import { flushPromises, shallowMount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MerchantBrowseView from '@/views/customer/MerchantBrowseView.vue'

const { listMerchants, listMerchantMenu, messageInfo } = vi.hoisted(() => ({
  listMerchants: vi.fn(),
  listMerchantMenu: vi.fn(),
  messageInfo: vi.fn(),
}))

vi.mock('@/api/customer', () => ({
  listMerchants,
  listMerchantMenu,
}))

vi.mock('element-plus', () => ({
  ElMessage: { info: messageInfo },
}))

const stubs = {
  'el-alert': {
    props: ['title'],
    template: '<div class="alert">{{ title }}<slot /></div>',
  },
  'el-button': {
    props: ['disabled', 'loading', 'nativeType'],
    template: '<button :disabled="disabled" :type="nativeType || \'button\'"><slot /></button>',
  },
  'el-card': { template: '<div><slot /><slot name="header" /></div>' },
  'el-col': { template: '<div><slot /></div>' },
  'el-empty': { props: ['description'], template: '<div>{{ description }}</div>' },
  'el-row': { template: '<div><slot /></div>' },
  'el-skeleton': { template: '<div />' },
  'el-tag': { template: '<span><slot /></span>' },
}

describe('MerchantBrowseView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    listMerchants.mockResolvedValue([
      {
        id: 1,
        name: 'Demo Merchant',
        address: 'Central',
        openingHours: '10:00-22:00',
        promotionInfo: 'Spend $20, save $3',
      },
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
    expect(wrapper.text()).toContain('Spend $20, save $3')
  })

  it('disables sold-out dishes and only handles add-to-cart for available dishes', async () => {
    const wrapper = shallowMount(MerchantBrowseView, { global: { stubs } })
    await flushPromises()

    const addButtons = wrapper.findAll('button').filter((button) => button.text().includes('加入购物车'))
    expect(addButtons).toHaveLength(2)
    expect(addButtons[0].attributes('disabled')).toBeUndefined()
    expect(addButtons[1].attributes('disabled')).toBeDefined()

    await addButtons[0].trigger('click')
    await addButtons[1].trigger('click')
    expect(messageInfo).toHaveBeenCalledOnce()
  })

  it('shows a useful message when the menu request fails', async () => {
    listMerchantMenu.mockRejectedValueOnce(new Error('unavailable'))
    const wrapper = shallowMount(MerchantBrowseView, { global: { stubs } })
    await flushPromises()

    expect(wrapper.text()).toContain('菜单加载失败，请稍后重试')
  })

  it('shows an empty state when there are no merchants', async () => {
    listMerchants.mockResolvedValueOnce([])
    const wrapper = shallowMount(MerchantBrowseView, { global: { stubs } })
    await flushPromises()

    expect(wrapper.text()).toContain('暂无商家')
    expect(listMerchantMenu).not.toHaveBeenCalled()
  })

  it('loads the selected merchant menu when the customer switches merchants', async () => {
    listMerchants.mockResolvedValueOnce([
      { id: 1, name: 'First Merchant' },
      { id: 2, name: 'Second Merchant' },
    ])
    listMerchantMenu
      .mockResolvedValueOnce([{ id: 21, name: 'First Dish', price: 8, inventory: 2 }])
      .mockResolvedValueOnce([{ id: 22, name: 'Second Dish', price: 9, inventory: 3 }])

    const wrapper = shallowMount(MerchantBrowseView, { global: { stubs } })
    await flushPromises()
    const secondMerchant = wrapper.findAll('button').find((button) => button.text().includes('Second Merchant'))
    await secondMerchant.trigger('click')
    await flushPromises()

    expect(listMerchantMenu).toHaveBeenNthCalledWith(2, 2)
    expect(wrapper.text()).toContain('Second Dish')
    expect(wrapper.text()).not.toContain('First Dish')
  })
})
