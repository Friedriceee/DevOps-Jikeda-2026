<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { usePricingStore } from '@/stores/pricing'
import { createAddress, createOrder, listAddresses } from '@/api/order'
import { buildOrderPayload, canPlaceOrder } from '@/utils/order'
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

const userId = computed(() => authStore.user?.profileId ?? null)
const canSubmit = computed(() => pricing.hasQuote
  && !pricing.loading
  && canPlaceOrder({ addressId: selectedAddressId.value, cart: cart.items }))

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

async function placeOrder() {
  if (!canSubmit.value) {
    ElMessage.warning('Choose a delivery address and resolve pricing errors before placing your order.')
    return
  }

  submitting.value = true
  try {
    const selectedCoupon = pricing.quote.selectedCoupon
    const payload = buildOrderPayload({
      userId: userId.value,
      addressId: selectedAddressId.value,
      cart: cart.items,
      riderPrice: pricing.quote.deliveryFee,
      price: pricing.quote.total,
      needUtensils: needUtensils.value ? 1 : 0,
      couponId: selectedCoupon?.id ?? 0,
      expirationDate: selectedCoupon?.expiresAtUtc ?? null,
    })
    await createOrder(payload)
    await cart.clear()
    pricing.reset()
    ElMessage.success('Order placed successfully.')
    await router.push({ name: 'user-orders' })
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

    <el-skeleton v-if="loading" :rows="6" animated />
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
              :label="`${coupon.name} — ${formatYuan(coupon.discountAmount)} off ${formatYuan(coupon.minimumSpend)}`"
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
          <el-button
            type="primary"
            size="large"
            class="place-order"
            :loading="submitting"
            :disabled="!canSubmit"
            @click="placeOrder"
          >
            Place order
          </el-button>
          <p class="pricing-note">The final amount is confirmed by the backend before the order is created.</p>
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
@media (max-width: 820px) {
  .checkout-grid { grid-template-columns: 1fr; }
  .page-heading { flex-direction: column; }
  .address-form { grid-template-columns: 1fr; }
}
</style>
