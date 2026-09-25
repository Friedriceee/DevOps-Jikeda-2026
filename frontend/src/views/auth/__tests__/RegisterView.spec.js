import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import RegisterView from '@/views/auth/RegisterView.vue'

const mocks = vi.hoisted(() => ({
  registerCustomer: vi.fn(),
  replace: vi.fn(),
  push: vi.fn(),
  messageSuccess: vi.fn(),
  messageWarning: vi.fn(),
  messageError: vi.fn(),
}))

vi.mock('@/api/auth', () => ({ registerCustomer: mocks.registerCustomer }))
vi.mock('vue-router', () => ({
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

describe('RegisterView', () => {
  beforeEach(() => vi.clearAllMocks())

  it('validates the form before calling the registration API', async () => {
    const wrapper = mount(RegisterView, { global: { stubs } })
    await wrapper.find('form').trigger('submit')

    expect(mocks.registerCustomer).not.toHaveBeenCalled()
    expect(mocks.messageWarning).toHaveBeenCalledWith('Username must be between 3 and 50 characters')
  })

  it('rejects mismatched passwords', async () => {
    const wrapper = mount(RegisterView, { global: { stubs } })
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('alice')
    await inputs[1].setValue('Alice')
    await inputs[2].setValue('81234567')
    await inputs[3].setValue('strong-password')
    await inputs[4].setValue('different-password')
    await wrapper.find('form').trigger('submit')

    expect(mocks.registerCustomer).not.toHaveBeenCalled()
    expect(mocks.messageWarning).toHaveBeenCalledWith('Passwords do not match')
  })

  it('registers a customer and returns to login with the username filled', async () => {
    mocks.registerCustomer.mockResolvedValueOnce({ accountId: 7, customerId: 12 })
    const wrapper = mount(RegisterView, { global: { stubs } })
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('  alice  ')
    await inputs[1].setValue(' Alice Customer ')
    await inputs[2].setValue(' 81234567 ')
    await inputs[3].setValue('strong-password')
    await inputs[4].setValue('strong-password')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mocks.registerCustomer).toHaveBeenCalledWith({
      username: 'alice',
      password: 'strong-password',
      displayName: 'Alice Customer',
      phoneNumber: '81234567',
    })
    expect(mocks.messageSuccess).toHaveBeenCalledWith(
      'Registration successful. Please log in with your new account.',
    )
    expect(mocks.replace).toHaveBeenCalledWith({
      name: 'login',
      query: { username: 'alice' },
    })
  })

  it('keeps the form usable when registration fails', async () => {
    mocks.registerCustomer.mockRejectedValueOnce(new Error('Registration failed'))
    const wrapper = mount(RegisterView, { global: { stubs } })
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('alice')
    await inputs[1].setValue('Alice')
    await inputs[2].setValue('81234567')
    await inputs[3].setValue('strong-password')
    await inputs[4].setValue('strong-password')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mocks.replace).not.toHaveBeenCalled()
    expect(mocks.messageError).toHaveBeenCalledWith('Registration failed. Please try again later.')
    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeUndefined()
  })
})
