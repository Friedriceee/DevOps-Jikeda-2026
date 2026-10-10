// Local-only smoke test. Never prints tokens or passwords.
import { writeFile } from 'node:fs/promises'
const base = process.env.JIKEDA_API_BASE || 'http://127.0.0.1:5080'
if (!['127.0.0.1', 'localhost'].includes(new URL(base).hostname)) throw new Error('Local demo only.')
const password = process.env.JIKEDA_DEMO_PASSWORD
if (!password) throw new Error('Set JIKEDA_DEMO_PASSWORD for disposable demo accounts.')
async function call(path, method = 'GET', body, token, expected = 200) {
  const response = await fetch(`${base}${path}`, {
    method, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  const result = await response.json()
  if (!(Array.isArray(expected) ? expected : [expected]).includes(response.status)) throw new Error(`${method} ${path}: ${response.status}: ${result.message}`)
  return result.data
}
const merchantName = 'member3-merchant'
const customerName = 'member3-customer'
await call('/api/merchant/register', 'POST', {
  username: merchantName, password, merchantName: 'Member 3 Kitchen', merchantAddress: 'Demo Campus',
  contact: '13000000003', walletPassword: password, dishType: 'Meals', timeForOpenBusiness: 0, timeForCloseBusiness: 86399,
}, null, [201, 409])
await call('/api/auth/customer/register', 'POST', { username: customerName, password, displayName: 'Member 3 Demo', phoneNumber: '13000000004' }, null, [201, 409])
const merchant = await call('/api/auth/login', 'POST', { username: merchantName, password })
const customer = await call('/api/auth/login', 'POST', { username: customerName, password })
const now = Date.now()
const dishes = []
const existing = await call(`/api/merchant/${merchant.profileId}/dishes`)
for (const settings of [
  { name: 'Standard Noodles', itemType: 'Standard', inventory: 8, price: 12 },
  { name: 'Lunch Bundle', itemType: 'Bundle', bundleSize: 3, inventory: 6, price: 25 },
  { name: 'Today Promotion', itemType: 'Promotional', inventory: 4, price: 9, promotionStartsAtUtc: new Date(now - 3600000).toISOString(), promotionEndsAtUtc: new Date(now + 7 * 86400000).toISOString() },
  { name: 'Expired Promotion', itemType: 'Promotional', inventory: 2, price: 8, promotionStartsAtUtc: new Date(now - 2 * 86400000).toISOString(), promotionEndsAtUtc: new Date(now - 86400000).toISOString() },
]) dishes.push(existing.find(dish => dish.name === settings.name) || await call('/api/merchant/dishes', 'POST', { ...settings, category: 'Demo Menu' }, merchant.accessToken, 201))
const address = await call('/api/user/addresses', 'POST', { userId: customer.profileId, contactName: 'Demo Customer', phoneNumber: '13000000004', address: 'Demo Campus', houseNumber: 'Block 3' }, customer.accessToken, 201)
const menu = () => call(`/api/merchant/${merchant.profileId}/dishes`)
const stock = async () => (await menu()).find(dish => dish.id === dishes[0].id).inventory
const before = await stock()
const request = (dish, qty) => ({ addressId: address.id, price: dish.price * qty, needUtensils: 1, riderPrice: 0,
  shoppingCart: [{ merchantId: merchant.profileId, dishId: dish.id, dishNum: qty }] })
const order = await call('/api/orders', 'POST', request(dishes[0], 2), customer.accessToken, 201)
const reserved = await stock()
await call(`/api/orders/${order.id}`, 'DELETE', undefined, customer.accessToken)
const released = await stock()
await call(`/api/orders/${order.id}`, 'DELETE', undefined, customer.accessToken, 404)
const afterRepeat = await stock()
await call('/api/orders', 'POST', request(dishes[0], 999), customer.accessToken, 400)
await call('/api/orders', 'POST', request(dishes[3], 1), customer.accessToken, 400)
if (before !== 8 || reserved !== 6 || released !== 8 || afterRepeat !== 8 || await stock() !== 8)
  throw new Error('Inventory smoke invariant failed.')
const evidence = JSON.stringify({ testedAtUtc: new Date().toISOString(), scope: 'Member 3 menu and inventory only',
  merchantId: merchant.profileId, customerId: customer.profileId, orderId: order.id,
  stock: { before, reserved, released, afterRepeatedCancel: afterRepeat },
  checks: { insufficientStockRejected: true, expiredPromotionRejected: true },
  menu: (await menu()).map(({ id, name, itemType, bundleSize, inventory, isAvailable }) => ({ id, name, itemType, bundleSize, inventory, isAvailable })),
}, null, 2)
if (process.env.JIKEDA_SMOKE_OUTPUT) await writeFile(process.env.JIKEDA_SMOKE_OUTPUT, evidence + '\n')
console.log(evidence)
