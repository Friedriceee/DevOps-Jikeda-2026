import { flushPromises, shallowMount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MerchantDishView from '@/views/merchant/MerchantDishView.vue'

const api = vi.hoisted(() => ({ createDish: vi.fn(), updateDish: vi.fn(), deleteDish: vi.fn(), listDishes: vi.fn(), success: vi.fn(), error: vi.fn(), validate: vi.fn() }))
const session = vi.hoisted(() => ({ role: null, user: null, merchantId: 1 }))
vi.mock('@/api/merchant', () => api)
vi.mock('@/stores/merchant', () => ({ useMerchantStore: () => ({ get merchantId() { return session.merchantId }, setMerchantId(id) { session.merchantId = id } }) }))
vi.mock('@/stores/auth', () => ({ useAuthStore: () => session }))
vi.mock('element-plus', () => ({ ElMessage: { success: api.success, error: api.error }, ElMessageBox: { confirm: vi.fn() } }))
const stubs = {
  'el-form': { setup(_props, { expose }) { expose({ validate: api.validate, clearValidate: vi.fn() }) }, template: '<form><slot /></form>' },
  'el-form-item': { props: ['label'], template: '<label>{{ label }}<slot /></label>' },
  'el-card': { template: '<section><slot name="header" /><slot /></section>' },
  'el-button': { props: ['disabled'], template: '<button :disabled="disabled"><slot /></button>' },
  'el-table': { template: '<div><slot /></div>' },
  'el-table-column': true, 'el-input': true, 'el-input-number': true, 'el-empty': true,
}
async function mountView() {
  const wrapper = shallowMount(MerchantDishView, { global: { stubs, directives: { loading: {} } } })
  await flushPromises()
  Object.assign(wrapper.vm.form, { name: 'English Meal', price: 10, inventory: 5 })
  return wrapper
}
async function submit(wrapper) {
  await wrapper.findAll('button').find(button => button.text() === 'Create Dish').trigger('click')
  await flushPromises()
}

describe('Member 3 menu management', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    Object.assign(session, { role: null, user: null, merchantId: 1 })
    api.validate.mockResolvedValue(true)
    api.listDishes.mockResolvedValue([])
    api.createDish.mockResolvedValue({ id: 1 })
    api.updateDish.mockResolvedValue({ id: 1 })
  })

  it.each(['Standard', 'Bundle', 'Promotional'])('creates %s through the menu API and resets the form', async (type) => {
    const wrapper = await mountView()
    await wrapper.find('select').setValue(type)
    if (type === 'Bundle') await wrapper.find('input[aria-label="Bundle Size"]').setValue('3')
    if (type === 'Promotional') {
      await wrapper.find('input[aria-label="Promotion Starts (UTC)"]').setValue('2026-10-10T00:00')
      await wrapper.find('input[aria-label="Promotion Ends (UTC)"]').setValue('2026-10-11T00:00')
    }
    await submit(wrapper)
    expect(api.createDish).toHaveBeenCalledWith(expect.objectContaining({ merchantId: 1, name: 'English Meal', itemType: type }))
    if (type === 'Bundle') expect(api.createDish.mock.calls[0][0].bundleSize).toBe(3)
    if (type === 'Promotional') expect(api.createDish.mock.calls[0][0].promotionStartsAtUtc).toBe('2026-10-10T00:00:00.000Z')
    expect(api.listDishes).toHaveBeenCalledTimes(2)
    expect(wrapper.vm.form.itemType).toBe('Standard')
    expect(wrapper.text()).not.toMatch(/[\u4e00-\u9fff]/)
  })

  it('does not submit invalid bundles or missing promotion dates', async () => {
    const wrapper = await mountView()
    await wrapper.find('select').setValue('Bundle')
    await wrapper.find('input[aria-label="Bundle Size"]').setValue('1')
    await submit(wrapper)
    expect(api.error).toHaveBeenLastCalledWith('Bundle size must be a whole number of at least 2.')
    await wrapper.find('select').setValue('Promotional')
    await submit(wrapper)
    expect(api.error).toHaveBeenLastCalledWith('Enter both promotion times in UTC.')
    expect(api.createDish).not.toHaveBeenCalled()
  })

  it('honours base form validation before sending a request', async () => {
    api.validate.mockResolvedValueOnce(false)
    const wrapper = await mountView()
    await submit(wrapper)
    expect(api.createDish).not.toHaveBeenCalled()
  })

  it('loads the authenticated merchant menu rather than the demo merchant', async () => {
    Object.assign(session, { role: 'Merchant', user: { profileId: 7 } })
    await mountView()
    expect(api.listDishes).toHaveBeenCalledWith(7)
  })

  it('edits promotional metadata and cancels without saving', async () => {
    const wrapper = await mountView()
    wrapper.vm.startEdit({ id: 9, name: 'Promo', price: 8, inventory: 4, itemType: 'Promotional', promotionStartsAtUtc: '2026-10-10T08:00:00+08:00', promotionEndsAtUtc: '2026-10-11T00:00:00Z' })
    await flushPromises()
    expect(wrapper.vm.form.promotionStartsAtUtc).toBe('2026-10-10T00:00')
    await wrapper.findAll('button').find(button => button.text() === 'Save Changes').trigger('click')
    await flushPromises()
    expect(api.updateDish).toHaveBeenCalledWith(9, 1, expect.objectContaining({ itemType: 'Promotional', bundleSize: null }))
    wrapper.vm.startEdit({ id: 9, name: 'Bundle', price: 12, inventory: 4, itemType: 'Bundle', bundleSize: 2 })
    wrapper.vm.cancelEdit()
    expect(wrapper.vm.form.itemType).toBe('Standard')
    expect(wrapper.vm.editingDishId).toBeNull()
  })
})
