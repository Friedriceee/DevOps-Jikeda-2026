<script setup>
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/stores/auth'
import { cancelOrder, createOrder, listAddresses, listOrders } from '@/api/order'
import { buildOrderPayload, canPlaceOrder, isPending, orderStatusText } from '@/utils/order'
import { formatYuan } from '@/utils/money'

const cart = useCartStore()
const authStore = useAuthStore()

const addresses = ref([])
const selectedAddressId = ref(null)
const orders = ref([])
const riderPrice = ref(3)
const loading = ref(false)
const submitting = ref(false)

const userId = computed(() => authStore.user?.profileId ?? null)
const totalPrice = computed(() => Math.round((Number(cart.total) + Number(riderPrice.value || 0)) * 100) / 100)
const canSubmit = computed(() =>
  canPlaceOrder({ addressId: selectedAddressId.value, cart: cart.items }))

async function loadAddresses() {
  if (!userId.value) return
  addresses.value = await listAddresses(userId.value)
  if (addresses.value.length > 0 && !selectedAddressId.value) {
    selectedAddressId.value = addresses.value[0].id
  }
}

async function loadOrders() {
  loading.value = true
  try {
    if (!userId.value) return
    orders.value = await listOrders(userId.value)
  } finally {
    loading.value = false
  }
}

async function placeOrder() {
  if (!canSubmit.value) {
    ElMessage.warning('Choose a delivery address and add items before placing an order.')
    return
  }
  submitting.value = true
  try {
    const payload = buildOrderPayload({
      userId: userId.value,
      addressId: selectedAddressId.value,
      cart: cart.items,
      riderPrice: riderPrice.value,
      price: totalPrice.value,
    })
    await createOrder(payload)
    ElMessage.success('Order placed successfully.')
    await cart.clear()
    await loadOrders()
  } finally {
    submitting.value = false
  }
}

async function removeOrder(order) {
  try {
    await ElMessageBox.confirm('Cancel this pending order?', 'Cancel order', {
      type: 'warning',
      confirmButtonText: 'Cancel order',
      cancelButtonText: 'Keep order',
    })
    await cancelOrder(order.id)
    ElMessage.success('Order cancelled.')
    await loadOrders()
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') throw error
  }
}

onMounted(async () => {
  await cart.load()
  await loadAddresses()
  await loadOrders()
})
</script>

<template>
  <section class="order-page">
    <div class="page-heading">
      <div>
        <h1>Your orders</h1>
        <p>Review your cart and delivery details before placing an order.</p>
      </div>
      <el-button :loading="loading" @click="loadOrders">Refresh</el-button>
    </div>

    <el-card class="form-card" shadow="never">
      <template #header>Checkout</template>
      <el-form label-position="top">
        <el-form-item label="Delivery address">
          <el-select v-model="selectedAddressId" placeholder="Select a delivery address" style="width: 100%">
            <el-option
              v-for="addr in addresses"
              :key="addr.id"
              :label="`${addr.contactName} ${addr.phoneNumber} - ${addr.address}`"
              :value="addr.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="Delivery fee">
          <el-input-number v-model="riderPrice" :min="0" :step="1" controls-position="right" />
        </el-form-item>
      </el-form>

      <el-table :data="cart.items" stripe>
        <el-table-column prop="name" label="Dish" min-width="160" />
        <el-table-column label="Unit price" width="120">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column prop="dishNum" label="Quantity" width="100" />
      </el-table>
      <el-empty v-if="cart.items.length === 0" description="Your cart is empty." />

      <div class="summary">
        <span>Total including delivery: <strong>{{ formatYuan(totalPrice) }}</strong></span>
        <el-button type="primary" :loading="submitting" :disabled="!canSubmit" @click="placeOrder">
          Place order
        </el-button>
      </div>
    </el-card>

    <el-card shadow="never">
      <template #header>Order history</template>
      <el-table v-loading="loading" :data="orders" stripe>
        <el-table-column prop="id" label="Order ID" width="100" />
        <el-table-column label="Amount" width="130">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column label="Status" width="140">
          <template #default="scope">{{ orderStatusText(scope.row.status) }}</template>
        </el-table-column>
        <el-table-column label="Actions" width="140" fixed="right">
          <template #default="scope">
            <el-button
              v-if="isPending(scope.row.status)"
              link
              type="danger"
              @click="removeOrder(scope.row)"
            >
              Cancel order
            </el-button>
            <span v-else>—</span>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loading && orders.length === 0" description="No orders yet." />
    </el-card>
  </section>
</template>

<style scoped>
.order-page { max-width: 1000px; margin: 0 auto; }
.page-heading { display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; }
.page-heading h1 { margin: 0; }
.page-heading p { color: #6b7280; margin: 6px 0 0; }
.form-card { margin-bottom: 18px; }
.summary { display: flex; justify-content: space-between; align-items: center; margin-top: 16px; }
</style>
