<script setup>
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { listMerchantOrders, transitionMerchantOrder } from '@/api/merchant'
import { formatYuan } from '@/utils/money'

const orders = ref([])
const loading = ref(false)
const updatingOrderId = ref(null)

const openOrders = computed(() => orders.value.filter((order) => !['Rejected'].includes(order.status)))
const rejectedOrders = computed(() => orders.value.filter((order) => order.status === 'Rejected'))

function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function statusType(status) {
  return {
    Confirmed: 'info',
    Accepted: 'primary',
    Preparing: 'warning',
    ReadyForDelivery: 'success',
    Rejected: 'danger',
  }[status] ?? 'info'
}

function statusLabel(status) {
  return status === 'ReadyForDelivery' ? 'Ready for delivery' : status
}

function readyNote(overallStatus) {
  if (['Paid', 'Accepted', 'Preparing'].includes(overallStatus)) return 'Waiting for other restaurants'
  if (overallStatus === 'ReadyForDelivery') return 'Waiting for a courier'
  if (['Assigned', 'PickedUp', 'Delivering'].includes(overallStatus)) return 'Courier delivery in progress'
  if (overallStatus === 'Completed') return 'Delivery completed'
  return 'Waiting for order coordination'
}

async function loadOrders() {
  loading.value = true
  try {
    orders.value = await listMerchantOrders()
  } finally {
    loading.value = false
  }
}

async function applyTransition(order, action, successMessage, reason = null) {
  updatingOrderId.value = order.orderId
  try {
    const updated = await transitionMerchantOrder(order.orderId, action, reason)
    const index = orders.value.findIndex((item) => item.orderId === order.orderId)
    if (index >= 0) orders.value[index] = updated
    ElMessage.success(successMessage)
  } finally {
    updatingOrderId.value = null
  }
}

async function rejectOrder(order) {
  try {
    const result = await ElMessageBox.prompt(
      'Tell the customer why this order cannot be fulfilled.',
      'Reject order',
      {
        inputPlaceholder: 'Rejection reason',
        inputValidator: (value) => value?.trim() ? true : 'A rejection reason is required.',
        confirmButtonText: 'Reject order',
        cancelButtonText: 'Keep order',
        type: 'warning',
      },
    )
    await applyTransition(order, 'Reject', 'Order rejected and inventory restored.', result.value)
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') throw error
  }
}

onMounted(loadOrders)
</script>

<template>
  <section class="orders-page">
    <div class="page-heading">
      <div>
        <p class="eyebrow">Merchant fulfilment</p>
        <h1>Incoming Orders</h1>
        <p>Accept confirmed orders, track kitchen progress, or reject with a clear reason.</p>
      </div>
      <el-button :loading="loading" @click="loadOrders">Refresh</el-button>
    </div>

    <el-alert
      title="Only confirmed orders for your restaurant are shown here."
      description="Order status changes are checked by the server and cannot be skipped."
      type="info"
      :closable="false"
      show-icon
    />

    <div v-loading="loading" class="order-list">
      <el-empty v-if="!loading && orders.length === 0" description="No confirmed orders yet" />

      <el-card v-for="order in openOrders" :key="order.orderId" class="order-card" shadow="never">
        <template #header>
          <div class="card-header">
            <div>
              <strong>Order #{{ order.orderId }}</strong>
              <span class="order-meta">{{ formatDate(order.orderTimestamp) }} · Customer #{{ order.customerId }}</span>
            </div>
            <el-tag :type="statusType(order.status)" effect="light">{{ statusLabel(order.status) }}</el-tag>
          </div>
        </template>

        <el-alert v-if="order.riderName" :title="`Courier assigned: ${order.riderName} · ${order.riderPhone}`" :description="`Delivery status: ${order.deliveryStatus}`" type="success" :closable="false" show-icon />

        <el-table :data="order.items" stripe>
          <el-table-column prop="dishName" label="Dish" min-width="200" />
          <el-table-column label="Unit price" width="140">
            <template #default="scope">{{ formatYuan(scope.row.unitPrice) }}</template>
          </el-table-column>
          <el-table-column prop="quantity" label="Quantity" width="120" />
          <el-table-column label="Item total" width="140">
            <template #default="scope">{{ formatYuan(scope.row.lineTotal) }}</template>
          </el-table-column>
        </el-table>

        <div class="order-footer">
          <div><span>{{ order.itemCount }} items</span><strong>{{ formatYuan(order.merchantSubtotal) }}</strong></div>
          <div class="actions">
            <template v-if="order.status === 'Confirmed'">
              <el-button
                type="primary"
                :loading="updatingOrderId === order.orderId"
                @click="applyTransition(order, 'Accept', 'Order accepted.')"
              >Accept order</el-button>
              <el-button type="danger" plain @click="rejectOrder(order)">Reject</el-button>
            </template>
            <template v-else-if="order.status === 'Accepted'">
              <el-button
                type="primary"
                :loading="updatingOrderId === order.orderId"
                @click="applyTransition(order, 'StartPreparing', 'Kitchen preparation started.')"
              >Start preparing</el-button>
              <el-button type="danger" plain @click="rejectOrder(order)">Reject</el-button>
            </template>
            <el-button
              v-else-if="order.status === 'Preparing'"
              type="success"
              :loading="updatingOrderId === order.orderId"
              @click="applyTransition(order, 'MarkReady', 'Order is ready for delivery.')"
            >Mark ready for delivery</el-button>
            <span v-else class="complete-note">{{ readyNote(order.overallStatus) }}</span>
          </div>
        </div>
      </el-card>

      <section v-if="rejectedOrders.length" class="rejected-section">
        <h2>Rejected Orders</h2>
        <el-card v-for="order in rejectedOrders" :key="order.orderId" class="order-card rejected" shadow="never">
          <div class="rejected-row">
            <div>
              <strong>Order #{{ order.orderId }}</strong>
              <p>{{ order.rejectionReason }}</p>
            </div>
            <el-tag type="danger">Rejected</el-tag>
          </div>
        </el-card>
      </section>
    </div>
  </section>
</template>

<style scoped>
.orders-page { max-width: 1100px; margin: 0 auto; padding-bottom: 48px; }
.page-heading { display: flex; justify-content: space-between; align-items: center; gap: 24px; margin: 30px 0 20px; }
.page-heading h1 { margin: 2px 0 6px; font-size: 34px; color: #173d36; }
.page-heading p { margin: 0; color: #687772; }
.eyebrow { color: #df6e45 !important; font-size: 13px; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }
.order-list { min-height: 180px; margin-top: 18px; }
.order-card { margin-bottom: 16px; }
.card-header, .rejected-row, .order-footer, .order-footer > div { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.card-header > div { display: flex; flex-direction: column; gap: 4px; }
.order-meta { color: #71807b; font-size: 13px; }
.order-footer { padding-top: 18px; }
.order-footer strong { font-size: 20px; color: #173d36; }
.actions { justify-content: flex-end !important; }
.complete-note { color: #32855e; font-weight: 700; }
.rejected-section { margin-top: 30px; }
.rejected-section h2 { color: #394743; font-size: 20px; }
.rejected { background: #fffafa; }
.rejected-row p { margin: 7px 0 0; color: #8a5a5a; }
@media (max-width: 700px) { .page-heading, .order-footer { align-items: stretch; flex-direction: column; } .actions { justify-content: flex-start !important; flex-wrap: wrap; } }
</style>
