export const routes = [
  { path: '/', name: 'home', component: () => import('@/views/HomeView.vue') },
  { path: '/login', name: 'login', component: () => import('@/views/auth/LoginView.vue') },
  {
    path: '/merchant/dishes',
    name: 'merchant-dishes',
    component: () => import('@/views/merchant/MerchantDishView.vue'),
    meta: { requiresAuth: true, role: 'Merchant' },
  },
  {
    path: '/customer/merchants',
    name: 'customer-merchants',
    component: () => import('@/views/customer/MerchantBrowseView.vue'),
    meta: { requiresAuth: false },
  },
]
