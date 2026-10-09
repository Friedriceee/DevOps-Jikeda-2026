import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getPricingQuote, listPricingCoupons } from '@/api/pricing'

export const usePricingStore = defineStore('pricing', () => {
  const coupons = ref([])
  const quote = ref(null)
  const selectedCouponCode = ref('')
  const loading = ref(false)
  const error = ref('')

  const hasQuote = computed(() => Boolean(quote.value))

  async function loadCoupons() {
    coupons.value = await listPricingCoupons()
  }

  async function refreshQuote(couponCode = selectedCouponCode.value) {
    selectedCouponCode.value = couponCode || ''
    loading.value = true
    error.value = ''
    try {
      quote.value = await getPricingQuote(selectedCouponCode.value || null)
      return quote.value
    } catch (requestError) {
      quote.value = null
      error.value = requestError.response?.data?.message
        || requestError.message
        || 'Could not calculate your order total.'
      throw requestError
    } finally {
      loading.value = false
    }
  }

  function reset() {
    quote.value = null
    selectedCouponCode.value = ''
    error.value = ''
  }

  return {
    coupons,
    quote,
    selectedCouponCode,
    loading,
    error,
    hasQuote,
    loadCoupons,
    refreshQuote,
    reset,
  }
})
