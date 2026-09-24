import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'
import LoginView from '@/views/auth/LoginView.vue'

const routes = [
  { path: '/', name: 'home', component: HomeView },
  { path: '/login', name: 'login', component: LoginView },
  {
    path: '/merchant/dishes',
    name: 'merchant-dishes',
    component: () => import('@/views/merchant/MerchantDishView.vue'),
  },
]

export default createRouter({
  history: createWebHistory(),
  routes,
})
