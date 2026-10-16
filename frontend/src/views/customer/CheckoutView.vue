<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { usePricingStore } from '@/stores/pricing'
import { submitCheckout } from '@/api/checkout'
import { createAddress, listAddresses } from '@/api/order'
import {
  PAYMENT_METHOD_APPROVE,
  buildCheckoutPayload,
  canPay,
  checkoutErrorMessage,
  createIdempotencyKey,
  paymentMethodOptions,
  shouldReuseIdempotencyKey,
} from '@/utils/checkout'
import { orderStatusText } from '@/utils/order'
import { formatYuan } from '@/utils/money'

const router = useRouter()
const authStore = useAuthStore()
const cart = useCartStore()
const pricing = usePricingStore()

const addresses = ref([])
const selectedAddressId = ref(null)
const needUtensils = ref(false)
const showAddressForm = ref(false)
const addressForm = reactive({
  contactName: '',
  phoneNumber: '',
  address: '',
  houseNumber: '',
})
const loading = ref(false)
const submitting = ref(false)

// Payment is simulated. The decline test card is only offered in development or
// when VITE_ENABLE_TEST_PAYMENT=true, so it never appears for a normal customer.
const showTestPayment = import.meta.env.DEV || import.meta.env.VITE_ENABLE_TEST_PAYMENT === 'true'
const paymentOptions = paymentMethodOptions({ includeTest: showTestPayment })
const paymentMethod = ref(PAYMENT_METHOD_APPROVE)
const checkoutError = ref('')
const confirmation = ref(null)

// One key per distinct checkout attempt. A retry after a network failure reuses it,
// so the server returns the earlier result instead of charging twice. Changing
// anything the customer chose starts a new attempt with a new key.
const idempotencyKey = ref(createIdempotencyKey())
watch(
  [selectedAddressId, () => pricing.selectedCouponCode, needUtensils, paymentMethod],
  () => {
    idempotencyKey.value = createIdempotencyKey()
    checkoutError.value = ''
  },
)

const userId = computed(() => authStore.user?.profileId ?? null)
const canSubmit = computed(() => canPay({
  addressId: selectedAddressId.value,
  hasItems: cart.items.length > 0,
  hasQuote: pricing.hasQuote,
  quoting: pricing.loading,
  submitting: submitting.value,
}))

async function loadCheckout() {
  loading.value = true
  try {
    await Promise.all([
      cart.load(),
      pricing.loadCoupons(),
      loadAddresses(),
    ])
    if (cart.items.length === 0) {
      pricing.reset()
      return
    }
    await pricing.refreshQuote()
  } catch {
    if (!pricing.error) ElMessage.error('Could not load checkout. Please try again.')
  } finally {
    loading.value = false
  }
}

async function loadAddresses() {
  if (!userId.value) return
  addresses.value = await listAddresses(userId.value)
  if (addresses.value.length > 0 && !selectedAddressId.value) {
    selectedAddressId.value = addresses.value[0].id
  }
}

async function selectCoupon(couponCode) {
  try {
    await pricing.refreshQuote(couponCode)
  } catch {
    // The pricing store keeps the server error for the inline alert.
  }
}

async function saveAddress() {
  if (!addressForm.contactName.trim() || !addressForm.address.trim()) {
    ElMessage.warning('Enter a contact name and delivery address.')
    return
  }
  if (!/^\d{11}$/.test(addressForm.phoneNumber.trim())) {
    ElMessage.warning('Enter an 11-digit phone number.')
    return
  }

  const created = await createAddress({
    contactName: addressForm.contactName.trim(),
    phoneNumber: addressForm.phoneNumber.trim(),
    address: addressForm.address.trim(),
    houseNumber: addressForm.houseNumber.trim() || null,
  })
  addresses.value.push(created)
  selectedAddressId.value = created.id
  Object.assign(addressForm, { contactName: '', phoneNumber: '', address: '', houseNumber: '' })
  showAddressForm.value = false
  ElMessage.success('Delivery address added.')
}

async function refreshAfterFailure(error) {
  const code = error?.response?.data?.code
  if (!['CartChanged', 'InsufficientStock', 'ItemUnavailable', 'EmptyCart'].includes(code)) return
  try {
    await cart.load()
    if (cart.items.length > 0) await pricing.refreshQuote()
    else pricing.reset()
  } catch {
    // The inline message already explains what happened.
  }
}

async function placeOrder() {
  if (!canSubmit.value) {
    ElMessage.warning('Choose a delivery address and resolve pricing errors before paying.')
    return
  }

  submitting.value = true
  checkoutError.value = ''
  try {
    const selectedCoupon = pricing.quote.selectedCoupon
    const result = await submitCheckout(
      buildCheckoutPayload({
        addressId: selectedAddressId.value,
        couponCode: selectedCoupon?.code ?? pricing.selectedCouponCode,
        needUtensils: needUtensils.value,
        paymentMethod: paymentMethod.value,
      }),
      idempotencyKey.value,
    )
    confirmation.value = result
    pricing.reset()
    // The server already emptied the cart; reload it so the rest of the app agrees.
    await cart.load().catch(() => {})
    ElMessage.success(result.replayed ? 'This order was already placed.' : 'Payment successful.')
  } catch (error) {
    checkoutError.value = checkoutErrorMessage(error)
    if (!shouldReuseIdempotencyKey(error)) idempotencyKey.value = createIdempotencyKey()
    await refreshAfterFailure(error)
  } finally {
    submitting.value = false
  }
}

onMounted(loadCheckout)
</script>

<template>
  <section class="checkout-page">
    <div class="page-heading">
      <div>
        <p class="eyebrow">SECURE CHECKOUT</p>
        <h1>Review and place your order</h1>
        <p>Restaurant offers and platform coupons are calculated by the server.</p>
      </div>
      <el-button @click="router.push({ name: 'customer-cart' })">Back to cart</el-button>
    </div>

    <div v-if="confirmation" class="confirmation" data-testid="checkout-confirmation">
      <div class="confirmation-icon" aria-hidden="true">✓</div>
      <h2>{{ confirmation.replayed ? 'This order was already placed' : 'Payment successful' }}</h2>
      <p>Your order is confirmed and has been sent to the restaurant.</p>
      <dl class="confirmation-details">
        <div><dt>Order number</dt><dd>#{{ confirmation.orderId }}</dd></div>
        <div><dt>Amount paid</dt><dd>{{ formatYuan(confirmation.amountPaid) }}</dd></div>
        <div><dt>Status</dt><dd>{{ orderStatusText(confirmation.status) }}</dd></div>
        <div><dt>Payment reference</dt><dd>{{ confirmation.paymentReference }}</dd></div>
      </dl>
      <div class="confirmation-actions">
        <el-button type="primary" @click="router.push({ name: 'user-orders' })">View my orders</el-button>
        <el-button @click="router.push({ name: 'customer-merchants' })">Keep browsing</el-button>
      </div>
    </div>
    <el-skeleton v-else-if="loading" :rows="6" animated />
    <el-empty v-else-if="cart.items.length === 0" description="Your cart is empty.">
      <el-button type="primary" @click="router.push({ name: 'customer-merchants' })">
        Browse restaurants
      </el-button>
    </el-empty>

    <div v-else class="checkout-grid">
      <div class="checkout-main">
        <el-card shadow="never" class="checkout-card">
          <template #header><strong>Delivery details</strong></template>
          <el-form label-position="top">
            <el-form-item label="Delivery address">
              <el-select
                v-model="selectedAddressId"
                placeholder="Select a delivery address"
                style="width: 100%"
              >
                <el-option
                  v-for="address in addresses"
                  :key="address.id"
                  :label="`${address.contactName} ${address.phoneNumber} — ${address.address}${address.houseNumber ? `, ${address.houseNumber}` : ''}`"
                  :value="address.id"
                />
              </el-select>
              <p v-if="addresses.length === 0" class="field-help">
                Add a delivery address before placing this order.
              </p>
            </el-form-item>
            <el-button link type="primary" @click="showAddressForm = !showAddressForm">
              {{ showAddressForm ? 'Cancel address entry' : 'Add a delivery address' }}
            </el-button>
            <div v-if="showAddressForm" class="address-form">
              <el-form-item label="Contact name">
                <el-input v-model="addressForm.contactName" placeholder="Full name" />
              </el-form-item>
              <el-form-item label="Phone number">
                <el-input v-model="addressForm.phoneNumber" placeholder="11-digit phone number" />
              </el-form-item>
              <el-form-item label="Street address">
                <el-input v-model="addressForm.address" placeholder="Street and building" />
              </el-form-item>
              <el-form-item label="Unit or house number">
                <el-input v-model="addressForm.houseNumber" placeholder="Optional" />
              </el-form-item>
              <el-button type="primary" @click="saveAddress">Save address</el-button>
            </div>
            <el-form-item>
              <el-checkbox v-model="needUtensils">Include disposable utensils</el-checkbox>
            </el-form-item>
          </el-form>
        </el-card>

        <el-card shadow="never" class="checkout-card">
          <template #header><strong>Order items</strong></template>
          <el-table :data="cart.items" stripe>
            <el-table-column prop="merchantName" label="Restaurant" min-width="150" />
            <el-table-column prop="name" label="Dish" min-width="160" />
            <el-table-column prop="dishNum" label="Qty" width="70" />
            <el-table-column label="Item total" width="130" align="right">
              <template #default="scope">
                {{ formatYuan(Number(scope.row.price) * Number(scope.row.dishNum)) }}
              </template>
            </el-table-column>
          </el-table>
        </el-card>

        <el-card shadow="never" class="checkout-card">
          <template #header><strong>Payment</strong></template>
          <el-radio-group v-model="paymentMethod" class="payment-options">
            <el-radio
              v-for="option in paymentOptions"
              :key="option.value"
              :value="option.value"
              border
            >
              <span class="payment-label">{{ option.label }}</span>
              <span class="payment-hint">{{ option.hint }}</span>
            </el-radio>
          </el-radio-group>
          <p class="field-help">Payments are simulated in this version. No real card is charged.</p>
        </el-card>

        <el-card shadow="never" class="checkout-card">
          <template #header><strong>Platform coupon</strong></template>
          <el-select
            :model-value="pricing.selectedCouponCode"
            placeholder="Choose a coupon"
            clearable
            style="width: 100%"
            @change="selectCoupon"
            @clear="selectCoupon('')"
          >
            <el-option
              v-for="coupon in pricing.coupons"
              :key="coupon.id"
              :label="`${coupon.name} — ${formatYuan(coupon.discountAmount)} off ${formatYuan(coupon.minimumSpend)} (${coupon.remainingQuantity} left)`"
              :value="coupon.code"
            />
          </el-select>
          <p class="field-help">Coupons apply after restaurant discounts. Delivery fees are not discounted.</p>
          <el-alert
            v-if="pricing.error"
            :title="pricing.error"
            type="error"
            :closable="false"
            show-icon
          />
        </el-card>
      </div>

      <aside>
        <el-card shadow="never" class="summary-card">
          <template #header><strong>Price summary</strong></template>
          <div v-loading="pricing.loading" class="price-lines">
            <div><span>Food subtotal</span><span>{{ formatYuan(pricing.quote?.subtotal) }}</span></div>
            <div class="saving"><span>Restaurant offers</span><span>-{{ formatYuan(pricing.quote?.merchantDiscount) }}</span></div>
            <div class="saving"><span>Platform coupon</span><span>-{{ formatYuan(pricing.quote?.couponDiscount) }}</span></div>
            <div><span>Delivery fee</span><span>{{ formatYuan(pricing.quote?.deliveryFee) }}</span></div>
            <div class="total-line"><strong>Total</strong><strong>{{ formatYuan(pricing.quote?.total) }}</strong></div>
          </div>
          <el-alert
            v-if="checkoutError"
            :title="checkoutError"
            type="error"
            :closable="false"
            show-icon
            class="checkout-error"
          />
          <el-button
            type="primary"
            size="large"
            class="place-order"
            :loading="submitting"
            :disabled="!canSubmit"
            @click="placeOrder"
          >
            Pay and place order
          </el-button>
          <p class="pricing-note">The server recalculates the total from your cart when you pay. Nothing you send can change the price.</p>
        </el-card>
      </aside>
    </div>
  </section>
</template>

<style scoped>
.checkout-page { max-width: 1120px; margin: 0 auto; padding: 30px 0 60px; color: #203b35; }
.page-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 24px; margin-bottom: 24px; }
.page-heading h1 { margin: 4px 0 8px; font-size: clamp(28px, 4vw, 42px); letter-spacing: -.035em; }
.page-heading p { margin: 0; color: #6a7c77; }
.eyebrow { color: #da7048 !important; font-size: 12px; font-weight: 800; letter-spacing: .14em; }
.checkout-grid { display: grid; grid-template-columns: minmax(0, 1fr) 340px; gap: 22px; align-items: start; }
.checkout-main { display: grid; gap: 18px; }
.checkout-card, .summary-card { border-color: #dfe7e3; border-radius: 14px; }
.field-help, .pricing-note { margin: 10px 0 0; color: #71817d; font-size: 13px; line-height: 1.5; }
.address-form { display: grid; grid-template-columns: 1fr 1fr; gap: 0 14px; margin: 14px 0 8px; padding: 16px; border-radius: 10px; background: #f5f8f6; }
.address-form .el-button { align-self: end; margin-bottom: 18px; }
.price-lines { display: grid; gap: 14px; }
.price-lines > div { display: flex; justify-content: space-between; gap: 20px; }
.saving { color: #218568; }
.total-line { margin-top: 4px; padding-top: 16px; border-top: 1px solid #e3e9e6; font-size: 20px; }
.place-order { width: 100%; margin-top: 22px; }
.checkout-error { margin-top: 18px; }
.payment-options { display: grid; width: 100%; gap: 10px; }
.payment-options :deep(.el-radio) { width: 100%; height: auto; margin: 0; padding: 12px 16px; }
.payment-label { display: block; font-weight: 700; }
.payment-hint { display: block; margin-top: 2px; color: #71817d; font-size: 13px; font-weight: 400; }
.confirmation { max-width: 560px; margin: 24px auto 0; padding: 36px 32px; border: 1px solid #dfe7e3; border-radius: 18px; background: #fff; text-align: center; }
.confirmation-icon { display: grid; width: 64px; height: 64px; margin: 0 auto 14px; place-items: center; border-radius: 50%; background: #e3f4ec; color: #1f8a63; font-size: 32px; font-weight: 800; }
.confirmation h2 { margin: 0 0 6px; font-size: 26px; }
.confirmation p { margin: 0; color: #6a7c77; }
.confirmation-details { display: grid; gap: 12px; margin: 26px 0; padding: 18px 20px; border-radius: 12px; background: #f5f8f6; text-align: left; }
.confirmation-details > div { display: flex; justify-content: space-between; gap: 20px; }
.confirmation-details dt { color: #6a7c77; }
.confirmation-details dd { margin: 0; font-weight: 700; word-break: break-all; text-align: right; }
.confirmation-actions { display: flex; justify-content: center; gap: 12px; }
@media (max-width: 820px) {
  .checkout-grid { grid-template-columns: 1fr; }
  .page-heading { flex-direction: column; }
  .address-form { grid-template-columns: 1fr; }
}
</style>
