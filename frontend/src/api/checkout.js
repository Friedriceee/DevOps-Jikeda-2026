import http from './http'

/**
 * Pays for the signed-in customer's cart. The body carries no totals, items or
 * user id: the server prices the stored cart and reads the identity from the JWT.
 *
 * `silent` stops the global interceptor from toasting the error, because the
 * checkout page shows its own, more specific message.
 */
export function submitCheckout(payload, idempotencyKey) {
  return http.post('/checkout', payload, {
    headers: { 'Idempotency-Key': idempotencyKey },
    silent: true,
  })
}
