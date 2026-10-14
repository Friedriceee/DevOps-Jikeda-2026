#!/usr/bin/env node
/**
 * End-to-end smoke test for a deployed environment (or a local stack).
 *
 * It walks ONE order through the whole Sprint 3 lifecycle using only the public
 * HTTP API, exactly as the three web apps do:
 *
 *   health -> login (customer, administrator, merchant, rider) -> browse -> cart -> address -> quote
 *   -> declined payment (nothing changes) -> paid checkout (tampered body is ignored)
 *   -> administrator sees the order -> idempotent replay -> merchant accepts / prepares / marks ready
 *   -> rider accepts / picks up / delivers / completes -> customer and administrator see "Completed"
 *
 * Requirements: Node 20+ (global fetch). No dependencies.
 *
 * Environment
 *   BASE_URL         origin that serves the app, e.g. http://localhost:8080 (required)
 *   API_URL          API origin if it is not proxied at BASE_URL (default: BASE_URL)
 *   SMOKE_PASSWORD   password of the seeded demo accounts (required, from a secret)
 *   CHECK_FRONTEND   "false" to skip the frontend health/index checks (default: true)
 *
 * Exit code 0 = every step passed, 1 = the first failing step is printed.
 */

const BASE_URL = required('BASE_URL').replace(/\/$/, '')
const API_URL = (process.env.API_URL || BASE_URL).replace(/\/$/, '')
const PASSWORD = required('SMOKE_PASSWORD')
const CHECK_FRONTEND = (process.env.CHECK_FRONTEND ?? 'true') !== 'false'

const ORDER_STATUS = { Paid: 1, Completed: 9 }
const results = []

function required(name) {
  const value = process.env[name]
  if (!value) {
    console.error(`Missing required environment variable ${name}`)
    process.exit(2)
  }
  return value
}

async function request(method, path, { token, body, headers = {} } = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    method,
    headers: {
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
    signal: AbortSignal.timeout(20_000),
  })
  const text = await response.text()
  let json = null
  try {
    json = text ? JSON.parse(text) : null
  } catch {
    // not JSON; keep text for the error message
  }
  return { status: response.status, json, text }
}

function expect(condition, message, details) {
  if (!condition) {
    const error = new Error(message)
    error.details = details
    throw error
  }
}

async function step(name, action) {
  const started = Date.now()
  try {
    const value = await action()
    results.push({ name, ok: true })
    console.log(`  ✓ ${name} (${Date.now() - started} ms)`)
    return value
  } catch (error) {
    results.push({ name, ok: false })
    console.error(`  ✗ ${name}\n    ${error.message}`)
    if (error.details !== undefined) {
      console.error(`    ${typeof error.details === 'string' ? error.details : JSON.stringify(error.details)}`)
    }
    console.error(`\nSmoke test FAILED at "${name}" (${results.filter((r) => r.ok).length} step(s) passed first).`)
    process.exit(1)
  }
}

async function login(username) {
  const response = await request('POST', '/api/auth/login', { body: { username, password: PASSWORD } })
  expect(response.status === 200 && response.json?.success, `login as ${username} failed`, response.text)
  return response.json.data // { accessToken, role, profileId, ... }
}

console.log(`Smoke test against ${BASE_URL}${API_URL === BASE_URL ? '' : ` (API ${API_URL})`}`)

await step('API health endpoint answers ok', async () => {
  const response = await request('GET', '/api/health')
  expect(response.status === 200 && response.json?.data?.status === 'ok', 'GET /api/health is not ok', response.text)
})

if (CHECK_FRONTEND) {
  await step('frontend serves the app shell and its health check', async () => {
    const health = await fetch(`${BASE_URL}/healthz`)
    expect(health.status === 200, `GET /healthz returned ${health.status}`)
    const index = await fetch(`${BASE_URL}/`)
    const html = await index.text()
    expect(index.status === 200 && html.includes('<div id="app">'), 'GET / did not return the SPA shell')
    const deepLink = await fetch(`${BASE_URL}/customer/checkout`)
    expect(deepLink.status === 200, `deep link /customer/checkout returned ${deepLink.status} (SPA fallback missing?)`)
  })
}

const customer = await step('customer signs in', () => login('demo_customer'))
const administrator = await step('administrator signs in', () => login('admin'))
const merchant = await step('merchant signs in', () => login('demo_merchant'))
const rider = await step('rider signs in', () => login('demo_rider'))

await step('protected endpoints reject anonymous callers and the wrong role', async () => {
  const anonymous = await request('POST', '/api/checkout', { body: { addressId: 1 } })
  expect(anonymous.status === 401, `anonymous checkout returned ${anonymous.status}, expected 401`)
  const wrongRole = await request('POST', '/api/checkout', { token: merchant.accessToken, body: { addressId: 1 } })
  expect(wrongRole.status === 403, `merchant checkout returned ${wrongRole.status}, expected 403`)
})

const dish = await step('customer can browse a restaurant and its menu', async () => {
  const merchants = await request('GET', '/api/merchants')
  expect(merchants.status === 200 && merchants.json.data.length > 0, 'no restaurants returned', merchants.text)
  const menu = await request('GET', `/api/merchant/${merchant.profileId}/dishes`)
  expect(menu.status === 200 && menu.json.data.length > 0, 'the demo merchant has no dishes', menu.text)
  return menu.json.data.find((item) => item.inventory >= 10) ?? menu.json.data[0]
})

const address = await step('customer saves a delivery address', async () => {
  const response = await request('POST', '/api/user/addresses', {
    token: customer.accessToken,
    body: { address: 'Smoke Test Road ZoneA', contactName: 'Smoke Tester', phoneNumber: '13800000009' },
  })
  expect(response.status === 201, `address creation returned ${response.status}`, response.text)
  return response.json.data
})

async function fillCart(quantity) {
  const clear = await request('DELETE', '/api/cart', { token: customer.accessToken })
  expect(clear.status === 200, `clearing the cart returned ${clear.status}`, clear.text)
  const add = await request('POST', '/api/cart/items', {
    token: customer.accessToken,
    body: { dishId: dish.id, dishNum: quantity },
  })
  expect(add.status === 200 || add.status === 201, `adding to the cart returned ${add.status}`, add.text)
}

await step('customer fills the cart', () => fillCart(5))

const quote = await step('server quotes the cart with a platform coupon', async () => {
  const response = await request('POST', '/api/pricing/quote', {
    token: customer.accessToken,
    body: { couponCode: 'WELCOME5' },
  })
  expect(response.status === 200, `quote returned ${response.status}`, response.text)
  expect(response.json.data.total > 0, 'quote total is not positive', response.text)
  return response.json.data
})

let stockBefore
await step('a declined payment changes nothing', async () => {
  const before = await request('GET', `/api/merchant/${merchant.profileId}/dishes`)
  stockBefore = before.json.data.find((item) => item.id === dish.id).inventory
  const response = await request('POST', '/api/checkout', {
    token: customer.accessToken,
    headers: { 'Idempotency-Key': `smoke-decline-${Date.now()}` },
    body: { addressId: address.id, couponCode: 'WELCOME5', paymentMethod: 'SimulatedCardDeclined' },
  })
  expect(response.status === 402, `declined payment returned ${response.status}, expected 402`, response.text)
  const after = await request('GET', `/api/merchant/${merchant.profileId}/dishes`)
  expect(after.json.data.find((item) => item.id === dish.id).inventory === stockBefore, 'stock changed after a declined payment')
  const cart = await request('GET', '/api/cart', { token: customer.accessToken })
  expect(cart.json.data.totalCount === 5, 'cart was emptied by a declined payment', cart.text)
})

const idempotencyKey = `smoke-pay-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`
const paid = await step('checkout charges the server-calculated total and ignores a tampered body', async () => {
  const response = await request('POST', '/api/checkout', {
    token: customer.accessToken,
    headers: { 'Idempotency-Key': idempotencyKey },
    body: { addressId: address.id, couponCode: 'WELCOME5', total: 0.01, price: 0.01, userId: 999999 },
  })
  expect(response.status === 201, `checkout returned ${response.status}, expected 201`, response.text)
  const data = response.json.data
  expect(data.amountPaid === quote.total, `charged ${data.amountPaid}, the server quote was ${quote.total}`)
  expect(data.status === ORDER_STATUS.Paid, `order status ${data.status}, expected Paid`)
  expect(/^SIM-/.test(data.paymentReference), 'missing payment reference')
  return data
})

await step(`administrator sees paid order #${paid.orderId}`, async () => {
  const response = await request('GET', `/api/admin/orders?userId=${customer.profileId}&status=Paid`, {
    token: administrator.accessToken,
  })
  expect(response.status === 200 && response.json.data.items.some((order) => order.orderId === paid.orderId),
    'the paid order is not visible in the administrator console', response.text)
})

await step('repeating the same request returns the same order and does not charge or deduct again', async () => {
  const response = await request('POST', '/api/checkout', {
    token: customer.accessToken,
    headers: { 'Idempotency-Key': idempotencyKey },
    body: { addressId: address.id, couponCode: 'WELCOME5' },
  })
  expect(response.status === 200 && response.json.data.replayed === true, `replay returned ${response.status}`, response.text)
  expect(response.json.data.orderId === paid.orderId, 'replay returned a different order')
  const after = await request('GET', `/api/merchant/${merchant.profileId}/dishes`)
  expect(
    after.json.data.find((item) => item.id === dish.id).inventory === stockBefore - 5,
    'stock was not reduced exactly once',
  )
})

await step(`merchant sees order #${paid.orderId} and moves it to ready for delivery`, async () => {
  const list = await request('GET', '/api/merchant/orders', { token: merchant.accessToken })
  expect(list.status === 200 && list.json.data.some((order) => order.orderId === paid.orderId),
    'the paid order is not in the merchant queue', list.text)
  for (const action of ['Accept', 'StartPreparing', 'MarkReady']) {
    const response = await request('PUT', `/api/merchant/orders/${paid.orderId}/status`, {
      token: merchant.accessToken,
      body: { action },
    })
    expect(response.status === 200, `merchant action ${action} returned ${response.status}`, response.text)
  }
})

await step(`rider accepts and delivers order #${paid.orderId}`, async () => {
  const available = await request('GET', '/api/rider/available', { token: rider.accessToken })
  expect(available.status === 200 && available.json.data.some((order) => order.orderId === paid.orderId),
    'the ready order is not offered to riders', available.text)
  const accept = await request('POST', `/api/rider/deliveries/${paid.orderId}/accept`, { token: rider.accessToken })
  expect(accept.status === 200, `rider accept returned ${accept.status}`, accept.text)
  for (const action of ['PickUp', 'StartDelivering', 'Complete']) {
    const response = await request('PUT', `/api/rider/deliveries/${paid.orderId}/status`, {
      token: rider.accessToken,
      body: { action },
    })
    expect(response.status === 200, `rider action ${action} returned ${response.status}`, response.text)
  }
})

await step('customer sees the same order as completed', async () => {
  const orders = await request('GET', `/api/orders/user/${customer.profileId}`, { token: customer.accessToken })
  const order = orders.json?.data?.find((item) => item.id === paid.orderId)
  expect(order, 'order missing from the customer history', orders.text)
  expect(order.status === ORDER_STATUS.Completed, `order status ${order.status}, expected Completed (9)`)
  expect(order.price === paid.amountPaid, 'order price differs from the amount paid')
})

await step('administrator sees the final completed status', async () => {
  const response = await request('GET', '/api/admin/orders?status=Completed', {
    token: administrator.accessToken,
  })
  expect(response.status === 200 && response.json.data.items.some((order) => order.orderId === paid.orderId),
    'the completed order is not visible in the administrator console', response.text)
})

console.log(`\nSmoke test passed: ${results.length} steps, order #${paid.orderId} went from checkout to delivery (paid ${paid.amountPaid}, ${paid.paymentReference}).`)
