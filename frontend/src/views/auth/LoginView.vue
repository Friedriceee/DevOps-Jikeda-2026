<script setup>
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { login } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'
import { postLoginDestination } from '@/router/guards'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const form = reactive({
  username: '',
  password: '',
})
const loading = ref(false)

function validate() {
  if (!form.username.trim()) {
    ElMessage.warning('请输入用户名')
    return false
  }
  if (!form.password) {
    ElMessage.warning('请输入密码')
    return false
  }
  return true
}

async function submit() {
  if (!validate()) return

  loading.value = true
  try {
    const session = await login({
      username: form.username.trim(),
      password: form.password,
    })
    authStore.setSession(session)
    ElMessage.success('登录成功')

    const redirect = postLoginDestination(route.query.redirect, authStore.role)
    await router.replace(redirect)
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('登录失败，请稍后重试')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="login-page">
    <el-card class="login-card" shadow="never">
      <template #header>
        <div class="login-title">顾客登录</div>
      </template>

      <el-form label-position="top" @submit.prevent="submit">
        <el-form-item label="用户名">
          <el-input v-model="form.username" autocomplete="username" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input
            v-model="form.password"
            type="password"
            show-password
            autocomplete="current-password"
            placeholder="请输入密码"
          />
        </el-form-item>
        <el-button type="primary" native-type="submit" :loading="loading" class="submit-button">
          登录
        </el-button>
      </el-form>
    </el-card>
  </section>
</template>

<style scoped>
.login-page { max-width: 460px; margin: 64px auto; }
.login-title { font-size: 20px; font-weight: 600; }
.submit-button { width: 100%; }
</style>
