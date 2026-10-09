<script setup>
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

async function logout() {
  authStore.clearSession()
  ElMessage.success('You have been signed out.')
  await router.push({ name: 'home' })
}
</script>

<template>
  <el-container class="app-shell">
    <el-header class="app-header">
      <button class="brand" type="button" @click="router.push({ name: 'home' })"><span>●</span> Plateful</button>
      <nav class="main-nav" aria-label="Main navigation">
        <el-button link @click="router.push({ name: 'customer-merchants' })">Explore</el-button>
        <el-button v-if="authStore.role === 'Customer'" link @click="router.push({ name: 'customer-cart' })">Cart</el-button>
        <el-button v-if="authStore.role === 'Merchant'" link @click="router.push({ name: 'merchant-dishes' })">Merchant portal</el-button>
      </nav>
      <div class="header-actions">
        <span v-if="authStore.isAuthenticated" class="user-info">
          {{ authStore.user?.displayName || authStore.role || 'Signed in' }}
        </span>
        <el-button v-if="authStore.isAuthenticated" @click="logout">Sign out</el-button>
        <el-button v-else type="primary" @click="router.push({ name: 'login' })">Sign in</el-button>
      </div>
    </el-header>
    <el-main class="app-main"><router-view /></el-main>
  </el-container>
</template>

<style scoped>
.app-shell { min-height: 100vh; }.app-header { display: flex; align-items: center; justify-content: space-between; height: 68px; padding: 0 max(24px, calc((100% - 1180px) / 2)); border-bottom: 1px solid #e5e9e5; background: rgba(255,255,255,.94); }.brand { border: 0; background: transparent; color: #153c35; cursor: pointer; font-size: 20px; font-weight: 800; letter-spacing: -.04em; }.brand span { color: #e36e43; }.main-nav, .header-actions { display: flex; align-items: center; gap: 14px; }.main-nav .el-button { color: #49615b; }.user-info { color: #597069; font-size: 14px; }.app-main { padding: 0 24px; }@media (max-width:620px) { .app-header { padding: 0 16px; }.main-nav { display: none; }.app-main { padding: 0 16px; } }
</style>
