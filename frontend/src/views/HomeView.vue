<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import http from '@/api/http'

const health = ref('checking...')
const router = useRouter()

onMounted(async () => {
  try {
    const data = await http.get('/health')
    health.value = data?.status ?? 'ok'
  } catch {
    health.value = 'unreachable'
  }
})
</script>

<template>
  <div>
    <p>骨架已就绪。后端健康检查：<strong>{{ health }}</strong></p>
    <p>第一周开发「商家创建菜品」。</p>
    <el-button type="primary" @click="router.push('/merchant/dishes')">Open Merchant Dish Management</el-button>
  </div>
</template>
