<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useMerchantStore } from '@/stores/merchant'
import {
  createSpecialOffer,
  deleteSpecialOffer,
  listSpecialOffers,
  updateSpecialOffer,
} from '@/api/merchant'
import {
  buildSpecialOfferPayload,
  buildSpecialOfferUpdatePayload,
  isValidSpecialOffer,
} from '@/utils/specialOffer'

const merchantStore = useMerchantStore()
const formRef = ref()
const offers = ref([])
const loading = ref(false)
const submitting = ref(false)
const editingOfferId = ref(null)

const form = reactive({
  minPrice: null,
  amountRemission: null,
})

function validateOffer(_rule, _value, callback) {
  if (!isValidSpecialOffer(form.minPrice, form.amountRemission)) {
    callback(new Error('满减门槛和减免金额必须为两位小数以内的正数，且减免金额小于门槛'))
    return
  }

  callback()
}

const rules = {
  minPrice: [{ validator: validateOffer, trigger: ['blur', 'change'] }],
  amountRemission: [{ validator: validateOffer, trigger: ['blur', 'change'] }],
}

async function loadOffers() {
  loading.value = true
  try {
    offers.value = await listSpecialOffers(merchantStore.merchantId)
  } finally {
    loading.value = false
  }
}

function resetForm() {
  form.minPrice = null
  form.amountRemission = null
  formRef.value?.clearValidate()
}

function startEdit(offer) {
  editingOfferId.value = offer.id
  form.minPrice = offer.minPrice
  form.amountRemission = offer.amountRemission
  formRef.value?.clearValidate()
}

function cancelEdit() {
  editingOfferId.value = null
  resetForm()
}

async function submit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    if (editingOfferId.value !== null) {
      await updateSpecialOffer(
        editingOfferId.value,
        merchantStore.merchantId,
        buildSpecialOfferUpdatePayload(form),
      )
      ElMessage.success('满减活动更新成功')
    } else {
      await createSpecialOffer(
        buildSpecialOfferPayload(form, merchantStore.merchantId),
      )
      ElMessage.success('满减活动创建成功')
    }

    await loadOffers()
    editingOfferId.value = null
    resetForm()
  } finally {
    submitting.value = false
  }
}

async function removeOffer(offer) {
  try {
    await ElMessageBox.confirm(
      `确定要删除“满${offer.minPrice}减${offer.amountRemission}”活动吗？`,
      '确认删除活动',
      { type: 'warning', confirmButtonText: '确认', cancelButtonText: '取消' },
    )
    await deleteSpecialOffer(offer.id, merchantStore.merchantId)
    if (editingOfferId.value === offer.id) {
      cancelEdit()
    }
    ElMessage.success('满减活动已删除')
    await loadOffers()
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') throw error
  }
}

onMounted(loadOffers)
</script>

<template>
  <section class="offer-page">
    <div class="page-heading">
      <div>
        <h1>Special Offer Management</h1>
        <p>Merchant ID: {{ merchantStore.merchantId }}</p>
      </div>
      <el-button :loading="loading" @click="loadOffers">Refresh</el-button>
    </div>

    <el-card class="form-card" shadow="never">
      <template #header>{{ editingOfferId !== null ? 'Edit Special Offer' : 'Create Special Offer' }}</template>
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top" @submit.prevent="submit">
        <div class="form-grid">
          <el-form-item label="Minimum Order Price" prop="minPrice">
            <el-input-number
              v-model="form.minPrice"
              :min="0.01"
              :precision="2"
              :step="1"
              controls-position="right"
            />
          </el-form-item>
          <el-form-item label="Discount Amount" prop="amountRemission">
            <el-input-number
              v-model="form.amountRemission"
              :min="0.01"
              :precision="2"
              :step="1"
              controls-position="right"
            />
          </el-form-item>
        </div>
        <el-button type="primary" :loading="submitting" @click="submit">
          {{ editingOfferId !== null ? 'Save Changes' : 'Create Offer' }}
        </el-button>
        <el-button v-if="editingOfferId !== null" :disabled="submitting" @click="cancelEdit">
          Cancel
        </el-button>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <template #header>Special Offer List</template>
      <el-table v-loading="loading" :data="offers" stripe>
        <el-table-column prop="minPrice" label="Minimum Order Price" min-width="200" />
        <el-table-column prop="amountRemission" label="Discount Amount" min-width="180" />
        <el-table-column label="Actions" width="180" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="startEdit(scope.row)">Edit</el-button>
            <el-button link type="danger" @click="removeOffer(scope.row)">Delete</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loading && offers.length === 0" description="No special offers yet" />
    </el-card>
  </section>
</template>

<style scoped>
.offer-page { max-width: 1100px; margin: 0 auto; }
.page-heading { display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; }
.page-heading h1 { margin: 0; }
.page-heading p { color: #6b7280; margin: 6px 0 0; }
.form-card { margin-bottom: 18px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0 18px; }
@media (max-width: 700px) { .form-grid { grid-template-columns: 1fr; } }
</style>

