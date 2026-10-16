<script setup>
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  acceptDelivery,
  advanceDelivery,
  listAvailableDeliveries,
  listMyDeliveries,
} from '@/api/rider'
import { canAdvance, deliveryStatusText, nextDeliveryAction } from '@/utils/delivery'
import { formatYuan } from '@/utils/money'

const available = ref([])
const myDeliveries = ref([])
const loadingAvailable = ref(false)
const loadingMine = ref(false)

async function loadAvailable() {
  loadingAvailable.value = true
  try {
    available.value = await listAvailableDeliveries()
  } finally {
    loadingAvailable.value = false
  }
}

async function loadMine() {
  loadingMine.value = true
  try {
    myDeliveries.value = await listMyDeliveries()
  } finally {
    loadingMine.value = false
  }
}

async function refresh() {
  await Promise.all([loadAvailable(), loadMine()])
}

async function accept(order) {
  try {
    await acceptDelivery(order.orderId)
    ElMessage.success('Order accepted')
    await refresh()
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('Failed to accept the order')
  }
}

async function advance(delivery) {
  const next = nextDeliveryAction(delivery.status)
  if (!next) return
  try {
    await advanceDelivery(delivery.orderId, next.action)
    ElMessage.success('Delivery status updated')
    await loadMine()
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('Operation failed')
  }
}

onMounted(refresh)
</script>

<template>
  <section class="rider-page">
    <div class="page-heading">
      <div>
        <h1>Rider Delivery</h1>
        <p>Accept orders and update delivery status</p>
      </div>
      <el-button :loading="loadingAvailable || loadingMine" @click="refresh">Refresh</el-button>
    </div>

    <el-card class="section-card" shadow="never">
      <template #header>Available Tasks</template>
      <el-table v-loading="loadingAvailable" :data="available" stripe>
        <el-table-column prop="orderId" label="Order #" width="100" />
        <el-table-column prop="zone" label="Zone" width="120" />
        <el-table-column prop="kitchenStatus" label="Kitchen" width="150" />
        <el-table-column label="Amount" width="120">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column prop="deliveryAddress" label="Delivery Address" min-width="200" />
        <el-table-column prop="contactName" label="Contact" width="120" />
        <el-table-column label="Action" width="120" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="accept(scope.row)">Accept</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loadingAvailable && available.length === 0" description="No available tasks" />
    </el-card>

    <el-card class="section-card" shadow="never">
      <template #header>My Deliveries</template>
      <el-table v-loading="loadingMine" :data="myDeliveries" stripe>
        <el-table-column prop="orderId" label="Order #" width="100" />
        <el-table-column label="Status" width="140">
          <template #default="scope">{{ deliveryStatusText(scope.row.status) }}</template>
        </el-table-column>
        <el-table-column label="Amount" width="120">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column prop="deliveryAddress" label="Delivery Address" min-width="200" />
        <el-table-column label="Action" width="160" fixed="right">
          <template #default="scope">
            <el-button
              v-if="canAdvance(scope.row.status)"
              link
              type="primary"
              :disabled="scope.row.status === 'Assigned' && !scope.row.readyForPickup"
              @click="advance(scope.row)"
            >
              {{ nextDeliveryAction(scope.row.status).label }}
            </el-button>
            <span v-if="scope.row.status === 'Assigned' && !scope.row.readyForPickup" class="waiting">Waiting for kitchen</span>
            <span v-else-if="!canAdvance(scope.row.status)">—</span>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loadingMine && myDeliveries.length === 0" description="No deliveries yet" />
    </el-card>
  </section>
</template>

<style scoped>
.rider-page { max-width: 1100px; margin: 0 auto; }
.page-heading { display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; }
.page-heading h1 { margin: 0; }
.page-heading p { color: #6b7280; margin: 6px 0 0; }
.section-card { margin-bottom: 18px; }
.waiting { color: #8a6a20; font-size: 13px; }
</style>
