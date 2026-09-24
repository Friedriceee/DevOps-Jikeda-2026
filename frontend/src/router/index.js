import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'
import LoginView from '@/views/auth/LoginView.vue'
import { useAuthStore } from '@/stores/auth'
import { authGuard } from './guards'

const routes = [
  { path: '/', name: 'home', component: HomeView },
  { path: '/login', name: 'login', component: LoginView },
  {
    path: '/merchant/dishes',
    name: 'merchant-dishes',
    component: () => import('@/views/merchant/MerchantDishView.vue'),
  },
  {
    path: '/customer/merchants',
    name: 'customer-merchants',
    component: () => import('@/views/customer/MerchantBrowseView.vue'),
    meta: { requiresAuth: false },
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  return authGuard(useAuthStore(), to)
})

export default router
