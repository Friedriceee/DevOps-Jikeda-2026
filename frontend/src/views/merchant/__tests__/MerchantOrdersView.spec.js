import { flushPromises, shallowMount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MerchantOrdersView from '@/views/merchant/MerchantOrdersView.vue'

const mocks = vi.hoisted(() => ({
  listMerchantOrders: vi.fn(),
  transitionMerchantOrder: vi.fn(),
  messageSuccess: vi.fn(),
  prompt: vi.fn(),
}))

vi.mock('@/api/merchant', () => ({
  listMerchantOrders: mocks.listMerchantOrders,
  transitionMerchantOrder: mocks.transitionMerchantOrder,
}))
vi.mock('element-plus', () => ({
  ElMessage: { success: mocks.messageSuccess },
  ElMessageBox: { prompt: mocks.prompt },
}))

const confirmedOrder = {
  orderId: 42,
  merchantId: 5,
  customerId: 9,
  orderTimestamp: '2026-10-10T08:00:00Z',
  status: 'Confirmed',
  overallStatus: 'Paid',
  rejectionReason: null,
  merchantSubtotal: 30,
  itemCount: 2,
  items: [{ dishId: 7, dishName: 'Green Curry', unitPrice: 15, quantity: 2, lineTotal: 30 }],
}

const stubs = {
  'el-alert': { props: ['title'], template: '<div>{{ title }}</div>' },
  'el-button': {
    props: ['loading'],
    emits: ['click'],
    template: '<button :disabled="loading" @click="$emit(\'click\')"><slot/></button>',
  },
  'el-card': { template: '<section><slot name="header"/><slot/></section>' },
  'el-empty': { props: ['description'], template: '<div>{{ description }}</div>' },
  'el-table': { template: '<div><slot/></div>' },
  'el-table-column': { template: '<div />' },
  'el-tag': { template: '<span><slot/></span>' },
}

describe('MerchantOrdersView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.listMerchantOrders.mockResolvedValue([{ ...confirmedOrder }])
  })

  it('loads confirmed orders and displays English fulfilment controls', async () => {
    const wrapper = shallowMount(MerchantOrdersView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()

    expect(mocks.listMerchantOrders).toHaveBeenCalledOnce()
    expect(wrapper.text()).toContain('Incoming Orders')
    expect(wrapper.text()).toContain('Order #42')
    expect(wrapper.text()).toContain('Accept order')
    expect(wrapper.text()).toContain('Reject')
  })

  it('accepts a confirmed order and replaces it with the server response', async () => {
    mocks.transitionMerchantOrder.mockResolvedValue({ ...confirmedOrder, status: 'Accepted' })
    const wrapper = shallowMount(MerchantOrdersView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()

    await wrapper.findAll('button').find((button) => button.text() === 'Accept order').trigger('click')
    await flushPromises()

    expect(mocks.transitionMerchantOrder).toHaveBeenCalledWith(42, 'Accept', null)
    expect(wrapper.text()).toContain('Start preparing')
    expect(mocks.messageSuccess).toHaveBeenCalledWith('Order accepted.')
  })

  it('requires and sends a reason when rejecting an order', async () => {
    mocks.prompt.mockResolvedValue({ value: 'Ingredient unavailable.' })
    mocks.transitionMerchantOrder.mockResolvedValue({
      ...confirmedOrder,
      status: 'Rejected',
      rejectionReason: 'Ingredient unavailable.',
    })
    const wrapper = shallowMount(MerchantOrdersView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()

    await wrapper.findAll('button').find((button) => button.text() === 'Reject').trigger('click')
    await flushPromises()

    expect(mocks.transitionMerchantOrder).toHaveBeenCalledWith(42, 'Reject', 'Ingredient unavailable.')
    expect(wrapper.text()).toContain('Ingredient unavailable.')
  })

  it('distinguishes another restaurant still preparing from a courier wait', async () => {
    mocks.listMerchantOrders.mockResolvedValue([
      { ...confirmedOrder, orderId: 43, status: 'ReadyForDelivery', overallStatus: 'Paid' },
      { ...confirmedOrder, orderId: 44, status: 'ReadyForDelivery', overallStatus: 'ReadyForDelivery' },
    ])
    const wrapper = shallowMount(MerchantOrdersView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()

    expect(wrapper.text()).toContain('Waiting for other restaurants')
    expect(wrapper.text()).toContain('Waiting for a courier')
  })
})
