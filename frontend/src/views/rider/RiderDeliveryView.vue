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
    ElMessage.success('接单成功')
    await refresh()
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('接单失败')
  }
}

async function advance(delivery) {
  const next = nextDeliveryAction(delivery.status)
  if (!next) return
  try {
    await advanceDelivery(delivery.orderId, next.action)
    ElMessage.success(`${next.label}成功`)
    await loadMine()
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('操作失败')
  }
}

onMounted(refresh)
</script>

<template>
  <section class="rider-page">
    <div class="page-heading">
      <div>
        <h1>骑手配送</h1>
        <p>接单并更新配送状态</p>
      </div>
      <el-button :loading="loadingAvailable || loadingMine" @click="refresh">刷新</el-button>
    </div>

    <el-card class="section-card" shadow="never">
      <template #header>可接任务</template>
      <el-table v-loading="loadingAvailable" :data="available" stripe>
        <el-table-column prop="orderId" label="订单号" width="100" />
        <el-table-column prop="zone" label="区域" width="120" />
        <el-table-column label="金额" width="120">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column prop="deliveryAddress" label="配送地址" min-width="200" />
        <el-table-column prop="contactName" label="联系人" width="120" />
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="accept(scope.row)">接单</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loadingAvailable && available.length === 0" description="暂无可接任务" />
    </el-card>

    <el-card class="section-card" shadow="never">
      <template #header>我的配送</template>
      <el-table v-loading="loadingMine" :data="myDeliveries" stripe>
        <el-table-column prop="orderId" label="订单号" width="100" />
        <el-table-column label="状态" width="120">
          <template #default="scope">{{ deliveryStatusText(scope.row.status) }}</template>
        </el-table-column>
        <el-table-column label="金额" width="120">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column prop="deliveryAddress" label="配送地址" min-width="200" />
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="scope">
            <el-button
              v-if="canAdvance(scope.row.status)"
              link
              type="primary"
              @click="advance(scope.row)"
            >
              {{ nextDeliveryAction(scope.row.status).label }}
            </el-button>
            <span v-else>—</span>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loadingMine && myDeliveries.length === 0" description="暂无配送任务" />
    </el-card>
  </section>
</template>

<style scoped>
.rider-page { max-width: 1100px; margin: 0 auto; }
.page-heading { display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; }
.page-heading h1 { margin: 0; }
.page-heading p { color: #6b7280; margin: 6px 0 0; }
.section-card { margin-bottom: 18px; }
</style>
