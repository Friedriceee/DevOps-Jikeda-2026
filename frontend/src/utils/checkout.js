/**
 * Pure helpers for the checkout and simulated-payment flow.
 * They mirror the backend contract in docs/api-contract.md (POST /api/checkout).
 */

export const PAYMENT_METHOD_APPROVE = 'SimulatedCard'
export const PAYMENT_METHOD_DECLINE = 'SimulatedCardDeclined'

const PAYMENT_METHODS = [
  { value: PAYMENT_METHOD_APPROVE, label: 'Simulated card', hint: 'Always approved. No real card is charged.' },
  {
    value: PAYMENT_METHOD_DECLINE,
    label: 'Simulated card (decline)',
    hint: 'Test only. The payment is declined and nothing is changed.',
    testOnly: true,
  },
]

/**
 * @param {{ includeTest?: boolean }} [options]
 * @returns {Array<{ value: string, label: string, hint: string }>}
 */
export function paymentMethodOptions({ includeTest = false } = {}) {
  return PAYMENT_METHODS.filter((method) => includeTest || !method.testOnly)
}

/**
 * A new Idempotency-Key. Accepted by the server: 8-64 characters of letters,
 * digits, '-' and '_'.
 * @returns {string}
 */
export function createIdempotencyKey() {
  const random = globalThis.crypto?.randomUUID
    ? globalThis.crypto.randomUUID()
    : `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 12)}`
  return `chk-${random}`
}

/**
 * The checkout request body. It deliberately contains no price, total, item
 * list or user id: those are decided by the server.
 */
export function buildCheckoutPayload({
  addressId,
  couponCode = null,
  needUtensils = false,
  paymentMethod = PAYMENT_METHOD_APPROVE,
}) {
  return {
    addressId: Number(addressId),
    couponCode: couponCode ? String(couponCode).trim() || null : null,
    needUtensils: needUtensils ? 1 : 0,
    paymentMethod,
  }
}

/** Can the customer pay? Needs an address, a server quote and no quote in flight. */
export function canPay({ addressId, hasItems, hasQuote, quoting = false, submitting = false }) {
  return Number(addressId) > 0 && Boolean(hasItems) && Boolean(hasQuote) && !quoting && !submitting
}

const MESSAGES_BY_CODE = {
  InsufficientStock: 'One of the items no longer has enough stock. Update your cart and try again.',
  ItemUnavailable: 'One of the items is no longer available. Update your cart and try again.',
  PaymentDeclined: 'Your payment was declined. You were not charged and your cart is unchanged.',
  CartChanged: 'Your cart changed while checking out. Review it and try again.',
  EmptyCart: 'Your cart is empty.',
  AddressNotFound: 'Choose a delivery address and try again.',
  IdempotencyKeyReused: 'This checkout was already submitted with different details. Refresh and try again.',
  UnsupportedPaymentMethod: 'This payment method is not available.',
}

/**
 * A customer-facing message for a failed checkout request.
 * @param {any} error an axios-style error
 * @returns {string}
 */
export function checkoutErrorMessage(error) {
  const data = error?.response?.data
  if (data?.code && MESSAGES_BY_CODE[data.code]) return MESSAGES_BY_CODE[data.code]
  if (data?.message) return data.message
  if (!error?.response) return 'Network problem. Your order was not changed. Please try again.'
  return 'Checkout failed. Please try again.'
}

/**
 * Should the next attempt reuse the same Idempotency-Key? Only when we cannot
 * tell what happened (no response, or a server error): the earlier attempt may
 * have succeeded, and the key lets the server return that result instead of
 * charging twice. A definitive 4xx answer needs a fresh key.
 */
export function shouldReuseIdempotencyKey(error) {
  const status = error?.response?.status
  return !status || status >= 500
}
