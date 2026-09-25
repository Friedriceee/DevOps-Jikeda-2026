<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { registerCustomer } from '@/api/auth'

const router = useRouter()
const loading = ref(false)
const form = reactive({
  username: '',
  displayName: '',
  phoneNumber: '',
  password: '',
  confirmPassword: '',
})

function validate() {
  const username = form.username.trim()
  const displayName = form.displayName.trim()
  const phoneNumber = form.phoneNumber.trim()

  if (username.length < 3 || username.length > 50) {
    ElMessage.warning('用户名长度应为 3 到 50 个字符')
    return false
  }
  if (!displayName || displayName.length > 50) {
    ElMessage.warning('请输入不超过 50 个字符的显示名称')
    return false
  }
  if (!/^\d{8,11}$/.test(phoneNumber)) {
    ElMessage.warning('手机号应为 8 到 11 位数字')
    return false
  }
  if (form.password.length < 8 || form.password.length > 100) {
    ElMessage.warning('密码长度应为 8 到 100 个字符')
    return false
  }
  if (form.password !== form.confirmPassword) {
    ElMessage.warning('两次输入的密码不一致')
    return false
  }
  return true
}

async function submit() {
  if (!validate()) return

  loading.value = true
  try {
    const username = form.username.trim()
    await registerCustomer({
      username,
      password: form.password,
      displayName: form.displayName.trim(),
      phoneNumber: form.phoneNumber.trim(),
    })
    ElMessage.success('注册成功，请使用新账号登录')
    await router.replace({ name: 'login', query: { username } })
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('注册失败，请稍后重试')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="register-page">
    <el-card class="register-card" shadow="never">
      <template #header>
        <div class="register-title">顾客注册</div>
      </template>

      <el-form label-position="top" @submit.prevent="submit">
        <el-form-item label="用户名">
          <el-input v-model="form.username" autocomplete="username" placeholder="3 到 50 个字符" />
        </el-form-item>
        <el-form-item label="显示名称">
          <el-input v-model="form.displayName" autocomplete="name" placeholder="请输入显示名称" />
        </el-form-item>
        <el-form-item label="手机号">
          <el-input v-model="form.phoneNumber" autocomplete="tel" placeholder="8 到 11 位数字" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input
            v-model="form.password"
            type="password"
            show-password
            autocomplete="new-password"
            placeholder="至少 8 个字符"
          />
        </el-form-item>
        <el-form-item label="确认密码">
          <el-input
            v-model="form.confirmPassword"
            type="password"
            show-password
            autocomplete="new-password"
            placeholder="请再次输入密码"
          />
        </el-form-item>
        <el-button type="primary" native-type="submit" :loading="loading" class="submit-button">
          注册
        </el-button>
        <div class="login-link">
          已有账号？
          <el-button link type="primary" @click="router.push({ name: 'login' })">返回登录</el-button>
        </div>
      </el-form>
    </el-card>
  </section>
</template>

<style scoped>
.register-page { max-width: 520px; margin: 40px auto; }
.register-title { font-size: 20px; font-weight: 600; }
.submit-button { width: 100%; }
.login-link { margin-top: 18px; text-align: center; color: #6b7280; }
</style>
