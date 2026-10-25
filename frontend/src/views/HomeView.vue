<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import http from '@/api/http'

const health = ref('Checking service…')
const router = useRouter()

onMounted(async () => {
  try {
    const data = await http.get('/health')
    health.value = data?.status ?? 'Online'
  } catch {
    health.value = 'Unavailable'
  }
})
</script>

<template>
  <section class="home-page">
    <div class="hero">
      <div class="hero-copy">
        <span class="eyebrow">FRESH FOOD, DELIVERED</span>
        <h1>Good food,<br><em>made simple.</em></h1>
        <p>Discover local menus, save your favourites to a persistent cart, and check out with transparent pricing.</p>
        <div class="hero-actions">
          <el-button type="primary" size="large" @click="router.push({ name: 'customer-merchants' })">Browse restaurants</el-button>
          <el-button size="large" @click="router.push({ name: 'login' })">Sign in</el-button>
        </div>
        <div class="service-status"><span class="status-dot" :class="{ offline: health === 'Unavailable' }" /> API status: {{ health }}</div>
      </div>
      <div class="hero-art" aria-hidden="true">
        <span class="dish-plate">🍜</span>
        <span class="floating-card card-one">Fast checkout</span>
        <span class="floating-card card-two">Live menu pricing</span>
      </div>
    </div>
    <div class="feature-grid">
      <article class="feature-card"><span class="feature-number">01</span><h2>Explore menus</h2><p>Compare dishes from merchants and see availability before you add an item.</p></article>
      <article class="feature-card"><span class="feature-number">02</span><h2>Review your cart</h2><p>Items are grouped by merchant and special offers are calculated by the server.</p></article>
      <article class="feature-card"><span class="feature-number">03</span><h2>Manage your menu</h2><p>Merchants can keep dishes, inventory, and special offers up to date.</p><el-button link type="primary" @click="router.push({ name: 'merchant-dishes' })">Open merchant tools →</el-button></article>
    </div>
  </section>
</template>

<style scoped>
.home-page { max-width: 1180px; margin: 0 auto; padding: 22px 0 40px; }
.hero { display: grid; grid-template-columns: 1.15fr .85fr; min-height: 390px; overflow: hidden; border-radius: 28px; background: #153c35; color: #fff; }.hero-copy { padding: 54px; }.eyebrow, .feature-number { color: #f6c85f; font-size: 12px; font-weight: 800; letter-spacing: .12em; }h1 { max-width: 580px; margin: 14px 0 18px; font-size: clamp(42px, 6vw, 72px); line-height: .98; letter-spacing: -.055em; }h1 em { color: #f6c85f; font-family: Georgia, serif; font-weight: 400; }.hero-copy p { max-width: 500px; color: #d0dfdb; font-size: 17px; line-height: 1.6; }.hero-actions { display: flex; gap: 12px; margin-top: 30px; }.service-status { display: flex; align-items: center; gap: 8px; margin-top: 38px; color: #bdd1cc; font-size: 13px; }.status-dot { width: 9px; height: 9px; border-radius: 50%; background: #67dba6; }.status-dot.offline { background: #ef6b5b; }.hero-art { position: relative; display: grid; place-items: center; background: radial-gradient(circle at 50% 50%, #f6c85f 0 16%, #db814c 17% 36%, transparent 37%), #e8b05d; }.dish-plate { z-index: 1; display: grid; width: 170px; height: 170px; place-items: center; border: 11px solid #fff7e8; border-radius: 50%; background: #e26e40; box-shadow: 0 22px 0 rgba(77,43,22,.16); font-size: 78px; transform: rotate(-8deg); }.floating-card { position: absolute; z-index: 2; padding: 10px 14px; border-radius: 12px; background: rgba(255,255,255,.92); box-shadow: 0 8px 20px rgba(82,53,19,.16); color: #173d36; font-size: 13px; font-weight: 700; }.card-one { top: 58px; right: 34px; }.card-two { bottom: 62px; left: 26px; }.feature-grid { display: grid; grid-template-columns: repeat(3,1fr); gap: 16px; margin-top: 22px; }.feature-card { min-height: 180px; padding: 25px; border: 1px solid #e6e8e5; border-radius: 18px; background: #fff; }.feature-card h2 { margin: 18px 0 8px; color: #183b34; font-size: 20px; }.feature-card p { margin: 0; color: #65716e; line-height: 1.55; }.feature-card .el-button { margin-top: 12px; padding: 0; }@media (max-width:760px) { .hero { grid-template-columns: 1fr; }.hero-copy { padding: 38px 26px; }.hero-art { min-height: 230px; }.feature-grid { grid-template-columns: 1fr; }.card-one { right: 20px; }.card-two { left: 20px; } }
</style>
