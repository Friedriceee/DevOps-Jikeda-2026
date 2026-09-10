<script setup>
import { onMounted, ref } from 'vue'
import http from '@/api/http'

const health = ref('checking...')

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
    <p>第一周开发「商家创建菜品」，见 <code>docs/week-1-plan.md</code>。</p>
  </div>
</template>
