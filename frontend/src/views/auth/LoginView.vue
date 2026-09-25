<script setup>
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { login, registerCustomer } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'
import { postLoginDestination } from '@/router/guards'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const form = reactive({
  username: '',
  password: '',
  displayName: '',
  phoneNumber: '',
})
const loading = ref(false)
const registering = ref(false)

function validate() {
  if (!form.username.trim()) {
    ElMessage.warning('Enter your username.')
    return false
  }
  if (!form.password) {
    ElMessage.warning('Enter your password.')
    return false
  }
  return true
}

function validateRegistration() {
  if (!validate()) return false
  if (!form.displayName.trim()) {
    ElMessage.warning('Enter your display name.')
    return false
  }
  if (!/^\d{8,11}$/.test(form.phoneNumber.trim())) {
    ElMessage.warning('Enter an 8–11 digit phone number.')
    return false
  }
  if (form.password.length < 8) {
    ElMessage.warning('Your password must contain at least 8 characters.')
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
    ElMessage.success('Welcome back!')

    const redirect = postLoginDestination(route.query.redirect, authStore.role)
    await router.replace(redirect)
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('Sign-in failed. Please try again.')
  } finally {
    loading.value = false
  }
}

async function register() {
  if (!validateRegistration()) return
  loading.value = true
  try {
    await registerCustomer({
      username: form.username.trim(),
      password: form.password,
      displayName: form.displayName.trim(),
      phoneNumber: form.phoneNumber.trim(),
    })
    ElMessage.success('Account created. You can now sign in.')
    form.displayName = ''
    form.phoneNumber = ''
    registering.value = false
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('Could not create your account. Please try again.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="login-page">
    <el-card class="login-card" shadow="never">
      <template #header>
        <div class="login-title">{{ registering ? 'Create your account' : 'Welcome back' }}</div>
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
        <template v-if="registering">
          <el-form-item label="Display name">
            <el-input v-model="form.displayName" maxlength="50" placeholder="Enter your name" />
          </el-form-item>
          <el-form-item label="Phone number">
            <el-input v-model="form.phoneNumber" maxlength="11" inputmode="numeric" placeholder="8–11 digits" />
          </el-form-item>
        </template>
        <el-button v-if="!registering" type="primary" native-type="submit" :loading="loading" class="submit-button">Sign in</el-button>
        <el-button v-else type="primary" :loading="loading" class="submit-button" @click="register">Create account</el-button>
        <el-button link class="switch-button" @click="registering = !registering">
          {{ registering ? 'Already have an account? Sign in' : 'New here? Create an account' }}
        </el-button>
      </el-form>
    </el-card>
  </section>
</template>

<style scoped>
.login-page { max-width: 460px; margin: 64px auto; }
.login-title { font-size: 20px; font-weight: 600; }
.submit-button { width: 100%; }
.switch-button { width: 100%; margin-top: 12px; }
</style>
