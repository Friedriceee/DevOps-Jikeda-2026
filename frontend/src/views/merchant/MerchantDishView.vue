<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useMerchantStore } from '@/stores/merchant'
import { useAuthStore } from '@/stores/auth'
import { createDish, deleteDish, listDishes, updateDish } from '@/api/merchant'
import { buildDishPayload, buildDishUpdatePayload, isValidDishName, isValidInventory, menuSettingsError, promotionInput } from '@/utils/dish'
import { formatYuan, isValidPrice } from '@/utils/money'

const merchantStore = useMerchantStore()
const auth = useAuthStore()
// Use the signed-in merchant instead of the original Sprint 1 demo id.
if (auth.role === 'Merchant' && auth.user?.profileId) merchantStore.setMerchantId(auth.user.profileId)
const formRef = ref()
const dishes = ref([])
const loading = ref(false)
const submitting = ref(false)
const editingDishId = ref(null)

const form = reactive({
  name: '',
  price: null,
  category: '',
  imageUrl: '',
  inventory: 0,
  itemType: 'Standard',
  bundleSize: 2,
  promotionStartsAtUtc: '',
  promotionEndsAtUtc: '',
})

const rules = {
  name: [
    {
      validator: (_rule, value, callback) => {
        if (!isValidDishName(value)) {
          callback(new Error('Dish name must be 1–50 characters.'))
          return
        }
        callback()
      },
      trigger: ['blur', 'change'],
    },
  ],
  price: [
    {
      validator: (_rule, value, callback) => {
        if (!isValidPrice(value)) {
          callback(new Error('Price must be greater than zero.'))
          return
        }
        if (Math.round(Number(value) * 100) / 100 !== Number(value)) {
          callback(new Error('Price can have at most two decimal places.'))
          return
        }
        callback()
      },
      trigger: ['blur', 'change'],
    },
  ],
  inventory: [
    {
      validator: (_rule, value, callback) => {
        if (!isValidInventory(value)) {
          callback(new Error('Inventory must be a whole number of zero or more.'))
          return
        }
        callback()
      },
      trigger: ['blur', 'change'],
    },
  ],
}

async function loadDishes() {
  loading.value = true
  try {
    dishes.value = await listDishes(merchantStore.merchantId)
  } finally {
    loading.value = false
  }
}

function resetForm() {
  form.name = ''
  form.price = null
  form.category = ''
  form.imageUrl = ''
  form.inventory = 0
  form.itemType = 'Standard'
  form.bundleSize = 2
  form.promotionStartsAtUtc = ''
  form.promotionEndsAtUtc = ''
  formRef.value?.clearValidate()
}

function startEdit(dish) {
  editingDishId.value = dish.id
  form.name = dish.name
  form.price = dish.price
  form.category = dish.category ?? ''
  form.imageUrl = dish.imageUrl ?? ''
  form.inventory = dish.inventory
  form.itemType = dish.itemType || 'Standard'
  form.bundleSize = dish.bundleSize ?? 2
  form.promotionStartsAtUtc = promotionInput(dish.promotionStartsAtUtc)
  form.promotionEndsAtUtc = promotionInput(dish.promotionEndsAtUtc)
  formRef.value?.clearValidate()
}

function cancelEdit() {
  editingDishId.value = null
  resetForm()
}

async function submit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  const settingsError = menuSettingsError(form)
  if (settingsError) {
    ElMessage.error(settingsError)
    return
  }

  submitting.value = true
  try {
    if (editingDishId.value) {
      await updateDish(
        editingDishId.value,
        merchantStore.merchantId,
        buildDishUpdatePayload(form),
      )
      ElMessage.success('Dish updated.')
      editingDishId.value = null
    } else {
      await createDish(buildDishPayload(form, merchantStore.merchantId))
      ElMessage.success('Dish created.')
    }
    resetForm()
    await loadDishes()
  } finally {
    submitting.value = false
  }
}

async function removeDish(dish) {
  try {
    await ElMessageBox.confirm(
      `Delist and delete “${dish.name}”?`,
      'Delist dish',
      { type: 'warning', confirmButtonText: 'Delist', cancelButtonText: 'Keep dish' },
    )
    await deleteDish(dish.id, merchantStore.merchantId)
    if (editingDishId.value === dish.id) cancelEdit()
    ElMessage.success('Dish delisted.')
    await loadDishes()
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') throw error
  }
}

onMounted(loadDishes)
</script>

<template>
  <section class="dish-page">
    <div class="page-heading">
      <div>
        <h1>Menu Management</h1>
        <p>Merchant ID: {{ merchantStore.merchantId }}</p>
      </div>
      <el-button :loading="loading" @click="loadDishes">Refresh</el-button>
    </div>

    <el-card class="form-card" shadow="never">
      <template #header>{{ editingDishId ? 'Edit Dish' : 'Create Dish' }}</template>
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top" @submit.prevent="submit">
        <div class="form-grid">
          <el-form-item label="Name" prop="name">
            <el-input v-model="form.name" maxlength="50" show-word-limit placeholder="Dish name" />
          </el-form-item>
          <el-form-item label="Price" prop="price">
            <el-input-number v-model="form.price" :min="0.01" :precision="2" :step="0.5" controls-position="right" />
          </el-form-item>
          <el-form-item label="Category" prop="category">
            <el-input v-model="form.category" maxlength="20" show-word-limit placeholder="Optional" />
          </el-form-item>
          <el-form-item label="Inventory" prop="inventory">
            <el-input-number v-model="form.inventory" :min="0" :step="1" controls-position="right" />
          </el-form-item>
          <el-form-item label="Item Type">
            <select v-model="form.itemType" aria-label="Item Type" class="menu-input">
              <option>Standard</option><option>Bundle</option><option>Promotional</option>
            </select>
          </el-form-item>
          <el-form-item v-if="form.itemType === 'Bundle'" label="Bundle Size">
            <input v-model="form.bundleSize" aria-label="Bundle Size" class="menu-input" type="number" min="2" step="1">
          </el-form-item>
          <template v-if="form.itemType === 'Promotional'">
            <el-form-item label="Promotion Starts (UTC)">
              <input v-model="form.promotionStartsAtUtc" aria-label="Promotion Starts (UTC)" class="menu-input" placeholder="2026-10-10T00:00" />
            </el-form-item>
            <el-form-item label="Promotion Ends (UTC)">
              <input v-model="form.promotionEndsAtUtc" aria-label="Promotion Ends (UTC)" class="menu-input" placeholder="2026-10-11T00:00" />
            </el-form-item>
          </template>
          <el-form-item class="wide-item" label="Image URL" prop="imageUrl">
            <el-input v-model="form.imageUrl" placeholder="Optional http/https URL" />
          </el-form-item>
        </div>
        <p class="inventory-help">One inventory unit is one dish or one complete bundle. Promotion times use UTC (YYYY-MM-DDTHH:mm).</p>
        <el-button type="primary" :loading="submitting" @click="submit">
          {{ editingDishId ? 'Save Changes' : 'Create Dish' }}
        </el-button>
        <el-button v-if="editingDishId" :disabled="submitting" @click="cancelEdit">Cancel</el-button>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <template #header>Dish List</template>
      <el-table v-loading="loading" :data="dishes" stripe>
        <el-table-column prop="name" label="Name" min-width="180" />
        <el-table-column label="Price" width="100">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column prop="category" label="Category" width="110" />
        <el-table-column prop="itemType" label="Type" width="110" />
        <el-table-column prop="inventory" label="Inventory" width="100" />
        <el-table-column label="Availability" width="140">
          <template #default="scope">{{ scope.row.isAvailable ? 'Available' : Number(scope.row.inventory) <= 0 ? 'Sold out' : 'Outside promotion' }}</template>
        </el-table-column>
        <el-table-column label="Image" min-width="100">
          <template #default="scope">
            <a v-if="scope.row.imageUrl" :href="scope.row.imageUrl" target="_blank" rel="noreferrer">Open image</a>
            <span v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="150" fixed="right">
          <template #default="scope">
            <el-button link type="primary" @click="startEdit(scope.row)">Edit</el-button>
            <el-button link type="danger" @click="removeDish(scope.row)">Delist</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loading && dishes.length === 0" description="No dishes yet" />
    </el-card>
  </section>
</template>

<style scoped>
.dish-page { max-width: 1100px; margin: 0 auto; }
.page-heading { display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; }
.page-heading h1 { margin: 0; }
.page-heading p { color: #6b7280; margin: 6px 0 0; }
.form-card { margin-bottom: 18px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0 18px; }
.wide-item { grid-column: 1 / -1; }
.menu-input { box-sizing: border-box; width: 100%; min-height: 32px; border: 1px solid #dcdfe6; border-radius: 4px; padding: 4px 10px; background: white; color: #303133; font: inherit; }
.inventory-help { font-size: 13px; color: #606266; }
@media (max-width: 700px) { .form-grid { grid-template-columns: 1fr; } .wide-item { grid-column: auto; } }
</style>
