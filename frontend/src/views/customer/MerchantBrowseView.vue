<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { listMerchantMenu, listMerchants } from '@/api/customer'
import { useCartStore } from '@/stores/cart'
import { formatYuan } from '@/utils/money'

const router = useRouter()
const cart = useCartStore()
const merchants = ref([])
const dishes = ref([])
const selectedMerchant = ref(null)
const merchantsLoading = ref(false)
const menuLoading = ref(false)
const loadError = ref('')
const menuError = ref('')
const addingDishId = ref(null)

const visibleDishes = computed(() => dishes.value.filter((dish) => dish.isActive !== false))

function isSoldOut(dish) {
  return dish.isSoldOut === true || Number(dish.inventory ?? 0) <= 0
}

async function loadMerchants() {
  merchantsLoading.value = true
  loadError.value = ''
  try {
    merchants.value = await listMerchants()
    if (merchants.value.length > 0) await selectMerchant(merchants.value[0])
  } catch {
    loadError.value = 'Could not load restaurants. Please try again.'
  } finally {
    merchantsLoading.value = false
  }
}

async function selectMerchant(merchant) {
  selectedMerchant.value = merchant
  menuLoading.value = true
  menuError.value = ''
  dishes.value = []
  try {
    dishes.value = await listMerchantMenu(merchant.id)
  } catch {
    menuError.value = 'Could not load this menu. Please try again.'
  } finally {
    menuLoading.value = false
  }
}

function showPromotion(merchant) {
  return merchant.promotionInfo || merchant.specialOffer?.description || ''
}

async function handleAddToCart(dish) {
  if (isSoldOut(dish)) return
  addingDishId.value = dish.id
  try {
    await cart.addItem(dish)
    ElMessage.success('Added to your cart.')
  } finally {
    addingDishId.value = null
  }
}

onMounted(loadMerchants)
</script>

<template>
  <section class="browse-page">
    <div class="page-heading">
      <div>
        <h1>Browse Merchants and Menu</h1>
        <p>Choose a restaurant and add available dishes to your cart.</p>
      </div>
      <div class="heading-actions">
        <el-button @click="router.push({ name: 'customer-cart' })">
          Cart{{ cart.totalCount ? ` (${cart.totalCount})` : '' }}
        </el-button>
        <el-button :loading="merchantsLoading" @click="loadMerchants">Refresh</el-button>
      </div>
    </div>

    <el-alert v-if="loadError" :title="loadError" type="error" show-icon />

    <el-row :gutter="18" class="content-grid">
      <el-col :xs="24" :md="8">
        <el-card shadow="never">
          <template #header>Merchants</template>
          <el-skeleton v-if="merchantsLoading" :rows="5" animated />
          <el-empty v-else-if="merchants.length === 0" description="No restaurants are available yet." />
          <div v-else class="merchant-list">
            <button
              v-for="merchant in merchants"
              :key="merchant.id"
              class="merchant-item"
              :class="{ selected: selectedMerchant?.id === merchant.id }"
              type="button"
              @click="selectMerchant(merchant)"
            >
              <strong>{{ merchant.name }}</strong>
              <span>{{ merchant.address || 'Address unavailable' }}</span>
              <small>{{ merchant.openingHours || 'Hours unavailable' }}</small>
            </button>
          </div>
        </el-card>
      </el-col>

      <el-col :xs="24" :md="16">
        <el-card shadow="never">
          <template #header>
            <div class="menu-heading">
              <span>{{ selectedMerchant?.name || 'Menu' }}</span>
              <small v-if="selectedMerchant?.contact">{{ selectedMerchant.contact }}</small>
            </div>
          </template>

          <el-skeleton v-if="menuLoading" :rows="6" animated />
          <el-alert v-else-if="menuError" :title="menuError" type="error" show-icon />
          <el-empty v-else-if="!selectedMerchant" description="Select a restaurant to view its menu." />
          <el-empty v-else-if="visibleDishes.length === 0" description="No dishes are currently available." />
          <div v-else class="dish-grid">
            <el-card v-for="dish in visibleDishes" :key="dish.id" class="dish-card" shadow="hover">
              <img v-if="dish.imageUrl" :src="dish.imageUrl" :alt="dish.name" class="dish-image">
              <div class="dish-content">
                <div class="dish-title">
                  <strong>{{ dish.name }}</strong>
                  <el-tag v-if="isSoldOut(dish)" type="info" size="small">Sold out</el-tag>
                </div>
                <span class="dish-category">{{ dish.category || 'Uncategorised' }}</span>
                <div class="dish-footer">
                  <strong class="dish-price">{{ formatYuan(dish.price) }}</strong>
                  <el-button
                    size="small"
                    type="primary"
                    :disabled="isSoldOut(dish) || addingDishId === dish.id"
                    :loading="addingDishId === dish.id"
                    @click="handleAddToCart(dish)"
                  >
                    Add to cart
                  </el-button>
                </div>
              </div>
            </el-card>
          </div>

          <el-alert
            v-if="selectedMerchant && showPromotion(selectedMerchant)"
            class="promotion-note"
            :title="`Special offer: ${showPromotion(selectedMerchant)}`"
            type="info"
            :closable="false"
          />
        </el-card>
      </el-col>
    </el-row>
  </section>
</template>

<style scoped>
.browse-page { max-width: 1180px; margin: 0 auto; }
.page-heading { display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; }
.heading-actions { display: flex; gap: 8px; }
.page-heading h1 { margin: 0; }
.page-heading p { color: #6b7280; margin: 6px 0 0; }
.content-grid { margin-top: 18px; }
.merchant-list { display: grid; gap: 8px; }
.merchant-item { display: grid; gap: 4px; width: 100%; padding: 12px; border: 1px solid #e5e7eb; border-radius: 8px; background: white; text-align: left; cursor: pointer; }
.merchant-item:hover, .merchant-item.selected { border-color: #409eff; background: #f0f8ff; }
.merchant-item span, .merchant-item small, .dish-category, .menu-heading small { color: #6b7280; }
.menu-heading { display: flex; justify-content: space-between; gap: 12px; align-items: center; }
.dish-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 14px; }
.dish-card { overflow: hidden; }
.dish-image { display: block; width: 100%; height: 130px; object-fit: cover; }
.dish-content { display: grid; gap: 8px; padding-top: 10px; }
.dish-title, .dish-footer { display: flex; justify-content: space-between; align-items: center; gap: 8px; }
.dish-price { color: #e6a23c; }
.promotion-note { margin-top: 18px; }
@media (max-width: 700px) { .page-heading { align-items: flex-start; gap: 12px; } .menu-heading { align-items: flex-start; flex-direction: column; } }
</style>
