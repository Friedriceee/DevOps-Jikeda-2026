<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  createAdminCoupon, listAdminAccounts, listAdminCoupons, listAdminOrders, listAuditLogs,
  issueCoupon, listCouponAllocations, setAdminAccountRole, setAdminAccountStatus,
  setAdminCouponStatus, updateAdminCoupon,
} from '@/api/admin'

const loading = ref(false)
const activeTab = ref('accounts')
const accounts = ref([])
const coupons = ref([])
const couponAllocations = ref([])
const orders = ref([])
const auditLogs = ref([])
const accountTotal = ref(0)
const orderTotal = ref(0)
const auditTotal = ref(0)
const accountFilter = reactive({ role: '', page: 1, pageSize: 20 })
const orderFilter = reactive({ userId: '', status: '', page: 1, pageSize: 20 })
const auditFilter = reactive({ actor: '', action: '', targetType: '', result: '', page: 1, pageSize: 20 })
const couponDialog = ref(false)
const editingCouponId = ref(null)
const issuingCoupon = ref(null)
const issueDialog = ref(false)
const issueForm = reactive({ userId: null, quantity: 1 })
const couponForm = reactive({ code: '', name: '', minimumSpend: 0, discountAmount: 1, startsAtUtc: '', expiresAtUtc: '', isActive: true })
const roles = ['Customer', 'Merchant', 'Rider', 'Administrator']

const money = (value) => `¥${Number(value ?? 0).toFixed(2)}`
const dateTime = (value) => value ? new Date(value).toLocaleString() : '—'
const params = (source) => Object.fromEntries(Object.entries(source).filter(([, value]) => value !== '' && value != null))

async function loadAccounts() {
  const result = await listAdminAccounts(params(accountFilter))
  accounts.value = result.items
  accountTotal.value = result.totalCount
}
async function loadCoupons() { coupons.value = await listAdminCoupons() }
async function loadCouponAllocations() { couponAllocations.value = await listCouponAllocations() }
async function loadOrders() {
  const result = await listAdminOrders(params(orderFilter))
  orders.value = result.items
  orderTotal.value = result.totalCount
}
async function loadAuditLogs() {
  const result = await listAuditLogs(params(auditFilter))
  auditLogs.value = result.items
  auditTotal.value = result.totalCount
}
async function loadAll() {
  loading.value = true
  try { await Promise.all([loadAccounts(), loadCoupons(), loadCouponAllocations(), loadOrders(), loadAuditLogs()]) }
  finally { loading.value = false }
}
async function changeAccountStatus(account) {
  await setAdminAccountStatus(account.id, !account.isActive)
  ElMessage.success(`Account ${account.isActive ? 'disabled' : 'enabled'}.`)
  await loadAccounts()
}
async function changeAccountRole(account, role) {
  await setAdminAccountRole(account.id, role)
  ElMessage.success('Account role updated.')
  await loadAccounts()
}
function openCoupon(coupon = null) {
  editingCouponId.value = coupon?.id ?? null
  Object.assign(couponForm, coupon ? {
    code: coupon.code, name: coupon.name, minimumSpend: coupon.minimumSpend,
    discountAmount: coupon.discountAmount, startsAtUtc: coupon.startsAtUtc,
    expiresAtUtc: coupon.expiresAtUtc, isActive: coupon.isActive,
  } : { code: '', name: '', minimumSpend: 0, discountAmount: 1, startsAtUtc: '', expiresAtUtc: '', isActive: true })
  couponDialog.value = true
}
async function saveCoupon() {
  const payload = { ...couponForm, startsAtUtc: new Date(couponForm.startsAtUtc).toISOString(), expiresAtUtc: new Date(couponForm.expiresAtUtc).toISOString() }
  if (editingCouponId.value) {
    const { code, isActive, ...update } = payload
    await updateAdminCoupon(editingCouponId.value, update)
  } else await createAdminCoupon(payload)
  couponDialog.value = false
  ElMessage.success(`Coupon ${editingCouponId.value ? 'updated' : 'created'}.`)
  await Promise.all([loadCoupons(), loadAuditLogs()])
}
async function changeCouponStatus(coupon) {
  await setAdminCouponStatus(coupon.id, !coupon.isActive)
  ElMessage.success(`Coupon ${coupon.isActive ? 'disabled' : 'enabled'}.`)
  await Promise.all([loadCoupons(), loadAuditLogs()])
}
function openIssue(coupon) {
  issuingCoupon.value = coupon
  Object.assign(issueForm, { userId: null, quantity: 1 })
  issueDialog.value = true
}
async function saveIssue() {
  if (!issueForm.userId) {
    ElMessage.warning('Select a customer account.')
    return
  }
  await issueCoupon(issuingCoupon.value.id, issueForm.userId, issueForm.quantity)
  issueDialog.value = false
  ElMessage.success('Coupon issued to the customer.')
  await Promise.all([loadCoupons(), loadCouponAllocations(), loadAuditLogs()])
}

onMounted(loadAll)
</script>

<template>
  <main class="admin-page" v-loading="loading">
    <header class="hero">
      <div><p class="eyebrow">ADMINISTRATION</p><h1>Operations console</h1><p>Manage platform access, promotions and operational records from one secure workspace.</p></div>
      <el-button @click="loadAll">Refresh all</el-button>
    </header>
    <el-tabs v-model="activeTab" class="admin-tabs">
      <el-tab-pane label="Accounts" name="accounts">
        <section class="toolbar"><el-select v-model="accountFilter.role" clearable placeholder="All roles"><el-option v-for="role in roles" :key="role" :label="role" :value="role" /></el-select><el-button type="primary" @click="loadAccounts">Apply filter</el-button></section>
        <el-table :data="accounts" empty-text="No accounts found.">
          <el-table-column prop="username" label="Username" /><el-table-column prop="profileId" label="Profile ID" width="110" />
          <el-table-column label="Role" min-width="180"><template #default="{ row }"><el-select :model-value="row.role" @change="changeAccountRole(row, $event)"><el-option v-for="role in roles" :key="role" :label="role" :value="role" /></el-select></template></el-table-column>
          <el-table-column label="Status" width="110"><template #default="{ row }"><el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? 'Active' : 'Disabled' }}</el-tag></template></el-table-column>
          <el-table-column label="Created" min-width="170"><template #default="{ row }">{{ dateTime(row.createdAtUtc) }}</template></el-table-column>
          <el-table-column label="Action" width="120"><template #default="{ row }"><el-button link :type="row.isActive ? 'danger' : 'success'" @click="changeAccountStatus(row)">{{ row.isActive ? 'Disable' : 'Enable' }}</el-button></template></el-table-column>
        </el-table><p class="count">{{ accountTotal }} account(s)</p>
      </el-tab-pane>
      <el-tab-pane label="Coupons" name="coupons">
        <section class="toolbar"><el-button type="primary" @click="openCoupon()">Create coupon</el-button></section>
        <el-table :data="coupons" empty-text="No coupons found.">
          <el-table-column prop="code" label="Code" /><el-table-column prop="name" label="Name" />
          <el-table-column label="Minimum spend"><template #default="{ row }">{{ money(row.minimumSpend) }}</template></el-table-column>
          <el-table-column label="Discount"><template #default="{ row }">{{ money(row.discountAmount) }}</template></el-table-column>
          <el-table-column label="Issued / used" width="130"><template #default="{ row }">{{ row.issuedQuantity }} / {{ row.usedQuantity }}</template></el-table-column>
          <el-table-column label="Status"><template #default="{ row }"><el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? 'Active' : 'Disabled' }}</el-tag></template></el-table-column>
          <el-table-column label="Actions" width="240"><template #default="{ row }"><el-button link type="primary" @click="openIssue(row)">Issue</el-button><el-button link @click="openCoupon(row)">Edit</el-button><el-button link :type="row.isActive ? 'danger' : 'success'" @click="changeCouponStatus(row)">{{ row.isActive ? 'Disable' : 'Enable' }}</el-button></template></el-table-column>
        </el-table>
        <h3>Customer coupon balances</h3>
        <el-table :data="couponAllocations" empty-text="No coupons have been issued.">
          <el-table-column prop="customerName" label="Customer" /><el-table-column prop="userId" label="Customer ID" width="110" />
          <el-table-column prop="couponCode" label="Coupon" /><el-table-column prop="issuedQuantity" label="Issued" width="90" />
          <el-table-column prop="usedQuantity" label="Used" width="90" /><el-table-column prop="remainingQuantity" label="Remaining" width="110" />
        </el-table>
      </el-tab-pane>
      <el-tab-pane label="Orders" name="orders">
        <section class="toolbar"><el-input v-model="orderFilter.userId" placeholder="Customer ID" /><el-input v-model="orderFilter.status" placeholder="Order status" /><el-button type="primary" @click="loadOrders">Search</el-button></section>
        <el-table :data="orders" empty-text="No orders found."><el-table-column prop="orderId" label="Order ID" /><el-table-column prop="userId" label="Customer ID" /><el-table-column prop="status" label="Status" /><el-table-column label="Total"><template #default="{ row }">{{ money(row.price) }}</template></el-table-column><el-table-column prop="itemCount" label="Items" /><el-table-column label="Placed"><template #default="{ row }">{{ dateTime(row.orderTimestamp) }}</template></el-table-column></el-table><p class="count">{{ orderTotal }} order(s)</p>
      </el-tab-pane>
      <el-tab-pane label="Audit logs" name="audit">
        <section class="toolbar audit-filter"><el-input v-model="auditFilter.actor" placeholder="Actor" /><el-input v-model="auditFilter.action" placeholder="Action" /><el-input v-model="auditFilter.targetType" placeholder="Target type" /><el-input v-model="auditFilter.result" placeholder="Result" /><el-button type="primary" @click="loadAuditLogs">Search</el-button></section>
        <el-alert title="Audit records are append-only and shown newest first." type="info" :closable="false" />
        <el-table :data="auditLogs" empty-text="No audit records found."><el-table-column label="Time" min-width="165"><template #default="{ row }">{{ dateTime(row.timestampUtc) }}</template></el-table-column><el-table-column prop="actor" label="Actor" /><el-table-column prop="role" label="Role" /><el-table-column prop="action" label="Action" min-width="150" /><el-table-column label="Target"><template #default="{ row }">{{ row.targetType }} #{{ row.targetId }}</template></el-table-column><el-table-column prop="result" label="Result" /><el-table-column prop="correlationId" label="Correlation ID" min-width="190" /></el-table><p class="count">{{ auditTotal }} audit record(s)</p>
      </el-tab-pane>
    </el-tabs>
    <el-dialog v-model="couponDialog" :title="editingCouponId ? 'Edit coupon' : 'Create coupon'" width="520px">
      <el-form label-position="top"><el-form-item label="Code"><el-input v-model="couponForm.code" :disabled="!!editingCouponId" /></el-form-item><el-form-item label="Name"><el-input v-model="couponForm.name" /></el-form-item><div class="form-grid"><el-form-item label="Minimum spend"><el-input-number v-model="couponForm.minimumSpend" :min="0" /></el-form-item><el-form-item label="Discount amount"><el-input-number v-model="couponForm.discountAmount" :min="0.01" /></el-form-item></div><el-form-item label="Starts at"><el-date-picker v-model="couponForm.startsAtUtc" type="datetime" /></el-form-item><el-form-item label="Expires at"><el-date-picker v-model="couponForm.expiresAtUtc" type="datetime" /></el-form-item></el-form>
      <template #footer><el-button @click="couponDialog = false">Cancel</el-button><el-button type="primary" @click="saveCoupon">Save coupon</el-button></template>
    </el-dialog>
    <el-dialog v-model="issueDialog" :title="`Issue ${issuingCoupon?.code || 'coupon'}`" width="460px">
      <el-form label-position="top">
        <el-form-item label="Customer">
          <el-select v-model="issueForm.userId" filterable placeholder="Select a customer" style="width:100%">
            <el-option v-for="account in accounts.filter(item => item.role === 'Customer')" :key="account.id" :label="`${account.username} (customer #${account.profileId})`" :value="account.profileId" />
          </el-select>
        </el-form-item>
        <el-form-item label="Quantity"><el-input-number v-model="issueForm.quantity" :min="1" :max="1000" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="issueDialog = false">Cancel</el-button><el-button type="primary" @click="saveIssue">Issue coupon</el-button></template>
    </el-dialog>
  </main>
</template>

<style scoped>
.admin-page{max-width:1180px;margin:0 auto;padding:48px 0 72px;color:#193d36}.hero{display:flex;justify-content:space-between;gap:24px;align-items:flex-start;margin-bottom:28px}.hero h1{font-size:42px;margin:4px 0 8px}.hero p{margin:0;color:#647a74}.eyebrow{color:#df6d43!important;font-weight:800;letter-spacing:.12em}.admin-tabs{background:white;border:1px solid #e2eae6;border-radius:14px;padding:18px 24px}.toolbar{display:flex;gap:12px;margin:12px 0 18px}.toolbar .el-input,.toolbar .el-select{max-width:200px}.audit-filter{flex-wrap:wrap}.count{color:#71837e;text-align:right}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:18px}@media(max-width:700px){.hero{display:block}.hero .el-button{margin-top:16px}.toolbar{flex-direction:column}.toolbar .el-input,.toolbar .el-select{max-width:none}.form-grid{grid-template-columns:1fr}}
</style>
