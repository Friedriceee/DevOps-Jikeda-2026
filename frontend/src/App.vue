<script setup>
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

async function logout() {
  authStore.clearSession()
  ElMessage.success('已退出登录')
  await router.push({ name: 'home' })
}
</script>

<template>
  <el-container style="min-height: 100vh">
    <el-header class="app-header">
      外卖平台
      <div class="header-actions">
        <span v-if="authStore.isAuthenticated" class="user-info">
          {{ authStore.user?.displayName || authStore.role || '已登录' }}
        </span>
        <el-button v-if="authStore.isAuthenticated" link @click="logout">退出登录</el-button>
        <el-button v-else link @click="router.push({ name: 'login' })">登录</el-button>
      </div>
    </el-header>
    <el-main>
      <router-view />
    </el-main>
  </el-container>
</template>

<style scoped>
.app-header { display: flex; align-items: center; justify-content: space-between; font-weight: 600; }
.header-actions { display: flex; align-items: center; gap: 12px; font-weight: 400; }
.user-info { color: #6b7280; }
</style>
