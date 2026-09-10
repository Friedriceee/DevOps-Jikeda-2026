import { defineStore } from 'pinia'
import { ref } from 'vue'

/**
 * 当前登录商家的信息。第一周先手填一个 merchantId 方便联调，
 * 后续接入登录态后从接口拿。
 */
export const useMerchantStore = defineStore('merchant', () => {
  const merchantId = ref(1)
  function setMerchantId(id) {
    merchantId.value = id
  }
  return { merchantId, setMerchantId }
})
