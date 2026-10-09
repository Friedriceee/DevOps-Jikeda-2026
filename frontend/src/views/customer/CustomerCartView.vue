<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useCartStore } from '@/stores/cart'
import { formatYuan } from '@/utils/money'

const router = useRouter()
const cart = useCartStore()
const updatingItemId = ref(null)

async function loadCart() {
  try {
    await cart.load()
  } catch {
    ElMessage.error('Could not load your cart. Please try again.')
  }
}

async function updateQuantity(item, dishNum) {
  if (!Number.isInteger(dishNum) || dishNum < 1 || item.dishNum === dishNum) return
  updatingItemId.value = item.id
  try {
    await cart.setQuantity(item.id, dishNum)
  } finally {
    updatingItemId.value = null
  }
}

async function removeItem(item) {
  try {
    await ElMessageBox.confirm(`Remove “${item.dishName}” from your cart?`, 'Remove item', {
      type: 'warning', confirmButtonText: 'Remove', cancelButtonText: 'Keep item',
    })
    updatingItemId.value = item.id
    await cart.removeItem(item.id)
    ElMessage.success('Item removed.')
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') throw error
  } finally {
    updatingItemId.value = null
  }
}

async function clearMerchant(group) {
  try {
    await ElMessageBox.confirm(`Remove all items from ${group.merchantName}?`, 'Clear restaurant cart', {
      type: 'warning', confirmButtonText: 'Clear cart', cancelButtonText: 'Keep items',
    })
    await cart.clearMerchant(group.merchantId)
    ElMessage.success('Restaurant cart cleared.')
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') throw error
  }
}

onMounted(loadCart)
</script>

<template>
  <section class="cart-page">
    <div class="page-heading">
      <div>
        <h1>Your cart</h1>
        <p>Prices and eligible special offers are calculated by the server using the current menu.</p>
      </div>
      <div class="heading-actions">
        <el-button :loading="cart.loading" @click="loadCart">Refresh</el-button>
        <el-button type="primary" :disabled="cart.items.length === 0" @click="router.push({ name: 'user-orders' })">
          Checkout
        </el-button>
      </div>
    </div>

    <el-empty v-if="!cart.loading && cart.groups.length === 0" description="Your cart is empty.">
      <el-button type="primary" @click="router.push({ name: 'customer-merchants' })">Browse restaurants</el-button>
    </el-empty>

    <el-card v-for="group in cart.groups" :key="group.merchantId" class="merchant-card" shadow="never">
      <template #header>
        <div class="merchant-heading">
          <strong>{{ group.merchantName }}</strong>
          <el-button link type="danger" @click="clearMerchant(group)">Clear restaurant</el-button>
        </div>
      </template>
      <el-table :data="group.items" stripe>
        <el-table-column prop="dishName" label="Dish" min-width="180" />
        <el-table-column label="Unit price" width="130">
          <template #default="scope">{{ formatYuan(scope.row.unitPrice) }}</template>
        </el-table-column>
        <el-table-column label="Quantity" width="180">
          <template #default="scope">
            <el-input-number
              :model-value="scope.row.dishNum"
              :min="1"
              :precision="0"
              :disabled="updatingItemId === scope.row.id"
              controls-position="right"
              @change="(value) => updateQuantity(scope.row, value)"
            />
          </template>
        </el-table-column>
        <el-table-column label="Item total" width="130">
          <template #default="scope">{{ formatYuan(scope.row.lineTotal) }}</template>
        </el-table-column>
        <el-table-column label="Actions" width="100" fixed="right">
          <template #default="scope">
            <el-button link type="danger" :loading="updatingItemId === scope.row.id" @click="removeItem(scope.row)">
              Remove
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="merchant-summary">
        <span>Subtotal: {{ formatYuan(group.subtotal) }}</span>
        <span>Discount: -{{ formatYuan(group.discount) }}</span>
        <strong>Restaurant total: {{ formatYuan(group.total) }}</strong>
      </div>
    </el-card>

    <el-card v-if="cart.groups.length > 0" class="cart-summary" shadow="never">
      <span>{{ cart.totalCount }} item{{ cart.totalCount === 1 ? '' : 's' }}</span>
      <span>Food subtotal: {{ formatYuan(cart.subtotal) }}</span>
      <span>Discount: -{{ formatYuan(cart.discount) }}</span>
      <strong>Total: {{ formatYuan(cart.total) }}</strong>
    </el-card>
  </section>
</template>

<style scoped>
.cart-page { max-width: 1000px; margin: 0 auto; }
.page-heading, .heading-actions, .merchant-heading, .merchant-summary, .cart-summary { display: flex; align-items: center; }
.page-heading, .merchant-heading { justify-content: space-between; }
.page-heading { margin-bottom: 18px; }
.page-heading h1 { margin: 0; }
.page-heading p { color: #6b7280; margin: 6px 0 0; }
.heading-actions { gap: 8px; }
.merchant-card { margin-bottom: 16px; }
.merchant-summary { justify-content: flex-end; gap: 20px; margin-top: 16px; }
.cart-summary { justify-content: flex-end; gap: 24px; font-size: 16px; }
@media (max-width: 700px) { .page-heading { align-items: flex-start; gap: 12px; flex-direction: column; } .merchant-summary, .cart-summary { align-items: flex-end; flex-direction: column; gap: 8px; } }
</style>
