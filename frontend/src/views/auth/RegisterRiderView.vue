<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { registerRider } from '@/api/auth'

const router = useRouter()
const loading = ref(false)
const form = reactive({ username: '', password: '', name: '', phoneNumber: '', zone: 'ZoneA', capacity: 3 })
async function submit() {
  if (form.username.trim().length < 3 || form.password.length < 8 || !form.name.trim() || !/^\d{8,11}$/.test(form.phoneNumber) || !form.zone.trim()) {
    ElMessage.warning('Complete all fields with a valid phone number and password.')
    return
  }
  loading.value = true
  try {
    const username = form.username.trim()
    await registerRider({ ...form, username, name: form.name.trim(), phoneNumber: form.phoneNumber.trim(), zone: form.zone.trim() })
    ElMessage.success('Rider account created. You can now sign in and accept deliveries.')
    await router.replace({ name: 'login', query: { username } })
  } finally { loading.value = false }
}
</script>
<template><section class="registration"><el-card shadow="never">
  <template #header><strong>Rider Registration</strong></template>
  <el-form label-position="top" @submit.prevent="submit">
    <el-form-item label="Username"><el-input v-model="form.username" /></el-form-item><el-form-item label="Password"><el-input v-model="form.password" type="password" show-password /></el-form-item>
    <el-form-item label="Full name"><el-input v-model="form.name" /></el-form-item><el-form-item label="Phone number"><el-input v-model="form.phoneNumber" /></el-form-item>
    <el-form-item label="Delivery zone"><el-input v-model="form.zone" placeholder="For example, ZoneA" /></el-form-item><el-form-item label="Maximum active deliveries"><el-input-number v-model="form.capacity" :min="1" :max="20" /></el-form-item>
    <el-button type="primary" native-type="submit" :loading="loading" class="wide">Create rider account</el-button><el-button link @click="router.push({ name: 'login' })">Back to login</el-button>
  </el-form>
</el-card></section></template>
<style scoped>.registration{max-width:520px;margin:40px auto}.wide{width:100%;margin-bottom:12px}</style>
