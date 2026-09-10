import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'

const routes = [
  { path: '/', name: 'home', component: HomeView },
  // 第一周由「前端功能」同学加：
  // { path: '/merchant/dishes', name: 'merchant-dishes',
  //   component: () => import('@/views/merchant/MerchantDishView.vue') },
]

export default createRouter({
  history: createWebHistory(),
  routes,
})
