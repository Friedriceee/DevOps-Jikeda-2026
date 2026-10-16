<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { registerMerchant } from '@/api/auth'

const router = useRouter()
const loading = ref(false)
const form = reactive({ username: '', password: '', merchantName: '', merchantAddress: '', contact: '', dishType: '', openHour: 9, closeHour: 21, walletPassword: '' })

async function submit() {
  if (form.username.trim().length < 3 || form.password.length < 6 || !form.merchantName.trim() || !form.merchantAddress.trim() || !form.contact.trim() || form.walletPassword.length < 6) {
    ElMessage.warning('Complete all required fields and use passwords of at least 6 characters.')
    return
  }
  loading.value = true
  try {
    const username = form.username.trim()
    await registerMerchant({
      username, password: form.password, merchantName: form.merchantName.trim(),
      merchantAddress: form.merchantAddress.trim(), contact: form.contact.trim(),
      dishType: form.dishType.trim() || null,
      timeForOpenBusiness: Number(form.openHour) * 3600,
      timeForCloseBusiness: Number(form.closeHour) * 3600,
      walletPassword: form.walletPassword,
    })
    ElMessage.success('Restaurant account created. Sign in to create dishes and offers.')
    await router.replace({ name: 'login', query: { username } })
  } finally { loading.value = false }
}
</script>

<template>
  <section class="registration"><el-card shadow="never">
    <template #header><strong>Restaurant Registration</strong></template>
    <el-form label-position="top" @submit.prevent="submit">
      <div class="grid"><el-form-item label="Username"><el-input v-model="form.username" /></el-form-item><el-form-item label="Login password"><el-input v-model="form.password" type="password" show-password /></el-form-item></div>
      <el-form-item label="Restaurant name"><el-input v-model="form.merchantName" /></el-form-item>
      <el-form-item label="Restaurant address"><el-input v-model="form.merchantAddress" /></el-form-item>
      <div class="grid"><el-form-item label="Contact"><el-input v-model="form.contact" /></el-form-item><el-form-item label="Cuisine"><el-input v-model="form.dishType" placeholder="Optional" /></el-form-item></div>
      <div class="grid"><el-form-item label="Opening hour"><el-input-number v-model="form.openHour" :min="0" :max="23" /></el-form-item><el-form-item label="Closing hour"><el-input-number v-model="form.closeHour" :min="0" :max="23" /></el-form-item></div>
      <el-form-item label="Wallet password"><el-input v-model="form.walletPassword" type="password" show-password /></el-form-item>
      <el-button type="primary" native-type="submit" :loading="loading" class="wide">Create restaurant</el-button>
      <el-button link @click="router.push({ name: 'login' })">Back to login</el-button>
    </el-form>
  </el-card></section>
</template>
<style scoped>.registration{max-width:620px;margin:40px auto}.grid{display:grid;grid-template-columns:1fr 1fr;gap:16px}.wide{width:100%;margin-bottom:12px}@media(max-width:620px){.grid{grid-template-columns:1fr}}</style>
