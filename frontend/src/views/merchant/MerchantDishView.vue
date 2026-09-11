<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useMerchantStore } from '@/stores/merchant'
import { createDish, deleteDish, listDishes, updateDish } from '@/api/merchant'
import { buildDishPayload, isValidDishName, isValidInventory } from '@/utils/dish'
import { formatYuan, isValidPrice } from '@/utils/money'

const merchantStore = useMerchantStore()
const formRef = ref()
const dishes = ref([])
const loading = ref(false)
const submitting = ref(false)

const form = reactive({
  name: '',
  price: null,
  category: '',
  imageUrl: '',
  inventory: 0,
})

const editDialogVisible = ref(false)
const editFormRef = ref()
const editSubmitting = ref(false)
const editingDishId = ref(null)
const editForm = reactive({
  name: '',
  price: null,
  category: '',
  imageUrl: '',
  inventory: 0,
})
const deletingId = ref(null)

const rules = {
  name: [
    {
      validator: (_rule, value, callback) => {
        if (!isValidDishName(value)) {
          callback(new Error('菜品名称必须为 1-50 个字符'))
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
          callback(new Error('价格必须大于 0'))
          return
        }
        if (Math.round(Number(value) * 100) / 100 !== Number(value)) {
          callback(new Error('价格最多保留两位小数'))
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
          callback(new Error('库存必须是大于等于 0 的整数'))
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
  formRef.value?.clearValidate()
}

async function submit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    await createDish(buildDishPayload(form, merchantStore.merchantId))
    ElMessage.success('菜品创建成功')
    resetForm()
    await loadDishes()
  } finally {
    submitting.value = false
  }
}

function openEdit(row) {
  editingDishId.value = row.id
  editForm.name = row.name
  editForm.price = row.price
  editForm.category = row.category ?? ''
  editForm.imageUrl = row.imageUrl ?? ''
  editForm.inventory = row.inventory
  editDialogVisible.value = true
  editFormRef.value?.clearValidate()
}

function closeEdit() {
  editDialogVisible.value = false
  editingDishId.value = null
}

async function submitEdit() {
  const valid = await editFormRef.value?.validate().catch(() => false)
  if (!valid) return

  editSubmitting.value = true
  try {
    await updateDish(editingDishId.value, buildDishPayload(editForm, merchantStore.merchantId))
    ElMessage.success('菜品已更新')
    closeEdit()
    await loadDishes()
  } finally {
    editSubmitting.value = false
  }
}

async function removeDish(row) {
  try {
    await ElMessageBox.confirm(`确认下架「${row.name}」？此操作不可撤销。`, '下架菜品', {
      type: 'warning',
      confirmButtonText: '下架',
      cancelButtonText: '取消',
    })
  } catch {
    return // 用户取消
  }

  deletingId.value = row.id
  try {
    await deleteDish(row.id, merchantStore.merchantId)
    ElMessage.success('菜品已下架')
    await loadDishes()
  } finally {
    deletingId.value = null
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
      <template #header>Create Dish</template>
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
          <el-form-item class="wide-item" label="Image URL" prop="imageUrl">
            <el-input v-model="form.imageUrl" placeholder="Optional http/https URL" />
          </el-form-item>
        </div>
        <el-button type="primary" :loading="submitting" @click="submit">Create Dish</el-button>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <template #header>Dish List</template>
      <el-table v-loading="loading" :data="dishes" stripe>
        <el-table-column prop="name" label="Name" min-width="180" />
        <el-table-column label="Price" width="130">
          <template #default="scope">{{ formatYuan(scope.row.price) }}</template>
        </el-table-column>
        <el-table-column prop="category" label="Category" width="160" />
        <el-table-column prop="inventory" label="Inventory" width="120" />
        <el-table-column label="Image" min-width="220">
          <template #default="scope">
            <a v-if="scope.row.imageUrl" :href="scope.row.imageUrl" target="_blank" rel="noreferrer">Open image</a>
            <span v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="160" fixed="right">
          <template #default="scope">
            <el-button size="small" @click="openEdit(scope.row)">Edit</el-button>
            <el-button
              size="small"
              type="danger"
              plain
              :loading="deletingId === scope.row.id"
              @click="removeDish(scope.row)"
            >
              Remove
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loading && dishes.length === 0" description="No dishes yet" />
    </el-card>

    <el-dialog v-model="editDialogVisible" title="Edit Dish" width="520px" @close="closeEdit">
      <el-form ref="editFormRef" :model="editForm" :rules="rules" label-position="top" @submit.prevent="submitEdit">
        <div class="form-grid">
          <el-form-item label="Name" prop="name">
            <el-input v-model="editForm.name" maxlength="50" show-word-limit placeholder="Dish name" />
          </el-form-item>
          <el-form-item label="Price" prop="price">
            <el-input-number v-model="editForm.price" :min="0.01" :precision="2" :step="0.5" controls-position="right" />
          </el-form-item>
          <el-form-item label="Category" prop="category">
            <el-input v-model="editForm.category" maxlength="20" show-word-limit placeholder="Optional" />
          </el-form-item>
          <el-form-item label="Inventory" prop="inventory">
            <el-input-number v-model="editForm.inventory" :min="0" :step="1" controls-position="right" />
          </el-form-item>
          <el-form-item class="wide-item" label="Image URL" prop="imageUrl">
            <el-input v-model="editForm.imageUrl" placeholder="Optional http/https URL" />
          </el-form-item>
        </div>
      </el-form>
      <template #footer>
        <el-button @click="closeEdit">Cancel</el-button>
        <el-button type="primary" :loading="editSubmitting" @click="submitEdit">Save</el-button>
      </template>
    </el-dialog>
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
@media (max-width: 700px) { .form-grid { grid-template-columns: 1fr; } .wide-item { grid-column: auto; } }
</style>
