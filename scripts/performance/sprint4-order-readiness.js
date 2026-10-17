import http from 'k6/http'
import { check, sleep } from 'k6'

/**
 * Sprint 4 read-path and validation performance gate.
 *
 * This script deliberately does not create orders or mutate stock. Successful
 * checkout is covered by scripts/smoke/staging-smoke.mjs. The checkout request
 * below is an authenticated invalid-input probe, so it exercises the route and
 * authorization pipeline without changing the database.
 *
 * Required environment:
 *   API_URL          API origin, e.g. http://localhost:5080
 *   SMOKE_PASSWORD   password for SMOKE_USERNAME (use a secret in CI)
 *
 * Optional environment:
 *   SMOKE_USERNAME       default demo_customer
 *   K6_VUS              default 20
 *   K6_DURATION         default 30s
 *   K6_SLEEP_SECONDS    default 0.2
 *   K6_CHECKOUT_PROBE   false to skip the safe invalid-input checkout probe
 *   K6_REQUIRE_QUOTE    true when a prepared cart must return a 200 quote
 *
 * Release target: p95 < 2 seconds, <1% failed requests and >99% checks.
 */

const API_URL = (__ENV.API_URL || '').replace(/\/$/, '')
const USERNAME = __ENV.SMOKE_USERNAME || 'demo_customer'
const PASSWORD = __ENV.SMOKE_PASSWORD
const VUS = Number(__ENV.K6_VUS || 20)
const DURATION = __ENV.K6_DURATION || '30s'
const SLEEP_SECONDS = Number(__ENV.K6_SLEEP_SECONDS || 0.2)
const CHECKOUT_PROBE = (__ENV.K6_CHECKOUT_PROBE || 'true') !== 'false'
const REQUIRE_QUOTE = (__ENV.K6_REQUIRE_QUOTE || 'false') === 'true'

// Quote/checkout validation may intentionally return a business 4xx. Treat
// 4xx as an expected HTTP response so `http_req_failed` represents transport or
// server failures; the checks below still require the exact business behavior.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 499 }))

if (!API_URL) {
  throw new Error('API_URL is required, for example http://localhost:5080')
}
if (!PASSWORD) {
  throw new Error('SMOKE_PASSWORD is required and must not be committed')
}
if (!Number.isInteger(VUS) || VUS < 1) {
  throw new Error(`K6_VUS must be a positive integer, received ${VUS}`)
}

export const options = {
  scenarios: {
    order_readiness: {
      executor: 'constant-vus',
      vus: VUS,
      duration: DURATION,
      gracefulStop: '5s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
  },
}

function authParams(token, tags = {}) {
  return {
    headers: { Authorization: `Bearer ${token}` },
    tags,
  }
}

function jsonParams(token, tags = {}) {
  return {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    tags,
  }
}

function responseData(response) {
  try {
    return response.json('data')
  } catch {
    return null
  }
}

export function setup() {
  const response = http.post(
    `${API_URL}/api/auth/login`,
    JSON.stringify({ username: USERNAME, password: PASSWORD }),
    { headers: { 'Content-Type': 'application/json' }, tags: { flow: 'setup-login' } },
  )
  const data = responseData(response)
  check(response, {
    'setup login returns 200': (r) => r.status === 200,
    'setup login returns a token': () => Boolean(data && data.accessToken),
  })
  if (response.status !== 200 || !data || !data.accessToken) {
    throw new Error(`Unable to log in as ${USERNAME}: HTTP ${response.status}`)
  }

  const merchantsResponse = http.get(`${API_URL}/api/merchants`, { tags: { flow: 'setup-merchants' } })
  const merchants = responseData(merchantsResponse) || []
  check(merchantsResponse, {
    'setup merchant directory returns 200': (r) => r.status === 200,
    'setup merchant directory is non-empty': () => merchants.length > 0,
  })
  if (merchantsResponse.status !== 200 || merchants.length === 0) {
    throw new Error(`No merchants are available: HTTP ${merchantsResponse.status}`)
  }

  return { accessToken: data.accessToken, profileId: data.profileId, merchantId: merchants[0].id }
}

export default function (state) {
  const tags = { flow: 'order-readiness', vu: String(__VU) }

  const health = http.get(`${API_URL}/api/health`, { tags })
  check(health, { 'health responds 200': (r) => r.status === 200 })

  const merchants = http.get(`${API_URL}/api/merchants`, { tags })
  const merchantData = responseData(merchants) || []
  check(merchants, {
    'merchant directory responds 200': (r) => r.status === 200,
    'merchant directory has data': () => merchantData.length > 0,
  })

  const menu = http.get(`${API_URL}/api/merchant/${state.merchantId}/dishes`, { tags })
  check(menu, { 'menu responds 200': (r) => r.status === 200 })

  const cart = http.get(`${API_URL}/api/cart`, authParams(state.accessToken, tags))
  check(cart, { 'cart responds 200': (r) => r.status === 200 })

  const quote = http.post(
    `${API_URL}/api/pricing/quote`,
    JSON.stringify({ couponCode: 'WELCOME5' }),
    jsonParams(state.accessToken, tags),
  )
  check(quote, {
    'quote is successful or reports an empty/unprepared cart': (r) =>
      r.status === 200 || (!REQUIRE_QUOTE && r.status >= 400 && r.status < 500),
  })

  const orders = http.get(
    `${API_URL}/api/orders/user/${state.profileId}`,
    authParams(state.accessToken, tags),
  )
  check(orders, { 'customer order query responds 200': (r) => r.status === 200 })

  if (CHECKOUT_PROBE) {
    const checkoutProbe = http.post(
      `${API_URL}/api/checkout`,
      JSON.stringify({ addressId: 0, paymentMethod: 'SimulatedCardDeclined' }),
      {
        ...jsonParams(state.accessToken, tags),
        headers: {
          ...jsonParams(state.accessToken, tags).headers,
          'Idempotency-Key': `k6-probe-${__VU}-${__ITER}`,
        },
      },
    )
    check(checkoutProbe, {
      'checkout validation rejects the safe probe without a server error': (r) =>
        r.status >= 400 && r.status < 500,
    })
  }

  sleep(SLEEP_SECONDS)
}
