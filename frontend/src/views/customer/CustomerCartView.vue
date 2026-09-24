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
    ElMessage.error('购物车加载失败，请稍后重试')
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
    await ElMessageBox.confirm(`确定移除“${item.dishName}”吗？`, '移除商品', {
      type: 'warning', confirmButtonText: '移除', cancelButtonText: '取消',
    })
    updatingItemId.value = item.id
    await cart.removeItem(item.id)
    ElMessage.success('商品已移除')
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') throw error
  } finally {
    updatingItemId.value = null
  }
}

async function clearMerchant(group) {
  try {
    await ElMessageBox.confirm(`确定清空“${group.merchantName}”的商品吗？`, '清空商家购物车', {
      type: 'warning', confirmButtonText: '清空', cancelButtonText: '取消',
    })
    await cart.clearMerchant(group.merchantId)
    ElMessage.success('该商家购物车已清空')
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
        <h1>我的购物车</h1>
        <p>金额与满减优惠由服务器按当前菜品价格计算。</p>
      </div>
      <div class="heading-actions">
        <el-button :loading="cart.loading" @click="loadCart">刷新</el-button>
        <el-button type="primary" :disabled="cart.items.length === 0" @click="router.push({ name: 'user-orders' })">
          去结算
        </el-button>
      </div>
    </div>

    <el-empty v-if="!cart.loading && cart.groups.length === 0" description="购物车为空">
      <el-button type="primary" @click="router.push({ name: 'customer-merchants' })">去选购</el-button>
    </el-empty>

    <el-card v-for="group in cart.groups" :key="group.merchantId" class="merchant-card" shadow="never">
      <template #header>
        <div class="merchant-heading">
          <strong>{{ group.merchantName }}</strong>
          <el-button link type="danger" @click="clearMerchant(group)">清空该商家</el-button>
        </div>
      </template>
      <el-table :data="group.items" stripe>
        <el-table-column prop="dishName" label="菜品" min-width="180" />
        <el-table-column label="单价" width="130">
          <template #default="scope">{{ formatYuan(scope.row.unitPrice) }}</template>
        </el-table-column>
        <el-table-column label="数量" width="180">
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
        <el-table-column label="小计" width="130">
          <template #default="scope">{{ formatYuan(scope.row.lineTotal) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="scope">
            <el-button link type="danger" :loading="updatingItemId === scope.row.id" @click="removeItem(scope.row)">
              移除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="merchant-summary">
        <span>小计：{{ formatYuan(group.subtotal) }}</span>
        <span>满减：-{{ formatYuan(group.discount) }}</span>
        <strong>商家合计：{{ formatYuan(group.total) }}</strong>
      </div>
    </el-card>

    <el-card v-if="cart.groups.length > 0" class="cart-summary" shadow="never">
      <span>共 {{ cart.totalCount }} 件商品</span>
      <span>商品小计：{{ formatYuan(cart.subtotal) }}</span>
      <span>优惠：-{{ formatYuan(cart.discount) }}</span>
      <strong>应付：{{ formatYuan(cart.total) }}</strong>
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
