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
  username: typeof route.query.username === 'string' ? route.query.username : '',
  password: '',
})
const loading = ref(false)

function validate() {
  if (!form.username.trim()) {
    ElMessage.warning('Enter your username')
    return false
  }
  if (!form.password) {
    ElMessage.warning('Enter your password')
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
    ElMessage.success('Login successful')

    const redirect = postLoginDestination(route.query.redirect, authStore.role)
    await router.replace(redirect)
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('Login failed. Please try again later.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="login-page">
    <el-card class="login-card" shadow="never">
      <template #header>
        <div class="login-title">Customer Login</div>
      </template>

      <el-form label-position="top" @submit.prevent="submit">
        <el-form-item label="Username">
          <el-input v-model="form.username" autocomplete="username" placeholder="Enter your username" />
        </el-form-item>
        <el-form-item label="Password">
          <el-input
            v-model="form.password"
            type="password"
            show-password
            autocomplete="current-password"
            placeholder="Enter your password"
          />
        </el-form-item>
        <el-button type="primary" native-type="submit" :loading="loading" class="submit-button">
          Log In
        </el-button>
        <div class="register-link">
          Don't have an account?
          <el-button link type="primary" @click="router.push({ name: 'register' })">Register now</el-button>
        </div>
      </el-form>
    </el-card>
  </section>
</template>

<style scoped>
.login-page { max-width: 460px; margin: 64px auto; }
.login-title { font-size: 20px; font-weight: 600; }
.submit-button { width: 100%; }
.register-link { margin-top: 18px; text-align: center; color: #6b7280; }
</style>
