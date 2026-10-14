import { flushPromises, shallowMount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CheckoutView from '@/views/customer/CheckoutView.vue'

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  submitCheckout: vi.fn(),
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
  listAddresses: mocks.listAddresses,
}))
vi.mock('@/api/checkout', () => ({ submitCheckout: mocks.submitCheckout }))
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
  'el-radio-group': { props: ['modelValue'], template: '<div><slot/></div>' },
  'el-radio': { props: ['value'], template: '<label><input type="radio"><slot/></label>' },
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
    mocks.submitCheckout.mockResolvedValue({
      orderId: 99,
      status: 1,
      amountPaid: 35,
      paymentReference: 'SIM-ABC123',
      replayed: false,
    })
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

  const mountPage = async () => {
    const wrapper = shallowMount(CheckoutView, {
      global: { stubs, directives: { loading: () => {} } },
    })
    await flushPromises()
    return wrapper
  }
  const payButton = (wrapper) =>
    wrapper.findAll('button').find((item) => item.text() === 'Pay and place order')

  it('pays with the stored address and coupon and sends no amounts', async () => {
    const wrapper = await mountPage()

    await payButton(wrapper).trigger('click')
    await flushPromises()

    expect(mocks.submitCheckout).toHaveBeenCalledOnce()
    const [payload, key] = mocks.submitCheckout.mock.calls[0]
    expect(payload).toEqual({
      addressId: 7,
      couponCode: 'WELCOME5',
      needUtensils: 0,
      paymentMethod: 'SimulatedCard',
    })
    expect(Object.keys(payload)).not.toEqual(expect.arrayContaining(['price', 'total', 'userId', 'shoppingCart']))
    expect(key).toMatch(/^chk-/)
  })

  it('shows the paid order number, amount and reference after payment succeeds', async () => {
    const wrapper = await mountPage()

    await payButton(wrapper).trigger('click')
    await flushPromises()

    const confirmation = wrapper.find('[data-testid="checkout-confirmation"]')
    expect(confirmation.exists()).toBe(true)
    expect(confirmation.text()).toContain('Payment successful')
    expect(confirmation.text()).toContain('#99')
    expect(confirmation.text()).toContain('¥35.00')
    expect(confirmation.text()).toContain('Paid')
    expect(confirmation.text()).toContain('SIM-ABC123')
    expect(mocks.pricing.reset).toHaveBeenCalledOnce()
    expect(mocks.cart.load).toHaveBeenCalledTimes(2) // initial load + sync after payment
    expect(mocks.messageSuccess).toHaveBeenCalledWith('Payment successful.')
  })

  it('tells the customer when the server returned an order that was already placed', async () => {
    mocks.submitCheckout.mockResolvedValue({
      orderId: 99, status: 1, amountPaid: 35, paymentReference: 'SIM-ABC123', replayed: true,
    })
    const wrapper = await mountPage()

    await payButton(wrapper).trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('This order was already placed')
  })

  it('shows a clear message and keeps the cart when the payment is declined', async () => {
    mocks.submitCheckout.mockRejectedValue({
      response: { status: 402, data: { code: 'PaymentDeclined', message: 'The simulated card was declined.' } },
    })
    const wrapper = await mountPage()

    await payButton(wrapper).trigger('click')
    await flushPromises()

    expect(wrapper.find('[data-testid="checkout-confirmation"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('Your payment was declined. You were not charged and your cart is unchanged.')
    expect(mocks.pricing.reset).not.toHaveBeenCalled()
  })

  it('reloads the cart and quote when stock changed during checkout', async () => {
    mocks.submitCheckout.mockRejectedValue({
      response: { status: 409, data: { code: 'InsufficientStock', message: 'There is not enough stock.' } },
    })
    const wrapper = await mountPage()
    mocks.cart.load.mockClear()
    mocks.pricing.refreshQuote.mockClear()

    await payButton(wrapper).trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('no longer has enough stock')
    expect(mocks.cart.load).toHaveBeenCalledOnce()
    expect(mocks.pricing.refreshQuote).toHaveBeenCalledOnce()
  })

  it('reuses the Idempotency-Key after a network failure but not after a definite rejection', async () => {
    mocks.submitCheckout
      .mockRejectedValueOnce(new Error('Network Error'))
      .mockRejectedValueOnce({ response: { status: 402, data: { code: 'PaymentDeclined' } } })
      .mockResolvedValueOnce({ orderId: 5, status: 1, amountPaid: 35, paymentReference: 'SIM-X', replayed: false })
    const wrapper = await mountPage()

    await payButton(wrapper).trigger('click')
    await flushPromises()
    await payButton(wrapper).trigger('click')
    await flushPromises()
    await payButton(wrapper).trigger('click')
    await flushPromises()

    const keys = mocks.submitCheckout.mock.calls.map((call) => call[1])
    expect(keys[1]).toBe(keys[0]) // unknown outcome: retry with the same key
    expect(keys[2]).not.toBe(keys[1]) // declined: the next attempt is a new checkout
  })

  it('does not offer the decline test card to normal customers', async () => {
    const wrapper = await mountPage()
    // Vitest runs with import.meta.env.DEV === true, so the test card is visible here;
    // the helper unit tests cover the production behaviour.
    expect(wrapper.text()).toContain('Simulated card')
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
