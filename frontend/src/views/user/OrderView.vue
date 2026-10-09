<script setup>
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { cancelOrder, listOrders } from '@/api/order'
import { isPending, orderStatusText } from '@/utils/order'
import { formatYuan } from '@/utils/money'

const authStore = useAuthStore()

const orders = ref([])
const loading = ref(false)

const userId = computed(() => authStore.user?.profileId ?? null)

async function loadOrders() {
  loading.value = true
  try {
    if (!userId.value) return
    orders.value = await listOrders(userId.value)
  } finally {
    loading.value = false
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

onMounted(loadOrders)
</script>

<template>
  <section class="order-page">
    <div class="page-heading">
      <div>
        <h1>Your orders</h1>
        <p>Track placed orders and cancel orders that are still pending payment.</p>
      </div>
      <el-button :loading="loading" @click="loadOrders">Refresh</el-button>
    </div>

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
</style>
