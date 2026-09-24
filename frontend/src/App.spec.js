import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import App from '@/App.vue'
import { useAuthStore } from '@/stores/auth'

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  messageSuccess: vi.fn(),
}))

vi.mock('vue-router', () => ({ useRouter: () => ({ push: mocks.push }) }))
vi.mock('element-plus', () => ({ ElMessage: { success: mocks.messageSuccess } }))

const localStorageMock = {
  values: new Map(),
  getItem(key) { return this.values.has(key) ? this.values.get(key) : null },
  setItem(key, value) { this.values.set(key, String(value)) },
  removeItem(key) { this.values.delete(key) },
  clear() { this.values.clear() },
}

const stubs = {
  'el-container': { template: '<div><slot/></div>' },
  'el-header': { template: '<header><slot/></header>' },
  'el-main': { template: '<main><slot/></main>' },
  'el-button': {
    props: ['link'],
    template: '<button @click="$emit(\'click\', $event)"><slot/></button>',
  },
  'router-view': { template: '<div />' },
}

describe('App authentication controls', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    localStorageMock.clear()
    Object.defineProperty(window, 'localStorage', { value: localStorageMock, configurable: true })
  })

  it('offers login to a visitor and navigates to the login page', async () => {
    const wrapper = mount(App, { global: { stubs } })
    await wrapper.find('button').trigger('click')

    expect(wrapper.text()).toContain('登录')
    expect(mocks.push).toHaveBeenCalledWith({ name: 'login' })
  })

  it('shows the logged-in user and clears the session on logout', async () => {
    useAuthStore().setSession({ accessToken: 'jwt-token', role: 'Customer', displayName: 'Alice' })
    const wrapper = mount(App, { global: { stubs } })
    const buttons = wrapper.findAll('button')
    expect(wrapper.text()).toContain('Alice')
    expect(buttons.map((button) => button.text())).toContain('退出登录')

    await buttons[0].trigger('click')

    expect(useAuthStore().isAuthenticated).toBe(false)
    expect(mocks.push).toHaveBeenCalledWith({ name: 'home' })
    expect(mocks.messageSuccess).toHaveBeenCalledWith('已退出登录')
  })
})
