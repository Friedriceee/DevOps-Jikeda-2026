export const routes = [
  { path: '/', name: 'home', component: () => import('@/views/HomeView.vue') },
  { path: '/login', name: 'login', component: () => import('@/views/auth/LoginView.vue') },
  { path: '/register', name: 'register', component: () => import('@/views/auth/RegisterView.vue') },
  {
    path: '/merchant/dishes',
    name: 'merchant-dishes',
    component: () => import('@/views/merchant/MerchantDishView.vue'),
    meta: { requiresAuth: true, role: 'Merchant' },
  },
  {
    path: '/merchant/special-offers',
    name: 'merchant-special-offers',
    component: () => import('@/views/merchant/MerchantSpecialOfferView.vue'),
    meta: { requiresAuth: true, role: 'Merchant' },
  },
  {
    path: '/user/orders',
    name: 'user-orders',
    component: () => import('@/views/user/OrderView.vue'),
    meta: { requiresAuth: true, role: 'Customer' },
  },
  {
    path: '/customer/merchants',
    name: 'customer-merchants',
    component: () => import('@/views/customer/MerchantBrowseView.vue'),
    meta: { requiresAuth: false },
  },
  {
    path: '/customer/cart',
    name: 'customer-cart',
    component: () => import('@/views/customer/CustomerCartView.vue'),
    meta: { requiresAuth: true, role: 'Customer' },
  },
]
