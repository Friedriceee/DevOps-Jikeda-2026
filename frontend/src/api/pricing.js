import http from './http'

export function listPricingCoupons() {
  return http.get('/pricing/coupons')
}

export function getPricingQuote(couponCode = null) {
  return http.post('/pricing/quote', {
    couponCode: couponCode || null,
  })
}
