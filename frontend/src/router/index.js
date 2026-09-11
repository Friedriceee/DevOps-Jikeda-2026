import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'

const routes = [
  { path: '/', name: 'home', component: HomeView },
  {
    path: '/merchant/dishes',
    name: 'merchant-dishes',
    component: () => import('@/views/merchant/MerchantDishView.vue'),
  },
  {
    path: '/merchant/special-offers',
    name: 'merchant-special-offers',
    component: () => import('@/views/merchant/MerchantSpecialOfferView.vue'),
  },
]

export default createRouter({
  history: createWebHistory(),
  routes,
})
