import { flushPromises, shallowMount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CheckoutView from '@/views/customer/CheckoutView.vue'

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  createOrder: vi.fn(),
  createAddress: vi.fn(),
  listAddresses: vi.fn(),
  messageSuccess: vi.fn(),
  messageWarning: vi.fn(),
  messageError: vi.fn(),
  cart: {
    items: [{
      id: 1,
      merchantId: 1,
      merchantName: 'Demo Merchant',
      dishId: 10,
      name: 'Noodles',
      price: 20,
      dishNum: 2,
    }],
    load: vi.fn(),
    clear: vi.fn(),
  },
  pricing: {
    coupons: [{
      id: 1,
      code: 'WELCOME5',
      name: 'Welcome ¥5 off',
      minimumSpend: 20,
      discountAmount: 5,
      expiresAtUtc: '2099-12-31T23:59:59Z',
    }],
    quote: {
      subtotal: 40,
      merchantDiscount: 5,
      couponDiscount: 5,
      deliveryFee: 5,
      total: 35,
      selectedCoupon: {
        id: 1,
        code: 'WELCOME5',
        expiresAtUtc: '2099-12-31T23:59:59Z',
      },
    },
    selectedCouponCode: 'WELCOME5',
    loading: false,
    error: '',
    hasQuote: true,
    loadCoupons: vi.fn(),
    refreshQuote: vi.fn(),
    reset: vi.fn(),
  },
}))

vi.mock('vue-router', () => ({ useRouter: () => ({ push: mocks.push }) }))
vi.mock('@/stores/auth', () => ({
  useAuthStore: () => ({ user: { profileId: 1 } }),
}))
vi.mock('@/stores/cart', () => ({ useCartStore: () => mocks.cart }))
vi.mock('@/stores/pricing', () => ({ usePricingStore: () => mocks.pricing }))
vi.mock('@/api/order', () => ({
  createAddress: mocks.createAddress,
  createOrder: mocks.createOrder,
  listAddresses: mocks.listAddresses,
}))
vi.mock('element-plus', () => ({
  ElMessage: {
    success: mocks.messageSuccess,
    warning: mocks.messageWarning,
    error: mocks.messageError,
  },
}))

const stubs = {
  'el-alert': { props: ['title'], template: '<div class="alert">{{ title }}</div>' },
  'el-button': {
    props: ['disabled', 'loading'],
    emits: ['click'],
    template: '<button :disabled="disabled || loading" @click="$emit(\'click\')"><slot/></button>',
  },
  'el-card': { template: '<section><slot name="header"/><slot/></section>' },
  'el-checkbox': {
    props: ['modelValue'],
    template: '<label><input type="checkbox"><slot/></label>',
  },
  'el-empty': { props: ['description'], template: '<div>{{ description }}<slot/></div>' },
  'el-form': { template: '<form><slot/></form>' },
  'el-form-item': { props: ['label'], template: '<label>{{ label }}<slot/></label>' },
  'el-input': {
    props: ['modelValue', 'placeholder'],
    emits: ['update:modelValue'],
    template: '<input :value="modelValue" :placeholder="placeholder" @input="$emit(\'update:modelValue\', $event.target.value)">',
  },
  'el-option': {
    props: ['label', 'value'],
    template: '<option :value="value">{{ label }}</option>',
  },
  'el-select': {
    props: ['modelValue'],
    emits: ['change', 'clear', 'update:modelValue'],
    template: '<select :value="modelValue" @change="$emit(\'change\', $event.target.value)"><slot/></select>',
  },
  'el-skeleton': { template: '<div />' },
  'el-table': { template: '<div><slot/></div>' },
  'el-table-column': { template: '<div><slot :row="{}"/></div>' },
}

describe('CheckoutView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.cart.items = [{
      id: 1,
      merchantId: 1,
      merchantName: 'Demo Merchant',
      dishId: 10,
      name: 'Noodles',
      price: 20,
      dishNum: 2,
    }]
    mocks.pricing.error = ''
    mocks.pricing.hasQuote = true
    mocks.pricing.loading = false
    mocks.pricing.selectedCouponCode = 'WELCOME5'
    mocks.cart.load.mockResolvedValue()
    mocks.cart.clear.mockResolvedValue()
    mocks.pricing.loadCoupons.mockResolvedValue()
    mocks.pricing.refreshQuote.mockResolvedValue(mocks.pricing.quote)
    mocks.listAddresses.mockResolvedValue([{
      id: 7,
      contactName: 'Alice',
      phoneNumber: '13800000000',
      address: '12 Main Street',
      houseNumber: '#08-01',
    }])
    mocks.createOrder.mockResolvedValue({ id: 99 })
    mocks.createAddress.mockResolvedValue({
      id: 8,
      contactName: 'Bob',
      phoneNumber: '13900000000',
      address: '20 River Road',
      houseNumber: null,
    })
  })

  it('loads cart, coupons, addresses and an initial server quote', async () => {
    const wrapper = shallowMount(CheckoutView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()

    expect(mocks.cart.load).toHaveBeenCalledOnce()
    expect(mocks.pricing.loadCoupons).toHaveBeenCalledOnce()
    expect(mocks.listAddresses).toHaveBeenCalledWith(1)
    expect(mocks.pricing.refreshQuote).toHaveBeenCalledWith()
    expect(wrapper.text()).toContain('Review and place your order')
    expect(wrapper.text()).toContain('Price summary')
    expect(wrapper.text()).toContain('¥35.00')
  })

  it('requests a new quote when the customer selects a coupon', async () => {
    const wrapper = shallowMount(CheckoutView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()

    await wrapper.findAll('select')[1].setValue('WELCOME5')

    expect(mocks.pricing.refreshQuote).toHaveBeenLastCalledWith('WELCOME5')
  })

  it('places the order using only amounts returned by the pricing API', async () => {
    const wrapper = shallowMount(CheckoutView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()

    const button = wrapper.findAll('button').find((item) => item.text() === 'Place order')
    await button.trigger('click')
    await flushPromises()

    expect(mocks.createOrder).toHaveBeenCalledWith(expect.objectContaining({
      addressId: 7,
      price: 35,
      riderPrice: 5,
      couponId: 1,
      expirationDate: '2099-12-31T23:59:59Z',
    }))
    expect(mocks.cart.clear).toHaveBeenCalledOnce()
    expect(mocks.pricing.reset).toHaveBeenCalledOnce()
    expect(mocks.push).toHaveBeenCalledWith({ name: 'user-orders' })
  })

  it('adds and selects a delivery address with English validation', async () => {
    mocks.listAddresses.mockResolvedValueOnce([])
    const wrapper = shallowMount(CheckoutView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()

    await wrapper.findAll('button').find((item) => item.text() === 'Add a delivery address').trigger('click')
    const inputs = wrapper.findAll('input').filter((input) => input.attributes('type') !== 'checkbox')
    await inputs[0].setValue('Bob')
    await inputs[1].setValue('13900000000')
    await inputs[2].setValue('20 River Road')
    await wrapper.findAll('button').find((item) => item.text() === 'Save address').trigger('click')
    await flushPromises()

    expect(mocks.createAddress).toHaveBeenCalledWith({
      contactName: 'Bob',
      phoneNumber: '13900000000',
      address: '20 River Road',
      houseNumber: null,
    })
    expect(mocks.messageSuccess).toHaveBeenCalledWith('Delivery address added.')
  })
})
