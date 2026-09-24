<script setup>
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useCartStore } from '@/stores/cart'
import { cancelOrder, createOrder, listAddresses, listOrders } from '@/api/order'
import {
  buildOrderPayload,
  canPlaceOrder,
  isPending,
  orderStatusText,
  orderTotalPrice,
} from '@/utils/order'
import { formatYuan } from '@/utils/money'

// 第一周先手填一个 userId 方便联调，后续接入登录态。
const userId = ref(1)
const cart = useCartStore()

const addresses = ref([])
const selectedAddressId = ref(null)
const orders = ref([])
const riderPrice = ref(3)
const loading = ref(false)
const submitting = ref(false)

const totalPrice = computed(() => orderTotalPrice(cart.items, riderPrice.value))
const canSubmit = computed(() =>
  canPlaceOrder({ addressId: selectedAddressId.value, cart: cart.items }))

async function loadAddresses() {
  addresses.value = await listAddresses(userId.value)
  if (addresses.value.length > 0 && !selectedAddressId.value) {
    selectedAddressId.value = addresses.value[0].id
  }
}

async function loadOrders() {
  loading.value = true
  try {
    orders.value = await listOrders(userId.value)
  } finally {
    loading.value = false
  }
}

async function placeOrder() {
  if (!canSubmit.value) {
    ElMessage.warning('请先选择收货地址并添加菜品')
    return
  }
  submitting.value = true
  try {
    const payload = buildOrderPayload({
      userId: userId.value,
      addressId: selectedAddressId.value,
      cart: cart.items,
      riderPrice: riderPrice.value,
    })
    await createOrder(payload)
    ElMessage.success('下单成功')
    await cart.clear()
    await loadOrders()
  } finally {
    submitting.value = false
  }
}

async function removeOrder(order) {
  try {
    await ElMessageBox.confirm('确定要取消这笔待支付订单吗？', '确认取消订单', {
      type: 'warning',
      confirmButtonText: '确认',
      cancelButtonText: '再想想',
    })
    await cancelOrder(order.id)
    ElMessage.success('订单已取消')
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
        <h1>我的订单</h1>
        <p>用户 ID：{{ userId }}</p>
      </div>
      <el-button :loading="loading" @click="loadOrders">刷新</el-button>
    </div>

    <el-card class="form-card" shadow="never">
      <template #header>下单</template>
      <el-form label-position="top">
        <el-form-item label="收货地址">
          <el-select v-model="selectedAddressId" placeholder="选择收货地址" style="width: 100%">
            <el-option
              v-for="addr in addresses"
              :key="addr.id"
              :label="`${addr.contactName} ${addr.phoneNumber} - ${addr.address}`"
              :value="addr.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="配送费">
          <el-input-number v-model="riderPrice" :min="0" :step="1" controls-position="right" />
        </el-form-item>
      </el-form>

      <el-table :data="cart.items" stripe>
        <el-table-column prop="name" label="菜品" min-width="160" />
        <el-table-column label="单价" width="120">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column prop="dishNum" label="数量" width="100" />
      </el-table>
      <el-empty v-if="cart.items.length === 0" description="购物车为空" />

      <div class="summary">
        <span>合计（含配送费）：<strong>{{ formatYuan(totalPrice) }}</strong></span>
        <el-button type="primary" :loading="submitting" :disabled="!canSubmit" @click="placeOrder">
          提交订单
        </el-button>
      </div>
    </el-card>

    <el-card shadow="never">
      <template #header>订单列表</template>
      <el-table v-loading="loading" :data="orders" stripe>
        <el-table-column prop="id" label="订单号" width="100" />
        <el-table-column label="金额" width="130">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="120">
          <template #default="scope">{{ orderStatusText(scope.row.status) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="scope">
            <el-button
              v-if="isPending(scope.row.status)"
              link
              type="danger"
              @click="removeOrder(scope.row)"
            >
              取消订单
            </el-button>
            <span v-else>—</span>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loading && orders.length === 0" description="暂无订单" />
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
