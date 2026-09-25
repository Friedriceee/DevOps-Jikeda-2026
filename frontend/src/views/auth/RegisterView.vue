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
    ElMessage.warning('Username must be between 3 and 50 characters')
    return false
  }
  if (!displayName || displayName.length > 50) {
    ElMessage.warning('Enter a display name of no more than 50 characters')
    return false
  }
  if (!/^\d{8,11}$/.test(phoneNumber)) {
    ElMessage.warning('Phone number must contain 8 to 11 digits')
    return false
  }
  if (form.password.length < 8 || form.password.length > 100) {
    ElMessage.warning('Password must be between 8 and 100 characters')
    return false
  }
  if (form.password !== form.confirmPassword) {
    ElMessage.warning('Passwords do not match')
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
    ElMessage.success('Registration successful. Please log in with your new account.')
    await router.replace({ name: 'login', query: { username } })
  } catch (error) {
    if (!error?.userNotified) ElMessage.error('Registration failed. Please try again later.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="register-page">
    <el-card class="register-card" shadow="never">
      <template #header>
        <div class="register-title">Customer Registration</div>
      </template>

      <el-form label-position="top" @submit.prevent="submit">
        <el-form-item label="Username">
          <el-input v-model="form.username" autocomplete="username" placeholder="3 to 50 characters" />
        </el-form-item>
        <el-form-item label="Display Name">
          <el-input v-model="form.displayName" autocomplete="name" placeholder="Enter your display name" />
        </el-form-item>
        <el-form-item label="Phone Number">
          <el-input v-model="form.phoneNumber" autocomplete="tel" placeholder="8 to 11 digits" />
        </el-form-item>
        <el-form-item label="Password">
          <el-input
            v-model="form.password"
            type="password"
            show-password
            autocomplete="new-password"
            placeholder="At least 8 characters"
          />
        </el-form-item>
        <el-form-item label="Confirm Password">
          <el-input
            v-model="form.confirmPassword"
            type="password"
            show-password
            autocomplete="new-password"
            placeholder="Enter your password again"
          />
        </el-form-item>
        <el-button type="primary" native-type="submit" :loading="loading" class="submit-button">
          Register
        </el-button>
        <div class="login-link">
          Already have an account?
          <el-button link type="primary" @click="router.push({ name: 'login' })">Back to login</el-button>
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
