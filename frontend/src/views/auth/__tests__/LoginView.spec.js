import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import LoginView from '@/views/auth/LoginView.vue'
import { useAuthStore } from '@/stores/auth'

const mocks = vi.hoisted(() => ({
  login: vi.fn(),
  replace: vi.fn(),
  push: vi.fn(),
  messageSuccess: vi.fn(),
  messageWarning: vi.fn(),
  messageError: vi.fn(),
  route: { query: {} },
}))

vi.mock('@/api/auth', () => ({ login: mocks.login }))
vi.mock('vue-router', () => ({
  useRoute: () => mocks.route,
  useRouter: () => ({ replace: mocks.replace, push: mocks.push }),
}))
vi.mock('element-plus', () => ({
  ElMessage: {
    success: mocks.messageSuccess,
    warning: mocks.messageWarning,
    error: mocks.messageError,
  },
}))

const stubs = {
  'el-card': { template: '<section><slot name="header"/><slot/></section>' },
  'el-form': {
    emits: ['submit'],
    template: '<form @submit="$emit(\'submit\', $event)"><slot/></form>',
  },
  'el-form-item': { template: '<label><slot/></label>' },
  'el-input': {
    props: ['modelValue', 'type', 'autocomplete', 'placeholder'],
    emits: ['update:modelValue'],
    template: '<input :type="type || \'text\'" :value="modelValue" :placeholder="placeholder" @input="$emit(\'update:modelValue\', $event.target.value)">',
  },
  'el-button': {
    props: ['nativeType', 'loading'],
    template: '<button :type="nativeType || \'button\'" :disabled="loading"><slot/></button>',
  },
}

const localStorageMock = {
  values: new Map(),
  getItem(key) {
    return this.values.has(key) ? this.values.get(key) : null
  },
  setItem(key, value) {
    this.values.set(key, String(value))
  },
  removeItem(key) {
    this.values.delete(key)
  },
  clear() {
    this.values.clear()
  },
}

describe('LoginView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    mocks.route.query = {}
    localStorageMock.clear()
    Object.defineProperty(window, 'localStorage', { value: localStorageMock, configurable: true })
  })

  it('requires both account and password before calling the API', async () => {
    const wrapper = mount(LoginView, { global: { stubs } })
    await wrapper.find('form').trigger('submit')

    expect(mocks.login).not.toHaveBeenCalled()
    expect(mocks.messageWarning).toHaveBeenCalledWith('请输入用户名')
  })

  it('links visitors to customer registration', async () => {
    const wrapper = mount(LoginView, { global: { stubs } })
    const registerButton = wrapper.findAll('button').find((button) => button.text() === '立即注册')

    await registerButton.trigger('click')

    expect(mocks.push).toHaveBeenCalledWith({ name: 'register' })
  })

  it('logs in, saves the session, and returns to the requested page', async () => {
    mocks.route.query = { redirect: '/customer/cart' }
    mocks.login.mockResolvedValueOnce({
      accessToken: 'jwt-token',
      accountId: 7,
      role: 'Customer',
      profileId: 12,
    })
    const wrapper = mount(LoginView, { global: { stubs } })
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('  alice  ')
    await inputs[1].setValue('secret')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mocks.login).toHaveBeenCalledWith({ username: 'alice', password: 'secret' })
    expect(useAuthStore().token).toBe('jwt-token')
    expect(mocks.replace).toHaveBeenCalledWith('/customer/cart')
    expect(mocks.messageSuccess).toHaveBeenCalledWith('登录成功')
  })

  it('uses the merchant browsing page as the default destination', async () => {
    mocks.login.mockResolvedValueOnce({ accessToken: 'jwt-token', role: 'Customer' })
    const wrapper = mount(LoginView, { global: { stubs } })
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('alice')
    await inputs[1].setValue('secret')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mocks.replace).toHaveBeenCalledWith('/customer/merchants')
  })

  it('uses the merchant management page as the default destination for merchants', async () => {
    mocks.login.mockResolvedValueOnce({ accessToken: 'merchant-token', role: 'Merchant' })
    const wrapper = mount(LoginView, { global: { stubs } })
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('merchant')
    await inputs[1].setValue('secret')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mocks.replace).toHaveBeenCalledWith('/merchant/dishes')
  })

  it('keeps the form usable when login fails', async () => {
    mocks.login.mockRejectedValueOnce(new Error('Invalid credentials'))
    const wrapper = mount(LoginView, { global: { stubs } })
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('alice')
    await inputs[1].setValue('wrong-password')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(useAuthStore().isAuthenticated).toBe(false)
    expect(mocks.replace).not.toHaveBeenCalled()
    expect(mocks.messageError).toHaveBeenCalledWith('登录失败，请稍后重试')
    expect(wrapper.find('button').attributes('disabled')).toBeUndefined()
  })
})
